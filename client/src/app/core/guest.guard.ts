import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Guest-only routes. Redirects immediately when the user is already in memory.
 * If only a token exists, allows the route so the screen can show a session check (see `subscribeRestoreSession`).
 */
@Injectable({ providedIn: 'root' })
export class GuestGuard implements CanActivate {
  constructor(
    private readonly auth: AuthService,
    private readonly router: Router
  ) {}

  canActivate(): boolean | UrlTree {
    if (!this.auth.token) {
      return true;
    }
    if (this.auth.current) {
      return this.router.parseUrl('/documents');
    }
    return true;
  }
}
