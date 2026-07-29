import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { resolveFieldError } from '../../shared/validation/field-error';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="login-wrap">
      <form [formGroup]="form" (ngSubmit)="submit()">
        <h1>Sign in</h1>
        <p>Employee Management System</p>
        <label>
          Username or Email
          <input formControlName="userNameOrEmail" placeholder="admin" />
          <span class="field-error" *ngIf="fieldError('userNameOrEmail', 'Username or email') as message">{{ message }}</span>
        </label>
        <label>
          Password
          <input type="password" formControlName="password" placeholder="Admin@123" />
          <span class="field-error" *ngIf="fieldError('password', 'Password') as message">{{ message }}</span>
        </label>
        <button type="submit" [disabled]="isLoading">{{ isLoading ? 'Signing in...' : 'Login' }}</button>
        <div class="error" *ngIf="errorMessage">{{ errorMessage }}</div>
        <a routerLink="/forgot-password">Forgot password?</a>
      </form>
    </div>
  `,
  styles: `
    .login-wrap { min-height: 100vh; display: grid; place-items: center; background: radial-gradient(circle at 20% 0%, #4e6f94, #243447 40%, #12202f); }
    form { width: min(420px, 92vw); background: #fff; padding: 1.25rem; border-radius: 0.8rem; box-shadow: 0 10px 30px rgba(0,0,0,0.2); display: grid; gap: 0.8rem; }
    h1 { margin: 0; }
    p { margin: 0 0 0.25rem; color: #5f6d7a; }
    label { display: grid; gap: 0.35rem; font-size: 0.9rem; }
    input { border: 1px solid #c6d3e0; border-radius: 0.4rem; padding: 0.55rem; }
    button { border: 0; background: #1f5e96; color: #fff; padding: 0.6rem; border-radius: 0.4rem; cursor: pointer; }
    .error { color: #c12d2d; font-size: 0.85rem; }
    .field-error { color: #c12d2d; font-size: 0.78rem; }
    a { font-size: 0.85rem; color: #1f5e96; text-decoration: none; }
  `
})
export class LoginComponent {
  readonly form;

  isLoading = false;
  errorMessage = '';

  constructor(
    private readonly fb: FormBuilder,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      userNameOrEmail: ['', [Validators.required]],
      password: ['', [Validators.required]]
    });
  }

  fieldError(controlName: string, label: string): string {
    return resolveFieldError(this.form.get(controlName), label);
  }

  submit() {
    if (this.form.invalid || this.isLoading) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.login(this.form.getRawValue() as { userNameOrEmail: string; password: string }).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error?.error?.title ?? 'Login failed. Check credentials and API status.';
        this.cdr.markForCheck();
      }
    });
  }
}
