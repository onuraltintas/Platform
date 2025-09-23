import { HttpInterceptorFn, HttpErrorResponse, HttpEvent } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, switchMap, throwError } from 'rxjs';
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

  // Handle async token retrieval
  return new Observable(observer => {
    const processRequest = async () => {
      try {
        // Get token asynchronously
        const token = isRefreshEndpoint
          ? await tokenService.getRefreshToken()
          : await tokenService.getAccessToken();

        // Clone request with auth header if token exists
        let modifiedReq = req;
        if (token) {
          modifiedReq = req.clone({
            setHeaders: {
              Authorization: `Bearer ${token}`,
              'X-Request-Id': crypto.randomUUID()
            }
          });
        }

        // Process the request
        next(modifiedReq).pipe(
          catchError((error: HttpErrorResponse) => {
            // Handle 401 Unauthorized
            if (error.status === 401 && !isRefreshEndpoint) {
              return new Observable(errorObserver => {
                const handleRefresh = async () => {
                  try {
                    const refreshToken = await tokenService.getRefreshToken();
                    const isRefreshValid = await tokenService.isRefreshTokenValid();

                    if (refreshToken && isRefreshValid) {
                      // Try to refresh the token
                      authService.refreshToken().pipe(
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
                      ).subscribe({
                        next: (response) => {
                          errorObserver.next(response as HttpEvent<unknown>);
                          errorObserver.complete();
                        },
                        error: (err) => errorObserver.error(err)
                      });
                    } else {
                      // No valid refresh token, redirect to login
                      authService.logout();
                      router.navigate(['/auth/login']);
                      errorObserver.error(error);
                    }
                  } catch (err) {
                    authService.logout();
                    router.navigate(['/auth/login']);
                    errorObserver.error(error);
                  }
                };
                handleRefresh();
              });
            }

            return throwError(() => error);
          })
        ).subscribe({
          next: (response) => {
            observer.next(response as HttpEvent<unknown>);
            observer.complete();
          },
          error: (error) => {
            observer.error(error);
          }
        });

      } catch (error) {
        console.error('JWT Interceptor error:', error);
        // Fallback: proceed without token
        next(req).subscribe({
          next: (response) => {
            observer.next(response as HttpEvent<unknown>);
            observer.complete();
          },
          error: (error) => {
            observer.error(error);
          }
        });
      }
    };

    processRequest();
  });
};