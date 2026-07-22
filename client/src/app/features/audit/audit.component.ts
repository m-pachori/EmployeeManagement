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
    <div class="pagination" *ngIf="totalPages > 1">
      <button type="button" (click)="goToPage(page - 1)" [disabled]="page <= 1">Previous</button>
      <span>Page {{ page }} of {{ totalPages }} ({{ totalCount }} total)</span>
      <button type="button" (click)="goToPage(page + 1)" [disabled]="page >= totalPages">Next</button>
    </div>
  `,
  styles: `
    table { width: 100%; border-collapse: collapse; background: #fff; border: 1px solid #d9e2ef; }
    th, td { border-bottom: 1px solid #e7edf5; text-align: left; padding: 0.5rem; }
    button { border: 0; background: #1f5e96; color: #fff; padding: 0.45rem 0.7rem; border-radius: 0.35rem; cursor: pointer; }
    button:disabled { background: #9bb3c9; cursor: not-allowed; }
    .pagination { display: flex; align-items: center; gap: 0.75rem; margin-top: 0.75rem; }
  `
})
export class AuditComponent implements OnInit {
  items: any[] = [];
  page = 1;
  pageSize = 50;
  totalCount = 0;
  totalPages = 0;

  constructor(
    private readonly api: ApiService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load() {
    this.api.get<any>('audit/logs', { page: this.page, pageSize: this.pageSize }).subscribe((value) => {
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

      this.totalCount = payload?.totalCount ?? this.items.length;
      this.totalPages = payload?.totalPages ?? 1;

      this.cdr.markForCheck();
    });
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages || page === this.page) {
      return;
    }

    this.page = page;
    this.load();
  }
}
