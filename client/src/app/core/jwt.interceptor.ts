import { Injectable, Injector } from '@angular/core';
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { ToastService } from './toast.service';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiUiError } from '../shared/ui-state';

const TOKEN_KEY = 'dm_token';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  constructor(
    private readonly injector: Injector,
    private readonly router: Router,
    private readonly toast: ToastService
  ) {}

  intercept(
    req: HttpRequest<unknown>,
    next: HttpHandler
  ): Observable<HttpEvent<unknown>> {
    const token = localStorage.getItem(TOKEN_KEY);
    let authReq = req;
    if (token) {
      authReq = req.clone({
        setHeaders: { Authorization: `Bearer ${token}` },
      });
    }
    return next.handle(authReq).pipe(
      catchError((err) => {
        const normalized = this.normalizeError(err);
        if (normalized.status === 401) {
          localStorage.removeItem(TOKEN_KEY);
          const auth = this.injector.get(AuthService);
          auth.logout();
          this.router.navigate(['/login']);
        } else if (normalized.status === 403) {
          this.toast.warning('Access denied');
        } else if (normalized.status === 404) {
          this.toast.info('Requested resource was not found');
        } else if (normalized.status >= 500) {
          this.toast.error('Server error. Please retry.');
        } else if (normalized.status === 0) {
          this.toast.error('Network error. Please check your connection.');
        }
        return throwError(() => normalized);
      })
    );
  }

  private normalizeError(err: unknown): ApiUiError {
    const http = err as HttpErrorResponse;
    const payload = (http?.error || {}) as {
      message?: string;
      error?: string;
      title?: string;
      traceId?: string;
    };
    return {
      message: payload.message || payload.error || payload.title || http?.message || 'Request failed',
      status: http?.status ?? 0,
      traceId: payload.traceId,
    };
  }
}
