import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { resolveFieldError } from '../../shared/validation/field-error';
import { PASSWORD_PATTERN, PASSWORD_POLICY_MESSAGE } from '../../shared/validation/password-policy';
import { passwordsMatchValidator } from '../../shared/validation/password-match.validator';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h2>Change Password</h2>
    <form [formGroup]="form" (ngSubmit)="submit()" class="grid">
      <label>Current Password<input type="password" formControlName="currentPassword" />
        <span class="field-error" *ngIf="fieldError('currentPassword', 'Current password') as message">{{ message }}</span>
      </label>
      <label>New Password<input type="password" formControlName="newPassword" />
        <span class="field-error" *ngIf="fieldError('newPassword', 'New password', passwordPolicyMessage) as message">{{ message }}</span>
      </label>
      <label>Confirm New Password<input type="password" formControlName="confirmNewPassword" />
        <span class="field-error" *ngIf="fieldError('confirmNewPassword', 'Password confirmation', 'Passwords do not match.') as message">{{ message }}</span>
      </label>
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
    .field-error { color: #c12828; font-size: 0.78rem; }
  `
})
export class ChangePasswordComponent {
  readonly form;
  readonly passwordPolicyMessage = PASSWORD_POLICY_MESSAGE;

  isLoading = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group(
      {
        currentPassword: ['', Validators.required],
        newPassword: ['', [Validators.required, Validators.pattern(PASSWORD_PATTERN)]],
        confirmNewPassword: ['', Validators.required]
      },
      { validators: passwordsMatchValidator('newPassword', 'confirmNewPassword') }
    );
  }

  fieldError(controlName: string, label: string, patternMessage?: string): string {
    return resolveFieldError(this.form.get(controlName), label, patternMessage);
  }

  submit() {
    if (this.form.invalid || this.isLoading) {
      this.form.markAllAsTouched();
      return;
    }

    const { currentPassword, newPassword } = this.form.getRawValue();
    this.errorMessage = '';
    this.successMessage = '';

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
