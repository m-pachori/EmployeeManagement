import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
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
      <button (click)="load()" [disabled]="isLoading">{{ isLoading ? 'Loading...' : 'Search' }}</button>
    </div>
    <p class="error" *ngIf="errorMessage">{{ errorMessage }}</p>
    <table>
      <thead>
        <tr><th>Code</th><th>Name</th><th>Email</th><th>Department</th><th>Status</th><th></th></tr>
      </thead>
      <tbody>
        <tr *ngIf="isLoading">
          <td colspan="6">Loading employees...</td>
        </tr>
        <tr *ngIf="!isLoading && items.length === 0">
          <td colspan="6">No employees found.</td>
        </tr>
        <tr *ngFor="let row of items">
          <td>{{ row.employeeCode }}</td>
          <td>{{ row.firstName }} {{ row.lastName }}</td>
          <td>{{ row.email }}</td>
          <td>{{ row.department }}</td>
          <td>{{ row.status }}</td>
          <td><a [routerLink]="['/employees', row.id, 'edit']">Edit</a></td>
        </tr>
      </tbody>
    </table>
  `,
  styles: `
    .head { display: flex; justify-content: space-between; align-items: center; gap: 0.5rem; }
    .filters { display: flex; gap: 0.5rem; margin: 0.75rem 0; }
    input { border: 1px solid #c6d3e0; border-radius: 0.35rem; padding: 0.45rem; min-width: 260px; }
    button { border: 0; background: #1f5e96; color: #fff; padding: 0.45rem 0.7rem; border-radius: 0.35rem; cursor: pointer; margin-right: 0.3rem; }
    table { width: 100%; border-collapse: collapse; background: #fff; border: 1px solid #d9e2ef; }
    th, td { border-bottom: 1px solid #e7edf5; text-align: left; padding: 0.5rem; }
    .error { color: #c12828; margin: 0.2rem 0 0.75rem; }
  `
})
export class EmployeesComponent implements OnInit {
  search = '';
  items: any[] = [];
  isLoading = false;
  errorMessage = '';

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    this.load();
  }

  load() {
    this.isLoading = true;
    this.errorMessage = '';

    this.api
      .get<any>('employees', { search: this.search, page: 1, pageSize: 25, sortBy: 'createdDate', sortDirection: 'desc' })
      .subscribe({
        next: (value) => {
          const payload = value as any;
          const rows = payload?.items ?? payload?.Items ?? payload?.data ?? payload?.Data ?? (Array.isArray(payload) ? payload : []);

          this.items = (rows as any[]).map((row) => ({
            id: row.id ?? row.Id,
            employeeCode: row.employeeCode ?? row.EmployeeCode,
            firstName: row.firstName ?? row.FirstName,
            lastName: row.lastName ?? row.LastName,
            email: row.email ?? row.Email,
            department: row.department ?? row.Department,
            status: row.status ?? row.Status
          }));

          this.isLoading = false;
        },
        error: (error) => {
          this.isLoading = false;
          this.errorMessage = error?.error?.title ?? error?.error?.message ?? 'Failed to load employees.';
        }
      });
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
