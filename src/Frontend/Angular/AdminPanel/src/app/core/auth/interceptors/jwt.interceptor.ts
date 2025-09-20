import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { TokenService } from '../services/token.service';
import { AuthService } from '../services/auth.service';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenService = inject(TokenService);
  const authService = inject(AuthService);
  const router = inject(Router);

  // Skip interceptor for auth endpoints except refresh
  const isAuthEndpoint = req.url.includes('/auth/') && !req.url.includes('/auth/refresh');
  const isRefreshEndpoint = req.url.includes('/auth/refresh');

  if (isAuthEndpoint) {
    return next(req);
  }

  // Get token
  const token = isRefreshEndpoint
    ? tokenService.getRefreshToken()
    : tokenService.getAccessToken();

  // Clone request with auth header if token exists
  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
        'X-Request-Id': crypto.randomUUID()
      }
    });
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Handle 401 Unauthorized
      if (error.status === 401 && !isRefreshEndpoint) {
        const refreshToken = tokenService.getRefreshToken();

        if (refreshToken && tokenService.isRefreshTokenValid()) {
          // Try to refresh the token
          return authService.refreshToken().pipe(
            switchMap((response) => {
              // Retry original request with new token
              const newReq = req.clone({
                setHeaders: {
                  Authorization: `Bearer ${response.accessToken}`,
                  'X-Request-Id': crypto.randomUUID()
                }
              });
              return next(newReq);
            }),
            catchError((refreshError) => {
              // Refresh failed, redirect to login
              authService.logout();
              router.navigate(['/auth/login']);
              return throwError(() => refreshError);
            })
          );
        } else {
          // No valid refresh token, redirect to login
          authService.logout();
          router.navigate(['/auth/login']);
        }
      }

      return throwError(() => error);
    })
  );
};