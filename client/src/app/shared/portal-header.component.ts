import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../core/auth.service';
import { DocumentsApiService } from '../services/documents-api.service';

/** Global app header: logo, portal title, profile menu (used on documents + admin pages). */
@Component({
  selector: 'app-portal-header',
  templateUrl: './portal-header.component.html',
})
export class PortalHeaderComponent implements OnInit, OnDestroy {
  profileMenuOpen = false;
  /** Pending employee registrations (Management only); null until first load or when not Management. */
  pendingApprovalsCount: number | null = null;

  private readonly destroy$ = new Subject<void>();

  constructor(
    readonly auth: AuthService,
    private readonly router: Router,
    private readonly api: DocumentsApiService
  ) {}

  ngOnInit(): void {
    this.auth.user$.pipe(takeUntil(this.destroy$)).subscribe((u) => {
      if (u?.roles?.includes('Management')) {
        this.refreshPendingApprovalsCount();
      } else {
        this.pendingApprovalsCount = null;
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(ev: MouseEvent): void {
    const t = ev.target as HTMLElement;
    if (!t.closest('[data-profile-menu]')) {
      this.profileMenuOpen = false;
    }
  }

  get isMgmt(): boolean {
    return this.auth.isManagement();
  }

  get profileInitials(): string {
    const name = this.auth.current?.displayName?.trim();
    if (name) {
      const parts = name.split(/\s+/).filter(Boolean);
      if (parts.length >= 2) {
        return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
      }
      return name.slice(0, 2).toUpperCase();
    }
    const email = this.auth.current?.email?.trim();
    return (email?.slice(0, 2) || '?').toUpperCase();
  }

  toggleProfileMenu(ev: MouseEvent): void {
    ev.stopPropagation();
    this.profileMenuOpen = !this.profileMenuOpen;
    if (this.profileMenuOpen && this.isMgmt) {
      this.refreshPendingApprovalsCount();
    }
  }

  closeProfileMenu(): void {
    this.profileMenuOpen = false;
  }

  logout(): void {
    this.profileMenuOpen = false;
    this.auth.logout();
    void this.router.navigate(['/login']);
  }

  private refreshPendingApprovalsCount(): void {
    this.api.pendingRegistrationCount().subscribe({
      next: (c) => (this.pendingApprovalsCount = c),
      error: () => (this.pendingApprovalsCount = null),
    });
  }
}
