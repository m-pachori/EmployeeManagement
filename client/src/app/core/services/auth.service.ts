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

  private readonly accessTokenSignal = signal<string | null>(localStorage.getItem(this.accessTokenKey));
  readonly isAuthenticated = computed(() => !!this.accessTokenSignal());
  readonly userName = computed(() => localStorage.getItem(this.userNameKey) ?? 'User');

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
        this.accessTokenSignal.set(response.accessToken);
      })
    );
  }

  logout() {
    const refreshToken = localStorage.getItem(this.refreshTokenKey) ?? '';
    this.api.post('auth/logout', { refreshToken }).subscribe({
      next: () => this.clearAndNavigate(),
      error: () => this.clearAndNavigate()
    });
  }

  forgotPassword(payload: { userNameOrEmail: string }) {
    return this.api.post<{ resetToken?: string; message?: string }>('auth/forgot-password', payload);
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
    this.accessTokenSignal.set(null);
    this.router.navigate(['/login']);
  }
}
