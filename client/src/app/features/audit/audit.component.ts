import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h2>Audit Logs</h2>
    <table>
      <thead><tr><th>Date</th><th>Event</th><th>Entity</th><th>User</th><th>Detail</th></tr></thead>
      <tbody>
        <tr *ngFor="let row of items">
          <td>{{ row.createdDate | date:'short' }}</td>
          <td>{{ row.eventType }}</td>
          <td>{{ row.entityName }}</td>
          <td>{{ row.createdBy }}</td>
          <td>{{ row.details }}</td>
        </tr>
      </tbody>
    </table>
  `,
  styles: `
    table { width: 100%; border-collapse: collapse; background: #fff; border: 1px solid #d9e2ef; }
    th, td { border-bottom: 1px solid #e7edf5; text-align: left; padding: 0.5rem; }
  `
})
export class AuditComponent implements OnInit {
  items: any[] = [];

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.api.get<any>('audit/logs', { page: 1, pageSize: 50 }).subscribe((value) => {
      this.items = value.items ?? [];
    });
  }
}
