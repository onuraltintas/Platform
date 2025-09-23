import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import { ToastService } from '../../../core/bildirimler/toast.service';

@Injectable({
  providedIn: 'root'
})
export abstract class BaseUserManagementService {
  protected readonly http = inject(HttpClient);
  protected readonly toastService = inject(ToastService);
  protected readonly baseUrl = environment.apiGateway;

  // Loading states
  private loadingSubject = new BehaviorSubject<boolean>(false);
  public loading$ = this.loadingSubject.asObservable();

  protected setLoading(loading: boolean): void {
    this.loadingSubject.next(loading);
  }

  /**
   * Build HTTP params from object
   */
  protected buildParams(params: Record<string, any>): HttpParams {
    let httpParams = new HttpParams();

    Object.keys(params).forEach(key => {
      const value = params[key];

      if (value !== null && value !== undefined && value !== '') {
        if (Array.isArray(value)) {
          value.forEach(item => {
            httpParams = httpParams.append(key, item.toString());
          });
        } else if (value instanceof Date) {
          httpParams = httpParams.set(key, value.toISOString());
        } else {
          httpParams = httpParams.set(key, value.toString());
        }
      }
    });

    return httpParams;
  }

  /**
   * Generic GET request with loading state
   */
  protected get<T>(endpoint: string, params?: Record<string, any>): Observable<T> {
    this.setLoading(true);

    const httpParams = params ? this.buildParams(params) : undefined;

    return this.http.get<T>(`${this.baseUrl}${endpoint}`, { params: httpParams })
      .pipe(
        tap(() => this.setLoading(false)),
        catchError(error => this.handleError(error)),
        finalize(() => this.setLoading(false))
      );
  }

  /**
   * Generic POST request with loading state
   */
  protected post<T>(endpoint: string, body: any, showSuccessMessage?: string): Observable<T> {
    this.setLoading(true);

    return this.http.post<T>(`${this.baseUrl}${endpoint}`, body)
      .pipe(
        tap(() => {
          this.setLoading(false);
          if (showSuccessMessage) {
            this.toastService.basari(showSuccessMessage);
          }
        }),
        catchError(error => this.handleError(error)),
        finalize(() => this.setLoading(false))
      );
  }

  /**
   * Generic PUT request with loading state
   */
  protected put<T>(endpoint: string, body: any, showSuccessMessage?: string): Observable<T> {
    this.setLoading(true);

    return this.http.put<T>(`${this.baseUrl}${endpoint}`, body)
      .pipe(
        tap(() => {
          this.setLoading(false);
          if (showSuccessMessage) {
            this.toastService.basari(showSuccessMessage);
          }
        }),
        catchError(error => this.handleError(error)),
        finalize(() => this.setLoading(false))
      );
  }

  /**
   * Generic DELETE request with loading state
   */
  protected delete<T>(endpoint: string, showSuccessMessage?: string): Observable<T> {
    this.setLoading(true);

    return this.http.delete<T>(`${this.baseUrl}${endpoint}`)
      .pipe(
        tap(() => {
          this.setLoading(false);
          if (showSuccessMessage) {
            this.toastService.basari(showSuccessMessage);
          }
        }),
        catchError(error => this.handleError(error)),
        finalize(() => this.setLoading(false))
      );
  }

  /**
   * Download file (Excel, PDF, etc.)
   */
  protected downloadFile(endpoint: string, filename: string, params?: Record<string, any>): Observable<Blob> {
    this.setLoading(true);

    const httpParams = params ? this.buildParams(params) : undefined;

    return this.http.get(`${this.baseUrl}${endpoint}`, {
      params: httpParams,
      responseType: 'blob'
    }).pipe(
      tap((blob: Blob) => {
        this.setLoading(false);
        this.downloadBlob(blob, filename);
        this.toastService.basari('Dosya başarıyla indirildi');
      }),
      catchError(error => this.handleError(error)),
      finalize(() => this.setLoading(false))
    );
  }

  /**
   * Handle HTTP errors with user-friendly messages
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'Beklenmeyen bir hata oluştu';

    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Ağ hatası: ${error.error.message}`;
    } else {
      // Server-side error
      switch (error.status) {
        case 400:
          errorMessage = error.error?.message || 'Geçersiz istek';
          break;
        case 401:
          errorMessage = 'Oturum süreniz dolmuş. Lütfen tekrar giriş yapın.';
          break;
        case 403:
          errorMessage = 'Bu işlem için yetkiniz bulunmuyor';
          break;
        case 404:
          errorMessage = 'Aranan kaynak bulunamadı';
          break;
        case 409:
          errorMessage = error.error?.message || 'Çakışan veri mevcut';
          break;
        case 422:
          errorMessage = error.error?.message || 'Doğrulama hatası';
          break;
        case 500:
          errorMessage = 'Sunucu hatası. Lütfen daha sonra tekrar deneyin.';
          break;
        default:
          errorMessage = `Sunucu hatası: ${error.status}`;
      }
    }

    this.toastService.hata(errorMessage);
    return throwError(() => error);
  }

  /**
   * Download blob as file
   */
  private downloadBlob(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  /**
   * Format date for API
   */
  protected formatDate(date: Date): string {
    return date.toISOString().split('T')[0];
  }

  /**
   * Build query string for complex filters
   */
  protected buildQueryString(filters: Record<string, any>): string {
    const params = new URLSearchParams();

    Object.keys(filters).forEach(key => {
      const value = filters[key];
      if (value !== null && value !== undefined && value !== '') {
        if (Array.isArray(value)) {
          value.forEach(item => params.append(key, item.toString()));
        } else {
          params.set(key, value.toString());
        }
      }
    });

    return params.toString();
  }
}