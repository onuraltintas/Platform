import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, timer, interval } from 'rxjs';
import { map, filter, take } from 'rxjs/operators';

export interface IntegrityResult {
  isValid: boolean;
  checksum: string;
  timestamp: number;
  dataLength: number;
  verificationMethod: 'sha256' | 'crc32' | 'simple';
}

export interface IntegrityMetrics {
  totalChecks: number;
  successfulChecks: number;
  failedChecks: number;
  averageCheckTime: number;
  backgroundChecks: number;
  criticalFailures: number;
  lastCheck: number;
}

export interface BackgroundValidationConfig {
  interval: number; // ms
  criticalDataPaths: string[];
  validationThreshold: number;
  autoCorrectEnabled: boolean;
}

/**
 * Integrity Check Service for Background Security Validation
 * Continuously monitors data integrity across the application
 */
@Injectable({
  providedIn: 'root'
})
export class IntegrityCheckService {
  private metrics$ = new BehaviorSubject<IntegrityMetrics>(this.getInitialMetrics());
  private validationQueue = new Map<string, any>();
  private checksumCache = new Map<string, IntegrityResult>();
  private criticalAlerts$ = new BehaviorSubject<string[]>([]);

  private readonly config: BackgroundValidationConfig = {
    interval: 30000, // 30 seconds
    criticalDataPaths: [
      '/api/v1/auth/me',
      '/api/v1/users',
      '/api/v1/roles',
      '/api/v1/permissions'
    ],
    validationThreshold: 0.95, // 95% success rate required
    autoCorrectEnabled: true
  };

  private readonly CACHE_TTL = 5 * 60 * 1000; // 5 minutes
  private readonly MAX_CACHE_SIZE = 200;

  constructor() {
    this.initializeBackgroundValidation();
  }

  /**
   * Generate checksum for data
   */
  async generateChecksum(data: any, method: 'sha256' | 'crc32' | 'simple' = 'sha256'): Promise<string> {
    const startTime = performance.now();

    try {
      const serializedData = this.serializeData(data);

      let checksum: string;
      switch (method) {
        case 'sha256':
          checksum = await this.generateSHA256(serializedData);
          break;
        case 'crc32':
          checksum = this.generateCRC32(serializedData);
          break;
        case 'simple':
        default:
          checksum = this.generateSimpleHash(serializedData);
          break;
      }

      this.updateMetrics('success', performance.now() - startTime);
      return checksum;

    } catch (error) {
      this.updateMetrics('error', performance.now() - startTime);
      console.error('Checksum generation failed:', error);
      throw error;
    }
  }

  /**
   * Verify data integrity
   */
  async verifyChecksum(data: any, expectedChecksum: string, method: 'sha256' | 'crc32' | 'simple' = 'sha256'): Promise<IntegrityResult> {
    const startTime = performance.now();

    try {
      const actualChecksum = await this.generateChecksum(data, method);
      const isValid = actualChecksum === expectedChecksum;
      const serializedData = this.serializeData(data);

      const result: IntegrityResult = {
        isValid,
        checksum: actualChecksum,
        timestamp: Date.now(),
        dataLength: serializedData.length,
        verificationMethod: method
      };

      // Cache successful results
      if (isValid && this.checksumCache.size < this.MAX_CACHE_SIZE) {
        const cacheKey = this.createCacheKey(data, method);
        this.checksumCache.set(cacheKey, result);
      }

      this.updateMetrics(isValid ? 'success' : 'error', performance.now() - startTime);

      if (!isValid) {
        this.handleIntegrityFailure(data, expectedChecksum, actualChecksum);
      }

      return result;

    } catch (error) {
      this.updateMetrics('error', performance.now() - startTime);
      console.error('Checksum verification failed:', error);
      throw error;
    }
  }

  /**
   * Quick integrity check with cache
   */
  quickIntegrityCheck(data: any, method: 'sha256' | 'crc32' | 'simple' = 'simple'): IntegrityResult | null {
    const cacheKey = this.createCacheKey(data, method);
    const cached = this.checksumCache.get(cacheKey);

    if (cached && Date.now() - cached.timestamp < this.CACHE_TTL) {
      return cached;
    }

    if (cached) {
      this.checksumCache.delete(cacheKey);
    }

    return null;
  }

  /**
   * Add data to background validation queue
   */
  scheduleBackgroundValidation(key: string, data: any, expectedChecksum?: string): void {
    this.validationQueue.set(key, {
      data,
      expectedChecksum,
      scheduled: Date.now(),
      priority: this.config.criticalDataPaths.includes(key) ? 'high' : 'normal'
    });
  }

  /**
   * Validate API response integrity
   */
  async validateApiResponse(response: any, endpoint: string): Promise<IntegrityResult> {
    try {
      // Check if response has integrity metadata
      if (response.checksum && response.data) {
        return await this.verifyChecksum(response.data, response.checksum);
      }

      // Generate checksum for response without metadata
      const checksum = await this.generateChecksum(response, 'simple');

      return {
        isValid: true,
        checksum,
        timestamp: Date.now(),
        dataLength: JSON.stringify(response).length,
        verificationMethod: 'simple'
      };

    } catch (error) {
      console.error(`API response validation failed for ${endpoint}:`, error);
      throw error;
    }
  }

  /**
   * Get integrity metrics
   */
  getMetrics(): Observable<IntegrityMetrics> {
    return this.metrics$.asObservable();
  }

  /**
   * Get critical alerts
   */
  getCriticalAlerts(): Observable<string[]> {
    return this.criticalAlerts$.asObservable();
  }

  /**
   * Clear integrity alerts
   */
  clearAlerts(): void {
    this.criticalAlerts$.next([]);
  }

  /**
   * Get validation health score
   */
  getValidationHealthScore(): number {
    const metrics = this.metrics$.value;
    if (metrics.totalChecks === 0) return 1.0;

    const successRate = metrics.successfulChecks / metrics.totalChecks;
    const recentHealthScore = successRate >= this.config.validationThreshold ? 1.0 : successRate;

    return Math.max(0, Math.min(1, recentHealthScore));
  }

  // Private methods

  private initializeBackgroundValidation(): void {
    // Background validation timer
    timer(5000, this.config.interval).subscribe(() => {
      this.performBackgroundValidation();
    });

    // Cleanup expired cache entries
    timer(60000, 60000).subscribe(() => {
      this.cleanupCache();
    });

    // Performance monitoring
    timer(0, 30000).subscribe(() => {
      this.logPerformanceMetrics();
    });

    console.log('🛡️ Background integrity validation initialized');
  }

  private async performBackgroundValidation(): Promise<void> {
    if (this.validationQueue.size === 0) {
      return;
    }

    const entries = Array.from(this.validationQueue.entries());

    // Process high priority items first
    const sortedEntries = entries.sort((a, b) => {
      const priorityA = a[1].priority === 'high' ? 1 : 0;
      const priorityB = b[1].priority === 'high' ? 1 : 0;
      return priorityB - priorityA;
    });

    // Process up to 5 items per cycle
    const itemsToProcess = sortedEntries.slice(0, 5);

    for (const [key, item] of itemsToProcess) {
      try {
        const result = await this.validateQueuedItem(key, item);
        this.updateMetrics('backgroundCheck', 0);

        if (!result.isValid) {
          this.handleBackgroundValidationFailure(key, item, result);
        }

      } catch (error) {
        console.error(`Background validation failed for ${key}:`, error);
        this.updateMetrics('error', 0);
      }

      // Remove processed item
      this.validationQueue.delete(key);
    }
  }

  private async validateQueuedItem(key: string, item: any): Promise<IntegrityResult> {
    if (item.expectedChecksum) {
      return await this.verifyChecksum(item.data, item.expectedChecksum);
    } else {
      // Generate new checksum for comparison later
      const checksum = await this.generateChecksum(item.data);
      return {
        isValid: true,
        checksum,
        timestamp: Date.now(),
        dataLength: JSON.stringify(item.data).length,
        verificationMethod: 'sha256'
      };
    }
  }

  private handleBackgroundValidationFailure(key: string, item: any, result: IntegrityResult): void {
    const alert = `Background validation failed for ${key}: checksum mismatch`;
    console.warn('🚨', alert);

    const currentAlerts = this.criticalAlerts$.value;
    this.criticalAlerts$.next([...currentAlerts, alert]);

    this.updateMetrics('criticalFailure', 0);

    // Auto-correct if enabled
    if (this.config.autoCorrectEnabled) {
      this.attemptAutoCorrection(key, item);
    }
  }

  private attemptAutoCorrection(key: string, item: any): void {
    console.log(`🔧 Attempting auto-correction for ${key}`);

    // For API endpoints, schedule a fresh fetch
    if (this.config.criticalDataPaths.includes(key)) {
      // Emit correction needed event
      // This would typically trigger a fresh API call
      console.log(`📡 Requesting fresh data for ${key}`);
    }
  }

  private handleIntegrityFailure(data: any, expected: string, actual: string): void {
    const alert = `Integrity check failed: expected ${expected}, got ${actual}`;
    console.error('🚨 Integrity Failure:', alert);

    const currentAlerts = this.criticalAlerts$.value;
    this.criticalAlerts$.next([...currentAlerts, alert]);
  }

  private async generateSHA256(data: string): Promise<string> {
    const encoder = new TextEncoder();
    const dataBuffer = encoder.encode(data);
    const hashBuffer = await crypto.subtle.digest('SHA-256', dataBuffer);

    return Array.from(new Uint8Array(hashBuffer))
      .map(b => b.toString(16).padStart(2, '0'))
      .join('');
  }

  private generateCRC32(data: string): string {
    let crc = 0xFFFFFFFF;

    for (let i = 0; i < data.length; i++) {
      crc ^= data.charCodeAt(i);
      for (let j = 0; j < 8; j++) {
        crc = (crc >>> 1) ^ (crc & 1 ? 0xEDB88320 : 0);
      }
    }

    return ((crc ^ 0xFFFFFFFF) >>> 0).toString(16);
  }

  private generateSimpleHash(data: string): string {
    let hash = 0;

    for (let i = 0; i < data.length; i++) {
      const char = data.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }

    return Math.abs(hash).toString(36);
  }

  private serializeData(data: any): string {
    if (typeof data === 'string') {
      return data;
    }

    // Sort object keys for consistent serialization
    if (typeof data === 'object' && data !== null) {
      const sortedKeys = Object.keys(data).sort();
      const sortedObj = sortedKeys.reduce((obj, key) => {
        obj[key] = data[key];
        return obj;
      }, {} as any);
      return JSON.stringify(sortedObj);
    }

    return JSON.stringify(data);
  }

  private createCacheKey(data: any, method: string): string {
    const serialized = this.serializeData(data);
    const hash = this.generateSimpleHash(serialized + method);
    return `${method}_${hash}`;
  }

  private updateMetrics(operation: 'success' | 'error' | 'backgroundCheck' | 'criticalFailure', duration: number): void {
    const currentMetrics = this.metrics$.value;

    const newMetrics: IntegrityMetrics = {
      ...currentMetrics,
      lastCheck: Date.now()
    };

    switch (operation) {
      case 'success':
        newMetrics.totalChecks++;
        newMetrics.successfulChecks++;
        newMetrics.averageCheckTime = this.calculateMovingAverage(
          currentMetrics.averageCheckTime,
          duration,
          currentMetrics.totalChecks
        );
        break;

      case 'error':
        newMetrics.totalChecks++;
        newMetrics.failedChecks++;
        newMetrics.averageCheckTime = this.calculateMovingAverage(
          currentMetrics.averageCheckTime,
          duration,
          currentMetrics.totalChecks
        );
        break;

      case 'backgroundCheck':
        newMetrics.backgroundChecks++;
        break;

      case 'criticalFailure':
        newMetrics.criticalFailures++;
        break;
    }

    this.metrics$.next(newMetrics);
  }

  private calculateMovingAverage(currentAvg: number, newValue: number, count: number): number {
    if (count === 1) return newValue;
    return ((currentAvg * (count - 1)) + newValue) / count;
  }

  private getInitialMetrics(): IntegrityMetrics {
    return {
      totalChecks: 0,
      successfulChecks: 0,
      failedChecks: 0,
      averageCheckTime: 0,
      backgroundChecks: 0,
      criticalFailures: 0,
      lastCheck: 0
    };
  }

  private cleanupCache(): void {
    const now = Date.now();
    const keysToDelete: string[] = [];

    this.checksumCache.forEach((value, key) => {
      if (now - value.timestamp > this.CACHE_TTL) {
        keysToDelete.push(key);
      }
    });

    keysToDelete.forEach(key => this.checksumCache.delete(key));

    if (keysToDelete.length > 0) {
      console.log(`🧹 Cleaned ${keysToDelete.length} expired integrity cache entries`);
    }
  }

  private logPerformanceMetrics(): void {
    const metrics = this.metrics$.value;
    const healthScore = this.getValidationHealthScore();

    if (metrics.totalChecks > 0) {
      console.log('🛡️ Integrity Check Performance:', {
        totalChecks: metrics.totalChecks,
        successRate: `${((metrics.successfulChecks / metrics.totalChecks) * 100).toFixed(1)}%`,
        avgCheckTime: `${metrics.averageCheckTime.toFixed(2)}ms`,
        backgroundChecks: metrics.backgroundChecks,
        healthScore: `${(healthScore * 100).toFixed(1)}%`,
        queueSize: this.validationQueue.size,
        cacheSize: this.checksumCache.size,
        criticalFailures: metrics.criticalFailures
      });
    }

    // Warning for low health score
    if (healthScore < this.config.validationThreshold) {
      console.warn(`⚠️ Integrity validation health below threshold: ${(healthScore * 100).toFixed(1)}%`);
    }
  }
}