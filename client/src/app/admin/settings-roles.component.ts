import { Component, OnInit } from '@angular/core';
import { requiredTrimmed } from '../core/required-field';
import { DocumentsApiService } from '../services/documents-api.service';
import { RoleOption } from '../models/models';

@Component({
  selector: 'app-settings-roles',
  templateUrl: './settings-roles.component.html',
})
export class SettingsRolesComponent implements OnInit {
  roles: RoleOption[] = [];
  loading = false;
  name = '';
  error = '';
  success = '';
  saving = false;
  addModalOpen = false;
  editModalOpen = false;
  editingRole: RoleOption | null = null;
  editName = '';
  get blockingLoading(): boolean {
    return this.loading || this.saving;
  }

  constructor(private readonly api: DocumentsApiService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.api.roles().subscribe({
      next: (r) => {
        this.roles = r;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.error = 'Could not load roles.';
      },
    });
  }

  submit(): void {
    this.error = '';
    this.success = '';
    const nameErr = requiredTrimmed(this.name, 'Role name');
    if (nameErr) {
      this.error = nameErr;
      return;
    }
    this.saving = true;
    this.api.adminAddRole(this.name.trim()).subscribe({
      next: () => {
        this.saving = false;
        this.name = '';
        this.success = 'Role added. It is available for registration, sharing, and new users.';
        this.addModalOpen = false;
        this.load();
      },
      error: (err) => {
        this.saving = false;
        this.error = err.error?.error || 'Could not add role.';
      },
    });
  }

  openAddModal(): void {
    this.error = '';
    this.success = '';
    this.addModalOpen = true;
  }

  closeAddModal(): void {
    this.addModalOpen = false;
  }

  openEditModal(r: RoleOption): void {
    if (this.isManagementRole(r)) return;
    this.error = '';
    this.success = '';
    this.editingRole = r;
    this.editName = r.name;
    this.editModalOpen = true;
  }

  closeEditModal(): void {
    this.editModalOpen = false;
    this.editingRole = null;
    this.editName = '';
  }

  submitEdit(): void {
    if (!this.editingRole) return;
    this.error = '';
    this.success = '';
    const nameErr = requiredTrimmed(this.editName, 'Role name');
    if (nameErr) {
      this.error = nameErr;
      return;
    }
    this.saving = true;
    this.api.adminUpdateRole(this.editingRole.id, this.editName.trim()).subscribe({
      next: () => {
        this.saving = false;
        this.success = 'Role updated.';
        this.closeEditModal();
        this.load();
      },
      error: (err) => {
        this.saving = false;
        this.error = err.error?.error || 'Could not update role.';
      },
    });
  }

  /** System role used for admin APIs; renaming would break access control. */
  isManagementRole(r: RoleOption): boolean {
    return (r.name || '').trim() === 'Management';
  }

  retryLoad(): void {
    this.error = '';
    this.load();
  }
}
