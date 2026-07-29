import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { resolveFieldError } from '../../shared/validation/field-error';

@Component({
  selector: 'app-departments',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h2>Departments</h2>
    <form [formGroup]="form" (ngSubmit)="create()" class="inline">
      <label>
        <input placeholder="Name" formControlName="name" />
        <span class="field-error" *ngIf="fieldError('name', 'Name') as message">{{ message }}</span>
      </label>
      <label>
        <input placeholder="Code" formControlName="code" />
        <span class="field-error" *ngIf="fieldError('code', 'Code') as message">{{ message }}</span>
      </label>
      <label>
        <input placeholder="Description" formControlName="description" />
      </label>
      <button type="submit">Add</button>
    </form>
    <table>
      <thead><tr><th>Name</th><th>Code</th><th>Employees</th><th></th></tr></thead>
      <tbody>
        <tr *ngFor="let row of items">
          <td>{{ row.name }}</td>
          <td>{{ row.code }}</td>
          <td>{{ row.employeeCount }}</td>
          <td><button (click)="remove(row.id)">Delete</button></td>
        </tr>
      </tbody>
    </table>
  `,
  styles: `
    .inline { display: grid; grid-template-columns: 1fr 160px 1fr auto; gap: 0.45rem; margin-bottom: 0.75rem; align-items: start; }
    label { display: grid; gap: 0.25rem; }
    input { border: 1px solid #c6d3e0; border-radius: 0.35rem; padding: 0.45rem; }
    button { border: 0; background: #1f5e96; color: #fff; padding: 0.45rem 0.65rem; border-radius: 0.35rem; height: fit-content; }
    .field-error { color: #c12828; font-size: 0.78rem; }
    table { width: 100%; border-collapse: collapse; background: #fff; border: 1px solid #d9e2ef; }
    th, td { border-bottom: 1px solid #e7edf5; text-align: left; padding: 0.5rem; }
  `
})
export class DepartmentsComponent implements OnInit {
  readonly form;

  items: any[] = [];

  constructor(
    private readonly api: ApiService,
    private readonly fb: FormBuilder,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      name: ['', Validators.required],
      code: ['', Validators.required],
      description: ['']
    });
  }

  fieldError(controlName: string, label: string): string {
    return resolveFieldError(this.form.get(controlName), label);
  }

  ngOnInit(): void {
    this.load();
  }

  load() {
    this.api.get<any>('departments').subscribe((value) => {
      const payload = value as any;
      const rawRows = payload?.items ?? payload?.Items ?? payload?.data ?? payload?.Data ?? payload;
      const rows = Array.isArray(rawRows) ? rawRows : rawRows ? [rawRows] : [];

      this.items = rows.map((row) => ({
        id: row.id ?? row.Id,
        name: row.name ?? row.Name,
        code: row.code ?? row.Code,
        description: row.description ?? row.Description,
        isActive: row.isActive ?? row.IsActive,
        employeeCount: row.employeeCount ?? row.EmployeeCount ?? 0
      }));

      this.cdr.markForCheck();
    });
  }

  create() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.api.post('departments', { ...this.form.getRawValue(), isActive: true }).subscribe(() => {
      this.form.reset({ name: '', code: '', description: '' });
      this.load();
    });
  }

  remove(id: number) {
    this.api.delete(`departments/${id}`).subscribe(() => this.load());
  }
}
