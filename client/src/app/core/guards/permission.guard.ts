import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Route guard factory that requires the authenticated user to hold a specific permission
 * (e.g. "Users.Read") in addition to being authenticated. Unauthenticated users are sent to
 * /login; authenticated users lacking the permission are redirected to /dashboard.
 */
export function permissionGuard(requiredPermission: string): CanActivateFn {
  return () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
      return router.createUrlTree(['/login']);
    }

    if (authService.hasPermission(requiredPermission)) {
      return true;
    }

    return router.createUrlTree(['/dashboard']);
  };
}
