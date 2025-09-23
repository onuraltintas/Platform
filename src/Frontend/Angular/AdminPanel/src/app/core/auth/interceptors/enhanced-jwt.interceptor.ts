import { HttpInterceptorFn, HttpEvent } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { TokenService } from '../services/token.service';

/**
 * Enhanced JWT Interceptor with Async Token Handling
 * Simplified version for reliable authentication
 */
export const enhancedJwtInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenService = inject(TokenService);
  const router = inject(Router);

  // Skip interceptor for auth endpoints except refresh
  const isAuthEndpoint = req.url.includes('/auth/') && !req.url.includes('/auth/refresh');
  if (isAuthEndpoint) {
    return next(req);
  }

  // Convert to async operation since TokenService is async
  return new Observable<HttpEvent<any>>(observer => {
    tokenService.getAccessToken().then(token => {
      // Attach token if available
      let modifiedReq = req;
      if (token) {
        modifiedReq = req.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`,
            'X-Request-Id': crypto.randomUUID(),
            'X-Client-Version': '1.0.0'
          }
        });
      }

      // Execute the request
      next(modifiedReq).subscribe({
        next: value => observer.next(value),
        error: error => {
          // Handle 401 unauthorized - redirect to login
          if (error.status === 401) {
            tokenService.clearTokens();
            router.navigate(['/auth/login']);
          }
          observer.error(error);
        },
        complete: () => observer.complete()
      });
    }).catch(error => {
      console.error('Token service error:', error);
      // Proceed without token on error
      next(req).subscribe({
        next: value => observer.next(value),
        error: error => observer.error(error),
        complete: () => observer.complete()
      });
    });
  });
};