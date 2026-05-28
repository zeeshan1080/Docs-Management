import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * If a token exists but the user is not hydrated, calls `loadMe()` and redirects to `/documents` on success.
 * Shows loading via `setChecking(true)` while the request runs.
 * On failure, clears the session and calls `onReady` (e.g. load guest-only data).
 * @returns A subscription to unsubscribe in `ngOnDestroy`, or `null` if no async work was started.
 */
export function subscribeRestoreSession(
  auth: AuthService,
  router: Router,
  setChecking: (checking: boolean) => void,
  onReady?: () => void
): Subscription | null {
  if (!auth.token) {
    setChecking(false);
    onReady?.();
    return null;
  }
  if (auth.current) {
    setChecking(false);
    void router.navigate(['/documents']);
    return null;
  }
  setChecking(true);
  return auth.loadMe().subscribe({
    next: () => void router.navigate(['/documents']),
    error: () => {
      auth.logout();
      setChecking(false);
      onReady?.();
    },
  });
}
