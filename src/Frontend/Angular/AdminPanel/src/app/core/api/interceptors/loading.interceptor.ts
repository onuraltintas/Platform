import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { LoadingService } from '../../../shared/services/loading.service';

// Endpoints that should not trigger loading indicator
const SILENT_ENDPOINTS = [
  '/auth/refresh',
  '/health',
  '/metrics',
  '/notifications/check'
];

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);

  // Check if request should trigger loading indicator
  const shouldShowLoading = !SILENT_ENDPOINTS.some(endpoint =>
    req.url.includes(endpoint)
  );

  // Skip loading for background requests
  const isBackgroundRequest = req.headers.has('X-Background-Request');

  if (shouldShowLoading && !isBackgroundRequest) {
    loadingService.show();
  }

  return next(req).pipe(
    finalize(() => {
      if (shouldShowLoading && !isBackgroundRequest) {
        loadingService.hide();
      }
    })
  );
};