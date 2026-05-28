import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { isWellFormedEmail } from '../../core/email-format';
import { passwordStrengthMessage } from '../../core/password-policy';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
})
export class ResetPasswordComponent implements OnInit {
  email = '';
  token = '';
  password = '';
  confirm = '';
  error = '';
  loading = false;
  done = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly auth: AuthService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((params) => {
      this.email = (params.get('email') ?? '').trim();
      this.token = params.get('token') ?? '';
      if (!this.email || !this.token) {
        this.error =
          'This reset link is invalid or incomplete. Request a new one from the forgot password page.';
      } else if (!isWellFormedEmail(this.email)) {
        this.error =
          'This reset link contains an invalid email. Request a new one from the forgot password page.';
      }
    });
  }

  submit(): void {
    this.error = '';
    const email = this.email.trim();
    if (!email || !this.token) return;
    if (!isWellFormedEmail(email)) {
      this.error = 'Enter a valid email address.';
      return;
    }
    const pwMsg = passwordStrengthMessage(this.password);
    if (pwMsg) {
      this.error = pwMsg;
      return;
    }
    if (!this.confirm) {
      this.error = 'Confirm password is required.';
      return;
    }
    if (this.password !== this.confirm) {
      this.error = 'Passwords do not match.';
      return;
    }
    this.loading = true;
    this.auth.resetPassword(email, this.token, this.password).subscribe({
      next: () => {
        this.loading = false;
        this.done = true;
      },
      error: (err) => {
        this.loading = false;
        const msg = err?.error?.error;
        this.error =
          typeof msg === 'string' ? msg : 'Could not reset password. The link may have expired.';
      },
    });
  }

  goLogin(): void {
    this.router.navigate(['/login']);
  }
}
