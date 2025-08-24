import { Injectable, OnDestroy } from '@angular/core';
import { Observable, of, timer, Subscription } from 'rxjs';
import { switchMap, catchError } from 'rxjs/operators';
import { ReadingContentApiService } from './reading-content-api.service';
import { ReadingProgressApiService } from './reading-progress-api.service';
import { HttpClientService } from '../../../core/services/http-client.service';

@Injectable({
  providedIn: 'root'
})
export class BackendSyncService implements OnDestroy {
  private syncInProgress = false;
  private periodicSyncSubscription: Subscription | null = null;

  constructor(
    private contentApi: ReadingContentApiService,
    private progressApi: ReadingProgressApiService,
    private httpClient: HttpClientService
  ) {
    // Start periodic sync check every 30 seconds
    this.startPeriodicSync();
  }

  /**
   * Check if backend is available and sync if needed
   */
  checkAndSync(): Observable<{ available: boolean; synced?: boolean; message?: string }> {
    if (this.syncInProgress) {
      return of({ available: false, message: 'Sync already in progress' });
    }

    return this.httpClient.isBackendAvailable().pipe(
      switchMap(available => {
        if (available) {
          return this.syncOfflineData().pipe(
            catchError(error => {
              console.warn('Sync failed:', error);
              return of({ available: true, synced: false, message: 'Sync failed' });
            })
          );
        } else {
          return of({ available: false, message: 'Backend not available' });
        }
      }),
      catchError(error => {
        console.warn('Backend check failed:', error);
        return of({ available: false, message: 'Backend check failed' });
      })
    );
  }

  /**
   * Sync offline data to backend
   */
  private syncOfflineData(): Observable<{ available: boolean; synced: boolean; message: string }> {
    this.syncInProgress = true;

    return this.progressApi.syncOfflineSessions().pipe(
      switchMap(syncResult => {
        this.syncInProgress = false;
        return of({
          available: true,
          synced: true,
          message: syncResult.message || 'Sync completed successfully'
        });
      }),
      catchError(error => {
        this.syncInProgress = false;
        console.error('Offline sync failed:', error);
        return of({
          available: true,
          synced: false,
          message: 'Offline sync failed'
        });
      })
    );
  }

  /**
   * Start periodic sync check
   */
  private startPeriodicSync(): void {
    // Cleanup existing subscription first
    this.stopPeriodicSync();

    // Check every 30 seconds if we're online
    this.periodicSyncSubscription = timer(0, 30000).pipe(
      switchMap(() => this.checkAndSync()),
      catchError(error => {
        console.warn('Periodic sync check failed:', error);
        return of({ available: false });
      })
    ).subscribe({
      next: (result) => {
        if (result.available && 'synced' in result && result.synced) {
          console.log('Background sync completed:', result.message);
        }
      },
      error: (error) => {
        console.error('Periodic sync subscription error:', error);
      }
    });
  }

  /**
   * Stop periodic sync check
   */
  private stopPeriodicSync(): void {
    if (this.periodicSyncSubscription) {
      this.periodicSyncSubscription.unsubscribe();
      this.periodicSyncSubscription = null;
    }
  }

  /**
   * Force manual sync
   */
  forceSync(): Observable<any> {
    console.log('Force sync initiated...');
    return this.checkAndSync();
  }

  /**
   * Get sync status
   */
  getSyncStatus(): { inProgress: boolean } {
    return { inProgress: this.syncInProgress };
  }

  /**
   * Cleanup on service destroy
   */
  ngOnDestroy(): void {
    console.log('BackendSyncService: Cleaning up periodic sync subscription');
    this.stopPeriodicSync();
  }

  /**
   * Configure sync interval (for testing or different environments)
   */
  configureSyncInterval(intervalMs: number): void {
    if (intervalMs < 5000) {
      console.warn('Sync interval too short, minimum is 5 seconds');
      return;
    }
    
    // Restart with new interval
    this.stopPeriodicSync();
    
    this.periodicSyncSubscription = timer(0, intervalMs).pipe(
      switchMap(() => this.checkAndSync()),
      catchError(error => {
        console.warn('Periodic sync check failed:', error);
        return of({ available: false });
      })
    ).subscribe({
      next: (result) => {
        if (result.available && 'synced' in result && result.synced) {
          console.log('Background sync completed:', result.message);
        }
      },
      error: (error) => {
        console.error('Periodic sync subscription error:', error);
      }
    });
  }
}