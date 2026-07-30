import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { permissionGuard } from './core/guards/permission.guard';
import { LoginComponent } from './features/auth/login.component';
import { ForgotPasswordComponent } from './features/auth/forgot-password.component';
import { ChangePasswordComponent } from './features/auth/change-password.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { EmployeesComponent } from './features/employees/employees.component';
import { EmployeeFormComponent } from './features/employees/employee-form.component';
import { DepartmentsComponent } from './features/departments/departments.component';
import { UsersComponent } from './features/users/users.component';
import { RolesComponent } from './features/roles/roles.component';
import { SettingsComponent } from './features/settings/settings.component';
import { AuditComponent } from './features/audit/audit.component';
import { ReportsComponent } from './features/reports/reports.component';
import { AppShellComponent } from './layout/app-shell.component';

export const routes: Routes = [
	{ path: 'login', component: LoginComponent },
	{ path: 'forgot-password', component: ForgotPasswordComponent },
	{
		path: '',
		component: AppShellComponent,
		canActivate: [authGuard],
		children: [
			{ path: '', pathMatch: 'full', redirectTo: 'dashboard' },
			{ path: 'dashboard', component: DashboardComponent, canActivate: [permissionGuard('Dashboard.Read')] },
			{ path: 'employees', component: EmployeesComponent, canActivate: [permissionGuard('Employees.Read')] },
			{ path: 'employees/new', component: EmployeeFormComponent, canActivate: [permissionGuard('Employees.Write')] },
			{ path: 'employees/:id/edit', component: EmployeeFormComponent, canActivate: [permissionGuard('Employees.Write')] },
			{ path: 'departments', component: DepartmentsComponent, canActivate: [permissionGuard('Departments.Read')] },
			{ path: 'users', component: UsersComponent, canActivate: [permissionGuard('Users.Read')] },
			{ path: 'roles', component: RolesComponent, canActivate: [permissionGuard('Roles.Read')] },
			{ path: 'settings', component: SettingsComponent, canActivate: [permissionGuard('Settings.Read')] },
			{ path: 'audit', component: AuditComponent, canActivate: [permissionGuard('Audit.Read')] },
			{ path: 'reports', component: ReportsComponent, canActivate: [permissionGuard('Reports.Read')] },
			{ path: 'change-password', component: ChangePasswordComponent }
		]
	},
	{ path: '**', redirectTo: '' }
];
