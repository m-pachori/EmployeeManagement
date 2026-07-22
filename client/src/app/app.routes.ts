import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
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
			{ path: 'dashboard', component: DashboardComponent },
			{ path: 'employees', component: EmployeesComponent },
			{ path: 'employees/new', component: EmployeeFormComponent },
			{ path: 'employees/:id/edit', component: EmployeeFormComponent },
			{ path: 'departments', component: DepartmentsComponent },
			{ path: 'users', component: UsersComponent },
			{ path: 'roles', component: RolesComponent },
			{ path: 'settings', component: SettingsComponent },
			{ path: 'audit', component: AuditComponent },
			{ path: 'reports', component: ReportsComponent },
			{ path: 'change-password', component: ChangePasswordComponent }
		]
	},
	{ path: '**', redirectTo: '' }
];
