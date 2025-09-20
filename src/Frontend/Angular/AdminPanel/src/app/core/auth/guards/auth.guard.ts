import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { map, catchError, of } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { TokenService } from '../services/token.service';

export const authGuard: CanActivateFn = (
  _route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
) => {
  const authService = inject(AuthService);
  const tokenService = inject(TokenService);
  const router = inject(Router);

  // Check if user is authenticated
  if (authService.isAuthenticated && tokenService.isTokenValid()) {
    return true;
  }

  // Check if refresh token is valid and try to refresh
  if (tokenService.isRefreshTokenValid()) {
    return authService.refreshToken().pipe(
      map(() => true),
      catchError(() => {
        // Store attempted URL for redirecting after login
        sessionStorage.setItem('redirectUrl', state.url);
        return of(router.createUrlTree(['/auth/login']));
      })
    );
  }

  // Store attempted URL for redirecting after login
  sessionStorage.setItem('redirectUrl', state.url);

  // Redirect to login page
  return router.createUrlTree(['/auth/login']);
};