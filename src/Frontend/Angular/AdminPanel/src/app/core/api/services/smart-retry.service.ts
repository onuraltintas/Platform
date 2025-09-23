import { Injectable, inject } from '@angular/core';
import { HttpRequest, HttpErrorResponse, HttpEvent } from '@angular/common/http';
import { Observable, throwError, timer } from 'rxjs';
import { catchError, switchMap, tap, retryWhen } from 'rxjs/operators';
import { OptimizedTokenService } from '../../cache/services/optimized-token.service';
import { AuthService } from '../../auth/services/auth.service';

export interface RetryConfig {
  maxRetries: number;
  backoffMultiplier: number;
  baseDelay: number;
  maxDelay: number;
  retryCondition?: (error: HttpErrorResponse) => boolean;
  onRetry?: (attempt: number, error: HttpErrorResponse) => void;
}

export interface TokenRefreshState {
  isRefreshing: boolean;
  refreshPromise: Promise<any> | null;
  waitingRequests: Array<{
    request: HttpRequest<any>;
    resolve: (token: string) => void;
    reject: (error: any) => void;
  }>;
}

export interface RetryStats {
  totalAttempts: number;
  successfulRetries: number;
  failedRetries: number;
  tokenRefreshCount: number;
  averageRetryDelay: number;
  lastRefreshTime: number;
}

/**
 * Smart Retry Service with Intelligent Token Management
 * Handles automatic retries, token refresh, and request queuing
 */
@Injectable({
  providedIn: 'root'
})
export class SmartRetryService {
  private readonly tokenService = inject(OptimizedTokenService);
  private readonly authService = inject(AuthService);

  private tokenRefreshState: TokenRefreshState = {
    isRefreshing: false,
    refreshPromise: null,
    waitingRequests: []
  };

  private retryStats: RetryStats = {
    totalAttempts: 0,
    successfulRetries: 0,
    failedRetries: 0,
    tokenRefreshCount: 0,
    averageRetryDelay: 0,
    lastRefreshTime: 0
  };

  private readonly DEFAULT_RETRY_CONFIG: RetryConfig = {
    maxRetries: 3,
    backoffMultiplier: 2,
    baseDelay: 1000,
    maxDelay: 10000,
    retryCondition: (error) => this.shouldRetry(error),
    onRetry: (attempt, error) => console.log(`Retry attempt ${attempt} for ${error.status}`)
  };

  /**
   * Execute HTTP request with intelligent retry logic
   */
  executeWithRetry<T>(
    requestFn: () => Observable<HttpEvent<T>>,
    config: Partial<RetryConfig> = {}
  ): Observable<HttpEvent<T>> {
    const retryConfig = { ...this.DEFAULT_RETRY_CONFIG, ...config };

    return requestFn().pipe(
      retryWhen(errors => this.createRetryLogic(errors, retryConfig)),
      tap(() => this.retryStats.successfulRetries++),
      catchError(error => {
        this.retryStats.failedRetries++;
        return throwError(() => error);
      })
    );
  }

  /**
   * Handle 401 errors with automatic token refresh
   */
  handleAuthError<T>(
    originalRequest: HttpRequest<any>,
    next: (req: HttpRequest<any>) => Observable<HttpEvent<T>>
  ): Observable<HttpEvent<T>> {

    // If already refreshing, queue this request
    if (this.tokenRefreshState.isRefreshing) {
      return this.queueRequest(originalRequest, next);
    }

    // Start token refresh process
    return this.refreshTokenAndRetry(originalRequest, next);
  }

  /**
   * Refresh token and retry original request
   */
  private refreshTokenAndRetry<T>(
    originalRequest: HttpRequest<any>,
    next: (req: HttpRequest<any>) => Observable<HttpEvent<T>>
  ): Observable<HttpEvent<T>> {

    this.tokenRefreshState.isRefreshing = true;
    this.retryStats.tokenRefreshCount++;
    this.retryStats.lastRefreshTime = Date.now();

    // Create refresh promise
    this.tokenRefreshState.refreshPromise = this.authService.refreshToken().toPromise();

    return new Observable(observer => {
      this.tokenRefreshState.refreshPromise!
        .then(response => {
          // Token refresh successful
          const newToken = response.accessToken;

          // Process all waiting requests
          this.processWaitingRequests(newToken);

          // Retry original request with new token
          const retryRequest = originalRequest.clone({
            setHeaders: {
              Authorization: `Bearer ${newToken}`,
              'X-Request-Id': crypto.randomUUID()
            }
          });

          next(retryRequest).subscribe({
            next: (event) => observer.next(event),
            error: (error) => observer.error(error),
            complete: () => observer.complete()
          });
        })
        .catch(error => {
          // Token refresh failed
          console.error('Token refresh failed:', error);
          this.handleTokenRefreshFailure(error);
          observer.error(error);
        })
        .finally(() => {
          this.resetRefreshState();
        });
    });
  }

  /**
   * Queue request while token is being refreshed
   */
  private queueRequest<T>(
    request: HttpRequest<any>,
    next: (req: HttpRequest<any>) => Observable<HttpEvent<T>>
  ): Observable<HttpEvent<T>> {

    return new Observable(observer => {
      const queueItem = {
        request,
        resolve: (token: string) => {
          const retryRequest = request.clone({
            setHeaders: {
              Authorization: `Bearer ${token}`,
              'X-Request-Id': crypto.randomUUID()
            }
          });

          next(retryRequest).subscribe({
            next: (event) => observer.next(event),
            error: (error) => observer.error(error),
            complete: () => observer.complete()
          });
        },
        reject: (error: any) => observer.error(error)
      };

      this.tokenRefreshState.waitingRequests.push(queueItem);

      // Timeout for queued requests
      setTimeout(() => {
        const index = this.tokenRefreshState.waitingRequests.indexOf(queueItem);
        if (index !== -1) {
          this.tokenRefreshState.waitingRequests.splice(index, 1);
          observer.error(new Error('Request timeout while waiting for token refresh'));
        }
      }, 30000); // 30 second timeout
    });
  }

  /**
   * Process all waiting requests after successful token refresh
   */
  private processWaitingRequests(newToken: string): void {
    const requests = [...this.tokenRefreshState.waitingRequests];
    this.tokenRefreshState.waitingRequests = [];

    requests.forEach(item => {
      try {
        item.resolve(newToken);
      } catch (error) {
        item.reject(error);
      }
    });
  }

  /**
   * Handle token refresh failure
   */
  private handleTokenRefreshFailure(error: any): void {
    const requests = [...this.tokenRefreshState.waitingRequests];
    this.tokenRefreshState.waitingRequests = [];

    requests.forEach(item => {
      item.reject(error);
    });

    // Redirect to login or show appropriate error
    this.authService.logout();
  }

  /**
   * Reset token refresh state
   */
  private resetRefreshState(): void {
    this.tokenRefreshState.isRefreshing = false;
    this.tokenRefreshState.refreshPromise = null;
  }

  /**
   * Create intelligent retry logic
   */
  private createRetryLogic(
    errors: Observable<HttpErrorResponse>,
    config: RetryConfig
  ): Observable<any> {
    return errors.pipe(
      switchMap((error, index) => {
        this.retryStats.totalAttempts++;

        // Check if we should retry
        if (index >= config.maxRetries || !config.retryCondition!(error)) {
          return throwError(() => error);
        }

        // Calculate delay with exponential backoff and jitter
        const delay = this.calculateRetryDelay(index, config);
        this.updateAverageDelay(delay);

        // Call retry callback
        if (config.onRetry) {
          config.onRetry(index + 1, error);
        }

        // Return timer for delay
        return timer(delay);
      })
    );
  }

  /**
   * Calculate retry delay with exponential backoff and jitter
   */
  private calculateRetryDelay(attempt: number, config: RetryConfig): number {
    // Exponential backoff: baseDelay * (backoffMultiplier ^ attempt)
    const exponentialDelay = config.baseDelay * Math.pow(config.backoffMultiplier, attempt);

    // Add jitter (±25% random variation)
    const jitter = exponentialDelay * 0.25 * (Math.random() * 2 - 1);
    const delayWithJitter = exponentialDelay + jitter;

    // Ensure delay doesn't exceed maximum
    return Math.min(delayWithJitter, config.maxDelay);
  }

  /**
   * Determine if error should trigger a retry
   */
  private shouldRetry(error: HttpErrorResponse): boolean {
    // Don't retry on client errors (except 401, 408, 429)
    if (error.status >= 400 && error.status < 500) {
      return [401, 408, 429].includes(error.status);
    }

    // Retry on server errors (5xx)
    if (error.status >= 500) {
      return true;
    }

    // Retry on network errors
    if (error.status === 0) {
      return true;
    }

    return false;
  }

  /**
   * Update running average of retry delays
   */
  private updateAverageDelay(delay: number): void {
    const totalDelays = this.retryStats.averageRetryDelay * (this.retryStats.totalAttempts - 1) + delay;
    this.retryStats.averageRetryDelay = totalDelays / this.retryStats.totalAttempts;
  }

  /**
   * Check if specific error indicates token issues
   */
  isTokenError(error: HttpErrorResponse): boolean {
    if (error.status === 401) {
      return true;
    }

    // Check for specific token error messages
    const tokenErrorMessages = [
      'token expired',
      'invalid token',
      'token not found',
      'unauthorized'
    ];

    const errorMessage = error.error?.message?.toLowerCase() || '';
    return tokenErrorMessages.some(msg => errorMessage.includes(msg));
  }

  /**
   * Preemptively refresh token if close to expiry
   */
  preemptiveTokenRefresh(): void {
    if (this.tokenService.shouldRefreshToken() && !this.tokenRefreshState.isRefreshing) {
      console.log('Performing preemptive token refresh...');

      this.authService.refreshToken().subscribe({
        next: (_response) => {
          console.log('Preemptive token refresh successful');
        },
        error: (error) => {
          console.warn('Preemptive token refresh failed:', error);
        }
      });
    }
  }

  /**
   * Get retry statistics for monitoring
   */
  getRetryStats(): RetryStats {
    return { ...this.retryStats };
  }

  /**
   * Get current token refresh state
   */
  getTokenRefreshState(): Readonly<TokenRefreshState> {
    return {
      isRefreshing: this.tokenRefreshState.isRefreshing,
      refreshPromise: this.tokenRefreshState.refreshPromise,
      waitingRequests: [...this.tokenRefreshState.waitingRequests]
    };
  }

  /**
   * Reset statistics
   */
  resetStats(): void {
    this.retryStats = {
      totalAttempts: 0,
      successfulRetries: 0,
      failedRetries: 0,
      tokenRefreshCount: 0,
      averageRetryDelay: 0,
      lastRefreshTime: 0
    };
  }

  /**
   * Create retry configuration for specific scenarios
   */
  createRetryConfig(scenario: 'aggressive' | 'conservative' | 'critical'): RetryConfig {
    switch (scenario) {
      case 'aggressive':
        return {
          maxRetries: 5,
          backoffMultiplier: 1.5,
          baseDelay: 500,
          maxDelay: 5000,
          retryCondition: (error) => error.status !== 400 && error.status !== 403
        };

      case 'conservative':
        return {
          maxRetries: 2,
          backoffMultiplier: 3,
          baseDelay: 2000,
          maxDelay: 15000,
          retryCondition: (error) => error.status >= 500 || error.status === 0
        };

      case 'critical':
        return {
          maxRetries: 1,
          backoffMultiplier: 1,
          baseDelay: 100,
          maxDelay: 1000,
          retryCondition: (error) => error.status === 429 || error.status >= 500
        };

      default:
        return this.DEFAULT_RETRY_CONFIG;
    }
  }
}