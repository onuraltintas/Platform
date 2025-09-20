import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { map, catchError } from 'rxjs/operators';
import { of } from 'rxjs';

import { AuthService } from '../services/auth.service';
import { NotificationService } from '../../../shared/services/notification.service';

export const permissionGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const notificationService = inject(NotificationService);

  const requiredPermission = route.data?.['permission'];

  if (!requiredPermission) {
    // Eğer permission belirtilmemişse, erişime izin ver
    return true;
  }

  return authService.hasPermission(requiredPermission).pipe(
    map(hasPermission => {
      if (hasPermission) {
        return true;
      } else {
        notificationService.error(
          'Bu sayfaya erişim yetkiniz bulunmamaktadır.',
          'Erişim Reddedildi'
        );
        return router.createUrlTree(['/403']);
      }
    }),
    catchError(() => {
      notificationService.error(
        'İzin kontrolü sırasında bir hata oluştu.',
        'Hata'
      );
      return of(router.createUrlTree(['/403']));
    })
  );
};