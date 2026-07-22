import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
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

  constructor(
    private readonly api: ApiService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.api.get<any>('audit/logs', { page: 1, pageSize: 50 }).subscribe((value) => {
      const payload = value as any;
      const rawRows = payload?.items ?? payload?.Items ?? payload?.data ?? payload?.Data ?? payload;
      const rows = Array.isArray(rawRows) ? rawRows : rawRows ? [rawRows] : [];

      this.items = rows.map((row) => ({
        id: row.id ?? row.Id,
        createdDate: row.createdDate ?? row.CreatedDate,
        eventType: row.eventType ?? row.EventType,
        entityName: row.entityName ?? row.EntityName,
        createdBy: row.createdBy ?? row.CreatedBy,
        details: row.details ?? row.Details
      }));

      this.cdr.markForCheck();
    });
  }
}
