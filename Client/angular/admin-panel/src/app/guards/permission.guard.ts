import { inject } from '@angular/core';
import { CanActivateFn, ActivatedRouteSnapshot, Router, RouterStateSnapshot } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../services/auth.service';
import { AUTHORIZATION_POLICIES } from '../auth/authorization.policies';
import { AuthorizationPolicyService } from '../services/authorization-policy.service';

export const permissionGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot) => {
  const router = inject(Router);
  const toastr = inject(ToastrService);
  const auth = inject(AuthService);
  const policyService = inject(AuthorizationPolicyService);

  // Beklenen roller/izinler (route.data öncelik, yoksa merkezi policy)
  let requiredRoles: string[] = route.data?.['requiredRoles'] ?? [];
  let requiredPermissions: string[] = route.data?.['requiredPermissions'] ?? [];

  if (requiredRoles.length === 0 && requiredPermissions.length === 0) {
    const path = state.url;
    const all = policyService.getPolicies();
    const policy = all.find(p => p.match.test(path));
    if (policy) {
      requiredRoles = policy.requiredRoles ?? [];
      requiredPermissions = policy.requiredPermissions ?? [];
    }
  }

  // Kullanıcı bilgisini senkron anlık al
  const user = (auth as any).currentUser$ ? undefined : undefined; // Type hint avoidance
  // AuthService içinde subject'e doğrudan erişim olmadığı için storage üzerinden oku
  const userStr = sessionStorage.getItem('user') ?? localStorage.getItem('user');
  const currentUser = userStr ? JSON.parse(userStr) : null;

  const hasRole = (r: string) => (currentUser?.roles || []).includes(r);
  const hasPermission = (p: string) => (currentUser?.permissions || []).includes(p);

  let authorized = true;

  if (requiredRoles.length > 0) {
    authorized = requiredRoles.some(hasRole);
  }

  if (authorized && requiredPermissions.length > 0) {
    authorized = requiredPermissions.some(hasPermission);
  }

  if (!authorized) {
    toastr.warning('Bu sayfayı görüntülemek için yetkiniz bulunmamaktadır.', 'Yetki Gerekli');
    return router.createUrlTree(['/dashboard']);
  }

  return true;
};

