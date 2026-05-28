export interface AuthUser {
  token: string;
  email: string;
  userId: string;
  roles: string[];
  approvalStatus: number;
  displayName: string;
}

export interface LocationOption {
  id: number;
  name: string;
  /** True when location is inactive (not offered for registration/shares). */
  inactive?: boolean;
}

export interface RoleOption {
  id: string;
  name: string;
}

export interface FolderNode {
  id: number;
  name: string;
  parentFolderId: number | null;
  /** When true, folder cannot be deleted (system default). */
  isDefault?: boolean;
  createdOn: string;
  children: FolderNode[];
  createdByUserId?: string | null;
  createdByDisplayName?: string | null;
}

export interface FolderStats {
  directChildFolderCount: number;
  documentCount: number;
  totalDocumentSizeBytes: number;
}

export interface DocumentItem {
  id: number;
  folderId: number;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  /** When true, opens `externalUrl` in a new tab instead of a stored file. */
  isLink?: boolean;
  externalUrl?: string | null;
  createdOn: string;
  createdByUserId?: string | null;
  createdByDisplayName?: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface FolderShare {
  id: number;
  folderId: number;
  shareType: number;
  /** 1 = view only, 2 = full access (default 2 if omitted by older API). */
  accessLevel?: number;
  roleId: string | null;
  roleName: string | null;
  locationId: number | null;
  locationName: string | null;
  userId: string | null;
  userEmail: string | null;
  createdOn: string;
  createdByUserId?: string | null;
  createdByDisplayName?: string | null;
}

export interface DocumentShare {
  id: number;
  documentId: number;
  shareType: number;
  roleId: string | null;
  roleName: string | null;
  locationId: number | null;
  locationName: string | null;
  userId: string | null;
  userEmail: string | null;
  createdOn: string;
  createdByUserId?: string | null;
  createdByDisplayName?: string | null;
}

export interface AdminUserListItem {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  approvalStatus: number;
  roles: string[];
  primaryLocationId: number | null;
  primaryLocationName: string | null;
}

export interface RegistrationRequest {
  id: number;
  userId: string;
  userEmail: string;
  userName: string;
  requestedRoleId: string;
  requestedRoleName: string;
  requestedLocationId: number;
  requestedLocationName: string;
  status: number;
  createdOn: string;
}
