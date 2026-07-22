import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h2>Settings</h2>
    <form [formGroup]="form" (ngSubmit)="save()" class="grid">
      <input formControlName="category" placeholder="Category e.g. SMTP" />
      <input formControlName="key" placeholder="Key e.g. Host" />
      <input formControlName="value" placeholder="Value" />
      <input formControlName="description" placeholder="Description" />
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
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0,1fr)); gap: 0.45rem; margin-bottom: 0.75rem; }
    input { border: 1px solid #c6d3e0; border-radius: 0.35rem; padding: 0.45rem; }
    button { width: fit-content; border: 0; background: #1f5e96; color: #fff; padding: 0.45rem 0.65rem; border-radius: 0.35rem; }
    table { width: 100%; border-collapse: collapse; background: #fff; border: 1px solid #d9e2ef; }
    th, td { border-bottom: 1px solid #e7edf5; text-align: left; padding: 0.5rem; }
  `
})
export class SettingsComponent implements OnInit {
  readonly form;

  items: any[] = [];

  constructor(
    private readonly api: ApiService,
    private readonly fb: FormBuilder
  ) {
    this.form = this.fb.group({
      category: ['', Validators.required],
      key: ['', Validators.required],
      value: [''],
      description: ['']
    });
  }

  ngOnInit(): void {
    this.load();
  }

  load() {
    this.api.get<any[]>('settings').subscribe((value) => {
      this.items = value;
    });
  }

  save() {
    if (this.form.invalid) {
      return;
    }

    this.api.post('settings', this.form.getRawValue()).subscribe(() => {
      this.form.reset({ category: '', key: '', value: '', description: '' });
      this.load();
    });
  }
}
