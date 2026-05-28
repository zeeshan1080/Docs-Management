import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { EMAIL_PATTERN, emailValidationMessage } from '../../core/email-format';
import { subscribeRestoreSession } from '../../core/guest-session';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
})
export class ForgotPasswordComponent implements OnInit, OnDestroy {
  readonly emailPattern = EMAIL_PATTERN;
  email = '';
  error = '';
  message = '';
  loading = false;
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
    this.message = '';
    const emailErr = emailValidationMessage(this.email, 'Enter your email address.');
    if (emailErr) {
      this.error = emailErr;
      return;
    }
    const email = this.email.trim();
    this.loading = true;
    this.auth.forgotPassword(email).subscribe({
      next: (res) => {
        this.loading = false;
        this.message = res.message;
      },
      error: () => {
        this.loading = false;
        this.error = 'Something went wrong. Try again later.';
      },
    });
  }

  goLogin(): void {
    this.router.navigate(['/login']);
  }
}
