import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h2>Reports</h2>
    <div class="controls">
      <select [(ngModel)]="report">
        <option value="employees">Employee Report</option>
        <option value="departments">Department Report</option>
        <option value="users">User Report</option>
        <option value="login-activity">Login Activity Report</option>
      </select>
      <select [(ngModel)]="format">
        <option value="csv">CSV</option>
        <option value="excel">Excel</option>
        <option value="pdf">PDF</option>
      </select>
      <button (click)="download()">Download</button>
    </div>
  `,
  styles: `
    .controls { display: flex; gap: 0.5rem; align-items: center; }
    select { border: 1px solid #c6d3e0; border-radius: 0.35rem; padding: 0.45rem; }
    button { border: 0; background: #1f5e96; color: #fff; padding: 0.45rem 0.65rem; border-radius: 0.35rem; }
  `
})
export class ReportsComponent {
  report = 'employees';
  format = 'csv';

  constructor(private readonly api: ApiService) {}

  download() {
    this.api.getFile(`reports/${this.report}`, { format: this.format }).subscribe((blob) => {
      const extension = this.format === 'excel' ? 'xls' : this.format;
      const fileName = `${this.report}_${new Date().toISOString().slice(0, 10)}.${extension}`;
      const url = window.URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = fileName;
      anchor.click();
      anchor.remove();
      window.URL.revokeObjectURL(url);
    });
  }
}
