import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="shell">
      <aside class="sidebar">
        <h2>EmployeeMS</h2>
        <nav>
          <a *ngIf="auth.hasPermission('Dashboard.Read')" routerLink="/dashboard" routerLinkActive="active">Dashboard</a>
          <a *ngIf="auth.hasPermission('Employees.Read')" routerLink="/employees" routerLinkActive="active">Employees</a>
          <a *ngIf="auth.hasPermission('Departments.Read')" routerLink="/departments" routerLinkActive="active">Departments</a>
          <a *ngIf="auth.hasPermission('Users.Read')" routerLink="/users" routerLinkActive="active">Users</a>
          <a *ngIf="auth.hasPermission('Roles.Read')" routerLink="/roles" routerLinkActive="active">Roles</a>
          <a *ngIf="auth.hasPermission('Settings.Read')" routerLink="/settings" routerLinkActive="active">Settings</a>
          <a *ngIf="auth.hasPermission('Audit.Read')" routerLink="/audit" routerLinkActive="active">Audit</a>
          <a *ngIf="auth.hasPermission('Reports.Read')" routerLink="/reports" routerLinkActive="active">Reports</a>
        </nav>
      </aside>
      <main>
        <header>
          <span>Welcome, {{ auth.userName() }}</span>
          <a routerLink="/change-password" class="change-password-link">Change Password</a>
          <button type="button" (click)="auth.logout()">Logout</button>
        </header>
        <section class="content">
          <router-outlet></router-outlet>
        </section>
      </main>
    </div>
  `,
  styles: `
    .shell { display: grid; grid-template-columns: 240px 1fr; min-height: 100vh; }
    .sidebar { background: #11324d; color: #fff; padding: 1rem; }
    .sidebar h2 { margin: 0 0 1rem; font-size: 1.25rem; }
    .sidebar nav { display: grid; gap: 0.4rem; }
    .sidebar a { color: #dce7f5; text-decoration: none; padding: 0.45rem 0.5rem; border-radius: 0.4rem; }
    .sidebar a.active, .sidebar a:hover { background: #1f4f74; color: #fff; }
    main { display: grid; grid-template-rows: auto 1fr; background: #f4f7fb; }
    header { display: flex; justify-content: space-between; align-items: center; gap: 0.75rem; padding: 0.8rem 1rem; background: #fff; border-bottom: 1px solid #d9e2ef; }
    header span { margin-right: auto; }
    .change-password-link { color: #1f5e96; text-decoration: none; font-size: 0.9rem; }
    button { border: 0; background: #d64545; color: #fff; padding: 0.45rem 0.75rem; border-radius: 0.35rem; cursor: pointer; }
    .content { padding: 1rem; }
    @media (max-width: 900px) {
      .shell { grid-template-columns: 1fr; }
      .sidebar { padding-bottom: 0.5rem; }
      .sidebar nav { grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 0.25rem; }
      .sidebar a { font-size: 0.85rem; text-align: center; }
    }
  `
})
export class AppShellComponent {
  constructor(public readonly auth: AuthService) {}
}
