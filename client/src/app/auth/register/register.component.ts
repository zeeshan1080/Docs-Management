import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { EMAIL_PATTERN, emailValidationMessage } from '../../core/email-format';
import { passwordStrengthMessage } from '../../core/password-policy';
import { subscribeRestoreSession } from '../../core/guest-session';
import { DocumentsApiService } from '../../services/documents-api.service';
import { LocationOption, RoleOption } from '../../models/models';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
})
export class RegisterComponent implements OnInit, OnDestroy {
  readonly emailPattern = EMAIL_PATTERN;
  email = '';
  password = '';
  confirmPassword = '';
  firstName = '';
  lastName = '';
  requestedRoleName = '';
  requestedLocationId: number | null = null;
  locations: LocationOption[] = [];
  roles: RoleOption[] = [];
  error = '';
  success = '';
  loading = false;
  sessionChecking = false;
  private sessionSub: Subscription | null = null;

  constructor(
    private readonly auth: AuthService,
    private readonly api: DocumentsApiService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.sessionSub = subscribeRestoreSession(this.auth, this.router, (v) => (this.sessionChecking = v), () =>
      this.loadCatalog()
    );
  }

  ngOnDestroy(): void {
    this.sessionSub?.unsubscribe();
  }

  private loadCatalog(): void {
    this.api.locations().subscribe((l) => (this.locations = l));
    this.api.roles().subscribe((r) => (this.roles = r));
  }

  submit(): void {
    this.error = '';
    this.success = '';
    const firstName = this.firstName.trim();
    const lastName = this.lastName.trim();
    if (!firstName) {
      this.error = 'First name is required.';
      return;
    }
    if (!lastName) {
      this.error = 'Last name is required.';
      return;
    }
    const emailErr = emailValidationMessage(this.email, 'Email is required.');
    if (emailErr) {
      this.error = emailErr;
      return;
    }
    const email = this.email.trim();
    if (!this.requestedLocationId) {
      this.error = 'Location is required.';
      return;
    }
    if (!this.requestedRoleName?.trim()) {
      this.error = 'Role is required.';
      return;
    }
    const pwMsg = passwordStrengthMessage(this.password);
    if (pwMsg) {
      this.error = pwMsg;
      return;
    }
    if (!this.confirmPassword) {
      this.error = 'Confirm password is required.';
      return;
    }
    if (this.password !== this.confirmPassword) {
      this.error = 'Passwords do not match.';
      return;
    }
    this.loading = true;
    this.auth
      .register({
        email,
        password: this.password,
        firstName,
        lastName,
        requestedRoleName: this.requestedRoleName.trim(),
        requestedLocationId: this.requestedLocationId,
      })
      .subscribe({
        next: () => {
          this.loading = false;
          this.success =
            'Registration submitted. Management will review your access. You can sign in once approved.';
        },
        error: (err: { error?: { error?: string } }) => {
          this.loading = false;
          const body = err?.error;
          this.error =
            (typeof body === 'object' && body?.error && String(body.error)) ||
            'Registration failed. Check your password meets the requirements.';
        },
      });
  }
}
