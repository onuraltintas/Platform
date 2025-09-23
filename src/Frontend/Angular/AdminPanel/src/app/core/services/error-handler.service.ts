import { Injectable, inject, ErrorHandler as NgErrorHandler } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, throwError } from 'rxjs';

export interface ErrorInfo {
  id: string;
  timestamp: Date;
  message: string;
  stack?: string;
  url?: string;
  userAgent?: string;
  userId?: string;
  context?: any;
}

export interface RetryConfig {
  maxAttempts: number;
  delay: number;
  backoff?: number;
  retryCondition?: (error: any) => boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ErrorHandlerService extends NgErrorHandler {
  private readonly router = inject(Router);
  private readonly errorLog: ErrorInfo[] = [];
  private readonly maxLogSize = 100;

  /**
   * Angular global error handler
   */
  override handleError(error: any): void {
    console.error('Global Error:', error);
    this.logError(error);

    // Production ortamında kritik hatalar için bildirim
    if (this.isCriticalError(error)) {
      this.handleCriticalError(error);
    }

    super.handleError(error);
  }

  /**
   * HTTP hatalarını işler
   */
  public handleHttpError(error: HttpErrorResponse, context?: string): Observable<never> {
    const errorInfo: ErrorInfo = {
      id: this.generateErrorId(),
      timestamp: new Date(),
      message: this.getHttpErrorMessage(error),
      url: error.url || undefined,
      userAgent: navigator.userAgent,
      context: {
        status: error.status,
        statusText: error.statusText,
        error: error.error,
        context
      }
    };

    this.logError(errorInfo);

    // Özel HTTP durum kodları için yönlendirmeler
    switch (error.status) {
      case 401:
        this.handleUnauthorized();
        break;
      case 403:
        this.handleForbidden();
        break;
      case 404:
        // 404 hatalarını log'la ama yönlendirme yapma
        break;
      case 500:
      case 502:
      case 503:
      case 504:
        this.handleServerError(error);
        break;
    }

    return throwError(() => error);
  }

  /**
   * Async/await ile kullanılabilir hata yakalayıcı
   */
  public async handleAsyncError<T>(
    operation: () => Promise<T>,
    context?: string
  ): Promise<T | null> {
    try {
      return await operation();
    } catch (error) {
      this.handleError(error);
      console.error(`Async operation failed: ${context}`, error);
      return null;
    }
  }

  /**
   * Retry mekanizması ile hata işleme
   */
  public shouldRetry(error: any, attempt: number = 1, maxAttempts: number = 3): boolean {
    // Network hataları için retry
    if (error.name === 'TimeoutError' || error.name === 'NetworkError') {
      return attempt < maxAttempts;
    }

    // HTTP 5xx hataları için retry
    if (error instanceof HttpErrorResponse) {
      const retryableStatuses = [500, 502, 503, 504];
      return retryableStatuses.includes(error.status) && attempt < maxAttempts;
    }

    return false;
  }

  /**
   * Kullanıcı dostu hata mesajı üretir
   */
  public getUserFriendlyMessage(error: any): string {
    if (error instanceof HttpErrorResponse) {
      return this.getHttpErrorMessage(error);
    }

    if (error?.error?.message) {
      return error.error.message;
    }

    if (error?.message) {
      // Teknik hataları kullanıcı dostu hale getir
      const technicalPatterns = [
        { pattern: /network error/i, message: 'İnternet bağlantınızı kontrol edin' },
        { pattern: /timeout/i, message: 'İşlem zaman aşımına uğradı, lütfen tekrar deneyin' },
        { pattern: /cors/i, message: 'Güvenlik hatası oluştu' },
        { pattern: /parsing/i, message: 'Veri işleme hatası' }
      ];

      for (const { pattern, message } of technicalPatterns) {
        if (pattern.test(error.message)) {
          return message;
        }
      }

      return error.message;
    }

    return 'Beklenmeyen bir hata oluştu';
  }

  /**
   * Hata loglarını döner
   */
  public getErrorLog(): ErrorInfo[] {
    return [...this.errorLog];
  }

  /**
   * Hata loglarını temizler
   */
  public clearErrorLog(): void {
    this.errorLog.length = 0;
  }

  /**
   * Son N hatayı döner
   */
  public getRecentErrors(count: number = 10): ErrorInfo[] {
    return this.errorLog.slice(-count);
  }

  /**
   * Belirli bir zaman aralığındaki hataları döner
   */
  public getErrorsInTimeRange(startDate: Date, endDate: Date): ErrorInfo[] {
    return this.errorLog.filter(
      error => error.timestamp >= startDate && error.timestamp <= endDate
    );
  }

  /**
   * Hata istatistiklerini döner
   */
  public getErrorStats(): {
    total: number;
    byType: Record<string, number>;
    byDay: Record<string, number>;
  } {
    const stats = {
      total: this.errorLog.length,
      byType: {} as Record<string, number>,
      byDay: {} as Record<string, number>
    };

    this.errorLog.forEach(error => {
      // Tip istatistikleri
      const errorType = this.getErrorType(error);
      stats.byType[errorType] = (stats.byType[errorType] || 0) + 1;

      // Günlük istatistikler
      const day = error.timestamp.toISOString().split('T')[0];
      stats.byDay[day] = (stats.byDay[day] || 0) + 1;
    });

    return stats;
  }

  /**
   * Hatayı log'a kaydeder
   */
  private logError(error: any): void {
    const errorInfo: ErrorInfo = {
      id: this.generateErrorId(),
      timestamp: new Date(),
      message: error?.message || 'Unknown error',
      stack: error?.stack,
      url: window.location.href,
      userAgent: navigator.userAgent,
      context: error
    };

    this.errorLog.push(errorInfo);

    // Log boyutunu sınırla
    if (this.errorLog.length > this.maxLogSize) {
      this.errorLog.shift();
    }

    // Development ortamında console'a yaz
    if (!this.isProduction()) {
      console.group('🐛 Error Details');
      console.error('Error:', error);
      console.log('Error Info:', errorInfo);
      console.groupEnd();
    }
  }

  /**
   * HTTP hata mesajını döner
   */
  private getHttpErrorMessage(error: HttpErrorResponse): string {
    // Backend'den gelen mesajı kullan
    if (error.error?.message) {
      return error.error.message;
    }

    // Status koda göre varsayılan mesajlar
    switch (error.status) {
      case 0:
        return 'İnternet bağlantınızı kontrol edin';
      case 400:
        return 'Geçersiz istek. Lütfen girdiğiniz bilgileri kontrol edin';
      case 401:
        return 'Bu işlem için giriş yapmanız gerekiyor';
      case 403:
        return 'Bu işlemi yapmaya yetkiniz bulunmuyor';
      case 404:
        return 'İstenen kaynak bulunamadı';
      case 408:
        return 'İstek zaman aşımına uğradı';
      case 409:
        return 'Veri çakışması. Sayfa yenilendikten sonra tekrar deneyin';
      case 422:
        return 'Girilen veriler doğrulama kurallarına uymuyor';
      case 429:
        return 'Çok fazla istek gönderildi. Lütfen bekleyin';
      case 500:
        return 'Sunucu hatası oluştu';
      case 502:
      case 503:
        return 'Servis şu anda kullanılamıyor';
      case 504:
        return 'Sunucu yanıt vermedi. Lütfen tekrar deneyin';
      default:
        return `Bir hata oluştu (${error.status})`;
    }
  }

  /**
   * 401 hatasını işler (Yetki yok)
   */
  private handleUnauthorized(): void {
    // Token'ı temizle ve login'e yönlendir
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    sessionStorage.clear();

    this.router.navigate(['/auth/login'], {
      queryParams: { returnUrl: this.router.url }
    });
  }

  /**
   * 403 hatasını işler (Yasak)
   */
  private handleForbidden(): void {
    this.router.navigate(['/403']);
  }

  /**
   * Sunucu hatalarını işler
   */
  private handleServerError(error: HttpErrorResponse): void {
    // Kritik sunucu hataları için özel işlemler
    if (error.status >= 500) {
      console.error('Server Error:', error);

      // Gerekirse monitoring servisi ile hata raporla
      this.reportCriticalError(error);
    }
  }

  /**
   * Kritik hataları işler
   */
  private handleCriticalError(error: any): void {
    console.error('Critical Error:', error);

    // Production'da hata raporlama servisi ile gönder
    this.reportCriticalError(error);
  }

  /**
   * Kritik hata kontrolü
   */
  private isCriticalError(error: any): boolean {
    const criticalPatterns = [
      /ChunkLoadError/,
      /Loading chunk \d+ failed/,
      /Script error/,
      /Network request failed/
    ];

    const errorMessage = error?.message || '';
    return criticalPatterns.some(pattern => pattern.test(errorMessage));
  }

  /**
   * Hata tipini belirler
   */
  private getErrorType(error: ErrorInfo): string {
    if (error.context?.status) {
      return `HTTP_${error.context.status}`;
    }

    if (error.message.includes('ChunkLoadError')) {
      return 'CHUNK_LOAD_ERROR';
    }

    if (error.message.includes('Script error')) {
      return 'SCRIPT_ERROR';
    }

    if (error.message.includes('Network')) {
      return 'NETWORK_ERROR';
    }

    return 'UNKNOWN_ERROR';
  }

  /**
   * Benzersiz hata ID'si üretir
   */
  private generateErrorId(): string {
    return `error_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Production ortamı kontrolü
   */
  private isProduction(): boolean {
    return false; // environment.production; // TODO: environment service eklenince düzelt
  }

  /**
   * Kritik hataları raporlar
   */
  private reportCriticalError(error: any): void {
    // TODO: Error reporting service entegrasyonu
    // Sentry, LogRocket vb. servislere gönderim
    console.warn('Critical error reporting not implemented yet:', error);
  }
}