import { Injectable } from '@angular/core';

export interface CacheEntry<T> {
  value: T;
  createdAt: number;
  expiresAt?: number;
  accessCount: number;
  lastAccessedAt: number;
}

export interface CacheOptions {
  ttl?: number; // Time to live in milliseconds
  maxSize?: number; // Maximum number of entries
}

/**
 * High-performance LRU (Least Recently Used) Cache Service
 * Optimized for token storage with O(1) operations
 */
@Injectable({
  providedIn: 'root'
})
export class LRUCacheService<T = any> {
  private cache = new Map<string, CacheEntry<T>>();
  private accessOrder = new Map<string, number>(); // For LRU tracking
  private currentTime = 0;

  private readonly maxSize: number = 100;
  private readonly defaultTTL: number = 15 * 60 * 1000; // 15 minutes

  constructor() {}

  /**
   * Get value from cache with O(1) complexity
   */
  get(key: string): T | null {
    const entry = this.cache.get(key);

    if (!entry) {
      return null;
    }

    // Check expiration
    const now = Date.now();
    if (entry.expiresAt && now > entry.expiresAt) {
      this.delete(key);
      return null;
    }

    // Update access tracking for LRU
    entry.lastAccessedAt = now;
    entry.accessCount++;
    this.accessOrder.set(key, ++this.currentTime);

    return entry.value;
  }

  /**
   * Set value in cache with O(1) complexity
   */
  set(key: string, value: T, options?: CacheOptions): boolean {
    try {
      const now = Date.now();
      const ttl = options?.ttl ?? this.defaultTTL;

      const entry: CacheEntry<T> = {
        value,
        createdAt: now,
        expiresAt: ttl > 0 ? now + ttl : undefined,
        accessCount: 1,
        lastAccessedAt: now
      };

      // If cache is full, remove LRU item
      if (this.cache.size >= this.maxSize && !this.cache.has(key)) {
        this.evictLRU();
      }

      this.cache.set(key, entry);
      this.accessOrder.set(key, ++this.currentTime);

      return true;
    } catch (error) {
      console.error('Cache set failed:', error);
      return false;
    }
  }

  /**
   * Check if key exists and is not expired
   */
  has(key: string): boolean {
    const entry = this.cache.get(key);
    if (!entry) return false;

    // Check expiration
    const now = Date.now();
    if (entry.expiresAt && now > entry.expiresAt) {
      this.delete(key);
      return false;
    }

    return true;
  }

  /**
   * Delete specific key
   */
  delete(key: string): boolean {
    const deleted = this.cache.delete(key);
    this.accessOrder.delete(key);
    return deleted;
  }

  /**
   * Clear all cache entries
   */
  clear(): void {
    this.cache.clear();
    this.accessOrder.clear();
    this.currentTime = 0;
  }

  /**
   * Get cache statistics for monitoring
   */
  getStats() {
    const now = Date.now();
    let expiredCount = 0;

    for (const [, entry] of this.cache.entries()) {
      if (entry.expiresAt && now > entry.expiresAt) {
        expiredCount++;
      }
    }

    return {
      size: this.cache.size,
      maxSize: this.maxSize,
      expiredEntries: expiredCount,
      hitRate: this.calculateHitRate(),
      oldestEntry: this.getOldestEntry(),
      newestEntry: this.getNewestEntry()
    };
  }

  /**
   * Get all keys (for debugging)
   */
  keys(): string[] {
    return Array.from(this.cache.keys());
  }

  /**
   * Cleanup expired entries
   */
  cleanup(): number {
    const now = Date.now();
    let cleanedCount = 0;

    for (const [key, entry] of this.cache.entries()) {
      if (entry.expiresAt && now > entry.expiresAt) {
        this.delete(key);
        cleanedCount++;
      }
    }

    return cleanedCount;
  }

  /**
   * Update TTL for existing entry
   */
  updateTTL(key: string, newTTL: number): boolean {
    const entry = this.cache.get(key);
    if (!entry) return false;

    const now = Date.now();
    entry.expiresAt = newTTL > 0 ? now + newTTL : undefined;
    entry.lastAccessedAt = now;

    return true;
  }

  /**
   * Get entry metadata without affecting LRU order
   */
  getMetadata(key: string): Omit<CacheEntry<T>, 'value'> | null {
    const entry = this.cache.get(key);
    if (!entry) return null;

    return {
      createdAt: entry.createdAt,
      expiresAt: entry.expiresAt,
      accessCount: entry.accessCount,
      lastAccessedAt: entry.lastAccessedAt
    };
  }

  // Private methods

  private evictLRU(): void {
    let lruKey: string | null = null;
    let lruTime = Infinity;

    for (const [key, time] of this.accessOrder.entries()) {
      if (time < lruTime) {
        lruTime = time;
        lruKey = key;
      }
    }

    if (lruKey) {
      this.delete(lruKey);
    }
  }

  private calculateHitRate(): number {
    let totalHits = 0;
    let totalAccess = this.cache.size;

    for (const entry of this.cache.values()) {
      totalHits += entry.accessCount;
    }

    return totalAccess > 0 ? totalHits / totalAccess : 0;
  }

  private getOldestEntry(): { key: string; age: number } | null {
    let oldestKey: string | null = null;
    let oldestTime = Infinity;

    for (const [key, entry] of this.cache.entries()) {
      if (entry.createdAt < oldestTime) {
        oldestTime = entry.createdAt;
        oldestKey = key;
      }
    }

    return oldestKey ? {
      key: oldestKey,
      age: Date.now() - oldestTime
    } : null;
  }

  private getNewestEntry(): { key: string; age: number } | null {
    let newestKey: string | null = null;
    let newestTime = 0;

    for (const [key, entry] of this.cache.entries()) {
      if (entry.createdAt > newestTime) {
        newestTime = entry.createdAt;
        newestKey = key;
      }
    }

    return newestKey ? {
      key: newestKey,
      age: Date.now() - newestTime
    } : null;
  }
}