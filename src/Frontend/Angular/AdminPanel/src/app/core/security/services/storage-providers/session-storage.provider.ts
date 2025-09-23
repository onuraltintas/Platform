import { Injectable } from '@angular/core';
import { StorageItem, StorageOptions, SecurityMetrics } from '../../interfaces/storage-security.interface';

/**
 * Secure sessionStorage provider
 * More secure than localStorage - data is cleared when tab/browser closes
 */
@Injectable({
  providedIn: 'root'
})
export class SessionStorageProvider {
  private readonly storagePrefix = 'platformv1_secure_';
  private metrics: SecurityMetrics = {
    accessAttempts: 0,
    decryptionFailures: 0,
    lastAccess: 0,
    integrityViolations: 0
  };

  constructor() {
    // Initialize metrics from existing data if available
    this.loadMetrics();
  }

  /**
   * Store item in sessionStorage
   */
  setItem<T>(key: string, value: T, options: StorageOptions = {}): boolean {
    try {
      if (!this.isAvailable()) {
        return false;
      }

      const item: StorageItem<T> = {
        data: value,
        createdAt: Date.now(),
        expiresAt: options.expiresIn ? Date.now() + options.expiresIn : undefined,
        encrypted: options.encrypted || false,
        version: '1.0.0'
      };

      const serializedItem = JSON.stringify(item);
      sessionStorage.setItem(this.getStorageKey(key), serializedItem);

      this.updateMetrics('access');
      return true;
    } catch (error) {
      console.error('SessionStorage setItem failed:', error);
      this.updateMetrics('integrityViolation');
      return false;
    }
  }

  /**
   * Retrieve item from sessionStorage
   */
  getItem<T>(key: string): T | null {
    try {
      if (!this.isAvailable()) {
        return null;
      }

      this.updateMetrics('access');

      const serializedItem = sessionStorage.getItem(this.getStorageKey(key));
      if (!serializedItem) {
        return null;
      }

      const item: StorageItem<T> = JSON.parse(serializedItem);

      // Validate item structure
      if (!this.validateItemStructure(item)) {
        this.removeItem(key);
        this.updateMetrics('integrityViolation');
        return null;
      }

      // Check expiration
      if (item.expiresAt && Date.now() > item.expiresAt) {
        this.removeItem(key);
        return null;
      }

      return item.data;
    } catch (error) {
      console.error('SessionStorage getItem failed:', error);
      this.updateMetrics('decryptionFailure');
      return null;
    }
  }

  /**
   * Remove item from sessionStorage
   */
  removeItem(key: string): boolean {
    try {
      if (!this.isAvailable()) {
        return false;
      }

      sessionStorage.removeItem(this.getStorageKey(key));
      this.updateMetrics('access');
      return true;
    } catch (error) {
      console.error('SessionStorage removeItem failed:', error);
      return false;
    }
  }

  /**
   * Clear all app-specific items from sessionStorage
   */
  clear(): boolean {
    try {
      if (!this.isAvailable()) {
        return false;
      }

      const keysToRemove: string[] = [];

      // Find all our keys
      for (let i = 0; i < sessionStorage.length; i++) {
        const key = sessionStorage.key(i);
        if (key && key.startsWith(this.storagePrefix)) {
          keysToRemove.push(key);
        }
      }

      // Remove all our keys
      keysToRemove.forEach(key => sessionStorage.removeItem(key));

      this.updateMetrics('access');
      return true;
    } catch (error) {
      console.error('SessionStorage clear failed:', error);
      return false;
    }
  }

  /**
   * Check if key exists
   */
  hasItem(key: string): boolean {
    try {
      if (!this.isAvailable()) {
        return false;
      }

      this.updateMetrics('access');

      const serializedItem = sessionStorage.getItem(this.getStorageKey(key));
      if (!serializedItem) {
        return false;
      }

      const item: StorageItem = JSON.parse(serializedItem);

      // Check expiration
      if (item.expiresAt && Date.now() > item.expiresAt) {
        this.removeItem(key);
        return false;
      }

      return true;
    } catch (error) {
      console.error('SessionStorage hasItem failed:', error);
      return false;
    }
  }

  /**
   * Get all app-specific keys
   */
  getAllKeys(): string[] {
    try {
      if (!this.isAvailable()) {
        return [];
      }

      const keys: string[] = [];
      const now = Date.now();

      for (let i = 0; i < sessionStorage.length; i++) {
        const storageKey = sessionStorage.key(i);
        if (storageKey && storageKey.startsWith(this.storagePrefix)) {
          const appKey = storageKey.substring(this.storagePrefix.length);

          try {
            const serializedItem = sessionStorage.getItem(storageKey);
            if (serializedItem) {
              const item: StorageItem = JSON.parse(serializedItem);

              // Check if item is expired
              if (!item.expiresAt || now < item.expiresAt) {
                keys.push(appKey);
              } else {
                // Clean up expired item
                sessionStorage.removeItem(storageKey);
              }
            }
          } catch (error) {
            // Remove corrupted item
            sessionStorage.removeItem(storageKey);
          }
        }
      }

      return keys;
    } catch (error) {
      console.error('SessionStorage getAllKeys failed:', error);
      return [];
    }
  }

  /**
   * Get storage metrics
   */
  getMetrics(): SecurityMetrics {
    return { ...this.metrics };
  }

  /**
   * Cleanup expired items
   */
  cleanup(): number {
    try {
      if (!this.isAvailable()) {
        return 0;
      }

      let cleanedCount = 0;
      const now = Date.now();
      const keysToRemove: string[] = [];

      for (let i = 0; i < sessionStorage.length; i++) {
        const storageKey = sessionStorage.key(i);
        if (storageKey && storageKey.startsWith(this.storagePrefix)) {
          try {
            const serializedItem = sessionStorage.getItem(storageKey);
            if (serializedItem) {
              const item: StorageItem = JSON.parse(serializedItem);

              if (item.expiresAt && now > item.expiresAt) {
                keysToRemove.push(storageKey);
              }
            }
          } catch (error) {
            // Mark corrupted items for removal
            keysToRemove.push(storageKey);
          }
        }
      }

      // Remove expired/corrupted items
      keysToRemove.forEach(key => {
        sessionStorage.removeItem(key);
        cleanedCount++;
      });

      return cleanedCount;
    } catch (error) {
      console.error('SessionStorage cleanup failed:', error);
      return 0;
    }
  }

  /**
   * Validate storage integrity
   */
  validateIntegrity(): boolean {
    try {
      if (!this.isAvailable()) {
        return false;
      }

      const keys = this.getAllKeys();

      for (const key of keys) {
        const storageKey = this.getStorageKey(key);
        const serializedItem = sessionStorage.getItem(storageKey);

        if (!serializedItem) {
          continue;
        }

        const item: StorageItem = JSON.parse(serializedItem);

        if (!this.validateItemStructure(item)) {
          this.updateMetrics('integrityViolation');
          return false;
        }
      }

      return true;
    } catch (error) {
      this.updateMetrics('integrityViolation');
      return false;
    }
  }

  /**
   * Get storage usage information
   */
  getStorageInfo(): {
    usedSpace: number;
    totalSpace: number;
    usagePercentage: number;
    itemCount: number;
  } {
    try {
      if (!this.isAvailable()) {
        return { usedSpace: 0, totalSpace: 0, usagePercentage: 0, itemCount: 0 };
      }

      let usedSpace = 0;
      let itemCount = 0;

      for (let i = 0; i < sessionStorage.length; i++) {
        const key = sessionStorage.key(i);
        if (key && key.startsWith(this.storagePrefix)) {
          const value = sessionStorage.getItem(key);
          if (value) {
            usedSpace += key.length + value.length;
            itemCount++;
          }
        }
      }

      // Estimate total sessionStorage capacity (usually 5-10MB)
      const totalSpace = 5 * 1024 * 1024; // 5MB estimate
      const usagePercentage = (usedSpace / totalSpace) * 100;

      return {
        usedSpace,
        totalSpace,
        usagePercentage,
        itemCount
      };
    } catch (error) {
      console.error('SessionStorage getStorageInfo failed:', error);
      return { usedSpace: 0, totalSpace: 0, usagePercentage: 0, itemCount: 0 };
    }
  }

  /**
   * Check if sessionStorage is available
   */
  isAvailable(): boolean {
    return SessionStorageProvider.isAvailable();
  }

  /**
   * Get storage key with prefix
   */
  private getStorageKey(key: string): string {
    return this.storagePrefix + key;
  }

  /**
   * Validate item structure
   */
  private validateItemStructure(item: any): boolean {
    return item &&
           typeof item === 'object' &&
           'data' in item &&
           'createdAt' in item &&
           'version' in item &&
           typeof item.createdAt === 'number' &&
           typeof item.version === 'string';
  }

  /**
   * Update security metrics
   */
  private updateMetrics(type: 'access' | 'decryptionFailure' | 'integrityViolation'): void {
    this.metrics.lastAccess = Date.now();

    switch (type) {
      case 'access':
        this.metrics.accessAttempts++;
        break;
      case 'decryptionFailure':
        this.metrics.decryptionFailures++;
        break;
      case 'integrityViolation':
        this.metrics.integrityViolations++;
        break;
    }

    this.saveMetrics();
  }

  /**
   * Load metrics from storage
   */
  private loadMetrics(): void {
    try {
      if (!this.isAvailable()) {
        return;
      }

      const metricsData = sessionStorage.getItem(this.getStorageKey('__metrics__'));
      if (metricsData) {
        this.metrics = { ...this.metrics, ...JSON.parse(metricsData) };
      }
    } catch (error) {
      // Ignore metrics loading errors
    }
  }

  /**
   * Save metrics to storage
   */
  private saveMetrics(): void {
    try {
      if (!this.isAvailable()) {
        return;
      }

      sessionStorage.setItem(
        this.getStorageKey('__metrics__'),
        JSON.stringify(this.metrics)
      );
    } catch (error) {
      // Ignore metrics saving errors
    }
  }

  /**
   * Static method to check sessionStorage availability
   */
  static isAvailable(): boolean {
    try {
      if (typeof window === 'undefined' || !window.sessionStorage) {
        return false;
      }

      const testKey = '__test_sessionStorage__';
      sessionStorage.setItem(testKey, 'test');
      sessionStorage.removeItem(testKey);
      return true;
    } catch (error) {
      return false;
    }
  }
}