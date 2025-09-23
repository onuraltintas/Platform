import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface StorageOptions {
  encrypt?: boolean;
  expiry?: number; // milliseconds
  namespace?: string;
  compressed?: boolean;
}

export interface StorageItem {
  value: any;
  timestamp: number;
  expiry?: number;
  namespace: string;
  encrypted: boolean;
  compressed: boolean;
}

/**
 * Secure Storage Service
 * Enhanced storage with encryption and expiry support
 */
@Injectable({
  providedIn: 'root'
})
export class SecureStorageService {
  private storageMetrics$ = new BehaviorSubject<{
    operations: number;
    errors: number;
    cacheHits: number;
    size: number;
  }>({ operations: 0, errors: 0, cacheHits: 0, size: 0 });

  private memoryCache = new Map<string, StorageItem>();
  private readonly defaultNamespace = 'secure';

  constructor() {
    this.initializeSecureStorage();
  }

  /**
   * Store item securely
   */
  setItem(key: string, value: any, options: StorageOptions = {}): boolean {
    try {
      const item: StorageItem = {
        value: options.encrypt ? this.encrypt(value) : value,
        timestamp: Date.now(),
        expiry: options.expiry ? Date.now() + options.expiry : undefined,
        namespace: options.namespace || this.defaultNamespace,
        encrypted: !!options.encrypt,
        compressed: !!options.compressed
      };

      const storageKey = this.createKey(key, item.namespace);

      // Store in memory cache
      this.memoryCache.set(storageKey, item);

      // Store in localStorage
      localStorage.setItem(storageKey, JSON.stringify(item));

      this.updateMetrics('operation');
      return true;

    } catch (error) {
      console.error('Secure storage setItem failed:', error);
      this.updateMetrics('error');
      return false;
    }
  }

  /**
   * Retrieve item securely
   */
  getItem(key: string, namespace?: string): any {
    try {
      const storageKey = this.createKey(key, namespace || this.defaultNamespace);

      // Check memory cache first
      const cached = this.memoryCache.get(storageKey);
      if (cached) {
        if (this.isExpired(cached)) {
          this.removeItem(key, namespace);
          return null;
        }
        this.updateMetrics('cacheHit');
        return cached.encrypted ? this.decrypt(cached.value) : cached.value;
      }

      // Check localStorage
      const stored = localStorage.getItem(storageKey);
      if (!stored) {
        return null;
      }

      const item: StorageItem = JSON.parse(stored);

      if (this.isExpired(item)) {
        this.removeItem(key, namespace);
        return null;
      }

      // Update memory cache
      this.memoryCache.set(storageKey, item);

      this.updateMetrics('operation');
      return item.encrypted ? this.decrypt(item.value) : item.value;

    } catch (error) {
      console.error('Secure storage getItem failed:', error);
      this.updateMetrics('error');
      return null;
    }
  }

  /**
   * Remove item
   */
  removeItem(key: string, namespace?: string): boolean {
    try {
      const storageKey = this.createKey(key, namespace || this.defaultNamespace);

      this.memoryCache.delete(storageKey);
      localStorage.removeItem(storageKey);

      this.updateMetrics('operation');
      return true;

    } catch (error) {
      console.error('Secure storage removeItem failed:', error);
      this.updateMetrics('error');
      return false;
    }
  }

  /**
   * Clear all items in namespace
   */
  clear(namespace?: string): void {
    const targetNamespace = namespace || this.defaultNamespace;

    // Clear memory cache
    this.memoryCache.forEach((item, key) => {
      if (item.namespace === targetNamespace) {
        this.memoryCache.delete(key);
      }
    });

    // Clear localStorage
    Object.keys(localStorage).forEach(key => {
      if (key.startsWith(`${targetNamespace}:`)) {
        localStorage.removeItem(key);
      }
    });

    this.updateMetrics('operation');
  }

  /**
   * Check if key exists
   */
  hasItem(key: string, namespace?: string): boolean {
    const storageKey = this.createKey(key, namespace || this.defaultNamespace);
    return this.memoryCache.has(storageKey) || localStorage.getItem(storageKey) !== null;
  }

  /**
   * Get all keys in namespace
   */
  getKeys(namespace?: string): string[] {
    const targetNamespace = namespace || this.defaultNamespace;
    const prefix = `${targetNamespace}:`;

    return Object.keys(localStorage)
      .filter(key => key.startsWith(prefix))
      .map(key => key.substring(prefix.length));
  }

  /**
   * Get storage size
   */
  getSize(namespace?: string): number {
    const targetNamespace = namespace || this.defaultNamespace;
    const prefix = `${targetNamespace}:`;

    return Object.keys(localStorage)
      .filter(key => key.startsWith(prefix))
      .reduce((size, key) => {
        const value = localStorage.getItem(key);
        return size + (value ? value.length : 0);
      }, 0);
  }

  // Private methods

  private initializeSecureStorage(): void {
    // Clean expired items on initialization
    this.cleanupExpiredItems();

    // Setup periodic cleanup
    setInterval(() => {
      this.cleanupExpiredItems();
    }, 5 * 60 * 1000); // Every 5 minutes

    console.log('🔐 Secure Storage Service initialized');
  }

  private createKey(key: string, namespace: string): string {
    return `${namespace}:${key}`;
  }

  private isExpired(item: StorageItem): boolean {
    return item.expiry ? Date.now() > item.expiry : false;
  }

  private encrypt(value: any): string {
    // Simple encoding for demo (not cryptographically secure)
    return btoa(JSON.stringify(value));
  }

  private decrypt(encrypted: string): any {
    try {
      return JSON.parse(atob(encrypted));
    } catch {
      return null;
    }
  }

  private cleanupExpiredItems(): void {
    let cleanedCount = 0;

    // Clean memory cache
    this.memoryCache.forEach((item, key) => {
      if (this.isExpired(item)) {
        this.memoryCache.delete(key);
        cleanedCount++;
      }
    });

    // Clean localStorage
    Object.keys(localStorage).forEach(key => {
      try {
        const stored = localStorage.getItem(key);
        if (stored && key.includes(':')) {
          const item: StorageItem = JSON.parse(stored);
          if (this.isExpired(item)) {
            localStorage.removeItem(key);
            cleanedCount++;
          }
        }
      } catch {
        // Invalid item, remove it
        localStorage.removeItem(key);
        cleanedCount++;
      }
    });

    if (cleanedCount > 0) {
      console.log(`🧹 Cleaned ${cleanedCount} expired storage items`);
    }
  }

  private updateMetrics(type: 'operation' | 'error' | 'cacheHit'): void {
    const current = this.storageMetrics$.value;
    const updated = { ...current };

    switch (type) {
      case 'operation':
        updated.operations++;
        break;
      case 'error':
        updated.errors++;
        break;
      case 'cacheHit':
        updated.cacheHits++;
        break;
    }

    updated.size = this.memoryCache.size;
    this.storageMetrics$.next(updated);
  }
}