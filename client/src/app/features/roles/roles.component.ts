import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { resolveFieldError } from '../../shared/validation/field-error';
import { extractErrorMessage } from '../../shared/http/extract-error-message';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h2>Roles & Permissions</h2>
    <p class="error" *ngIf="loadErrorMessage">{{ loadErrorMessage }}</p>

    <form [formGroup]="form" (ngSubmit)="createRole()" class="inline">
      <label>
        <input formControlName="name" placeholder="Role name" />
        <span class="field-error" *ngIf="fieldError('name', 'Role name') as message">{{ message }}</span>
      </label>
      <label>
        <input formControlName="description" placeholder="Description" />
      </label>
      <button type="submit" [disabled]="isSaving">{{ isSaving ? 'Saving...' : 'Add Role' }}</button>
    </form>
    <p class="error" *ngIf="saveErrorMessage">{{ saveErrorMessage }}</p>

    <table [formGroup]="permissionsForm">
      <thead><tr><th>Id</th><th>Name</th><th>Permission Count</th><th>Assign Permission Ids</th><th></th></tr></thead>
      <tbody>
        <tr *ngIf="isLoading">
          <td colspan="5">Loading roles...</td>
        </tr>
        <tr *ngIf="!isLoading && roles.length === 0">
          <td colspan="5">No roles found.</td>
        </tr>
        <tr *ngFor="let row of roles">
          <td>{{ row.id }}</td>
          <td>{{ row.name }}</td>
          <td>{{ row.permissionCount }}</td>
          <td><input [formControlName]="row.id" placeholder="1,2,3" /></td>
          <td><button (click)="assignPermissions(row.id)">Save</button></td>
        </tr>
      </tbody>
    </table>
  `,
  styles: `
    .inline { display: grid; grid-template-columns: 180px 1fr auto; gap: 0.45rem; margin-bottom: 0.75rem; align-items: start; }
    label { display: grid; gap: 0.25rem; }
    input { border: 1px solid #c6d3e0; border-radius: 0.35rem; padding: 0.45rem; }
    button { border: 0; background: #1f5e96; color: #fff; padding: 0.45rem 0.65rem; border-radius: 0.35rem; height: fit-content; }
    .field-error { color: #c12828; font-size: 0.78rem; }
    .error { color: #c12828; font-size: 0.82rem; margin: 0.2rem 0 0.5rem; }
    table { width: 100%; border-collapse: collapse; background: #fff; border: 1px solid #d9e2ef; }
    th, td { border-bottom: 1px solid #e7edf5; text-align: left; padding: 0.5rem; }
  `
})
export class RolesComponent implements OnInit {
  readonly form;
  readonly permissionsForm: FormGroup;

  roles: any[] = [];
  isLoading = false;
  isSaving = false;
  loadErrorMessage = '';
  saveErrorMessage = '';

  constructor(
    private readonly api: ApiService,
    private readonly fb: FormBuilder,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      name: ['', Validators.required],
      description: ['']
    });
    this.permissionsForm = this.fb.group({});
  }

  fieldError(controlName: string, label: string): string {
    return resolveFieldError(this.form.get(controlName), label);
  }

  ngOnInit(): void {
    this.loadRoles();
  }

  loadRoles() {
    this.isLoading = true;
    this.loadErrorMessage = '';

    this.api.get<any>('roles').subscribe({
      next: (value) => {
        try {
          const payload = value as any;
          const rawRows = payload?.items ?? payload?.Items ?? payload?.data ?? payload?.Data ?? payload;
          const rows = Array.isArray(rawRows) ? rawRows : rawRows ? [rawRows] : [];

          this.roles = rows.map((row) => ({
            id: row.id ?? row.Id,
            name: row.name ?? row.Name,
            description: row.description ?? row.Description,
            permissionCount: row.permissionCount ?? row.PermissionCount ?? 0,
            userCount: row.userCount ?? row.UserCount ?? 0
          }));

          for (const role of this.roles) {
            const controlName = String(role.id);
            if (!this.permissionsForm.contains(controlName)) {
              this.permissionsForm.addControl(controlName, this.fb.control(''));
            }
          }
        } catch {
          this.roles = [];
          this.loadErrorMessage = 'Failed to parse roles response.';
        }

        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.isLoading = false;
        this.loadErrorMessage = extractErrorMessage(error, 'Failed to load roles.');
        this.cdr.markForCheck();
      }
    });
  }

  createRole() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.saveErrorMessage = '';

    this.api.post('roles', this.form.getRawValue()).subscribe({
      next: () => {
        this.isSaving = false;
        this.form.reset({ name: '', description: '' });
        this.loadRoles();
      },
      error: (error) => {
        this.isSaving = false;
        this.saveErrorMessage = extractErrorMessage(error, 'Failed to create role.');
        this.cdr.markForCheck();
      }
    });
  }

  assignPermissions(roleId: number) {
    const rawValue = (this.permissionsForm.get(String(roleId))?.value as string) ?? '';
    const ids = rawValue
      .split(',')
      .map((v) => Number(v.trim()))
      .filter((v) => Number.isFinite(v) && v > 0);

    this.saveErrorMessage = '';

    this.api.put(`roles/${roleId}/permissions`, { permissionIds: ids }).subscribe({
      next: () => this.loadRoles(),
      error: (error) => {
        this.saveErrorMessage = extractErrorMessage(error, 'Failed to assign permissions.');
        this.cdr.markForCheck();
      }
    });
  }
}
