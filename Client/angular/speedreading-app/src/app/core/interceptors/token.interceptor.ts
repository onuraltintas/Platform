import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpErrorResponse } from '@angular/common/http';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

@Injectable()
export class TokenInterceptor implements HttpInterceptor {

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler) {
    const token = this.authService.getAccessToken();
    
    let authReq = req;
    if (token) {
      authReq = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(authReq).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401 && !req.url.includes('/auth/')) {
          return this.handle401Error(req, next);
        }
        return throwError(() => error);
      })
    );
  }

  private handle401Error(req: HttpRequest<any>, next: HttpHandler) {
    return this.authService.refreshToken().pipe(
      switchMap(() => {
        const newToken = this.authService.getAccessToken();
        if (newToken) {
          const newReq = req.clone({
            setHeaders: {
              Authorization: `Bearer ${newToken}`
            }
          });
          return next.handle(newReq);
        }
        this.router.navigate(['/auth/login']);
        return throwError(() => new Error('Token refresh failed'));
      }),
      catchError(() => {
        this.authService.logout().subscribe();
        this.router.navigate(['/auth/login']);
        return throwError(() => new Error('Authentication failed'));
      })
    );
  }
}