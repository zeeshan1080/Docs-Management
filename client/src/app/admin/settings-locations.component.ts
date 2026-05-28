import { Component, OnInit } from '@angular/core';
import { requiredTrimmed } from '../core/required-field';
import { DocumentsApiService } from '../services/documents-api.service';
import { LocationOption } from '../models/models';

@Component({
  selector: 'app-settings-locations',
  templateUrl: './settings-locations.component.html',
})
export class SettingsLocationsComponent implements OnInit {
  locations: LocationOption[] = [];
  loading = false;
  name = '';
  addInactive = false;
  error = '';
  success = '';
  saving = false;
  addModalOpen = false;
  editModalOpen = false;
  editingLocation: LocationOption | null = null;
  editName = '';
  editInactive = false;
  get blockingLoading(): boolean {
    return this.loading || this.saving;
  }

  constructor(private readonly api: DocumentsApiService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.api.adminListLocations(true).subscribe({
      next: (l) => {
        this.locations = l;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.error = 'Could not load locations.';
      },
    });
  }

  submit(): void {
    this.error = '';
    this.success = '';
    const nameErr = requiredTrimmed(this.name, 'Display name');
    if (nameErr) {
      this.error = nameErr;
      return;
    }
    this.saving = true;
    this.api.adminAddLocation(this.name.trim(), this.addInactive).subscribe({
      next: () => {
        this.saving = false;
        this.name = '';
        this.addInactive = false;
        this.success = 'Location added.';
        this.addModalOpen = false;
        this.load();
      },
      error: (err) => {
        this.saving = false;
        this.error = err.error?.error || 'Could not add location.';
      },
    });
  }

  openAddModal(): void {
    this.error = '';
    this.success = '';
    this.addInactive = false;
    this.addModalOpen = true;
  }

  closeAddModal(): void {
    this.addModalOpen = false;
  }

  openEditModal(loc: LocationOption): void {
    this.error = '';
    this.success = '';
    this.editingLocation = loc;
    this.editName = loc.name;
    this.editInactive = !!loc.inactive;
    this.editModalOpen = true;
  }

  closeEditModal(): void {
    this.editModalOpen = false;
    this.editingLocation = null;
    this.editName = '';
  }

  submitEdit(): void {
    if (!this.editingLocation) return;
    this.error = '';
    this.success = '';
    const nameErr = requiredTrimmed(this.editName, 'Display name');
    if (nameErr) {
      this.error = nameErr;
      return;
    }
    this.saving = true;
    const id = this.editingLocation.id;
    this.api.adminUpdateLocation(id, this.editName.trim(), this.editInactive).subscribe({
      next: () => {
        this.saving = false;
        this.closeEditModal();
        this.load();
      },
      error: (err) => {
        this.saving = false;
        this.error = err.error?.error || 'Could not update location.';
      },
    });
  }

  retryLoad(): void {
    this.error = '';
    this.load();
  }
}
