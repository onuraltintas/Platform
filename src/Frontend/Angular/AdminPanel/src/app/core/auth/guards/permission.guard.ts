import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { map, catchError } from 'rxjs/operators';
import { of, forkJoin } from 'rxjs';

import { AuthService } from '../services/auth.service';
import { NotificationService } from '../../../shared/services/notification.service';

export const permissionGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const notificationService = inject(NotificationService);

  const requiredPermission = route.data?.['permission'] as string | undefined;
  const requiredPermissions = route.data?.['permissions'] as string[] | undefined;

  // No permissions specified
  if (!requiredPermission && (!requiredPermissions || requiredPermissions.length === 0)) {
    return true;
  }

  // Single permission case
  if (requiredPermission) {
    return authService.hasPermission(requiredPermission).pipe(
      map(hasPermission => hasPermission ? true : router.createUrlTree(['/403'])),
      catchError(() => of(router.createUrlTree(['/403'])))
    );
  }

  // Multiple permissions (require all)
  const checks$ = (requiredPermissions || []).map(p => authService.hasPermission(p));
  return forkJoin(checks$).pipe(
    map(results => {
      const ok = results.every(r => r);
      if (ok) return true;
      notificationService.error('Bu sayfaya erişim yetkiniz bulunmamaktadır.', 'Erişim Reddedildi');
      return router.createUrlTree(['/403']);
    }),
    catchError(() => {
      notificationService.error('İzin kontrolü sırasında bir hata oluştu.', 'Hata');
      return of(router.createUrlTree(['/403']));
    })
  );
};