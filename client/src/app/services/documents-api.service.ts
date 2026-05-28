import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent, HttpParams } from '@angular/common/http';
import { Observable, range } from 'rxjs';
import { concatMap, map, switchMap, toArray } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import {
  AdminUserListItem,
  DocumentItem,
  DocumentShare,
  FolderNode,
  FolderShare,
  FolderStats,
  LocationOption,
  PagedResult,
  RegistrationRequest,
  RoleOption,
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class DocumentsApiService {
  private readonly base = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  locations(): Observable<LocationOption[]> {
    return this.http.get<LocationOption[]>(`${this.base}/api/locations`);
  }

  /**
   * Management locations list. Pass true only on the Locations settings page so inactive rows can be managed.
   */
  adminListLocations(includeInactive = false): Observable<LocationOption[]> {
    const params = includeInactive ? { params: { includeInactive: 'true' } } : {};
    return this.http.get<LocationOption[]>(
      `${this.base}/api/admin/settings/locations`,
      params
    );
  }

  roles(): Observable<RoleOption[]> {
    return this.http.get<RoleOption[]>(`${this.base}/api/roles/options`);
  }

  folderTree(): Observable<FolderNode[]> {
    return this.http.get<FolderNode[]>(`${this.base}/api/folders/tree`);
  }

  createFolder(name: string, parentFolderId: number | null): Observable<FolderNode> {
    return this.http.post<FolderNode>(`${this.base}/api/folders`, {
      name,
      parentFolderId,
    });
  }

  deleteFolder(folderId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/folders/${folderId}`);
  }

  renameFolder(folderId: number, name: string): Observable<FolderNode> {
    return this.http.put<FolderNode>(`${this.base}/api/folders/${folderId}`, { name });
  }

  uploadAllowed(folderId: number): Observable<{ allowed: boolean }> {
    return this.http.get<{ allowed: boolean }>(
      `${this.base}/api/folders/${folderId}/upload-allowed`
    );
  }

  folderStats(folderId: number): Observable<FolderStats> {
    return this.http.get<FolderStats>(`${this.base}/api/folders/${folderId}/stats`);
  }

  documentsInFolder(folderId: number): Observable<DocumentItem[]> {
    return this.http.get<DocumentItem[]>(
      `${this.base}/api/documents/folder/${folderId}`
    );
  }

  documentsInFolderPaged(
    folderId: number,
    page: number,
    pageSize: number,
    search?: string
  ): Observable<PagedResult<DocumentItem>> {
    let params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize));
    if (search?.trim()) params = params.set('search', search.trim());
    return this.http.get<PagedResult<DocumentItem>>(
      `${this.base}/api/documents/folder/${folderId}/paged`,
      { params }
    );
  }

  uploadDocument(folderId: number, file: File): Observable<DocumentItem> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<DocumentItem>(
      `${this.base}/api/documents/folder/${folderId}/upload`,
      fd
    );
  }

  /**
   * Multipart chunk upload (init → sequential chunks → complete). Use for large files; chunk size must
   * keep each part within the API MaxChunkUploadBytes (default 16 MiB).
   */
  uploadDocumentChunked(
    folderId: number,
    file: File,
    chunkSizeBytes = 5 * 1024 * 1024
  ): Observable<DocumentItem> {
    const totalChunks = Math.max(1, Math.ceil(file.size / chunkSizeBytes));
    return this.http
      .post<{ sessionId: string }>(
        `${this.base}/api/documents/folder/${folderId}/upload/chunk/init`,
        {
          fileName: file.name,
          contentType: file.type || 'application/octet-stream',
          totalSize: file.size,
          totalChunks,
        }
      )
      .pipe(
        switchMap((res) => {
          const sessionId = res.sessionId;
          return range(0, totalChunks).pipe(
            concatMap((chunkIndex) => {
              const start = chunkIndex * chunkSizeBytes;
              const end = Math.min(start + chunkSizeBytes, file.size);
              const piece = file.slice(start, end);
              const fd = new FormData();
              fd.append('sessionId', sessionId);
              fd.append('chunkIndex', String(chunkIndex));
              fd.append(
                'chunk',
                new File([piece], 'chunk.bin', { type: 'application/octet-stream' })
              );
              return this.http.post(
                `${this.base}/api/documents/folder/${folderId}/upload/chunk`,
                fd
              );
            }),
            toArray(),
            switchMap(() =>
              this.http.post<DocumentItem>(
                `${this.base}/api/documents/folder/${folderId}/upload/chunk/complete`,
                { sessionId }
              )
            )
          );
        })
      );
  }

  /** Same upload as `uploadDocument` with progress events (parallel-friendly). */
  uploadDocumentWithProgress(
    folderId: number,
    file: File
  ): Observable<HttpEvent<DocumentItem>> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<DocumentItem>(
      `${this.base}/api/documents/folder/${folderId}/upload`,
      fd,
      { reportProgress: true, observe: 'events' }
    );
  }

  /** Google Docs Viewer iframe URL (server wraps file URL in docs.google.com/viewer). */
  getDocumentViewerUrl(documentId: number): Observable<{ viewerUrl: string }> {
    return this.http.get<{ viewerUrl: string }>(`${this.base}/api/documents/${documentId}/viewer`);
  }

  createDocumentLink(
    folderId: number,
    body: { displayName: string; url: string }
  ): Observable<DocumentItem> {
    return this.http.post<DocumentItem>(`${this.base}/api/documents/folder/${folderId}/link`, body);
  }

  downloadBlob(documentId: number): Observable<Blob> {
    return this.http.get(`${this.base}/api/documents/${documentId}/download`, {
      responseType: 'blob',
    });
  }

  deleteDocument(documentId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/documents/${documentId}`);
  }

  listShares(folderId: number): Observable<FolderShare[]> {
    return this.http.get<FolderShare[]>(
      `${this.base}/api/folders/${folderId}/shares`
    );
  }

  addShare(
    folderId: number,
    body: {
      accessLevel: number;
      shareType: number;
      roleId?: string | null;
      locationId?: number | null;
      userId?: string | null;
    }
  ): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(
      `${this.base}/api/folders/${folderId}/shares`,
      body
    );
  }

  removeShare(folderId: number, shareId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.base}/api/folders/${folderId}/shares/${shareId}`
    );
  }

  listDocumentShares(documentId: number): Observable<DocumentShare[]> {
    return this.http.get<DocumentShare[]>(
      `${this.base}/api/documents/${documentId}/shares`
    );
  }

  addDocumentShare(
    documentId: number,
    body: {
      shareType: number;
      roleId?: string | null;
      locationId?: number | null;
      userId?: string | null;
    }
  ): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(
      `${this.base}/api/documents/${documentId}/shares`,
      body
    );
  }

  removeDocumentShare(documentId: number, shareId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.base}/api/documents/${documentId}/shares/${shareId}`
    );
  }

  pendingRegistrations(): Observable<RegistrationRequest[]> {
    return this.http.get<RegistrationRequest[]>(
      `${this.base}/api/registrationrequests/pending`
    );
  }

  pendingRegistrationCount(): Observable<number> {
    return this.http
      .get<{ count: number }>(`${this.base}/api/registrationrequests/pending/count`)
      .pipe(map((r) => r.count));
  }

  reviewRegistration(
    id: number,
    body: {
      approve: boolean;
      assignedRoleId?: string | null;
      assignedLocationId?: number | null;
      notes?: string | null;
    }
  ): Observable<void> {
    return this.http.post<void>(
      `${this.base}/api/registrationrequests/${id}/review`,
      body
    );
  }

  adminUsers(): Observable<AdminUserListItem[]> {
    return this.http.get<AdminUserListItem[]>(`${this.base}/api/admin/users`);
  }

  adminCreateUser(body: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    roleId: string;
    locationId: number;
  }): Observable<{ userId: string }> {
    return this.http.post<{ userId: string }>(`${this.base}/api/admin/users`, body);
  }

  adminUpdateUser(
    userId: string,
    body: {
      email: string;
      firstName: string;
      lastName: string;
      roleId: string;
      locationId: number;
      approvalStatus: number;
      newPassword?: string | null;
    }
  ): Observable<void> {
    return this.http.put<void>(
      `${this.base}/api/admin/users/${encodeURIComponent(userId)}`,
      body
    );
  }

  adminDeleteUser(userId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/admin/users/${encodeURIComponent(userId)}`);
  }

  adminAddLocation(name: string, inactive = false): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.base}/api/admin/settings/locations`, {
      name,
      inactive,
    });
  }

  adminUpdateLocation(id: number, name: string, inactive: boolean): Observable<void> {
    return this.http.put<void>(`${this.base}/api/admin/settings/locations/${id}`, { name, inactive });
  }

  adminAddRole(name: string): Observable<{ roleId: string }> {
    return this.http.post<{ roleId: string }>(
      `${this.base}/api/admin/settings/roles`,
      { name }
    );
  }

  adminUpdateRole(roleId: string, name: string): Observable<void> {
    return this.http.put<void>(`${this.base}/api/admin/settings/roles/${encodeURIComponent(roleId)}`, {
      name,
    });
  }
}
