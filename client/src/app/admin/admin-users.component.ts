import { Component, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';
import { swalConfirmDelete, swalError, swalToastSuccess } from '../core/swal';
import { AuthService } from '../core/auth.service';
import { EMAIL_PATTERN, emailValidationMessage } from '../core/email-format';
import { passwordStrengthMessage } from '../core/password-policy';
import { requiredTrimmed } from '../core/required-field';
import { DocumentsApiService } from '../services/documents-api.service';
import { AdminUserListItem, LocationOption, RoleOption } from '../models/models';

@Component({
  selector: 'app-admin-users',
  templateUrl: './admin-users.component.html',
})
export class AdminUsersComponent implements OnInit {
  readonly emailPattern = EMAIL_PATTERN;
  users: AdminUserListItem[] = [];
  locations: LocationOption[] = [];
  roles: RoleOption[] = [];
  loading = true;

  showCreateUser = false;
  createBusy = false;
  createError = '';
  createEmail = '';
  createPassword = '';
  createFirstName = '';
  createLastName = '';
  createRoleId = '';
  createLocationId: number | null = null;

  editingUser: AdminUserListItem | null = null;
  editBusy = false;
  editError = '';
  editEmail = '';
  editFirstName = '';
  editLastName = '';
  editRoleId = '';
  editLocationId: number | null = null;
  editApprovalStatus = 1;
  editNewPassword = '';

  constructor(
    private readonly api: DocumentsApiService,
    readonly auth: AuthService
  ) {}

  ngOnInit(): void {
    forkJoin({
      users: this.api.adminUsers(),
      locations: this.api.locations(),
      roles: this.api.roles(),
    }).subscribe({
      next: ({ users, locations, roles }) => {
        this.users = users;
        this.locations = locations;
        this.roles = roles;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        void swalError('Could not load users', 'Try again later or check your connection.');
      },
    });
  }

  private refreshUsers(): void {
    this.api.adminUsers().subscribe({
      next: (list) => (this.users = list),
      error: () => void swalError('Could not refresh users', ''),
    });
  }

  approvalLabel(status: number): string {
    switch (status) {
      case 1:
        return 'Approved';
      case 0:
        return 'Pending';
      case 2:
        return 'Rejected';
      default:
        return 'Unknown';
    }
  }

  approvalBadgeClass(status: number): string {
    if (status === 1) return 'bg-emerald-100 text-emerald-900';
    if (status === 2) return 'bg-red-100 text-red-900';
    return 'bg-amber-100 text-amber-900';
  }

  toggleCreateUser(): void {
    this.showCreateUser = !this.showCreateUser;
    if (this.showCreateUser) {
      this.createError = '';
    }
  }

  submitCreate(): void {
    this.createError = '';
    const emailErr = emailValidationMessage(this.createEmail, 'Email is required.');
    if (emailErr) {
      this.createError = emailErr;
      return;
    }
    const fnErr = requiredTrimmed(this.createFirstName, 'First name');
    if (fnErr) {
      this.createError = fnErr;
      return;
    }
    const lnErr = requiredTrimmed(this.createLastName, 'Last name');
    if (lnErr) {
      this.createError = lnErr;
      return;
    }
    const createPwMsg = passwordStrengthMessage(this.createPassword);
    if (createPwMsg) {
      this.createError = createPwMsg;
      return;
    }
    if (!this.createRoleId?.trim()) {
      this.createError = 'Role is required.';
      return;
    }
    if (this.createLocationId == null) {
      this.createError = 'Location is required.';
      return;
    }
    const createEmail = this.createEmail.trim();
    this.createBusy = true;
    this.api
      .adminCreateUser({
        email: createEmail,
        password: this.createPassword,
        firstName: this.createFirstName.trim(),
        lastName: this.createLastName.trim(),
        roleId: this.createRoleId,
        locationId: this.createLocationId,
      })
      .subscribe({
        next: () => {
          this.createBusy = false;
          this.showCreateUser = false;
          this.createEmail = '';
          this.createPassword = '';
          this.createFirstName = '';
          this.createLastName = '';
          this.createRoleId = '';
          this.createLocationId = null;
          this.refreshUsers();
          void swalToastSuccess('User created');
        },
        error: (err) => {
          this.createBusy = false;
          this.createError = err.error?.error || 'Could not create user.';
        },
      });
  }

  openEdit(u: AdminUserListItem): void {
    this.editError = '';
    this.editingUser = u;
    this.editEmail = u.email;
    this.editFirstName = u.firstName || '';
    this.editLastName = u.lastName || '';
    this.editRoleId = this.roleIdFromUser(u);
    this.editLocationId = u.primaryLocationId ?? this.locations[0]?.id ?? null;
    this.editApprovalStatus = u.approvalStatus;
    this.editNewPassword = '';
  }

  cancelEdit(): void {
    this.editingUser = null;
    this.editError = '';
  }

  private roleIdFromUser(u: AdminUserListItem): string {
    const sorted = [...(u.roles || [])].sort();
    const name = sorted[0];
    if (!name) return '';
    return this.roles.find((r) => r.name === name)?.id ?? '';
  }

  submitEdit(): void {
    if (!this.editingUser) return;
    this.editError = '';
    const emailErr = emailValidationMessage(this.editEmail, 'Email is required.');
    if (emailErr) {
      this.editError = emailErr;
      return;
    }
    const fnErr = requiredTrimmed(this.editFirstName, 'First name');
    if (fnErr) {
      this.editError = fnErr;
      return;
    }
    const lnErr = requiredTrimmed(this.editLastName, 'Last name');
    if (lnErr) {
      this.editError = lnErr;
      return;
    }
    if (!this.editRoleId?.trim()) {
      this.editError = 'Role is required.';
      return;
    }
    if (this.editLocationId == null) {
      this.editError = 'Location is required.';
      return;
    }
    const np = this.editNewPassword.trim();
    if (np.length > 0) {
      const editPwMsg = passwordStrengthMessage(np);
      if (editPwMsg) {
        this.editError = editPwMsg;
        return;
      }
    }
    const body = {
      email: this.editEmail.trim(),
      firstName: this.editFirstName.trim(),
      lastName: this.editLastName.trim(),
      roleId: this.editRoleId,
      locationId: this.editLocationId,
      approvalStatus: this.editApprovalStatus,
      ...(np.length > 0 ? { newPassword: np } : {}),
    };
    this.editBusy = true;
    this.api.adminUpdateUser(this.editingUser.id, body).subscribe({
      next: () => {
        this.editBusy = false;
        this.editingUser = null;
        this.refreshUsers();
        void swalToastSuccess('User updated');
      },
      error: (err) => {
        this.editBusy = false;
        this.editError = err.error?.error || 'Could not update user.';
      },
    });
  }

  isCurrentUser(userId: string): boolean {
    return this.auth.current?.userId === userId;
  }

  async confirmDelete(u: AdminUserListItem): Promise<void> {
    if (this.isCurrentUser(u.id)) {
      void swalError('Cannot delete your own account', '');
      return;
    }
    const ok = await swalConfirmDelete(
      'Delete this user?',
      `${u.firstName} ${u.lastName} (${u.email}) will lose access. This cannot be undone.`
    );
    if (!ok) return;
    this.api.adminDeleteUser(u.id).subscribe({
      next: () => {
        this.refreshUsers();
        void swalToastSuccess('User deleted');
      },
      error: (err) => {
        void swalError('Could not delete user', err.error?.error || '');
      },
    });
  }
}
