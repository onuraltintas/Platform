import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface LoadingState {
  isLoading: boolean;
  message: string;
  progress?: number;
  operation?: string;
}

@Injectable({
  providedIn: 'root'
})
export class LoadingService {
  private readonly loadingMap = new Map<string, boolean>();
  private readonly messageMap = new Map<string, string>();
  private readonly progressMap = new Map<string, number>();

  private readonly loadingSubject = new BehaviorSubject<boolean>(false);
  private readonly loadingMessageSubject = new BehaviorSubject<string>('');
  private readonly loadingProgressSubject = new BehaviorSubject<number>(0);
  private readonly loadingStateSubject = new BehaviorSubject<LoadingState>({
    isLoading: false,
    message: '',
    progress: 0
  });

  // Public observables
  public readonly isLoading$ = this.loadingSubject.asObservable();
  public readonly loadingMessage$ = this.loadingMessageSubject.asObservable();
  public readonly loadingProgress$ = this.loadingProgressSubject.asObservable();
  public readonly loadingState$ = this.loadingStateSubject.asObservable();

  /**
   * URL bazında loading durumunu yönetir
   */
  public setLoading(loading: boolean, url?: string, message?: string): void {
    const key = url || 'global';

    if (loading) {
      this.loadingMap.set(key, loading);
      if (message) {
        this.messageMap.set(key, message);
      }
    } else {
      this.loadingMap.delete(key);
      this.messageMap.delete(key);
      this.progressMap.delete(key);
    }

    this.updateState();
  }

  /**
   * Global loading işlemini başlatır
   */
  public show(message?: string): void {
    this.setLoading(true, 'global', message);
  }

  /**
   * Data loading başlatır ve ref döner
   */
  public showLoadingData(message: string): { dismiss(): void } {
    this.setLoading(true, 'data-loading', message);
    return {
      dismiss: () => this.setLoading(false, 'data-loading')
    };
  }

  /**
   * Global loading işlemini durdurur
   */
  public hide(): void {
    this.setLoading(false, 'global');
  }

  /**
   * Belirli bir operasyon için loading başlatır
   */
  public startOperation(operation: string, message?: string): void {
    this.setLoading(true, operation, message || `${operation} işlemi devam ediyor...`);
  }

  /**
   * Belirli bir operasyonu bitirir
   */
  public endOperation(operation?: string): void {
    if (operation) {
      this.setLoading(false, operation);
    } else {
      this.hide();
    }
  }

  /**
   * Toplu işlem için loading başlatır
   */
  public startBulkOperation(message: string, total?: number): void {
    this.setLoading(true, 'bulk-operation', message);
    if (total) {
      this.setProgress(0, 'bulk-operation', total);
    }
  }

  /**
   * Toplu işlem ilerlemesini günceller
   */
  public updateBulkProgress(completed: number, total: number, message?: string): void {
    const progress = Math.round((completed / total) * 100);
    this.setProgress(progress, 'bulk-operation');

    if (message) {
      this.messageMap.set('bulk-operation', message);
    }

    this.updateState();
  }

  /**
   * Toplu işlemi bitirir
   */
  public endBulkOperation(): void {
    this.setLoading(false, 'bulk-operation');
  }

  /**
   * Progress değerini ayarlar
   */
  public setProgress(progress: number, operation?: string, total?: number): void {
    const key = operation || 'global';
    this.progressMap.set(key, progress);

    if (total && progress < 100) {
      const message = `İşlem devam ediyor... (${progress}%)`;
      this.messageMap.set(key, message);
    }

    this.updateState();
  }

  /**
   * Tüm loading durumlarını sıfırlar
   */
  public reset(): void {
    this.loadingMap.clear();
    this.messageMap.clear();
    this.progressMap.clear();
    this.updateState();
  }

  /**
   * Belirli bir operasyonun loading durumunu kontrol eder
   */
  public isOperationLoading(operation: string): boolean {
    return this.loadingMap.has(operation);
  }

  /**
   * Mevcut loading durumunu döner
   */
  public get isLoading(): boolean {
    return this.loadingSubject.value;
  }

  /**
   * Aktif operasyon sayısını döner
   */
  public get activeOperationsCount(): number {
    return this.loadingMap.size;
  }

  /**
   * Tüm aktif operasyonların listesini döner
   */
  public getActiveOperations(): string[] {
    return Array.from(this.loadingMap.keys());
  }

  /**
   * Belirli bir operasyonun mesajını döner
   */
  public getOperationMessage(operation: string): string {
    return this.messageMap.get(operation) || '';
  }

  /**
   * Loading durumunu günceller
   */
  private updateState(): void {
    const hasActiveOperations = this.loadingMap.size > 0;
    const primaryMessage = this.getPrimaryMessage();
    const primaryProgress = this.getPrimaryProgress();

    this.loadingSubject.next(hasActiveOperations);
    this.loadingMessageSubject.next(primaryMessage);
    this.loadingProgressSubject.next(primaryProgress);

    this.loadingStateSubject.next({
      isLoading: hasActiveOperations,
      message: primaryMessage,
      progress: primaryProgress,
      operation: this.getPrimaryOperation()
    });
  }

  /**
   * Ana mesajı döner (öncelik sırasına göre)
   */
  private getPrimaryMessage(): string {
    // Öncelik sırası: bulk-operation > global > diğerleri
    if (this.messageMap.has('bulk-operation')) {
      return this.messageMap.get('bulk-operation')!;
    }
    if (this.messageMap.has('global')) {
      return this.messageMap.get('global')!;
    }

    // İlk bulunan mesajı döner
    const firstMessage = this.messageMap.values().next().value;
    return firstMessage || 'Yükleniyor...';
  }

  /**
   * Ana progress değerini döner
   */
  private getPrimaryProgress(): number {
    // Bulk operation progress'i varsa onu döner
    if (this.progressMap.has('bulk-operation')) {
      return this.progressMap.get('bulk-operation')!;
    }

    // Diğer operasyonların progress ortalamasını döner
    const progressValues = Array.from(this.progressMap.values());
    if (progressValues.length === 0) return 0;

    return Math.round(
      progressValues.reduce((sum, progress) => sum + progress, 0) / progressValues.length
    );
  }

  /**
   * Ana operasyonu döner
   */
  private getPrimaryOperation(): string {
    if (this.loadingMap.has('bulk-operation')) return 'bulk-operation';
    if (this.loadingMap.has('global')) return 'global';
    return Array.from(this.loadingMap.keys())[0] || '';
  }

  /**
   * Promise/Observable wrapper ile otomatik loading yönetimi
   */
  public wrap<T>(
    operation: Observable<T> | Promise<T>,
    options?: {
      operation?: string;
      message?: string;
      showProgress?: boolean;
    }
  ): Observable<T> | Promise<T> {
    const operationKey = options?.operation || 'global';
    const message = options?.message;

    this.setLoading(true, operationKey, message);

    if (operation instanceof Promise) {
      return operation.finally(() => {
        this.setLoading(false, operationKey);
      });
    } else {
      // Observable için finalize operator'ı kullan
      return new Observable<T>(subscriber => {
        const subscription = operation.subscribe({
          next: value => subscriber.next(value),
          error: error => {
            this.setLoading(false, operationKey);
            subscriber.error(error);
          },
          complete: () => {
            this.setLoading(false, operationKey);
            subscriber.complete();
          }
        });

        return () => subscription.unsubscribe();
      });
    }
  }
}