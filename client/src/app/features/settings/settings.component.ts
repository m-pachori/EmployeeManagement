import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { resolveFieldError } from '../../shared/validation/field-error';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h2>Settings</h2>
    <form [formGroup]="form" (ngSubmit)="save()" class="grid">
      <label>
        <input formControlName="category" placeholder="Category e.g. SMTP" />
        <span class="field-error" *ngIf="fieldError('category', 'Category') as message">{{ message }}</span>
      </label>
      <label>
        <input formControlName="key" placeholder="Key e.g. Host" />
        <span class="field-error" *ngIf="fieldError('key', 'Key') as message">{{ message }}</span>
      </label>
      <label>
        <input formControlName="value" placeholder="Value" />
      </label>
      <label>
        <input formControlName="description" placeholder="Description" />
      </label>
      <button type="submit">Save Setting</button>
    </form>
    <table>
      <thead><tr><th>Category</th><th>Key</th><th>Value</th><th>Description</th></tr></thead>
      <tbody>
        <tr *ngFor="let row of items">
          <td>{{ row.category }}</td>
          <td>{{ row.key }}</td>
          <td>{{ row.value }}</td>
          <td>{{ row.description }}</td>
        </tr>
      </tbody>
    </table>
  `,
  styles: `
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0,1fr)); gap: 0.45rem; margin-bottom: 0.75rem; align-items: start; }
    label { display: grid; gap: 0.25rem; }
    input { border: 1px solid #c6d3e0; border-radius: 0.35rem; padding: 0.45rem; }
    button { width: fit-content; border: 0; background: #1f5e96; color: #fff; padding: 0.45rem 0.65rem; border-radius: 0.35rem; }
    .field-error { color: #c12828; font-size: 0.78rem; }
    table { width: 100%; border-collapse: collapse; background: #fff; border: 1px solid #d9e2ef; }
    th, td { border-bottom: 1px solid #e7edf5; text-align: left; padding: 0.5rem; }
  `
})
export class SettingsComponent implements OnInit {
  readonly form;

  items: any[] = [];

  constructor(
    private readonly api: ApiService,
    private readonly fb: FormBuilder,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      category: ['', Validators.required],
      key: ['', Validators.required],
      value: [''],
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
    this.api.get<any>('settings').subscribe((value) => {
      const payload = value as any;
      const rawRows = payload?.items ?? payload?.Items ?? payload?.data ?? payload?.Data ?? payload;
      const rows = Array.isArray(rawRows) ? rawRows : rawRows ? [rawRows] : [];

      this.items = rows.map((row) => ({
        id: row.id ?? row.Id,
        category: row.category ?? row.Category,
        key: row.key ?? row.Key,
        value: row.value ?? row.Value,
        description: row.description ?? row.Description,
        updatedDate: row.updatedDate ?? row.UpdatedDate
      }));

      this.cdr.markForCheck();
    });
  }

  save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.api.post('settings', this.form.getRawValue()).subscribe(() => {
      this.form.reset({ category: '', key: '', value: '', description: '' });
      this.load();
    });
  }
}
