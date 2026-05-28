import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../core/auth.service';
import {
  swalConfirmDelete,
  swalConfirmRemove,
  swalError,
  swalSuccess,
  swalToastError,
  swalToastSuccess,
  swalToastWarning,
  swalWarning,
} from '../core/swal';
import { DocumentsApiService } from '../services/documents-api.service';
import {
  AdminUserListItem,
  DocumentItem,
  DocumentShare,
  FolderNode,
  FolderShare,
  FolderStats,
  LocationOption,
  RoleOption,
} from '../models/models';
import { ApiUiError, UiState } from '../shared/ui-state';

/** Tracks in-flight uploads so the UI can show “Uploading…”. */
export interface UploadJob {
  id: string;
  fileName: string;
}

@Component({
  selector: 'app-documents',
  templateUrl: './documents.component.html',
})
export class DocumentsComponent implements OnInit, OnDestroy {
  documentsState: UiState<DocumentItem[]> = { data: null, loading: false, error: null, empty: false };
  foldersState: UiState<FolderNode[]> = { data: null, loading: false, error: null, empty: false };
  sharingState: UiState<FolderShare[]> = { data: null, loading: false, error: null, empty: false };
  auditLogsState: UiState<unknown[]> = { data: null, loading: false, error: null, empty: true };
  tree: FolderNode[] = [];
  treeLoading = false;
  selected: FolderNode | null = null;
  documents: DocumentItem[] = [];
  docsPage = 1;
  readonly docsPageSize = 24;
  docsTotalCount = 0;
  docsTotalPages = 0;
  docsLoading = false;
  fileSearchTerm = '';
  folderSearchTerm = '';
  shares: FolderShare[] = [];
  selectedDocForShares: DocumentItem | null = null;
  documentShares: DocumentShare[] = [];
  /** Shares for the file highlighted in the explorer (details panel). */
  documentPanelShares: DocumentShare[] = [];
  folderStats: FolderStats | null = null;
  folderStatsLoading = false;
  locations: LocationOption[] = [];
  roles: RoleOption[] = [];
  newFolderName = '';
  shareType: 1 | 2 | 3 = 2;
  /** Folder share only: 1 = view only, 2 = full access */
  folderShareAccessLevel: 1 | 2 = 2;
  /** Multi-select for role-based shares (folder + document modals). */
  selectedShareRoleIds: string[] = [];
  shareRolePickFilter = '';
  /** Multi-select for location-based shares. */
  selectedShareLocationIds: number[] = [];
  shareLocationPickFilter = '';
  /** Approved users only; used when share type is User (multi-select). */
  shareEligibleUsers: AdminUserListItem[] = [];
  selectedShareUserIds: string[] = [];
  /** Filter text for the share user checkbox list. */
  shareUserPickFilter = '';
  /** In-flight uploads only (removed as each finishes). */
  uploadJobs: UploadJob[] = [];

  /** Which file row’s ⋮ menu is open. */
  openFileMenuId: number | null = null;

  /** Selected file tile in explorer grid (highlight). */
  explorerSelectedDocId: number | null = null;
  /** Whether current user may upload into the selected folder (full folder access). */
  folderAllowsUpload = false;

  /** Highlight explorer drop zone while dragging files over it. */
  uploadDragActive = false;

  foldersModalOpen = false;
  uploadModalOpen = false;
  /** Add named shortcut to an external URL (same permission as upload). */
  linkModalOpen = false;
  linkDisplayName = '';
  linkUrl = '';
  linkSaving = false;
  linkError = '';
  folderShareModalOpen = false;
  documentShareModalOpen = false;
  renameFolderModalOpen = false;
  renameFolderName = '';
  /** Folder / file metadata and shares (popup). */
  selectionDetailsModalOpen = false;

  pdfViewerOpen = false;
  pdfPreviewLoading = false;
  pdfPreviewTitle = '';
  pdfPreviewUrl: SafeResourceUrl | null = null;
  private pdfObjectUrl: string | null = null;

  /** Space upload result toasts so they appear one-after-another instead of stacking. */
  private uploadToastChain: Promise<void> = Promise.resolve();

  /** Above this size, uploads use the chunked API (each part must be ≤ server MaxChunkUploadBytes). */
  private readonly chunkUploadThresholdBytes = 8 * 1024 * 1024;
  private readonly chunkUploadChunkBytes = 5 * 1024 * 1024;

  get blockingLoading(): boolean {
    return this.treeLoading || this.docsLoading;
  }

  constructor(
    readonly auth: AuthService,
    private readonly api: DocumentsApiService,
    private readonly sanitizer: DomSanitizer
  ) {}

  ngOnDestroy(): void {
    this.revokePdfObjectUrl();
  }

  ngOnInit(): void {
    this.auth.loadMe().subscribe({
      next: () => this.afterProfileRefresh(),
      error: () => this.afterProfileRefresh(),
    });
  }

  private afterProfileRefresh(): void {
    this.refreshTree();
    this.api.locations().subscribe((l) => (this.locations = l));
    this.api.roles().subscribe((r) => (this.roles = r));
    this.loadShareEligibleUsers();
  }

  /** Management: approved accounts eligible for user-targeted shares. */
  private loadShareEligibleUsers(): void {
    if (!this.isMgmt) {
      this.shareEligibleUsers = [];
      return;
    }
    this.api.adminUsers().subscribe({
      next: (users) => {
        this.shareEligibleUsers = users
          .filter((u) => u.approvalStatus === 1)
          .sort((a, b) => a.email.localeCompare(b.email));
      },
      error: () => {
        this.shareEligibleUsers = [];
      },
    });
  }

  shareUserOptionLabel(u: AdminUserListItem): string {
    const name = `${u.firstName || ''} ${u.lastName || ''}`.trim();
    return name ? `${name} — ${u.email}` : u.email;
  }

  shareUserDisplayName(u: AdminUserListItem): string {
    const name = `${u.firstName || ''} ${u.lastName || ''}`.trim();
    return name || u.email;
  }

  shareUserShowEmailSubline(u: AdminUserListItem): boolean {
    return !!(`${u.firstName || ''} ${u.lastName || ''}`.trim());
  }

  get filteredShareEligibleUsers(): AdminUserListItem[] {
    const t = this.shareUserPickFilter.trim().toLowerCase();
    if (!t) return this.shareEligibleUsers;
    return this.shareEligibleUsers.filter((u) => {
      const blob = `${u.firstName || ''} ${u.lastName || ''} ${u.email || ''}`.toLowerCase();
      return blob.includes(t);
    });
  }

  isShareUserSelected(userId: string): boolean {
    return this.selectedShareUserIds.includes(userId);
  }

  toggleShareUserSelection(userId: string): void {
    if (this.isShareUserSelected(userId)) {
      this.selectedShareUserIds = this.selectedShareUserIds.filter((id) => id !== userId);
    } else {
      this.selectedShareUserIds = [...this.selectedShareUserIds, userId];
    }
  }

  clearShareUserPickerSelection(): void {
    this.selectedShareUserIds = [];
  }

  get filteredShareRoles(): RoleOption[] {
    // Management already has access to all content; role shares are for other roles.
    const list = this.roles.filter((r) => r.name !== 'Management');
    const t = this.shareRolePickFilter.trim().toLowerCase();
    if (!t) return list;
    return list.filter((r) => (r.name || '').toLowerCase().includes(t));
  }

  get filteredShareLocations(): LocationOption[] {
    const t = this.shareLocationPickFilter.trim().toLowerCase();
    if (!t) return this.locations;
    return this.locations.filter((l) => {
      const blob = `${l.name || ''}`.toLowerCase();
      return blob.includes(t);
    });
  }

  isShareRoleSelected(roleId: string): boolean {
    return this.selectedShareRoleIds.includes(roleId);
  }

  toggleShareRoleSelection(roleId: string): void {
    if (this.isShareRoleSelected(roleId)) {
      this.selectedShareRoleIds = this.selectedShareRoleIds.filter((id) => id !== roleId);
    } else {
      this.selectedShareRoleIds = [...this.selectedShareRoleIds, roleId];
    }
  }

  clearShareRolePickerSelection(): void {
    this.selectedShareRoleIds = [];
  }

  isShareLocationSelected(locationId: number): boolean {
    return this.selectedShareLocationIds.includes(locationId);
  }

  toggleShareLocationSelection(locationId: number): void {
    if (this.isShareLocationSelected(locationId)) {
      this.selectedShareLocationIds = this.selectedShareLocationIds.filter((id) => id !== locationId);
    } else {
      this.selectedShareLocationIds = [...this.selectedShareLocationIds, locationId];
    }
  }

  clearShareLocationPickerSelection(): void {
    this.selectedShareLocationIds = [];
  }

  onShareTypeChange(_t: number): void {
    this.selectedShareUserIds = [];
    this.shareUserPickFilter = '';
    this.selectedShareRoleIds = [];
    this.selectedShareLocationIds = [];
    this.shareRolePickFilter = '';
    this.shareLocationPickFilter = '';
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(ev: MouseEvent): void {
    const t = ev.target as HTMLElement;
    if (t.closest('[data-overflow-anchor]')) return;
    this.openFileMenuId = null;
  }

  get isMgmt(): boolean {
    return this.auth.isManagement();
  }

  get selectionDetailsModalHeading(): string {
    return this.selectedExplorerDoc ? 'File details' : 'Folder details';
  }

  /** File tile selected in the grid (for details panel). */
  get selectedExplorerDoc(): DocumentItem | null {
    if (this.explorerSelectedDocId == null) return null;
    return this.documents.find((d) => d.id === this.explorerSelectedDocId) ?? null;
  }

  /** Root → current folder (from tree), for breadcrumbs. */
  get folderBreadcrumb(): FolderNode[] {
    if (!this.selected) return [];
    const path = this.findFolderPath(this.tree, this.selected.id);
    return path?.length ? path : [this.selected];
  }

  /** Subfolders of the current folder (for explorer tiles). */
  get childFolders(): FolderNode[] {
    if (!this.selected?.id || !this.tree.length) return [];
    const node = this.findNode(this.tree, this.selected.id);
    const ch = node?.children ?? [];
    const term = this.folderSearchTerm.trim().toLowerCase();
    const list = [...ch].sort((a, b) => a.name.localeCompare(b.name));
    if (!term) return list;
    return list.filter((x) => x.name.toLowerCase().includes(term));
  }

  get filteredTree(): FolderNode[] {
    const term = this.folderSearchTerm.trim().toLowerCase();
    if (!term) return this.tree;
    return this.filterTree(this.tree, term);
  }

  refreshTree(): void {
    this.treeLoading = true;
    this.foldersState = { data: this.tree, loading: true, error: null, empty: false };
    this.api.folderTree().subscribe({
      next: (t) => {
        this.tree = t;
        this.treeLoading = false;
        this.foldersState = { data: t, loading: false, error: null, empty: t.length === 0 };
        if (!this.selected && t.length) {
          this.select(t[0]);
        } else if (this.selected) {
          const still = this.findNode(t, this.selected.id);
          if (still) this.select(still);
          else if (t.length) {
            this.select(t[0]);
          } else {
            this.selected = null;
            this.documents = [];
          }
        }
      },
      error: () => {
        this.tree = [];
        this.treeLoading = false;
        this.foldersState = { data: null, loading: false, error: 'Could not load folders.', empty: false };
        this.selected = null;
        this.documents = [];
        void swalError('Could not load folders', 'Please refresh the page or try again later.');
      },
    });
  }

  select(node: FolderNode): void {
    const folder = this.findNode(this.tree, node.id) ?? node;
    this.selected = folder;
    this.selectedDocForShares = null;
    this.documentShares = [];
    this.documentShareModalOpen = false;
    this.selectionDetailsModalOpen = false;
    this.renameFolderModalOpen = false;
    this.renameFolderName = '';
    this.openFileMenuId = null;
    this.explorerSelectedDocId = null;
    this.documentPanelShares = [];
    this.uploadJobs = [];
    this.docsPage = 1;
    this.loadFolderStats();
    if (this.isMgmt) {
      this.folderAllowsUpload = true;
    } else if (this.auth.isApproved()) {
      this.api.uploadAllowed(folder.id).subscribe({
        next: (r) => (this.folderAllowsUpload = r.allowed),
        error: () => (this.folderAllowsUpload = false),
      });
    } else {
      this.folderAllowsUpload = false;
    }
    this.loadDocuments();
    if (this.isMgmt) {
      this.sharingState = { data: this.shares, loading: true, error: null, empty: false };
      this.api.listShares(folder.id).subscribe({
        next: (s) => {
          this.shares = s;
          this.sharingState = { data: s, loading: false, error: null, empty: s.length === 0 };
        },
        error: (err: ApiUiError) => {
          this.shares = [];
          this.sharingState = { data: null, loading: false, error: err?.message || 'Could not load shares.', empty: false };
        },
      });
    } else {
      this.shares = [];
      this.sharingState = { data: [], loading: false, error: null, empty: true };
    }
  }

  loadDocuments(): void {
    if (!this.selected) return;
    this.docsLoading = true;
    this.documentsState = { data: this.documents, loading: true, error: null, empty: false };
    this.api
      .documentsInFolderPaged(
        this.selected.id,
        this.docsPage,
        this.docsPageSize,
        this.fileSearchTerm
      )
      .subscribe({
        next: (res) => {
          this.documents = res.items || [];
          this.docsPage = res.page || 1;
          this.docsTotalCount = res.totalCount || 0;
          this.docsTotalPages = res.totalPages || 0;
          this.docsLoading = false;
          this.documentsState = {
            data: this.documents,
            loading: false,
            error: null,
            empty: this.documents.length === 0,
          };
          this.loadFolderStats();
        },
        error: (err: ApiUiError) => {
          this.documents = [];
          this.docsTotalCount = 0;
          this.docsTotalPages = 0;
          this.docsLoading = false;
          this.documentsState = {
            data: null,
            loading: false,
            error: err?.message || 'Could not load files.',
            empty: false,
          };
        },
      });
  }

  private loadFolderStats(): void {
    if (!this.selected) {
      this.folderStats = null;
      this.folderStatsLoading = false;
      return;
    }
    const folderId = this.selected.id;
    this.folderStatsLoading = true;
    this.api.folderStats(folderId).subscribe({
      next: (s) => {
        if (this.selected?.id === folderId) {
          this.folderStats = s;
        }
        this.folderStatsLoading = false;
      },
      error: () => {
        if (this.selected?.id === folderId) {
          this.folderStats = null;
        }
        this.folderStatsLoading = false;
      },
    });
  }

  private loadDocumentPanelShares(docId: number): void {
    if (!this.isMgmt) {
      this.documentPanelShares = [];
      return;
    }
    this.api.listDocumentShares(docId).subscribe({
      next: (s) => {
        if (this.explorerSelectedDocId === docId) {
          this.documentPanelShares = s;
        }
      },
      error: () => {
        if (this.explorerSelectedDocId === docId) {
          this.documentPanelShares = [];
        }
      },
    });
  }

  formatDocumentSize(doc: DocumentItem): string {
    if (doc.isLink) return 'Web link';
    return this.formatBytes(doc.sizeBytes);
  }

  formatBytes(n: number): string {
    if (n == null || !Number.isFinite(n) || n < 0) return '—';
    if (n === 0) return '0 B';
    const units = ['B', 'KB', 'MB', 'GB', 'TB'];
    let i = 0;
    let v = n;
    while (v >= 1024 && i < units.length - 1) {
      v /= 1024;
      i++;
    }
    const rounded = i === 0 ? Math.round(v) : v < 10 ? Math.round(v * 10) / 10 : Math.round(v);
    return `${rounded} ${units[i]}`;
  }

  formatDetailsDateTime(iso: string): string {
    const d = this.parseUtcInstant(iso);
    if (!d) return '—';
    return d.toLocaleString(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short',
    });
  }

  folderShareTargetLine(s: FolderShare): string {
    if (s.shareType === 3) return s.userEmail ? `User · ${s.userEmail}` : 'User';
    if (s.shareType === 1) return s.roleName ? `Role · ${s.roleName}` : 'Role';
    if (s.shareType === 2) return s.locationName ? `Location · ${s.locationName}` : 'Location';
    return 'Share';
  }

  folderShareAccessLabel(s: FolderShare): string {
    const al = s.accessLevel ?? 2;
    return al === 1 ? 'View only' : 'Full access';
  }

  documentShareTargetLine(s: DocumentShare): string {
    if (s.shareType === 3) return s.userEmail ? `User · ${s.userEmail}` : 'User';
    if (s.shareType === 1) return s.roleName ? `Role · ${s.roleName}` : 'Role';
    if (s.shareType === 2) return s.locationName ? `Location · ${s.locationName}` : 'Location';
    return 'Share';
  }

  retryDocuments(): void {
    this.loadDocuments();
  }

  retryFolders(): void {
    this.refreshTree();
  }

  onFileSearchChanged(): void {
    this.docsPage = 1;
    this.loadDocuments();
  }

  clearFileSearch(): void {
    if (!this.fileSearchTerm) return;
    this.fileSearchTerm = '';
    this.docsPage = 1;
    this.loadDocuments();
  }

  prevDocsPage(): void {
    if (this.docsPage <= 1) return;
    this.docsPage -= 1;
    this.loadDocuments();
  }

  nextDocsPage(): void {
    if (this.docsTotalPages > 0 && this.docsPage >= this.docsTotalPages) return;
    this.docsPage += 1;
    this.loadDocuments();
  }

  openFoldersModal(): void {
    this.foldersModalOpen = true;
  }

  closeFoldersModal(): void {
    this.foldersModalOpen = false;
  }

  onFolderPickedFromModal(node: FolderNode): void {
    this.select(node);
  }

  openUploadModal(): void {
    this.uploadModalOpen = true;
  }

  closeUploadModal(): void {
    this.uploadModalOpen = false;
  }

  openLinkModal(): void {
    this.linkError = '';
    this.linkDisplayName = '';
    this.linkUrl = '';
    this.linkModalOpen = true;
  }

  closeLinkModal(): void {
    this.linkModalOpen = false;
    this.linkSaving = false;
    this.linkError = '';
  }

  submitAddLink(): void {
    if (!this.selected) return;
    const name = this.linkDisplayName.trim();
    const url = this.linkUrl.trim();
    this.linkError = '';
    if (!name) {
      this.linkError = 'Enter a name (e.g. Time Off Request).';
      return;
    }
    if (!url) {
      this.linkError = 'Enter a URL starting with https://';
      return;
    }
    let parsed: URL;
    try {
      parsed = new URL(url);
    } catch {
      this.linkError = 'Enter a valid URL (https://…).';
      return;
    }
    if (parsed.protocol !== 'http:' && parsed.protocol !== 'https:') {
      this.linkError = 'Only http and https links are allowed.';
      return;
    }
    this.linkSaving = true;
    const folderId = this.selected.id;
    this.api.createDocumentLink(folderId, { displayName: name, url }).subscribe({
      next: () => {
        this.linkSaving = false;
        this.closeLinkModal();
        void swalToastSuccess(`Added link: ${name}`);
        if (this.selected?.id === folderId) this.loadDocuments();
      },
      error: (err: { error?: { error?: string } }) => {
        this.linkSaving = false;
        const msg = err?.error && typeof err.error === 'object' && err.error.error;
        this.linkError = typeof msg === 'string' ? msg : 'Could not add link.';
      },
    });
  }

  get uploadInFlight(): boolean {
    return this.uploadJobs.length > 0;
  }

  openFolderShareModal(): void {
    if (!this.isMgmt || !this.selected) return;
    this.selectedShareUserIds = [];
    this.shareUserPickFilter = '';
    this.selectedShareRoleIds = [];
    this.selectedShareLocationIds = [];
    this.shareRolePickFilter = '';
    this.shareLocationPickFilter = '';
    this.folderShareModalOpen = true;
  }

  closeFolderShareModal(): void {
    this.folderShareModalOpen = false;
  }

  openRenameFolderModal(): void {
    if (!this.isMgmt || !this.selected) return;
    this.renameFolderName = this.selected.name;
    this.renameFolderModalOpen = true;
  }

  closeRenameFolderModal(): void {
    this.renameFolderModalOpen = false;
    this.renameFolderName = '';
  }

  openSelectionDetailsModal(): void {
    if (!this.selected) return;
    this.selectionDetailsModalOpen = true;
    this.loadFolderStats();
    if (this.explorerSelectedDocId != null && this.isMgmt) {
      this.loadDocumentPanelShares(this.explorerSelectedDocId);
    }
  }

  closeSelectionDetailsModal(): void {
    this.selectionDetailsModalOpen = false;
  }

  saveRenameFolder(): void {
    if (!this.isMgmt || !this.selected) return;
    const name = this.renameFolderName.trim();
    if (!name) {
      void swalWarning('Folder name required', 'Enter a name before saving.');
      return;
    }
    const folderId = this.selected.id;
    this.api.renameFolder(folderId, name).subscribe({
      next: () => {
        this.renameFolderModalOpen = false;
        this.renameFolderName = '';
        void swalSuccess('Folder renamed', `The folder is now named "${name}".`);
        this.refreshTree();
      },
      error: () =>
        void swalError(
          'Could not rename folder',
          'Another folder in the same location may already use that name, or the name is invalid.'
        ),
    });
  }

  createFolder(): void {
    if (!this.isMgmt || !this.newFolderName.trim()) return;
    const parentId = this.selected?.id ?? null;
    this.api.createFolder(this.newFolderName.trim(), parentId).subscribe({
      next: () => {
        this.newFolderName = '';
        this.refreshTree();
        swalToastSuccess('Folder created');
      },
      error: () => void swalError('Could not create folder', 'Check the name and try again.'),
    });
  }

  deleteFolder(): void {
    if (!this.isMgmt || !this.selected || this.selected.isDefault) return;
    const sel = this.selected;
    void swalConfirmDelete(
      'Delete this folder?',
      `"${sel.name}" must be empty (no subfolders and no files) before it can be deleted.`
    ).then((ok) => {
      if (!ok) return;
      this.api.deleteFolder(sel.id).subscribe({
        next: () => {
          void swalSuccess('Folder deleted', `"${sel.name}" was removed.`);
          this.selected = null;
          this.refreshTree();
        },
        error: () =>
          void swalError(
            'Could not delete folder',
            'Make sure it has no subfolders and no documents, then try again.'
          ),
      });
    });
  }

  toggleFileMenu(id: number, ev: MouseEvent): void {
    ev.stopPropagation();
    this.openFileMenuId = this.openFileMenuId === id ? null : id;
  }

  closeFileMenu(): void {
    this.openFileMenuId = null;
  }

  fileExtension(doc: DocumentItem): string {
    if (doc.isLink) return 'LINK';
    const name = doc.originalFileName || '';
    const dot = name.lastIndexOf('.');
    let ext = dot >= 0 ? name.slice(dot + 1) : '';
    if (!ext && doc.contentType) {
      const sub = doc.contentType.split('/')[1];
      if (sub) ext = sub.split('+')[0];
    }
    return ext.toUpperCase().slice(0, 5) || 'FILE';
  }

  fileTone(doc: DocumentItem): string {
    if (doc.isLink) return 'link';
    const ext = this.fileExtension(doc).toLowerCase();
    if (ext === 'pdf') return 'pdf';
    if (ext.startsWith('doc')) return 'doc';
    if (ext === 'csv' || ext.startsWith('xls')) return 'sheet';
    if (ext.startsWith('ppt')) return 'slide';
    if (['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg', 'bmp', 'ico'].includes(ext))
      return 'image';
    if (['zip', 'rar', '7z', 'gz', 'tar'].includes(ext)) return 'archive';
    if (
      ['js', 'ts', 'tsx', 'jsx', 'json', 'html', 'css', 'cs', 'py', 'java', 'xml'].includes(ext)
    )
      return 'code';
    return 'generic';
  }

  fileBadgeClass(doc: DocumentItem): string {
    switch (this.fileTone(doc)) {
      case 'link':
        return 'bg-sky-600 text-white';
      case 'pdf':
        return 'bg-red-500 text-white';
      case 'doc':
        return 'bg-blue-600 text-white';
      case 'sheet':
        return 'bg-emerald-600 text-white';
      case 'slide':
        return 'bg-orange-500 text-white';
      case 'image':
        return 'bg-portal-600 text-white';
      case 'archive':
        return 'bg-amber-700 text-white';
      case 'code':
        return 'bg-slate-700 text-white';
      default:
        return 'bg-slate-500 text-white';
    }
  }

  formatExplorerDate(iso: string): string {
    const d = this.parseUtcInstant(iso);
    if (!d) return '';
    const diffMs = Date.now() - d.getTime();
    if (diffMs < 60_000) return 'Just now';
    if (diffMs < 3600_000) return `${Math.floor(diffMs / 60_000)} min ago`;
    if (diffMs < 86400_000) return `${Math.floor(diffMs / 3600_000)} hours ago`;
    if (diffMs < 7 * 86400_000) return `${Math.floor(diffMs / 86400_000)} days ago`;
    return d.toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    });
  }

  /** API timestamps are UTC; normalize so the browser does not treat naive ISO strings as local time. */
  private parseUtcInstant(iso: string): Date | null {
    const s = iso?.trim() ?? '';
    if (!s) return null;
    const hasZone = /[zZ]$|[+-]\d{2}:\d{2}$/.test(s);
    const d = new Date(hasZone ? s : `${s}Z`);
    return Number.isNaN(d.getTime()) ? null : d;
  }

  folderTileCaption(f: FolderNode): string {
    const n = f.children?.length ?? 0;
    if (n === 1) return '(1) subfolder';
    if (n > 1) return `(${n}) subfolders`;
    return 'Folder';
  }

  onExplorerFolderOpen(f: FolderNode): void {
    this.explorerSelectedDocId = null;
    this.select(f);
  }

  onDocTileClick(d: DocumentItem, ev: MouseEvent): void {
    if ((ev.target as HTMLElement).closest('[data-overflow-anchor]')) return;
    this.explorerSelectedDocId = d.id;
    this.loadDocumentPanelShares(d.id);
  }

  onDocTileDblClick(d: DocumentItem): void {
    if (d.isLink) {
      this.openExternalDocumentLink(d);
      return;
    }
    if (this.supportsGoogleDocViewer(d)) this.openFilePreview(d);
    else this.download(d);
  }

  isLinkDoc(doc: DocumentItem): boolean {
    return !!doc.isLink && !!(doc.externalUrl || '').trim();
  }

  openExternalDocumentLink(doc: DocumentItem): void {
    this.closeFileMenu();
    const u = (doc.externalUrl || '').trim();
    if (!u) {
      void swalError('Missing link', 'This shortcut has no URL.');
      return;
    }
    window.open(u, '_blank', 'noopener,noreferrer');
  }

  isDocTileSelected(d: DocumentItem): boolean {
    return (
      this.explorerSelectedDocId === d.id ||
      this.selectedDocForShares?.id === d.id
    );
  }

  private dragDropUploadAllowed(): boolean {
    if (!this.selected || (!this.auth.isApproved() && !this.isMgmt)) return false;
    if (!this.isMgmt && !this.folderAllowsUpload) return false;
    return true;
  }

  onUploadDragOver(ev: DragEvent): void {
    if (!this.dragDropUploadAllowed()) return;
    ev.preventDefault();
    ev.stopPropagation();
    if (ev.dataTransfer?.types?.includes('Files')) this.uploadDragActive = true;
  }

  onUploadDragLeave(ev: DragEvent): void {
    const el = ev.currentTarget as HTMLElement;
    const related = ev.relatedTarget as Node | null;
    if (related && el.contains(related)) return;
    this.uploadDragActive = false;
  }

  onUploadDrop(ev: DragEvent): void {
    this.uploadDragActive = false;
    if (!this.selected || (!this.auth.isApproved() && !this.isMgmt)) return;
    ev.preventDefault();
    if (!this.isMgmt && !this.folderAllowsUpload) {
      void swalWarning(
        'Upload not allowed',
        'This folder is shared with you as view only. You can open and download files, but not upload.'
      );
      return;
    }
    const list = ev.dataTransfer?.files;
    if (list?.length) {
      const files = Array.from(list);
      this.uploadFiles(files);
    }
  }

  onFileSelected(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const list = input.files;
    input.value = '';
    if (list?.length) {
      this.uploadFiles(Array.from(list));
    }
  }

  uploadFiles(files: File[]): void {
    if (!this.selected || (!this.auth.isApproved() && !this.isMgmt)) {
      void swalWarning(
        !this.auth.isApproved() ? 'Account pending' : 'Select a folder',
        !this.auth.isApproved()
          ? 'Your account is pending Management approval. Uploads are not available yet.'
          : 'Choose a folder before uploading files.'
      );
      return;
    }
    if (!this.isMgmt && !this.folderAllowsUpload) {
      void swalWarning(
        'Upload not allowed',
        'This folder is shared with you as view only. You can open and download files, but not upload.'
      );
      return;
    }
    const queue = files.filter((f) => f.name);
    if (!queue.length) return;

    const folderId = this.selected.id;
    for (const file of queue) {
      if (file.size === 0) {
        void swalToastError(`Skipped empty file: ${file.name}`);
        continue;
      }
      const id = `${Date.now()}-${Math.random().toString(36).slice(2, 11)}`;
      this.uploadJobs.push({ id, fileName: file.name });

      const upload$ =
        file.size > this.chunkUploadThresholdBytes
          ? this.api.uploadDocumentChunked(folderId, file, this.chunkUploadChunkBytes)
          : this.api.uploadDocument(folderId, file);

      upload$.subscribe({
        next: (doc) => {
          this.queueUploadToast(true, `Uploaded: ${doc.originalFileName}`);
          this.uploadJobs = this.uploadJobs.filter((j) => j.id !== id);
          if (this.selected?.id === folderId) {
            this.loadDocuments();
          }
        },
        error: () => {
          this.queueUploadToast(false, `Could not upload: ${file.name}`);
          this.uploadJobs = this.uploadJobs.filter((j) => j.id !== id);
        },
      });
    }
  }

  private queueUploadToast(ok: boolean, message: string): void {
    this.uploadToastChain = this.uploadToastChain.then(
      () =>
        new Promise<void>((resolve) => {
          if (ok) swalToastSuccess(message);
          else swalToastError(message);
          window.setTimeout(resolve, 1400);
        })
    );
  }

  download(doc: DocumentItem): void {
    this.closeFileMenu();
    if (doc.isLink) {
      this.openExternalDocumentLink(doc);
      return;
    }
    this.api.downloadBlob(doc.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = doc.originalFileName;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => void swalError('Download failed', 'The file could not be downloaded. Try again.'),
    });
  }

  isPdf(doc: DocumentItem): boolean {
    const t = (doc.contentType || '').toLowerCase();
    if (t.includes('pdf')) return true;
    return (doc.originalFileName || '').toLowerCase().endsWith('.pdf');
  }

  /** Types we open in preview (Google Docs Viewer with PDF fallback to in-browser blob). */
  supportsGoogleDocViewer(doc: DocumentItem): boolean {
    if (doc.isLink) return false;
    const name = (doc.originalFileName || '').toLowerCase();
    const t = (doc.contentType || '').toLowerCase();
    if (name.endsWith('.pdf') || t.includes('pdf')) return true;
    if (name.endsWith('.docx') || t.includes('wordprocessingml')) return true;
    if (name.endsWith('.doc') || t.includes('msword')) return true;
    if (name.endsWith('.png') || t.includes('png')) return true;
    return false;
  }

  openFilePreview(doc: DocumentItem): void {
    if (doc.isLink) {
      this.openExternalDocumentLink(doc);
      return;
    }
    if (!this.supportsGoogleDocViewer(doc)) {
      this.download(doc);
      return;
    }
    this.closeFileMenu();
    this.revokePdfObjectUrl();
    this.pdfPreviewUrl = null;
    this.pdfPreviewTitle = doc.originalFileName;
    this.pdfViewerOpen = true;
    this.pdfPreviewLoading = true;
    this.api.getDocumentViewerUrl(doc.id).subscribe({
      next: ({ viewerUrl }) => {
        this.pdfPreviewLoading = false;
        this.pdfPreviewUrl = this.sanitizer.bypassSecurityTrustResourceUrl(viewerUrl);
      },
      error: () => {
        this.pdfPreviewLoading = false;
        if (this.isPdf(doc)) {
          this.openPdfPreview(doc);
        } else {
          this.pdfViewerOpen = false;
          void swalError(
            'Preview unavailable',
            'Online preview could not be started. Use a public HTTPS API URL (set DocumentViewer:PublicOrigin) so Google can fetch the file, or download it to open locally.'
          );
        }
      },
    });
  }

  openPdfPreview(doc: DocumentItem): void {
    if (!this.isPdf(doc)) return;
    this.closeFileMenu();
    this.revokePdfObjectUrl();
    this.pdfPreviewTitle = doc.originalFileName;
    this.pdfViewerOpen = true;
    this.pdfPreviewLoading = true;
    this.api.downloadBlob(doc.id).subscribe({
      next: (blob) => {
        this.pdfPreviewLoading = false;
        const typedBlob =
          blob.type && blob.type !== 'application/octet-stream'
            ? blob
            : new Blob([blob], { type: 'application/pdf' });
        this.pdfObjectUrl = URL.createObjectURL(typedBlob);
        this.pdfPreviewUrl = this.sanitizer.bypassSecurityTrustResourceUrl(
          this.pdfObjectUrl
        );
      },
      error: () => {
        this.pdfPreviewLoading = false;
        this.pdfViewerOpen = false;
        void swalError('Could not open PDF', 'The preview could not be loaded. Try downloading the file instead.');
      },
    });
  }

  closePdfViewer(): void {
    this.pdfViewerOpen = false;
    this.pdfPreviewLoading = false;
    this.revokePdfObjectUrl();
  }

  private revokePdfObjectUrl(): void {
    this.pdfPreviewUrl = null;
    if (this.pdfObjectUrl) {
      URL.revokeObjectURL(this.pdfObjectUrl);
      this.pdfObjectUrl = null;
    }
  }

  removeDocument(doc: DocumentItem): void {
    if (!this.isMgmt) return;
    void swalConfirmDelete('Delete this file?', doc.originalFileName).then((ok) => {
      if (!ok) return;
      this.closeFileMenu();
      this.api.deleteDocument(doc.id).subscribe({
        next: () => {
          this.loadDocuments();
          if (this.explorerSelectedDocId === doc.id) {
            this.explorerSelectedDocId = null;
          }
          if (this.selectedDocForShares?.id === doc.id) {
            this.selectedDocForShares = null;
            this.documentShares = [];
            this.documentShareModalOpen = false;
          }
          swalToastSuccess('File deleted');
        },
        error: () => void swalError('Could not delete file', 'Try again or check your permissions.'),
      });
    });
  }

  addShare(): void {
    if (!this.isMgmt || !this.selected) return;
    const folderId = this.selected.id;

    if (this.shareType === 3) {
      const ids = [...new Set(this.selectedShareUserIds.filter(Boolean))];
      if (!ids.length) {
        void swalWarning('Select users', 'Choose at least one approved user.');
        return;
      }
      const reqs = ids.map((userId) =>
        this.api
          .addShare(folderId, {
            accessLevel: this.folderShareAccessLevel,
            shareType: 3,
            userId,
          })
          .pipe(catchError(() => of(null)))
      );
      forkJoin(reqs).subscribe((results) => {
        const failed = results.filter((r) => r === null).length;
        const succeeded = results.length - failed;
        this.api.listShares(folderId).subscribe((s) => {
          this.shares = s;
          if (!succeeded) {
            void swalError('Could not add shares', 'Check the selections and try again.');
          } else if (failed > 0) {
            void swalToastWarning(
              `Added ${succeeded} share(s); ${failed} could not be added (may already exist).`
            );
          } else {
            swalToastSuccess(succeeded > 1 ? `Added ${succeeded} folder shares` : 'Folder share added');
          }
        });
      });
      return;
    }

    if (this.shareType === 1) {
      const roleIds = [...new Set(this.selectedShareRoleIds.filter(Boolean))];
      if (!roleIds.length) {
        void swalWarning('Select roles', 'Choose at least one role.');
        return;
      }
      const reqs = roleIds.map((roleId) =>
        this.api
          .addShare(folderId, {
            accessLevel: this.folderShareAccessLevel,
            shareType: 1,
            roleId,
          })
          .pipe(catchError(() => of(null)))
      );
      forkJoin(reqs).subscribe((results) => {
        const failed = results.filter((r) => r === null).length;
        const succeeded = results.length - failed;
        this.api.listShares(folderId).subscribe((s) => {
          this.shares = s;
          if (!succeeded) {
            void swalError('Could not add shares', 'Check the selections and try again.');
          } else if (failed > 0) {
            void swalToastWarning(
              `Added ${succeeded} share(s); ${failed} could not be added (may already exist).`
            );
          } else {
            swalToastSuccess(succeeded > 1 ? `Added ${succeeded} folder shares` : 'Folder share added');
          }
        });
      });
      return;
    }

    if (this.shareType === 2) {
      const locIds = [...new Set(this.selectedShareLocationIds.filter((id) => id != null))];
      if (!locIds.length) {
        void swalWarning('Select locations', 'Choose at least one location.');
        return;
      }
      const reqs = locIds.map((locationId) =>
        this.api
          .addShare(folderId, {
            accessLevel: this.folderShareAccessLevel,
            shareType: 2,
            locationId,
          })
          .pipe(catchError(() => of(null)))
      );
      forkJoin(reqs).subscribe((results) => {
        const failed = results.filter((r) => r === null).length;
        const succeeded = results.length - failed;
        this.api.listShares(folderId).subscribe((s) => {
          this.shares = s;
          if (!succeeded) {
            void swalError('Could not add shares', 'Check the selections and try again.');
          } else if (failed > 0) {
            void swalToastWarning(
              `Added ${succeeded} share(s); ${failed} could not be added (may already exist).`
            );
          } else {
            swalToastSuccess(succeeded > 1 ? `Added ${succeeded} folder shares` : 'Folder share added');
          }
        });
      });
      return;
    }
  }

  removeShare(share: FolderShare): void {
    if (!this.isMgmt || !this.selected) return;
    void swalConfirmRemove(
      'Remove this folder share?',
      'People matched by this rule will lose access granted through this share.'
    ).then((ok) => {
      if (!ok) return;
      this.api.removeShare(this.selected!.id, share.id).subscribe({
        next: () => {
          this.shares = this.shares.filter((s) => s.id !== share.id);
          swalToastSuccess('Share removed');
        },
        error: () => void swalError('Could not remove share', 'Try again.'),
      });
    });
  }

  openDocShares(doc: DocumentItem): void {
    if (!this.isMgmt) return;
    this.closeFileMenu();
    this.explorerSelectedDocId = doc.id;
    this.selectedDocForShares = doc;
    this.selectedShareUserIds = [];
    this.shareUserPickFilter = '';
    this.selectedShareRoleIds = [];
    this.selectedShareLocationIds = [];
    this.shareRolePickFilter = '';
    this.shareLocationPickFilter = '';
    this.documentShareModalOpen = true;
    this.api.listDocumentShares(doc.id).subscribe({
      next: (s) => {
        this.documentShares = s;
        this.documentPanelShares = s;
      },
      error: () => {
        this.documentShares = [];
        this.documentPanelShares = [];
        void swalToastWarning('Could not load file shares');
      },
    });
  }

  closeDocumentShareModal(): void {
    this.documentShareModalOpen = false;
    const keepDocId = this.explorerSelectedDocId;
    this.selectedDocForShares = null;
    this.documentShares = [];
    if (keepDocId != null && this.isMgmt) {
      this.loadDocumentPanelShares(keepDocId);
    }
  }

  addDocumentShare(): void {
    if (!this.isMgmt || !this.selectedDocForShares) return;
    const docId = this.selectedDocForShares.id;

    if (this.shareType === 3) {
      const ids = [...new Set(this.selectedShareUserIds.filter(Boolean))];
      if (!ids.length) {
        void swalWarning('Select users', 'Choose at least one approved user.');
        return;
      }
      const reqs = ids.map((userId) =>
        this.api.addDocumentShare(docId, { shareType: 3, userId }).pipe(catchError(() => of(null)))
      );
      forkJoin(reqs).subscribe((results) => {
        const failed = results.filter((r) => r === null).length;
        const succeeded = results.length - failed;
        this.api.listDocumentShares(docId).subscribe((s) => {
          this.documentShares = s;
          if (this.explorerSelectedDocId === docId) {
            this.documentPanelShares = s;
          }
          if (!succeeded) {
            void swalError('Could not add file shares', 'Check the selections and try again.');
          } else if (failed > 0) {
            void swalToastWarning(
              `Added ${succeeded} share(s); ${failed} could not be added (may already exist).`
            );
          } else {
            swalToastSuccess(succeeded > 1 ? `Added ${succeeded} file shares` : 'File share added');
          }
        });
      });
      return;
    }

    if (this.shareType === 1) {
      const roleIds = [...new Set(this.selectedShareRoleIds.filter(Boolean))];
      if (!roleIds.length) {
        void swalWarning('Select roles', 'Choose at least one role.');
        return;
      }
      const reqs = roleIds.map((roleId) =>
        this.api.addDocumentShare(docId, { shareType: 1, roleId }).pipe(catchError(() => of(null)))
      );
      forkJoin(reqs).subscribe((results) => {
        const failed = results.filter((r) => r === null).length;
        const succeeded = results.length - failed;
        this.api.listDocumentShares(docId).subscribe((s) => {
          this.documentShares = s;
          if (this.explorerSelectedDocId === docId) {
            this.documentPanelShares = s;
          }
          if (!succeeded) {
            void swalError('Could not add file shares', 'Check the selections and try again.');
          } else if (failed > 0) {
            void swalToastWarning(
              `Added ${succeeded} share(s); ${failed} could not be added (may already exist).`
            );
          } else {
            swalToastSuccess(succeeded > 1 ? `Added ${succeeded} file shares` : 'File share added');
          }
        });
      });
      return;
    }

    if (this.shareType === 2) {
      const locIds = [...new Set(this.selectedShareLocationIds.filter((id) => id != null))];
      if (!locIds.length) {
        void swalWarning('Select locations', 'Choose at least one location.');
        return;
      }
      const reqs = locIds.map((locationId) =>
        this.api.addDocumentShare(docId, { shareType: 2, locationId }).pipe(catchError(() => of(null)))
      );
      forkJoin(reqs).subscribe((results) => {
        const failed = results.filter((r) => r === null).length;
        const succeeded = results.length - failed;
        this.api.listDocumentShares(docId).subscribe((s) => {
          this.documentShares = s;
          if (this.explorerSelectedDocId === docId) {
            this.documentPanelShares = s;
          }
          if (!succeeded) {
            void swalError('Could not add file shares', 'Check the selections and try again.');
          } else if (failed > 0) {
            void swalToastWarning(
              `Added ${succeeded} share(s); ${failed} could not be added (may already exist).`
            );
          } else {
            swalToastSuccess(succeeded > 1 ? `Added ${succeeded} file shares` : 'File share added');
          }
        });
      });
      return;
    }
  }

  removeDocumentShare(share: DocumentShare): void {
    if (!this.isMgmt || !this.selectedDocForShares) return;
    void swalConfirmRemove(
      'Remove this file share?',
      'People matched by this rule will lose access to this file through this share.'
    ).then((ok) => {
      if (!ok) return;
      this.api
        .removeDocumentShare(this.selectedDocForShares!.id, share.id)
        .subscribe({
          next: () => {
            this.documentShares = this.documentShares.filter((s) => s.id !== share.id);
            if (this.explorerSelectedDocId === this.selectedDocForShares?.id) {
              this.documentPanelShares = this.documentShares;
            }
            swalToastSuccess('Share removed');
          },
          error: () => void swalError('Could not remove share', 'Try again.'),
        });
    });
  }

  private findNode(nodes: FolderNode[], id: number): FolderNode | null {
    for (const n of nodes) {
      if (n.id === id) return n;
      const c = this.findNode(n.children || [], id);
      if (c) return c;
    }
    return null;
  }

  private findFolderPath(nodes: FolderNode[], id: number): FolderNode[] | null {
    for (const n of nodes) {
      if (n.id === id) return [n];
      const childPath = this.findFolderPath(n.children || [], id);
      if (childPath) return [n, ...childPath];
    }
    return null;
  }

  private filterTree(nodes: FolderNode[], term: string): FolderNode[] {
    const acc: FolderNode[] = [];
    for (const n of nodes) {
      const children = this.filterTree(n.children || [], term);
      const match = n.name.toLowerCase().includes(term);
      if (match || children.length) {
        acc.push({ ...n, children });
      }
    }
    return acc;
  }
}
