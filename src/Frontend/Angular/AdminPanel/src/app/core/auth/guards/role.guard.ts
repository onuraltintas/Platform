import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot } from '@angular/router';
import { map } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../../../shared/services/notification.service';

export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const notificationService = inject(NotificationService);

  const requiredRole = route.data['role'] as string;
  const requiredRoles = route.data['roles'] as string[];

  // Check single role
  if (requiredRole) {
    return authService.hasRole(requiredRole).pipe(
      map(hasRole => {
        if (!hasRole) {
          notificationService.error('Bu sayfaya erişim için gerekli role sahip değilsiniz.');
          return router.createUrlTree(['/403']);
        }
        return true;
      })
    );
  }

  // Check multiple roles (any)
  if (requiredRoles && requiredRoles.length > 0) {
    const user = authService.currentUserValue;
    if (!user) {
      return router.createUrlTree(['/auth/login']);
    }

    const hasRole = user.roles.some(role =>
      requiredRoles.includes(role.name)
    );

    if (!hasRole) {
      notificationService.error('Bu sayfaya erişim için gerekli role sahip değilsiniz.');
      return router.createUrlTree(['/403']);
    }

    return true;
  }

  // No role required
  return true;
};