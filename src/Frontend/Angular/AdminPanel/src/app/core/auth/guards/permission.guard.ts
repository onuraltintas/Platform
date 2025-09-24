import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';

import { PermissionService } from '../../services/permission.service';
import { NotificationService } from '../../../shared/services/notification.service';

export const permissionGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const permissionService = inject(PermissionService);
  const router = inject(Router);
  const notificationService = inject(NotificationService);

  const requiredPermission = route.data?.['permission'] as string | undefined;
  const requiredPermissions = route.data?.['permissions'] as string[] | undefined;

  console.log('🛡️ Permission guard activated for route:', route.url.join('/'));
  console.log('🎯 Required permission:', requiredPermission);
  console.log('🎯 Required permissions:', requiredPermissions);

  // No permissions specified
  if (!requiredPermission && (!requiredPermissions || requiredPermissions.length === 0)) {
    console.log('✅ No permissions required - access granted');
    return true;
  }

  // Single permission case - Use synchronous check to prevent infinite loops
  if (requiredPermission) {
    const hasPermission = permissionService.canAccess(requiredPermission);

    console.log('✅ Single permission check result:', hasPermission);

    if (hasPermission) {
      return true;
    } else {
      console.log('❌ Permission denied, redirecting to 403');
      notificationService.error('Bu sayfaya erişim yetkiniz bulunmamaktadır.', 'Erişim Reddedildi');
      return router.createUrlTree(['/403']);
    }
  }

  // Multiple permissions (require all) - Use synchronous check to prevent infinite loops
  const hasAllPermissions = (requiredPermissions || []).every(p => permissionService.canAccess(p));

  console.log('✅ Multiple permission check result:', hasAllPermissions);

  if (hasAllPermissions) {
    return true;
  } else {
    console.log('❌ Permission denied, redirecting to 403');
    notificationService.error('Bu sayfaya erişim yetkiniz bulunmamaktadır.', 'Erişim Reddedildi');
    return router.createUrlTree(['/403']);
  }
};