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
      <button type="submit">Save</button>
    </form>
  `,
  styles: `
    .grid { display: grid; gap: 0.65rem; max-width: 640px; }
    label { display: grid; gap: 0.3rem; }
    input, select { border: 1px solid #c6d3e0; border-radius: 0.35rem; padding: 0.45rem; }
    button { width: fit-content; border: 0; background: #1f5e96; color: #fff; padding: 0.5rem 0.8rem; border-radius: 0.35rem; }
  `
})
export class EmployeeFormComponent implements OnInit {
  readonly form;

  departments: Array<{ id: number; name: string }> = [];
  isEdit = false;
  private employeeId: number | null = null;

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
      dateOfJoining: ['', Validators.required],
      status: [1, Validators.required],
      departmentId: [0, Validators.required]
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

    if (this.employeeId) {
      this.api.get<any>(`employees/${this.employeeId}`).subscribe((value) => {
        this.form.patchValue({
          employeeCode: value.employeeCode,
          firstName: value.firstName,
          lastName: value.lastName,
          email: value.email,
          phoneNumber: value.phoneNumber,
          dateOfJoining: this.toDateInput(value.dateOfJoining),
          status: this.toStatusNumber(value.status),
          departmentId: value.departmentId
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

    const payload = this.form.getRawValue();
    const request = {
      ...payload,
      dateOfJoining: `${payload.dateOfJoining}T00:00:00Z`
    };

    if (this.employeeId) {
      this.api.put(`employees/${this.employeeId}`, request).subscribe(() => this.router.navigate(['/employees']));
      return;
    }

    this.api.post('employees', request).subscribe(() => this.router.navigate(['/employees']));
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
