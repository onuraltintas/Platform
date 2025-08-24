import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, of } from 'rxjs';
import { catchError, timeout, retry, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  message?: string;
}

export interface ApiError {
  status: number;
  message: string;
  details?: any;
}

@Injectable({
  providedIn: 'root'
})
export class HttpClientService {
  private readonly baseUrl = environment.apiUrl || 'https://localhost:7001';
  private readonly timeout = 30000; // 30 seconds
  private readonly retryAttempts = 2;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  /**
   * GET request with automatic error handling and fallback
   */
  get<T>(endpoint: string, params?: any, options?: { 
    skipAuth?: boolean; 
    fallback?: T;
    offline?: boolean;
  }): Observable<ApiResponse<T>> {
    
    // If offline mode is enabled, return fallback immediately
    if (options?.offline && options?.fallback) {
      return of({
        success: true,
        data: options.fallback,
        message: 'Offline mode - using cached data'
      });
    }

    const httpOptions = this.buildHttpOptions(options?.skipAuth);
    
    if (params) {
      httpOptions.params = this.buildHttpParams(params);
    }

    return this.http.get<T>(`${this.baseUrl}${endpoint}`, httpOptions).pipe(
      map(data => ({ success: true, data } as ApiResponse<T>)),
      catchError((error: HttpErrorResponse) => this.handleError<T>(error, options?.fallback))
    );
  }

  /**
   * POST request with automatic error handling
   */
  post<T>(endpoint: string, body: any, options?: { 
    skipAuth?: boolean; 
    fallback?: T;
    offline?: boolean;
  }): Observable<ApiResponse<T>> {
    
    // If offline mode, queue for later sync
    if (options?.offline) {
      this.queueOfflineRequest('POST', endpoint, body);
      return of({
        success: true,
        data: options?.fallback,
        message: 'Queued for sync when online'
      });
    }

    const httpOptions = this.buildHttpOptions(options?.skipAuth);

    return this.http.post<T>(`${this.baseUrl}${endpoint}`, body, httpOptions).pipe(
      map(data => ({ success: true, data } as ApiResponse<T>)),
      catchError((error: HttpErrorResponse) => this.handleError<T>(error, options?.fallback))
    );
  }

  /**
   * PUT request with automatic error handling
   */
  put<T>(endpoint: string, body: any, options?: { 
    skipAuth?: boolean; 
    fallback?: T;
    offline?: boolean;
  }): Observable<ApiResponse<T>> {
    
    if (options?.offline) {
      this.queueOfflineRequest('PUT', endpoint, body);
      return of({
        success: true,
        data: options?.fallback,
        message: 'Queued for sync when online'
      });
    }

    const httpOptions = this.buildHttpOptions(options?.skipAuth);

    return this.http.put<T>(`${this.baseUrl}${endpoint}`, body, httpOptions).pipe(
      map(data => ({ success: true, data } as ApiResponse<T>)),
      catchError((error: HttpErrorResponse) => this.handleError<T>(error, options?.fallback))
    );
  }

  /**
   * DELETE request with automatic error handling
   */
  delete<T>(endpoint: string, options?: { 
    skipAuth?: boolean; 
    fallback?: T;
    offline?: boolean;
  }): Observable<ApiResponse<T>> {
    
    if (options?.offline) {
      this.queueOfflineRequest('DELETE', endpoint, null);
      return of({
        success: true,
        data: options?.fallback,
        message: 'Queued for sync when online'
      });
    }

    const httpOptions = this.buildHttpOptions(options?.skipAuth);

    return this.http.delete<T>(`${this.baseUrl}${endpoint}`, httpOptions).pipe(
      map(data => ({ success: true, data } as ApiResponse<T>)),
      catchError((error: HttpErrorResponse) => this.handleError<T>(error, options?.fallback))
    );
  }

  /**
   * Check if backend is available
   */
  isBackendAvailable(): Observable<boolean> {
    return this.http.get(`${this.baseUrl}/sr-content/health`, { 
      headers: { 'Content-Type': 'application/json' }
    }).pipe(
      timeout(5000),
      retry(1),
      catchError(() => of(false)),
      // Map any successful response to true
      this.mapToBoolean()
    );
  }

  private buildHttpOptions(skipAuth?: boolean): any {
    let headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    });

    if (!skipAuth) {
      const token = this.authService.getToken();
      if (token) {
        headers = headers.set('Authorization', `Bearer ${token}`);
      }
    }

    return { headers };
  }

  private buildHttpParams(params: any): HttpParams {
    let httpParams = new HttpParams();
    
    Object.keys(params).forEach(key => {
      if (params[key] !== null && params[key] !== undefined) {
        httpParams = httpParams.set(key, params[key].toString());
      }
    });

    return httpParams;
  }

  private mapToApiResponse<T>() {
    return (source: Observable<T>) => 
      new Observable<ApiResponse<T>>(observer => {
        source.subscribe({
          next: (data: T) => {
            observer.next({
              success: true,
              data: data
            });
            observer.complete();
          },
          error: (error: any) => observer.error(error)
        });
      });
  }

  private mapToBoolean() {
    return (source: Observable<any>) => 
      source.pipe(
        catchError(() => of(false)),
        // Transform any response to boolean
        source => new Observable<boolean>(observer => {
          source.subscribe({
            next: () => {
              observer.next(true);
              observer.complete();
            },
            error: () => {
              observer.next(false);
              observer.complete();
            }
          });
        })
      );
  }

  private handleError<T>(error: HttpErrorResponse, fallback?: T): Observable<ApiResponse<T>> {
    console.error('HTTP Error:', error);

    const apiError: ApiError = {
      status: error.status || 0,
      message: error.message || 'Unknown error occurred',
      details: error.error
    };

    // If we have fallback data, use it
    if (fallback !== undefined) {
      console.warn('Using fallback data due to API error:', apiError);
      return of({
        success: true, // Mark as success but with fallback data
        data: fallback,
        message: `API unavailable - using cached data (${apiError.message})`
      });
    }

    // Return error response
    return of({
      success: false,
      error: apiError.message,
      message: this.getErrorMessage(error)
    });
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'Backend service unavailable. Working in offline mode.';
    }
    
    switch (error.status) {
      case 400:
        return 'Invalid request data';
      case 401:
        return 'Authentication required';
      case 403:
        return 'Access denied';
      case 404:
        return 'Resource not found';
      case 500:
        return 'Server error occurred';
      default:
        return `HTTP Error ${error.status}: ${error.message}`;
    }
  }

  private queueOfflineRequest(method: string, endpoint: string, body: any): void {
    const request = {
      method,
      endpoint,
      body,
      timestamp: new Date().toISOString()
    };

    // Store in localStorage for later sync
    const offlineQueue = JSON.parse(localStorage.getItem('offline_requests') || '[]');
    offlineQueue.push(request);
    localStorage.setItem('offline_requests', JSON.stringify(offlineQueue));

    console.log('Queued offline request:', request);
  }

  /**
   * Sync offline requests when back online
   */
  syncOfflineRequests(): Observable<any> {
    const offlineQueue = JSON.parse(localStorage.getItem('offline_requests') || '[]');
    
    if (offlineQueue.length === 0) {
      return of({ success: true, message: 'No offline requests to sync' });
    }

    console.log(`Syncing ${offlineQueue.length} offline requests...`);

    // Process each request
    const syncPromises = offlineQueue.map((request: any) => {
      switch (request.method) {
        case 'POST':
          return this.post(request.endpoint, request.body, { skipAuth: false });
        case 'PUT':
          return this.put(request.endpoint, request.body, { skipAuth: false });
        case 'DELETE':
          return this.delete(request.endpoint, { skipAuth: false });
        default:
          return of({ success: false, error: 'Unknown method' });
      }
    });

    // Clear queue after sync attempt
    localStorage.removeItem('offline_requests');

    return new Observable(observer => {
      Promise.all(syncPromises.map((obs: Observable<any>) => obs.toPromise())).then(results => {
        const successful = results.filter(r => r?.success).length;
        observer.next({
          success: true,
          message: `Synced ${successful}/${results.length} offline requests`
        });
        observer.complete();
      }).catch(error => {
        observer.error(error);
      });
    });
  }
}