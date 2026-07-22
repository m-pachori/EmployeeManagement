import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h2>Change Password</h2>
    <form [formGroup]="form" (ngSubmit)="submit()" class="grid">
      <label>Current Password<input type="password" formControlName="currentPassword" /></label>
      <label>New Password<input type="password" formControlName="newPassword" /></label>
      <label>Confirm New Password<input type="password" formControlName="confirmNewPassword" /></label>
      <button type="submit" [disabled]="isLoading">{{ isLoading ? 'Saving...' : 'Change Password' }}</button>
      <div class="error" *ngIf="errorMessage">{{ errorMessage }}</div>
      <div class="success" *ngIf="successMessage">{{ successMessage }}</div>
    </form>
  `,
  styles: `
    .grid { display: grid; gap: 0.65rem; max-width: 420px; }
    label { display: grid; gap: 0.3rem; }
    input { border: 1px solid #c6d3e0; border-radius: 0.35rem; padding: 0.45rem; }
    button { width: fit-content; border: 0; background: #1f5e96; color: #fff; padding: 0.5rem 0.8rem; border-radius: 0.35rem; }
    .error { color: #c12828; }
    .success { color: #1a7a3d; }
  `
})
export class ChangePasswordComponent {
  readonly form;

  isLoading = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmNewPassword: ['', Validators.required]
    });
  }

  submit() {
    if (this.form.invalid || this.isLoading) {
      this.form.markAllAsTouched();
      return;
    }

    const { currentPassword, newPassword, confirmNewPassword } = this.form.getRawValue();
    this.errorMessage = '';
    this.successMessage = '';

    if (newPassword !== confirmNewPassword) {
      this.errorMessage = 'New password and confirmation do not match.';
      return;
    }

    this.isLoading = true;

    this.authService
      .changePassword({ currentPassword: currentPassword as string, newPassword: newPassword as string })
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.successMessage = 'Password changed successfully.';
          this.form.reset();
          this.cdr.markForCheck();
        },
        error: (error) => {
          this.isLoading = false;
          this.errorMessage = error?.error?.title ?? 'Unable to change password.';
          this.cdr.markForCheck();
        }
      });
  }
}
