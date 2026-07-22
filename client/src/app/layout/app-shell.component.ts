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
          <a routerLink="/dashboard" routerLinkActive="active">Dashboard</a>
          <a routerLink="/employees" routerLinkActive="active">Employees</a>
          <a routerLink="/departments" routerLinkActive="active">Departments</a>
          <a routerLink="/users" routerLinkActive="active">Users</a>
          <a routerLink="/roles" routerLinkActive="active">Roles</a>
          <a routerLink="/settings" routerLinkActive="active">Settings</a>
          <a routerLink="/audit" routerLinkActive="active">Audit</a>
          <a routerLink="/reports" routerLinkActive="active">Reports</a>
        </nav>
      </aside>
      <main>
        <header>
          <span>Welcome, {{ auth.userName() }}</span>
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
    header { display: flex; justify-content: space-between; align-items: center; padding: 0.8rem 1rem; background: #fff; border-bottom: 1px solid #d9e2ef; }
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
