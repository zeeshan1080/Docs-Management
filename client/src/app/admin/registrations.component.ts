import { Component, OnInit } from '@angular/core';
import { swalConfirm, swalError, swalToastSuccess } from '../core/swal';
import { DocumentsApiService } from '../services/documents-api.service';
import { LocationOption, RegistrationRequest, RoleOption } from '../models/models';

@Component({
  selector: 'app-registrations',
  templateUrl: './registrations.component.html',
})
export class RegistrationsComponent implements OnInit {
  items: RegistrationRequest[] = [];
  roles: RoleOption[] = [];
  locations: LocationOption[] = [];
  busy: Record<number, boolean> = {};

  constructor(private readonly api: DocumentsApiService) {}

  ngOnInit(): void {
    this.load();
    this.api.roles().subscribe((r) => (this.roles = r));
    this.api.locations().subscribe((l) => (this.locations = l));
  }

  load(): void {
    this.api.pendingRegistrations().subscribe({
      next: (x) => (this.items = x),
      error: () =>
        void swalError('Could not load requests', 'Refresh the page or try again later.'),
    });
  }

  approve(
    row: RegistrationRequest,
    assignedRoleId: string,
    assignedLocationId: number
  ): void {
    if (!assignedRoleId?.trim()) {
      void swalError('Role required', 'Select a role before approving.');
      return;
    }
    if (!Number.isFinite(assignedLocationId) || assignedLocationId <= 0) {
      void swalError('Location required', 'Select a location before approving.');
      return;
    }
    this.busy[row.id] = true;
    this.api
      .reviewRegistration(row.id, {
        approve: true,
        assignedRoleId,
        assignedLocationId,
      })
      .subscribe({
        next: () => {
          this.busy[row.id] = false;
          swalToastSuccess('Registration approved');
          this.load();
        },
        error: () => {
          this.busy[row.id] = false;
          void swalError('Approve failed', 'Could not approve this registration.');
        },
      });
  }

  reject(row: RegistrationRequest): void {
    void swalConfirm(
      'Reject this registration?',
      'The applicant will remain pending until they register again or you approve another request.',
      'Reject'
    ).then((ok) => {
      if (!ok) return;
      this.busy[row.id] = true;
      this.api
        .reviewRegistration(row.id, { approve: false, notes: 'Rejected' })
        .subscribe({
          next: () => {
            this.busy[row.id] = false;
            swalToastSuccess('Registration rejected');
            this.load();
          },
          error: () => {
            this.busy[row.id] = false;
            void swalError('Reject failed', 'Could not reject this registration.');
          },
        });
    });
  }
}
