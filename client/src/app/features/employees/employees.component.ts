import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-employees',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="head">
      <h2>Employees</h2>
      <div>
        <button routerLink="/employees/new">Add Employee</button>
        <button (click)="exportCsv()">Export CSV</button>
      </div>
    </div>
    <div class="filters">
      <input [(ngModel)]="search" placeholder="Search by code, name, email" />
      <button (click)="onSearch()" [disabled]="isLoading">{{ isLoading ? 'Loading...' : 'Search' }}</button>
    </div>
    <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>
    <table>
      <thead>
        <tr><th>Code</th><th>Name</th><th>Email</th><th>Designation</th><th>Department</th><th>Manager</th><th>Status</th><th></th></tr>
      </thead>
      <tbody>
        <tr *ngIf="isLoading">
          <td colspan="8">Loading employees...</td>
        </tr>
        <tr *ngIf="!isLoading && items.length === 0">
          <td colspan="8">No employees found.</td>
        </tr>
        <tr *ngFor="let row of items">
          <td>{{ row.employeeCode }}</td>
          <td>{{ row.firstName }} {{ row.lastName }}</td>
          <td>{{ row.email }}</td>
          <td>{{ row.designation || '-' }}</td>
          <td>{{ row.department }}</td>
          <td>{{ row.managerName || '-' }}</td>
          <td>{{ row.status }}</td>
          <td><a [routerLink]="['/employees', row.id, 'edit']">Edit</a></td>
        </tr>
      </tbody>
    </table>
    <div class="pagination" *ngIf="!isLoading && totalPages > 1">
      <button type="button" (click)="goToPage(page - 1)" [disabled]="page <= 1">Previous</button>
      <span>Page {{ page }} of {{ totalPages }} ({{ totalCount }} total)</span>
      <button type="button" (click)="goToPage(page + 1)" [disabled]="page >= totalPages">Next</button>
    </div>
  `,
  styles: `
    .head { display: flex; justify-content: space-between; align-items: center; gap: 0.5rem; }
    .filters { display: flex; gap: 0.5rem; margin: 0.75rem 0; }
    input { border: 1px solid #c6d3e0; border-radius: 0.35rem; padding: 0.45rem; min-width: 260px; }
    button { border: 0; background: #1f5e96; color: #fff; padding: 0.45rem 0.7rem; border-radius: 0.35rem; cursor: pointer; margin-right: 0.3rem; }
    button:disabled { background: #9bb3c9; cursor: not-allowed; }
    table { width: 100%; border-collapse: collapse; background: #fff; border: 1px solid #d9e2ef; }
    th, td { border-bottom: 1px solid #e7edf5; text-align: left; padding: 0.5rem; }
    .error { color: #c12828; margin: 0.2rem 0 0.75rem; }
    .pagination { display: flex; align-items: center; gap: 0.75rem; margin-top: 0.75rem; }
  `
})
export class EmployeesComponent implements OnInit {
  search = '';
  items: any[] = [];
  isLoading = false;
  errorMessage = '';
  page = 1;
  pageSize = 25;
  totalCount = 0;
  totalPages = 0;

  constructor(
    private readonly api: ApiService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.load();
  }

  onSearch() {
    this.page = 1;
    this.load();
  }

  load() {
    this.isLoading = true;
    this.errorMessage = '';

    this.api
      .get<any>('employees', {
        search: this.search,
        page: this.page,
        pageSize: this.pageSize,
        sortBy: 'createdDate',
        sortDirection: 'desc'
      })
      .subscribe({
        next: (value) => {
          try {
            const payload = value as any;
            const rawRows = payload?.items ?? payload?.Items ?? payload?.data ?? payload?.Data ?? payload;
            const rows = Array.isArray(rawRows) ? rawRows : rawRows ? [rawRows] : [];

            this.items = rows.map((row) => ({
              id: row.id ?? row.Id,
              employeeCode: row.employeeCode ?? row.EmployeeCode,
              firstName: row.firstName ?? row.FirstName,
              lastName: row.lastName ?? row.LastName,
              email: row.email ?? row.Email,
              designation: row.designation ?? row.Designation,
              department: row.department ?? row.Department,
              managerName: row.managerName ?? row.ManagerName,
              status: row.status ?? row.Status
            }));

            this.totalCount = payload?.totalCount ?? this.items.length;
            this.totalPages = payload?.totalPages ?? 1;
          } catch {
            this.items = [];
            this.errorMessage = 'Failed to parse employees response.';
          }

          this.isLoading = false;
          this.cdr.markForCheck();
        },
        error: (error) => {
          this.isLoading = false;
          this.errorMessage = error?.error?.title ?? error?.error?.message ?? 'Failed to load employees.';
          this.cdr.markForCheck();
        }
      });
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages || page === this.page) {
      return;
    }

    this.page = page;
    this.load();
  }

  exportCsv() {
    this.api.getFile('employees/export/csv').subscribe((blob) => this.download(blob, 'employees.csv'));
  }

  private download(blob: Blob, fileName: string) {
    const url = window.URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    anchor.remove();
    window.URL.revokeObjectURL(url);
  }
}
