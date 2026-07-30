import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { resolveFieldError } from '../../shared/validation/field-error';
import { extractErrorMessage } from '../../shared/http/extract-error-message';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h2>Settings</h2>
    <p class="error" *ngIf="loadErrorMessage">{{ loadErrorMessage }}</p>

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
      <button type="submit" [disabled]="isSaving">{{ isSaving ? 'Saving...' : 'Save Setting' }}</button>
    </form>
    <p class="error" *ngIf="saveErrorMessage">{{ saveErrorMessage }}</p>

    <table>
      <thead><tr><th>Category</th><th>Key</th><th>Value</th><th>Description</th></tr></thead>
      <tbody>
        <tr *ngIf="isLoading">
          <td colspan="4">Loading settings...</td>
        </tr>
        <tr *ngIf="!isLoading && items.length === 0">
          <td colspan="4">No settings found.</td>
        </tr>
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
    .error { color: #c12828; font-size: 0.82rem; margin: 0.2rem 0 0.5rem; }
    table { width: 100%; border-collapse: collapse; background: #fff; border: 1px solid #d9e2ef; }
    th, td { border-bottom: 1px solid #e7edf5; text-align: left; padding: 0.5rem; }
  `
})
export class SettingsComponent implements OnInit {
  readonly form;

  items: any[] = [];
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
    this.isLoading = true;
    this.loadErrorMessage = '';

    this.api.get<any>('settings').subscribe({
      next: (value) => {
        try {
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
        } catch {
          this.items = [];
          this.loadErrorMessage = 'Failed to parse settings response.';
        }

        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.isLoading = false;
        this.loadErrorMessage = extractErrorMessage(error, 'Failed to load settings.');
        this.cdr.markForCheck();
      }
    });
  }

  save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.saveErrorMessage = '';

    this.api.post('settings', this.form.getRawValue()).subscribe({
      next: () => {
        this.isSaving = false;
        this.form.reset({ category: '', key: '', value: '', description: '' });
        this.load();
      },
      error: (error) => {
        this.isSaving = false;
        this.saveErrorMessage = extractErrorMessage(error, 'Failed to save setting.');
        this.cdr.markForCheck();
      }
    });
  }
}
