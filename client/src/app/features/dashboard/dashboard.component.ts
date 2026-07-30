import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { extractErrorMessage } from '../../shared/http/extract-error-message';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h2>Dashboard</h2>
    <p class="error" *ngIf="loadErrorMessage">{{ loadErrorMessage }}</p>
    <p *ngIf="isLoading">Loading dashboard...</p>

    <ng-container *ngIf="!isLoading && !loadErrorMessage">
      <div class="cards">
        <article><h3>Employees</h3><p>{{ summary?.employeeCount ?? 0 }}</p></article>
        <article><h3>Departments</h3><p>{{ summary?.departmentCount ?? 0 }}</p></article>
        <article><h3>Active Users</h3><p>{{ summary?.activeUserCount ?? 0 }}</p></article>
      </div>
      <section>
        <h3>Recent Activity</h3>
        <p *ngIf="(summary?.recentActivity ?? []).length === 0">No recent activity.</p>
        <ul>
          <li *ngFor="let row of summary?.recentActivity ?? []">{{ row.eventType }} - {{ row.entityName }} - {{ row.createdDate | date:'short' }}</li>
        </ul>
      </section>
    </ng-container>
  `,
  styles: `
    .cards { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 0.75rem; margin-bottom: 1rem; }
    article { background: #fff; border: 1px solid #d9e2ef; border-radius: 0.6rem; padding: 0.75rem; }
    h3 { margin: 0 0 0.4rem; }
    p { margin: 0; font-size: 1.4rem; font-weight: 600; }
    ul { margin: 0; padding-left: 1rem; }
    .error { color: #c12828; font-size: 0.82rem; margin: 0.2rem 0 0.5rem; }
    @media (max-width: 800px) { .cards { grid-template-columns: 1fr; } }
  `
})
export class DashboardComponent implements OnInit {
  summary: any;
  isLoading = false;
  loadErrorMessage = '';

  constructor(
    private readonly api: ApiService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.isLoading = true;
    this.loadErrorMessage = '';

    this.api.get<any>('dashboard/summary').subscribe({
      next: (value) => {
        this.summary = value;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.isLoading = false;
        this.loadErrorMessage = extractErrorMessage(error, 'Failed to load dashboard summary.');
        this.cdr.markForCheck();
      }
    });
  }
}
