import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, timer } from 'rxjs';
import { retry, catchError, map, timeout, finalize } from 'rxjs/operators';
import { LoadingService } from '../services/loading.service';
import { ErrorHandlerService } from '../services/error-handler.service';

export interface ApiGatewayConfig {
  baseUrl: string;
  version: string;
  timeout: number;
  retryAttempts: number;
  retryDelay: number;
  retryBackoff: number;
}

export interface RequestOptions {
  headers?: HttpHeaders | { [header: string]: string | string[] };
  params?: HttpParams | { [param: string]: string | number | boolean | ReadonlyArray<string | number | boolean> };
  timeout?: number;
  retry?: boolean;
  retryAttempts?: number;
  skipLoading?: boolean;
  skipErrorHandling?: boolean;
  operationName?: string;
}

export interface CircuitBreakerState {
  failures: number;
  lastFailureTime: Date | null;
  state: 'CLOSED' | 'OPEN' | 'HALF_OPEN';
}

@Injectable({
  providedIn: 'root'
})
export class ApiGatewayService {
  private readonly http = inject(HttpClient);
  private readonly loading = inject(LoadingService);
  private readonly errorHandler = inject(ErrorHandlerService);

  // Yapılandırma
  private readonly config: ApiGatewayConfig = {
    baseUrl: 'http://localhost:5000', // TODO: environment'tan al
    version: 'v1',
    timeout: 30000,
    retryAttempts: 3,
    retryDelay: 1000,
    retryBackoff: 2
  };

  // Mikroservis yönlendirmeleri
  private readonly services = {
    identity: '/identity-service',
    user: '/user-service',
    notification: '/notification-service',
    speedReading: '/speed-reading-service',
    reporting: '/reporting-service'
  };

  // Circuit Breaker durumları
  private readonly circuitBreakers = new Map<string, CircuitBreakerState>();

  // Rate limiting
  private readonly requestCounts = new Map<string, { count: number; resetTime: Date }>();
  private readonly rateLimit = { requests: 100, windowMs: 60000 }; // 100 request/dakika

  /**
   * GET isteği gönderir
   */
  public get<T>(
    service: keyof typeof this.services,
    endpoint: string,
    options?: RequestOptions
  ): Observable<T> {
    const url = this.buildUrl(service, endpoint);
    return this.executeRequest<T>('GET', url, null, options);
  }

  /**
   * POST isteği gönderir
   */
  public post<T>(
    service: keyof typeof this.services,
    endpoint: string,
    body: unknown,
    options?: RequestOptions
  ): Observable<T> {
    const url = this.buildUrl(service, endpoint);
    return this.executeRequest<T>('POST', url, body, options);
  }

  /**
   * PUT isteği gönderir
   */
  public put<T>(
    service: keyof typeof this.services,
    endpoint: string,
    body: unknown,
    options?: RequestOptions
  ): Observable<T> {
    const url = this.buildUrl(service, endpoint);
    return this.executeRequest<T>('PUT', url, body, options);
  }

  /**
   * PATCH isteği gönderir
   */
  public patch<T>(
    service: keyof typeof this.services,
    endpoint: string,
    body: unknown,
    options?: RequestOptions
  ): Observable<T> {
    const url = this.buildUrl(service, endpoint);
    return this.executeRequest<T>('PATCH', url, body, options);
  }

  /**
   * DELETE isteği gönderir
   */
  public delete<T>(
    service: keyof typeof this.services,
    endpoint: string,
    options?: RequestOptions
  ): Observable<T> {
    const url = this.buildUrl(service, endpoint);
    return this.executeRequest<T>('DELETE', url, null, options);
  }

  /**
   * Dosya upload işlemi
   */
  public upload<T>(
    service: keyof typeof this.services,
    endpoint: string,
    formData: FormData,
    options?: RequestOptions
  ): Observable<T> {
    const url = this.buildUrl(service, endpoint);
    const uploadOptions = {
      ...options,
      headers: new HttpHeaders() // Content-Type'ı browser otomatik ayarlasın
    };
    return this.executeRequest<T>('POST', url, formData, uploadOptions);
  }

  /**
   * Dosya download işlemi
   */
  public download(
    service: keyof typeof this.services,
    endpoint: string,
    options?: RequestOptions
  ): Observable<Blob> {
    const url = this.buildUrl(service, endpoint);
    const downloadOptions = {
      ...options,
      headers: {
        ...options?.headers,
        'Accept': 'application/octet-stream'
      }
    };

    return this.http.get(url, {
      headers: downloadOptions.headers,
      params: downloadOptions.params,
      responseType: 'blob'
    }).pipe(
      timeout(options?.timeout || this.config.timeout),
      catchError((error: HttpErrorResponse) => this.handleError(error, url))
    );
  }

  /**
   * Health check işlemi
   */
  public healthCheck(service?: keyof typeof this.services): Observable<unknown> {
    if (service) {
      return this.get(service, '/health', { skipLoading: true, timeout: 5000 });
    }

    // Tüm servislerin health check'i
    const healthChecks: Observable<unknown>[] = [];
    Object.keys(this.services).forEach(serviceName => {
      const healthCheck = this.get(serviceName as keyof typeof this.services, '/health', {
        skipLoading: true,
        timeout: 5000,
        skipErrorHandling: true
      }).pipe(
        map(result => ({ service: serviceName, status: 'healthy', result })),
        catchError(error => {
          console.warn(`Health check failed for ${serviceName}:`, error);
          return [{ service: serviceName, status: 'unhealthy', error: error.message }];
        })
      );
      healthChecks.push(healthCheck);
    });

    return new Observable(observer => {
      const results: unknown[] = [];
      let completed = 0;

      healthChecks.forEach(healthCheck => {
        healthCheck.subscribe({
          next: result => {
            results.push(result);
            completed++;
            if (completed === healthChecks.length) {
              observer.next(results);
              observer.complete();
            }
          },
          error: error => {
            completed++;
            results.push({ service: 'unknown', status: 'unhealthy', error: error.message });
            if (completed === healthChecks.length) {
              observer.next(results);
              observer.complete();
            }
          }
        });
      });
    });
  }

  /**
   * Batch request işlemi
   */
  public batch<T>(requests: Array<{
    service: keyof typeof this.services;
    endpoint: string;
    method: 'GET' | 'POST' | 'PUT' | 'DELETE';
    body?: unknown;
  }>): Observable<T[]> {
    return this.post<T[]>('identity', '/batch', { requests });
  }

  /**
   * URL oluşturur
   */
  private buildUrl(service: keyof typeof this.services, endpoint: string): string {
    const serviceUrl = this.services[service];
    const cleanEndpoint = endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
    return `${this.config.baseUrl}${serviceUrl}/api/${this.config.version}${cleanEndpoint}`;
  }

  /**
   * HTTP isteğini çalıştırır
   */
  private executeRequest<T>(
    method: string,
    url: string,
    body: unknown,
    options?: RequestOptions
  ): Observable<T> {
    // Rate limiting kontrolü
    if (!this.checkRateLimit(url)) {
      return throwError(() => new Error('Rate limit exceeded'));
    }

    // Circuit breaker kontrolü
    if (!this.checkCircuitBreaker(url)) {
      return throwError(() => new Error('Circuit breaker is open'));
    }

    // Loading başlat
    if (!options?.skipLoading) {
      const operationName = options?.operationName || `${method} ${url}`;
      this.loading.startOperation(operationName);
    }

    // HTTP seçenekleri
    const httpOptions: { headers?: HttpHeaders; params?: unknown; body?: unknown } = {
      headers: this.buildHeaders(options?.headers),
      params: options?.params
    };

    if (body && method !== 'GET' && method !== 'DELETE') {
      httpOptions.body = body;
    }

    // İstek gönder
    const request$ = this.http.request<T>(method, url, httpOptions).pipe(
      timeout(options?.timeout || this.config.timeout),
      options?.retry !== false ? this.addRetryLogic(options) : (source: Observable<T>) => source,
      map(response => {
        this.onRequestSuccess(url);
        return response;
      }),
      catchError((error: HttpErrorResponse) => {
        this.onRequestError(url, error);
        return this.handleError(error, url, options);
      }),
      finalize(() => {
        if (!options?.skipLoading) {
          const operationName = options?.operationName || `${method} ${url}`;
          this.loading.endOperation(operationName);
        }
      })
    );

    return request$;
  }

  /**
   * HTTP headers'ı oluşturur
   */
  private buildHeaders(customHeaders?: Record<string, string | string[]>): HttpHeaders {
    let headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      'X-API-Version': this.config.version,
      'X-Requested-With': 'XMLHttpRequest'
    });

    // JWT token ekle
    const token = localStorage.getItem('accessToken');
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }

    // Request ID ekle (tracing için)
    const requestId = this.generateRequestId();
    headers = headers.set('X-Request-ID', requestId);

    // Custom headers ekle
    if (customHeaders) {
      Object.keys(customHeaders).forEach(key => {
        headers = headers.set(key, customHeaders[key]);
      });
    }

    return headers;
  }

  /**
   * Retry logic ekler
   */
  private addRetryLogic<T>(options?: RequestOptions) {
    return (source: Observable<T>) => {
      const retryAttempts = options?.retryAttempts || this.config.retryAttempts;
      let attempt = 0;

      return source.pipe(
        retry({
          count: retryAttempts,
          delay: (error: HttpErrorResponse) => {
            attempt++;
            console.warn(`Request failed, retrying ${attempt}/${retryAttempts}:`, error);

            if (!this.errorHandler.shouldRetry(error, attempt, retryAttempts)) {
              return throwError(() => error);
            }

            const delay = this.config.retryDelay * Math.pow(this.config.retryBackoff, attempt - 1);
            return timer(delay);
          }
        })
      );
    };
  }

  /**
   * Hata işleme
   */
  private handleError(
    error: HttpErrorResponse,
    url: string,
    options?: RequestOptions
  ): Observable<never> {
    if (!options?.skipErrorHandling) {
      return this.errorHandler.handleHttpError(error, `API Gateway: ${url}`);
    }
    return throwError(() => error);
  }

  /**
   * Rate limiting kontrolü
   */
  private checkRateLimit(url: string): boolean {
    const now = new Date();
    const key = this.extractServiceFromUrl(url);
    const current = this.requestCounts.get(key);

    if (!current || now > current.resetTime) {
      this.requestCounts.set(key, {
        count: 1,
        resetTime: new Date(now.getTime() + this.rateLimit.windowMs)
      });
      return true;
    }

    if (current.count >= this.rateLimit.requests) {
      console.warn(`Rate limit exceeded for ${key}`);
      return false;
    }

    current.count++;
    return true;
  }

  /**
   * Circuit breaker kontrolü
   */
  private checkCircuitBreaker(url: string): boolean {
    const key = this.extractServiceFromUrl(url);
    const breaker = this.circuitBreakers.get(key);

    if (!breaker) {
      this.circuitBreakers.set(key, {
        failures: 0,
        lastFailureTime: null,
        state: 'CLOSED'
      });
      return true;
    }

    const now = new Date();

    switch (breaker.state) {
      case 'CLOSED':
        return true;

      case 'OPEN':
        // 30 saniye sonra yarı açık duruma geç
        if (breaker.lastFailureTime &&
            now.getTime() - breaker.lastFailureTime.getTime() > 30000) {
          breaker.state = 'HALF_OPEN';
          return true;
        }
        return false;

      case 'HALF_OPEN':
        return true;

      default:
        return true;
    }
  }

  /**
   * İstek başarılı olduğunda
   */
  private onRequestSuccess(url: string): void {
    const key = this.extractServiceFromUrl(url);
    const breaker = this.circuitBreakers.get(key);

    if (breaker) {
      breaker.failures = 0;
      breaker.state = 'CLOSED';
      breaker.lastFailureTime = null;
    }
  }

  /**
   * İstek başarısız olduğunda
   */
  private onRequestError(url: string, _error: HttpErrorResponse): void {
    const key = this.extractServiceFromUrl(url);
    const breaker = this.circuitBreakers.get(key) || {
      failures: 0,
      lastFailureTime: null,
      state: 'CLOSED' as const
    };

    breaker.failures++;
    breaker.lastFailureTime = new Date();

    // 5 başarısız denemeden sonra circuit breaker'ı aç
    if (breaker.failures >= 5) {
      breaker.state = 'OPEN';
      console.warn(`Circuit breaker opened for ${key} due to ${breaker.failures} failures`);
    }

    this.circuitBreakers.set(key, breaker);
  }

  /**
   * URL'den servis adını çıkarır
   */
  private extractServiceFromUrl(url: string): string {
    const serviceNames = Object.keys(this.services);
    for (const service of serviceNames) {
      if (url.includes(this.services[service as keyof typeof this.services])) {
        return service;
      }
    }
    return 'unknown';
  }

  /**
   * Benzersiz request ID üretir
   */
  private generateRequestId(): string {
    return `req_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Circuit breaker durumlarını döner
   */
  public getCircuitBreakerStates(): Map<string, CircuitBreakerState> {
    return new Map(this.circuitBreakers);
  }

  /**
   * Rate limit durumlarını döner
   */
  public getRateLimitStates(): Map<string, { count: number; resetTime: Date }> {
    return new Map(this.requestCounts);
  }

  /**
   * İstatistikleri döner
   */
  public getStats(): {
    circuitBreakers: number;
    rateLimits: number;
    configuration: ApiGatewayConfig;
  } {
    return {
      circuitBreakers: this.circuitBreakers.size,
      rateLimits: this.requestCounts.size,
      configuration: { ...this.config }
    };
  }
}