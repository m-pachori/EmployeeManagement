import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-employee-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h2>{{ isEdit ? 'Edit Employee' : 'New Employee' }}</h2>
    <form [formGroup]="form" (ngSubmit)="submit()" class="grid">
      <label>Employee Code<input formControlName="employeeCode" /></label>
      <label>First Name<input formControlName="firstName" /></label>
      <label>Last Name<input formControlName="lastName" /></label>
      <label>Email<input formControlName="email" /></label>
      <label>Phone<input formControlName="phoneNumber" /></label>
      <label>Designation<input formControlName="designation" /></label>
      <label>Salary<input type="number" min="0" step="0.01" formControlName="salary" /></label>
      <label>Date Of Joining<input type="date" formControlName="dateOfJoining" /></label>
      <label>Status
        <select formControlName="status">
          <option value="1">Active</option>
          <option value="2">Inactive</option>
          <option value="3">OnLeave</option>
          <option value="4">Terminated</option>
        </select>
      </label>
      <label>Department
        <select formControlName="departmentId">
          <option [ngValue]="0">Select</option>
          <option *ngFor="let d of departments" [ngValue]="d.id">{{ d.name }}</option>
        </select>
      </label>
      <label>Manager
        <select formControlName="managerId">
          <option [ngValue]="null">None</option>
          <option *ngFor="let m of managers" [ngValue]="m.id" [disabled]="m.id === employeeId">{{ m.firstName }} {{ m.lastName }}</option>
        </select>
      </label>
      <button type="submit">Save</button>
      <div class="error" *ngIf="errorMessage">{{ errorMessage }}</div>
    </form>

    <div class="photo-section" *ngIf="isEdit">
      <h3>Photo</h3>
      <img *ngIf="photoUrl" [src]="photoUrl" alt="Employee photo" class="photo-preview" (error)="onPhotoLoadError()" />
      <input type="file" accept=".jpg,.jpeg,image/jpeg" (change)="onPhotoSelected($event)" />
      <small>JPG only, max 250 KB.</small>
      <button type="button" [disabled]="!selectedFile" (click)="uploadPhoto()">Upload Photo</button>
      <div class="success" *ngIf="photoMessage">{{ photoMessage }}</div>
    </div>
  `,
  styles: `
    .grid { display: grid; gap: 0.65rem; max-width: 640px; }
    label { display: grid; gap: 0.3rem; }
    input, select { border: 1px solid #c6d3e0; border-radius: 0.35rem; padding: 0.45rem; }
    button { width: fit-content; border: 0; background: #1f5e96; color: #fff; padding: 0.5rem 0.8rem; border-radius: 0.35rem; }
    .error { color: #c12828; }
    .success { color: #1a7a3d; }
    .photo-section { margin-top: 1.25rem; display: grid; gap: 0.5rem; max-width: 320px; }
    .photo-preview { width: 120px; height: 120px; object-fit: cover; border-radius: 0.5rem; border: 1px solid #c6d3e0; }
  `
})
export class EmployeeFormComponent implements OnInit {
  readonly form;

  departments: Array<{ id: number; name: string }> = [];
  managers: Array<{ id: number; firstName: string; lastName: string }> = [];
  isEdit = false;
  employeeId: number | null = null;
  errorMessage = '';
  photoUrl = '';
  photoMessage = '';
  selectedFile: File | null = null;

  constructor(
    private readonly fb: FormBuilder,
    private readonly api: ApiService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      employeeCode: ['', Validators.required],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', Validators.required],
      phoneNumber: [''],
      designation: [''],
      salary: [null],
      dateOfJoining: ['', Validators.required],
      status: [1, Validators.required],
      departmentId: [0, Validators.required],
      managerId: [null]
    });
  }

  ngOnInit(): void {
    const idValue = this.route.snapshot.paramMap.get('id');
    this.employeeId = idValue ? Number(idValue) : null;
    this.isEdit = this.employeeId !== null;

    this.api.get<Array<{ id: number; name: string }>>('departments').subscribe((value) => {
      this.departments = value;
      this.cdr.markForCheck();
    });

    this.api.get<any>('employees', { page: 1, pageSize: 100 }).subscribe((value) => {
      const rows = value?.items ?? [];
      this.managers = rows.map((row: any) => ({ id: row.id, firstName: row.firstName, lastName: row.lastName }));
      this.cdr.markForCheck();
    });

    if (this.employeeId) {
      this.api.get<any>(`employees/${this.employeeId}`).subscribe((value) => {
        this.photoUrl = value.photoUrl ?? '';
        this.form.patchValue({
          employeeCode: value.employeeCode,
          firstName: value.firstName,
          lastName: value.lastName,
          email: value.email,
          phoneNumber: value.phoneNumber,
          designation: value.designation,
          salary: value.salary,
          dateOfJoining: this.toDateInput(value.dateOfJoining),
          status: this.toStatusNumber(value.status),
          departmentId: value.departmentId,
          managerId: value.managerId
        });
        this.cdr.markForCheck();
      });
    }
  }

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage = '';
    const payload = this.form.getRawValue();
    const request = {
      ...payload,
      dateOfJoining: `${payload.dateOfJoining}T00:00:00Z`
    };

    if (this.employeeId) {
      this.api.put(`employees/${this.employeeId}`, request).subscribe({
        next: () => this.router.navigate(['/employees']),
        error: (error) => {
          this.errorMessage = error?.error?.title ?? error?.error?.message ?? 'Failed to update employee.';
          this.cdr.markForCheck();
        }
      });
      return;
    }

    this.api.post('employees', request).subscribe({
      next: () => this.router.navigate(['/employees']),
      error: (error) => {
        this.errorMessage = error?.error?.title ?? error?.error?.message ?? 'Failed to create employee.';
        this.cdr.markForCheck();
      }
    });
  }

  onPhotoSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.photoMessage = '';

    if (file) {
      const validationError = this.validatePhotoFile(file);
      if (validationError) {
        this.photoMessage = validationError;
        this.selectedFile = null;
        input.value = '';
        return;
      }
    }

    this.selectedFile = file;
  }

  uploadPhoto() {
    if (!this.selectedFile || !this.employeeId) {
      return;
    }

    const formData = new FormData();
    formData.append('file', this.selectedFile);

    this.api.postFile<any>(`employees/${this.employeeId}/photo`, formData).subscribe({
      next: (response) => {
        this.photoUrl = response.photoUrl;
        this.photoMessage = 'Photo uploaded successfully.';
        this.selectedFile = null;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.photoMessage = error?.error?.title ?? 'Failed to upload photo.';
        this.cdr.markForCheck();
      }
    });
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

  onPhotoLoadError() {
    this.photoMessage = 'Failed to load photo.';
    this.photoUrl = '';
    this.cdr.markForCheck();
  }

  private toDateInput(value: string): string {
    const date = new Date(value);
    return `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, '0')}-${String(date.getUTCDate()).padStart(2, '0')}`;
  }

  private toStatusNumber(value: string): number {
    switch (value) {
      case 'Inactive':
        return 2;
      case 'OnLeave':
        return 3;
      case 'Terminated':
        return 4;
      default:
        return 1;
    }
  }
}
