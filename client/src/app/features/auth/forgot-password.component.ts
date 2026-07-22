import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="wrap">
      <form [formGroup]="requestForm" (ngSubmit)="requestReset()" *ngIf="!resetToken">
        <h1>Forgot Password</h1>
        <p>Enter your username or email to receive a password reset token.</p>
        <label>
          Username or Email
          <input formControlName="userNameOrEmail" />
        </label>
        <button type="submit" [disabled]="isLoading">{{ isLoading ? 'Requesting...' : 'Request Reset Token' }}</button>
        <div class="error" *ngIf="errorMessage">{{ errorMessage }}</div>
        <a routerLink="/login">Back to login</a>
      </form>

      <form [formGroup]="resetForm" (ngSubmit)="resetPassword()" *ngIf="resetToken">
        <h1>Reset Password</h1>
        <p class="token-note">Reset token: <code>{{ resetToken }}</code></p>
        <label>
          Reset Token
          <input formControlName="resetToken" />
        </label>
        <label>
          New Password
          <input type="password" formControlName="newPassword" />
        </label>
        <button type="submit" [disabled]="isLoading">{{ isLoading ? 'Resetting...' : 'Reset Password' }}</button>
        <div class="error" *ngIf="errorMessage">{{ errorMessage }}</div>
        <div class="success" *ngIf="successMessage">{{ successMessage }}</div>
        <a routerLink="/login">Back to login</a>
      </form>
    </div>
  `,
  styles: `
    .wrap { min-height: 100vh; display: grid; place-items: center; background: radial-gradient(circle at 20% 0%, #4e6f94, #243447 40%, #12202f); }
    form { width: min(420px, 92vw); background: #fff; padding: 1.25rem; border-radius: 0.8rem; box-shadow: 0 10px 30px rgba(0,0,0,0.2); display: grid; gap: 0.8rem; }
    h1 { margin: 0; }
    p { margin: 0 0 0.25rem; color: #5f6d7a; }
    label { display: grid; gap: 0.35rem; font-size: 0.9rem; }
    input { border: 1px solid #c6d3e0; border-radius: 0.4rem; padding: 0.55rem; }
    button { border: 0; background: #1f5e96; color: #fff; padding: 0.6rem; border-radius: 0.4rem; cursor: pointer; }
    .error { color: #c12d2d; font-size: 0.85rem; }
    .success { color: #1a7a3d; font-size: 0.85rem; }
    .token-note { word-break: break-all; }
    a { font-size: 0.85rem; color: #1f5e96; }
  `
})
export class ForgotPasswordComponent {
  readonly requestForm;
  readonly resetForm;

  isLoading = false;
  errorMessage = '';
  successMessage = '';
  resetToken = '';

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.requestForm = this.fb.group({
      userNameOrEmail: ['', [Validators.required]]
    });
    this.resetForm = this.fb.group({
      resetToken: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.minLength(8)]]
    });
  }

  requestReset() {
    if (this.requestForm.invalid || this.isLoading) {
      this.requestForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const userNameOrEmail = this.requestForm.getRawValue().userNameOrEmail as string;

    this.authService.forgotPassword({ userNameOrEmail }).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.resetToken = response?.resetToken ?? '';
        this.resetForm.patchValue({ resetToken: this.resetToken });
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error?.error?.title ?? 'Unable to request a reset token.';
        this.cdr.markForCheck();
      }
    });
  }

  resetPassword() {
    if (this.resetForm.invalid || this.isLoading) {
      this.resetForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const userNameOrEmail = this.requestForm.getRawValue().userNameOrEmail as string;
    const { resetToken, newPassword } = this.resetForm.getRawValue();

    this.authService
      .resetPassword({ userNameOrEmail, resetToken: resetToken as string, newPassword: newPassword as string })
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.successMessage = 'Password reset successfully. Redirecting to login...';
          this.cdr.markForCheck();
          setTimeout(() => this.router.navigate(['/login']), 1500);
        },
        error: (error) => {
          this.isLoading = false;
          this.errorMessage = error?.error?.title ?? 'Unable to reset password.';
          this.cdr.markForCheck();
        }
      });
  }
}
