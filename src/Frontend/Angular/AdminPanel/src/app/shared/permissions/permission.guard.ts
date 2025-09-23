import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PermissionsService } from './permissions.service';

export const permissionGuard: CanActivateFn = (route) => {
  const perms = inject(PermissionsService);
  const router = inject(Router);

  const required = (route.data?.['requiredPermissions'] as string[] | undefined) ?? [];
  if (required.length === 0 || perms.hasAll(required)) {
    return true;
  }

  // Lacking permissions -> redirect to home
  router.navigate(['/admin']);
  return false;
};

