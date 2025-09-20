import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../../../shared/services/notification.service';
import { ApiError } from '../models/api.models';
import { environment } from '../../../../environments/environment';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const notificationService = inject(NotificationService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'Bir hata oluştu';
      let apiErrors: ApiError[] = [];

      if (error.error instanceof ErrorEvent) {
        // Client-side error
        errorMessage = `İstemci hatası: ${error.error.message}`;
      } else {
        // Server-side error
        switch (error.status) {
          case 400:
            errorMessage = 'Geçersiz istek';
            if (error.error?.errors && Array.isArray(error.error.errors)) {
              apiErrors = error.error.errors;
              errorMessage = apiErrors.map(e => e.message).join(', ');
            } else if (error.error?.message) {
              errorMessage = error.error.message;
            }
            break;

          case 401:
            errorMessage = 'Oturum süreniz dolmuş. Lütfen tekrar giriş yapın.';
            break;

          case 403:
            errorMessage = 'Bu işlem için yetkiniz bulunmamaktadır.';
            router.navigate(['/403']);
            break;

          case 404:
            errorMessage = 'Kayıt bulunamadı';
            break;

          case 409:
            errorMessage = 'Çakışma hatası. Bu kayıt zaten mevcut.';
            break;

          case 422:
            errorMessage = 'Doğrulama hatası';
            if (error.error?.errors && Array.isArray(error.error.errors)) {
              apiErrors = error.error.errors;
              errorMessage = apiErrors.map(e => e.message).join(', ');
            } else if (error.error?.message) {
              errorMessage = error.error.message;
            }
            break;

          case 429:
            errorMessage = 'Çok fazla istek gönderdiniz. Lütfen biraz bekleyin.';
            break;

          case 500:
            errorMessage = 'Sunucu hatası. Lütfen daha sonra tekrar deneyin.';
            break;

          case 502:
          case 503:
          case 504:
            errorMessage = 'Servis geçici olarak kullanılamıyor. Lütfen daha sonra tekrar deneyin.';
            break;

          default:
            errorMessage = error.error?.message || `Beklenmeyen hata: ${error.status}`;
        }
      }

      // Show notification (except for 401 which is handled by JWT interceptor)
      if (error.status !== 401) {
        notificationService.error(errorMessage);
      }

      // Log error details in development
      if (!environment.production) {
        console.error('HTTP Error:', {
          status: error.status,
          message: errorMessage,
          url: error.url,
          errors: apiErrors,
          fullError: error
        });
      }

      // Return error with enhanced information
      const enhancedError = {
        ...error,
        apiErrors,
        userMessage: errorMessage
      };

      return throwError(() => enhancedError);
    })
  );
};