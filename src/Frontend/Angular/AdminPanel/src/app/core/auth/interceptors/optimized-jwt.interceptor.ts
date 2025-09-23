import { HttpInterceptorFn, HttpErrorResponse, HttpEvent } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, throwError } from 'rxjs';
import { OptimizedTokenService } from '../../cache/services/optimized-token.service';
import { AuthService } from '../services/auth.service';

/**
 * Optimized JWT Interceptor with Memory-First Token Retrieval
 * Performance target: <1ms for token attachment
 */
export const optimizedJwtInterceptor: HttpInterceptorFn = (req, next) => {
  const optimizedTokenService = inject(OptimizedTokenService);
  const authService = inject(AuthService);
  const router = inject(Router);

  // Skip interceptor for auth endpoints except refresh
  const isAuthEndpoint = req.url.includes('/auth/') && !req.url.includes('/auth/refresh');
  const isRefreshEndpoint = req.url.includes('/auth/refresh');

  if (isAuthEndpoint) {
    return next(req);
  }

  // PERFORMANCE CRITICAL PATH: Synchronous token retrieval
  const startTime = performance.now();

  try {
    // STEP 1: Get token synchronously from memory cache (0-1ms)
    const token = isRefreshEndpoint
      ? optimizedTokenService.getRefreshToken()
      : optimizedTokenService.getAccessTokenSync();

    // STEP 2: Attach token if available (0-1ms)
    let modifiedReq = req;
    if (token) {
      modifiedReq = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
          'X-Request-Id': crypto.randomUUID()
        }
      });

      // STEP 3: Background refresh check (non-blocking)
      if (!isRefreshEndpoint && optimizedTokenService.shouldRefreshToken()) {
        // Trigger background refresh without blocking current request
        setTimeout(() => {
          authService.refreshToken().subscribe({
            next: () => console.log('Background token refresh successful'),
            error: (error) => console.warn('Background token refresh failed:', error)
          });
        }, 0);
      }
    }

    // Log performance if too slow
    const responseTime = performance.now() - startTime;
    if (responseTime > 2) { // Warning if >2ms
      console.warn(`JWT Interceptor slow: ${responseTime.toFixed(2)}ms`);
    }

    // STEP 4: Process request with error handling
    return next(modifiedReq).pipe(
      catchError((error: HttpErrorResponse) => {
        return handleAuthError(error, req, next, authService, router, optimizedTokenService);
      })
    );

  } catch (error) {
    console.error('JWT Interceptor error:', error);
    // Fallback: proceed without token
    return next(req).pipe(
      catchError((httpError: HttpErrorResponse) => {
        return handleAuthError(httpError, req, next, authService, router, optimizedTokenService);
      })
    );
  }
};

/**
 * Optimized error handling with smart retry logic
 */
function handleAuthError(
  error: HttpErrorResponse,
  originalReq: any,
  next: any,
  authService: AuthService,
  router: Router,
  tokenService: OptimizedTokenService
): Observable<HttpEvent<unknown>> {

  // Handle 401 Unauthorized
  if (error.status === 401) {
    const isRefreshEndpoint = originalReq.url.includes('/auth/refresh');

    if (!isRefreshEndpoint) {
      // Try refresh token approach
      const refreshToken = tokenService.getRefreshToken();

      if (refreshToken) {
        // Attempt token refresh
        return new Observable(observer => {
          authService.refreshToken().subscribe({
            next: (response) => {
              // Retry original request with new token
              const retryReq = originalReq.clone({
                setHeaders: {
                  Authorization: `Bearer ${response.accessToken}`,
                  'X-Request-Id': crypto.randomUUID()
                }
              });

              next(retryReq).subscribe({
                next: (retryResponse) => {
                  observer.next(retryResponse as HttpEvent<unknown>);
                  observer.complete();
                },
                error: (retryError) => {
                  // Refresh worked but retry failed - redirect to login
                  handleAuthFailure(authService, router);
                  observer.error(retryError);
                }
              });
            },
            error: () => {
              // Refresh failed - redirect to login
              handleAuthFailure(authService, router);
              observer.error(error);
            }
          });
        });
      } else {
        // No refresh token - redirect to login
        handleAuthFailure(authService, router);
      }
    } else {
      // Refresh endpoint failed - redirect to login
      handleAuthFailure(authService, router);
    }
  }

  // Handle other HTTP errors
  if (error.status >= 500) {
    console.error('Server error:', error.status, error.message);
  } else if (error.status >= 400) {
    console.warn('Client error:', error.status, error.message);
  }

  return throwError(() => error);
}

/**
 * Handle authentication failure with cleanup
 */
function handleAuthFailure(authService: AuthService, router: Router): void {
  // Clear tokens and redirect
  authService.logout();
  router.navigate(['/auth/login'], {
    queryParams: { returnUrl: router.url }
  });
}

/**
 * Performance monitoring decorator
 */
function measurePerformance<T extends (...args: any[]) => any>(
  target: any,
  propertyName: string,
  descriptor: TypedPropertyDescriptor<T>
): TypedPropertyDescriptor<T> | void {
  const method = descriptor.value!;

  descriptor.value = function (...args: any[]) {
    const start = performance.now();
    const result = method.apply(this, args);
    const end = performance.now();

    if (end - start > 1) { // Log if >1ms
      console.log(`${propertyName} took ${(end - start).toFixed(2)}ms`);
    }

    return result;
  } as T;
}