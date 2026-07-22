import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  template: `
    <h2>Roles & Permissions</h2>
    <form [formGroup]="form" (ngSubmit)="createRole()" class="inline">
      <input formControlName="name" placeholder="Role name" />
      <input formControlName="description" placeholder="Description" />
      <button type="submit">Add Role</button>
    </form>
    <table>
      <thead><tr><th>Id</th><th>Name</th><th>Permission Count</th><th>Assign Permission Ids</th><th></th></tr></thead>
      <tbody>
        <tr *ngFor="let row of roles">
          <td>{{ row.id }}</td>
          <td>{{ row.name }}</td>
          <td>{{ row.permissionCount }}</td>
          <td><input [(ngModel)]="permissionMap[row.id]" [ngModelOptions]="{standalone: true}" placeholder="1,2,3" /></td>
          <td><button (click)="assignPermissions(row.id)">Save</button></td>
        </tr>
      </tbody>
    </table>
  `,
  styles: `
    .inline { display: grid; grid-template-columns: 180px 1fr auto; gap: 0.45rem; margin-bottom: 0.75rem; }
    input { border: 1px solid #c6d3e0; border-radius: 0.35rem; padding: 0.45rem; }
    button { border: 0; background: #1f5e96; color: #fff; padding: 0.45rem 0.65rem; border-radius: 0.35rem; }
    table { width: 100%; border-collapse: collapse; background: #fff; border: 1px solid #d9e2ef; }
    th, td { border-bottom: 1px solid #e7edf5; text-align: left; padding: 0.5rem; }
  `
})
export class RolesComponent implements OnInit {
  readonly form;

  roles: any[] = [];
  permissionMap: Record<number, string> = {};

  constructor(
    private readonly api: ApiService,
    private readonly fb: FormBuilder
  ) {
    this.form = this.fb.group({
      name: ['', Validators.required],
      description: ['']
    });
  }

  ngOnInit(): void {
    this.loadRoles();
  }

  loadRoles() {
    this.api.get<any[]>('roles').subscribe((value) => {
      this.roles = value;
    });
  }

  createRole() {
    if (this.form.invalid) {
      return;
    }

    this.api.post('roles', this.form.getRawValue()).subscribe(() => {
      this.form.reset({ name: '', description: '' });
      this.loadRoles();
    });
  }

  assignPermissions(roleId: number) {
    const ids = (this.permissionMap[roleId] ?? '')
      .split(',')
      .map((v) => Number(v.trim()))
      .filter((v) => Number.isFinite(v) && v > 0);

    this.api.put(`roles/${roleId}/permissions`, { permissionIds: ids }).subscribe(() => this.loadRoles());
  }
}
