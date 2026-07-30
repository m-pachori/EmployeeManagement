import { Injectable, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { ApiService } from './api.service';

type LoginResponse = {
  accessToken: string;
  refreshToken: string;
  userName: string;
  email: string;
  roles: string[];
  permissions: string[];
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly accessTokenKey = 'ems_access_token';
  private readonly refreshTokenKey = 'ems_refresh_token';
  private readonly userNameKey = 'ems_user_name';
  private readonly permissionsKey = 'ems_permissions';

  private readonly accessTokenSignal = signal<string | null>(localStorage.getItem(this.accessTokenKey));
  private readonly permissionsSignal = signal<string[]>(this.readStoredPermissions());
  readonly isAuthenticated = computed(() => !!this.accessTokenSignal());
  readonly userName = computed(() => localStorage.getItem(this.userNameKey) ?? 'User');
  readonly permissions = computed(() => this.permissionsSignal());

  constructor(
    private readonly api: ApiService,
    private readonly router: Router
  ) {}

  login(payload: { userNameOrEmail: string; password: string }) {
    return this.api.post<LoginResponse>('auth/login', payload).pipe(
      tap((response) => {
        localStorage.setItem(this.accessTokenKey, response.accessToken);
        localStorage.setItem(this.refreshTokenKey, response.refreshToken);
        localStorage.setItem(this.userNameKey, response.userName);
        localStorage.setItem(this.permissionsKey, JSON.stringify(response.permissions ?? []));
        this.accessTokenSignal.set(response.accessToken);
        this.permissionsSignal.set(response.permissions ?? []);
      })
    );
  }

  /** Returns true when the current user holds the given permission code (e.g. "Users.Read"). */
  hasPermission(permission: string): boolean {
    return this.permissionsSignal().includes(permission);
  }

  private readStoredPermissions(): string[] {
    try {
      const raw = localStorage.getItem(this.permissionsKey);
      return raw ? (JSON.parse(raw) as string[]) : [];
    } catch {
      return [];
    }
  }

  logout() {
    const refreshToken = localStorage.getItem(this.refreshTokenKey) ?? '';
    this.api.post('auth/logout', { refreshToken }).subscribe({
      next: () => this.clearAndNavigate(),
      error: () => this.clearAndNavigate()
    });
  }

  forgotPassword(payload: { userNameOrEmail: string }) {
    return this.api.post<{ message?: string }>('auth/forgot-password', payload);
  }

  resetPassword(payload: { userNameOrEmail: string; resetToken: string; newPassword: string }) {
    return this.api.post('auth/reset-password', payload);
  }

  changePassword(payload: { currentPassword: string; newPassword: string }) {
    return this.api.post('auth/change-password', payload);
  }

  getAccessToken(): string | null {
    return this.accessTokenSignal();
  }

  clearAndNavigate() {
    localStorage.removeItem(this.accessTokenKey);
    localStorage.removeItem(this.refreshTokenKey);
    localStorage.removeItem(this.userNameKey);
    localStorage.removeItem(this.permissionsKey);
    this.accessTokenSignal.set(null);
    this.permissionsSignal.set([]);
    this.router.navigate(['/login']);
  }
}
