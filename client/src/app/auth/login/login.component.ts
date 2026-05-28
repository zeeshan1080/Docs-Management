import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { EMAIL_PATTERN, emailValidationMessage } from '../../core/email-format';
import { subscribeRestoreSession } from '../../core/guest-session';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
})
export class LoginComponent implements OnInit, OnDestroy {
  readonly emailPattern = EMAIL_PATTERN;
  email = '';
  password = '';
  error = '';
  loading = false;
  /** True while validating an existing token before redirect. */
  sessionChecking = false;
  private sessionSub: Subscription | null = null;

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.sessionSub = subscribeRestoreSession(this.auth, this.router, (v) => (this.sessionChecking = v));
  }

  ngOnDestroy(): void {
    this.sessionSub?.unsubscribe();
  }

  submit(): void {
    this.error = '';
    const emailErr = emailValidationMessage(this.email, 'Email is required.');
    if (emailErr) {
      this.error = emailErr;
      return;
    }
    const email = this.email.trim();
    if (!this.password) {
      this.error = 'Password is required.';
      return;
    }
    this.loading = true;
    this.auth.login(email, this.password).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/documents']);
      },
      error: () => {
        this.loading = false;
        this.error = 'Invalid email or password.';
      },
    });
  }
}
