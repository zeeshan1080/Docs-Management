import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthUser } from '../models/models';

const TOKEN_KEY = 'dm_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly userSubject = new BehaviorSubject<AuthUser | null>(null);
  readonly user$ = this.userSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  get current(): AuthUser | null {
    return this.userSubject.value;
  }

  login(email: string, password: string): Observable<AuthUser> {
    return this.http
      .post<AuthUser>(`${environment.apiUrl}/api/auth/login`, { email, password })
      .pipe(tap((u) => this.persist(u)));
  }

  register(body: Record<string, unknown>): Observable<unknown> {
    return this.http.post(`${environment.apiUrl}/api/auth/register`, body);
  }

  forgotPassword(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${environment.apiUrl}/api/auth/forgot-password`,
      { email }
    );
  }

  resetPassword(email: string, token: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/api/auth/reset-password`, {
      email,
      token,
      newPassword,
    });
  }

  loadMe(): Observable<AuthUser> {
    return this.http
      .get<AuthUser>(`${environment.apiUrl}/api/auth/me`)
      .pipe(tap((u) => this.persist(u)));
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.userSubject.next(null);
  }

  isManagement(): boolean {
    return !!this.current?.roles?.includes('Management');
  }

  isApproved(): boolean {
    return this.current?.approvalStatus === 1;
  }

  isPending(): boolean {
    return this.current?.approvalStatus === 0;
  }

  private persist(u: AuthUser): void {
    localStorage.setItem(TOKEN_KEY, u.token);
    this.userSubject.next(u);
  }
}
