import { inject } from '@angular/core';
import { CanActivateFn, Router, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ToastrService } from 'ngx-toastr';

export const authGuard: CanActivateFn = (route, state: RouterStateSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const toastr = inject(ToastrService);

  if (authService.isLoggedIn()) {
    return true;
  } else {
    // Check if user is navigating away from dashboard/protected routes via logout
    const isComingFromLogout = authService.isCurrentlyLoggingOut();
    const isDashboardUrl = state.url.includes('/dashboard');
    const isInitialNavigation = !router.getCurrentNavigation()?.previousNavigation;
    
    console.log('Auth Guard - URL:', state.url, 'isLoggingOut:', isComingFromLogout, 'isDashboard:', isDashboardUrl, 'isInitialNav:', isInitialNavigation);
    
    // Don't show warning if:
    // 1. Coming to root path
    // 2. Currently logging out
    // 3. Coming from dashboard during logout process
    // 4. First app load (initial navigation) - sessizce login'e yönlendir
    if (state.url !== '/' && !isComingFromLogout && !(isDashboardUrl && localStorage.getItem('isLoggingOut')) && !isInitialNavigation) {
      console.log('Showing auth guard warning');
      toastr.warning('Bu sayfayı görüntülemek için giriş yapmalısınız.', 'Yetkisiz Erişim');
    } else {
      console.log('Skipping auth guard warning');
    }
    return router.createUrlTree(['/auth/login']);
  }
};
