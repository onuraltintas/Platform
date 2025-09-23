import { Injectable } from '@angular/core';
import { StorageItem, StorageOptions, SecurityMetrics } from '../../interfaces/storage-security.interface';

/**
 * Secure in-memory storage provider
 * Most secure option - data exists only in memory and is cleared on page refresh
 */
@Injectable({
  providedIn: 'root'
})
export class MemoryStorageProvider {
  private storage = new Map<string, StorageItem>();
  private metrics: SecurityMetrics = {
    accessAttempts: 0,
    decryptionFailures: 0,
    lastAccess: 0,
    integrityViolations: 0
  };

  /**
   * Store item in memory
   */
  setItem<T>(key: string, value: T, options: StorageOptions = {}): boolean {
    try {
      const item: StorageItem<T> = {
        data: value,
        createdAt: Date.now(),
        expiresAt: options.expiresIn ? Date.now() + options.expiresIn : undefined,
        encrypted: options.encrypted || false,
        version: '1.0.0'
      };

      this.storage.set(key, item);
      this.updateMetrics('access');
      return true;
    } catch (error) {
      console.error('Memory storage setItem failed:', error);
      return false;
    }
  }

  /**
   * Retrieve item from memory
   */
  getItem<T>(key: string): T | null {
    try {
      this.updateMetrics('access');

      const item = this.storage.get(key);
      if (!item) {
        return null;
      }

      // Check expiration
      if (item.expiresAt && Date.now() > item.expiresAt) {
        this.storage.delete(key);
        return null;
      }

      return item.data as T;
    } catch (error) {
      console.error('Memory storage getItem failed:', error);
      return null;
    }
  }

  /**
   * Remove item from memory
   */
  removeItem(key: string): boolean {
    try {
      const deleted = this.storage.delete(key);
      this.updateMetrics('access');
      return deleted;
    } catch (error) {
      console.error('Memory storage removeItem failed:', error);
      return false;
    }
  }

  /**
   * Clear all items from memory
   */
  clear(): boolean {
    try {
      this.storage.clear();
      this.updateMetrics('access');
      return true;
    } catch (error) {
      console.error('Memory storage clear failed:', error);
      return false;
    }
  }

  /**
   * Check if key exists
   */
  hasItem(key: string): boolean {
    this.updateMetrics('access');
    const item = this.storage.get(key);

    if (!item) {
      return false;
    }

    // Check expiration
    if (item.expiresAt && Date.now() > item.expiresAt) {
      this.storage.delete(key);
      return false;
    }

    return true;
  }

  /**
   * Get all keys
   */
  getAllKeys(): string[] {
    const validKeys: string[] = [];
    const now = Date.now();

    for (const [key, item] of this.storage.entries()) {
      if (!item.expiresAt || now < item.expiresAt) {
        validKeys.push(key);
      } else {
        // Clean up expired item
        this.storage.delete(key);
      }
    }

    return validKeys;
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
    let cleanedCount = 0;
    const now = Date.now();

    for (const [key, item] of this.storage.entries()) {
      if (item.expiresAt && now > item.expiresAt) {
        this.storage.delete(key);
        cleanedCount++;
      }
    }

    return cleanedCount;
  }

  /**
   * Get storage size
   */
  getSize(): number {
    return this.storage.size;
  }

  /**
   * Validate storage integrity
   */
  validateIntegrity(): boolean {
    try {
      // Memory storage has inherent integrity
      // Check for corruption indicators
      const keys = this.getAllKeys();

      for (const key of keys) {
        const item = this.storage.get(key);
        if (!item || !item.createdAt || !item.version) {
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
  }

  /**
   * Get storage statistics
   */
  getStats(): {
    totalItems: number;
    expiredItems: number;
    memoryUsage: string;
    oldestItem: number | null;
    newestItem: number | null;
  } {
    const now = Date.now();
    let expiredCount = 0;
    let oldestTimestamp: number | null = null;
    let newestTimestamp: number | null = null;

    for (const [_, item] of this.storage.entries()) {
      if (item.expiresAt && now > item.expiresAt) {
        expiredCount++;
      }

      if (!oldestTimestamp || item.createdAt < oldestTimestamp) {
        oldestTimestamp = item.createdAt;
      }

      if (!newestTimestamp || item.createdAt > newestTimestamp) {
        newestTimestamp = item.createdAt;
      }
    }

    return {
      totalItems: this.storage.size,
      expiredItems: expiredCount,
      memoryUsage: this.estimateMemoryUsage(),
      oldestItem: oldestTimestamp,
      newestItem: newestTimestamp
    };
  }

  /**
   * Estimate memory usage (rough calculation)
   */
  private estimateMemoryUsage(): string {
    let totalSize = 0;

    for (const [key, item] of this.storage.entries()) {
      // Rough estimation
      totalSize += key.length * 2; // UTF-16 encoding
      totalSize += JSON.stringify(item).length * 2;
    }

    if (totalSize < 1024) {
      return `${totalSize} bytes`;
    } else if (totalSize < 1024 * 1024) {
      return `${(totalSize / 1024).toFixed(2)} KB`;
    } else {
      return `${(totalSize / (1024 * 1024)).toFixed(2)} MB`;
    }
  }

  /**
   * Check if storage is available
   */
  static isAvailable(): boolean {
    return true; // Memory storage is always available
  }
}