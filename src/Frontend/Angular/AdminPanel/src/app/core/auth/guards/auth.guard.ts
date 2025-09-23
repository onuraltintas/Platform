import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { Observable, map, catchError, of } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { TokenService } from '../services/token.service';

export const authGuard: CanActivateFn = (
  _route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
) => {
  const authService = inject(AuthService);
  const tokenService = inject(TokenService);
  const router = inject(Router);

  // Return observable for async token validation
  return new Observable<boolean | UrlTree>(observer => {
    const checkAuthentication = async () => {
      try {
        // Check if user is authenticated with async token validation
        const isAuthenticated = await authService.getAuthenticationStatus();

        if (isAuthenticated) {
          observer.next(true);
          observer.complete();
          return;
        }

        // Check if refresh token is valid and try to refresh
        const refreshTokenValid = await tokenService.isRefreshTokenValid();
        if (refreshTokenValid) {
          authService.refreshToken().pipe(
            map(() => {
              observer.next(true);
              observer.complete();
              return true;
            }),
            catchError(() => {
              // Store attempted URL for redirecting after login
              sessionStorage.setItem('redirectUrl', state.url);
              observer.next(router.createUrlTree(['/auth/login']));
              observer.complete();
              return of(false);
            })
          ).subscribe();
        } else {
          // Store attempted URL for redirecting after login
          sessionStorage.setItem('redirectUrl', state.url);
          observer.next(router.createUrlTree(['/auth/login']));
          observer.complete();
        }
      } catch (error) {
        console.error('Auth guard error:', error);
        sessionStorage.setItem('redirectUrl', state.url);
        observer.next(router.createUrlTree(['/auth/login']));
        observer.complete();
      }
    };

    checkAuthentication();
  });
};