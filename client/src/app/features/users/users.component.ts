import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h2>Users</h2>
    <p class="error" *ngIf="loadErrorMessage">{{ loadErrorMessage }}</p>

    <form [formGroup]="form" (ngSubmit)="create()" class="grid">
      <label>
        Username
        <input formControlName="userName" placeholder="Username" />
        <span class="error" *ngIf="hasError('userName', 'required')">Username is required.</span>
      </label>

      <label>
        Email
        <input formControlName="email" placeholder="Email" />
        <span class="error" *ngIf="hasError('email', 'required')">Email is required.</span>
        <span class="error" *ngIf="hasError('email', 'email')">Enter a valid email address.</span>
      </label>

      <label>
        First Name
        <input formControlName="firstName" placeholder="First Name" />
        <span class="error" *ngIf="hasError('firstName', 'required')">First name is required.</span>
      </label>

      <label>
        Last Name
        <input formControlName="lastName" placeholder="Last Name" />
        <span class="error" *ngIf="hasError('lastName', 'required')">Last name is required.</span>
      </label>

      <label>
        Password
        <input formControlName="password" placeholder="Password" type="password" />
        <span class="error" *ngIf="hasError('password', 'required')">Password is required.</span>
        <span class="error" *ngIf="hasError('password', 'pattern')">
          Password must be 8+ chars with uppercase, lowercase, number, and special character.
        </span>
      </label>

      <label>
        Role Ids
        <input formControlName="roleIdsCsv" placeholder="Role Ids e.g. 1,2" />
      </label>

      <div class="actions">
        <button type="submit" [disabled]="isSaving">{{ isSaving ? 'Saving...' : 'Create User' }}</button>
      </div>
    </form>

    <p class="error" *ngIf="saveErrorMessage">{{ saveErrorMessage }}</p>

    <table>
      <thead><tr><th>Username</th><th>Email</th><th>Status</th><th>Roles</th></tr></thead>
      <tbody>
        <tr *ngIf="isLoading">
          <td colspan="4">Loading users...</td>
        </tr>
        <tr *ngIf="!isLoading && items.length === 0">
          <td colspan="4">No users found.</td>
        </tr>
        <tr *ngFor="let row of items">
          <td>{{ row.userName }}</td>
          <td>{{ row.email }}</td>
          <td>{{ row.isActive ? 'Active' : 'Inactive' }}</td>
          <td>{{ row.roles.join(', ') }}</td>
        </tr>
      </tbody>
    </table>
  `,
  styles: `
    .grid { display: grid; grid-template-columns: repeat(3, minmax(0,1fr)); gap: 0.6rem; margin-bottom: 0.75rem; }
    label { display: grid; gap: 0.3rem; font-size: 0.9rem; }
    input { border: 1px solid #c6d3e0; border-radius: 0.35rem; padding: 0.45rem; }
    button { width: fit-content; border: 0; background: #1f5e96; color: #fff; padding: 0.45rem 0.65rem; border-radius: 0.35rem; }
    table { width: 100%; border-collapse: collapse; background: #fff; border: 1px solid #d9e2ef; }
    th, td { border-bottom: 1px solid #e7edf5; text-align: left; padding: 0.5rem; }
    .error { color: #c12828; font-size: 0.82rem; margin: 0.2rem 0 0; }
    .actions { grid-column: 1 / -1; }
  `
})
export class UsersComponent implements OnInit {
  readonly form;

  items: any[] = [];
  isLoading = false;
  isSaving = false;
  loadErrorMessage = '';
  saveErrorMessage = '';

  constructor(
    private readonly api: ApiService,
    private readonly fb: FormBuilder
  ) {
    this.form = this.fb.group({
      userName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      password: ['', [Validators.required, Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$/)]],
      roleIdsCsv: ['']
    });
  }

  ngOnInit(): void {
    this.load();
  }

  load() {
    this.isLoading = true;
    this.loadErrorMessage = '';

    this.api.get<any>('users').subscribe({
      next: (value) => {
        const payload = value as any;
        const rows = payload?.items ?? payload?.Items ?? payload?.data ?? payload?.Data ?? (Array.isArray(payload) ? payload : []);

        this.items = (rows as any[]).map((row) => ({
          id: row.id ?? row.Id,
          userName: row.userName ?? row.UserName,
          email: row.email ?? row.Email,
          isActive: row.isActive ?? row.IsActive,
          roles: row.roles ?? row.Roles ?? []
        }));

        this.isLoading = false;
      },
      error: (error) => {
        this.isLoading = false;
        this.loadErrorMessage = this.extractErrorMessage(error, 'Failed to load users.');
      }
    });
  }

  create() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.saveErrorMessage = '';

    const value = this.form.getRawValue();
    const roleIds = (value.roleIdsCsv ?? '')
      .split(',')
      .map((v) => Number(v.trim()))
      .filter((v) => Number.isFinite(v) && v > 0);

    this.api
      .post('users', {
        userName: value.userName,
        email: value.email,
        firstName: value.firstName,
        lastName: value.lastName,
        password: value.password,
        isActive: true,
        roleIds
      })
      .subscribe({
        next: () => {
          this.isSaving = false;
          this.form.reset({ userName: '', email: '', firstName: '', lastName: '', password: '', roleIdsCsv: '' });
          this.load();
        },
        error: (error) => {
          this.isSaving = false;
          this.saveErrorMessage = this.extractErrorMessage(error, 'Failed to create user.');
        }
      });
  }

  hasError(controlName: string, errorName: string): boolean {
    const control = this.form.get(controlName);
    if (!control) {
      return false;
    }

    return control.touched && control.hasError(errorName);
  }

  private extractErrorMessage(error: any, fallback: string): string {
    const apiError = error?.error;

    if (apiError?.errors && typeof apiError.errors === 'object') {
      const firstEntry = Object.values(apiError.errors)[0] as string[] | undefined;
      if (Array.isArray(firstEntry) && firstEntry.length > 0) {
        return firstEntry[0];
      }
    }

    return apiError?.title ?? apiError?.message ?? fallback;
  }
}
