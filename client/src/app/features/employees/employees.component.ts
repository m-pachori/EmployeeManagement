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
        <tr><th>Code</th><th>Name</th><th>Photo</th><th>Email</th><th>Designation</th><th>Department</th><th>Manager</th><th>Status</th><th>Actions</th></tr>
      </thead>
      <tbody>
        <tr *ngIf="isLoading">
          <td colspan="9">Loading employees...</td>
        </tr>
        <tr *ngIf="!isLoading && items.length === 0">
          <td colspan="9">No employees found.</td>
        </tr>
        <tr *ngFor="let row of items">
          <td>{{ row.employeeCode }}</td>
          <td>{{ row.firstName }} {{ row.lastName }}</td>
          <td>
            <img *ngIf="row.photoUrl" [src]="row.photoUrl" alt="Employee photo" class="thumb" (error)="onPhotoLoadError(row)" />
            <span *ngIf="!row.photoUrl">-</span>
          </td>
          <td>{{ row.email }}</td>
          <td>{{ row.designation || '-' }}</td>
          <td>{{ row.department }}</td>
          <td>{{ row.managerName || '-' }}</td>
          <td>{{ row.status }}</td>
          <td>
            <div class="row-actions">
              <a [routerLink]="['/employees', row.id, 'edit']">Edit</a>
              <button type="button" (click)="openPhotoPicker(row.id)">Upload Photo</button>
              <input
                [id]="'employee-photo-' + row.id"
                type="file"
                accept=".jpg,.jpeg,image/jpeg"
                class="hidden-input"
                (change)="onPhotoSelected(row, $event)" />
              <div class="row-message" *ngIf="uploadMessages[row.id]">{{ uploadMessages[row.id] }}</div>
            </div>
          </td>
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
    .thumb { width: 44px; height: 44px; border-radius: 0.35rem; object-fit: cover; border: 1px solid #d9e2ef; }
    .hidden-input { display: none; }
    .row-actions { display: flex; flex-direction: column; align-items: flex-start; gap: 0.35rem; }
    .row-actions button { margin-right: 0; }
    .row-message { font-size: 0.85rem; color: #1a7a3d; }
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
  uploadMessages: Record<number, string> = {};

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
              photoUrl: row.photoUrl ?? row.PhotoUrl,
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

  openPhotoPicker(employeeId: number) {
    const input = document.getElementById(`employee-photo-${employeeId}`) as HTMLInputElement | null;
    input?.click();
  }

  onPhotoSelected(row: any, event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file || !row?.id) {
      return;
    }

    const validationError = this.validatePhotoFile(file);
    if (validationError) {
      this.uploadMessages[row.id] = validationError;
      input.value = '';
      this.cdr.markForCheck();
      return;
    }

    const formData = new FormData();
    formData.append('file', file);

    this.uploadMessages[row.id] = '';
    this.api.postFile<any>(`employees/${row.id}/photo`, formData).subscribe({
      next: (response) => {
        row.photoUrl = response.photoUrl;
        this.uploadMessages[row.id] = 'Photo uploaded successfully.';
        input.value = '';
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.uploadMessages[row.id] = error?.error?.title ?? error?.error?.message ?? 'Failed to upload photo.';
        input.value = '';
        this.cdr.markForCheck();
      }
    });
  }

  onPhotoLoadError(row: any) {
    this.uploadMessages[row.id] = 'Failed to load photo.';
    row.photoUrl = '';
    this.cdr.markForCheck();
  }

  private validatePhotoFile(file: File): string {
    const maxSizeInBytes = 250 * 1024;
    const allowedTypes = ['image/jpeg'];
    const isJpgExtension = /\.jpe?g$/i.test(file.name);

    if (!allowedTypes.includes(file.type) || !isJpgExtension) {
      return 'Only JPG photo files are allowed.';
    }

    if (file.size > maxSizeInBytes) {
      return 'Photo file size must not exceed 250 KB.';
    }

    return '';
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
