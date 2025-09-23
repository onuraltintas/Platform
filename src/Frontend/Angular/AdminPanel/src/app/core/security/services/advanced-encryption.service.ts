import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface EncryptionConfig {
  algorithm: 'AES-GCM' | 'AES-CBC';
  keyLength: 128 | 192 | 256;
  ivLength: number;
  tagLength?: number;
}

export interface EncryptionResult {
  data: string;
  iv: string;
  tag?: string;
  timestamp: number;
}

export interface EncryptionMetrics {
  operationsCount: number;
  averageEncryptTime: number;
  averageDecryptTime: number;
  cacheHits: number;
  cacheMisses: number;
  errorCount: number;
  lastOperation: number;
}

/**
 * Advanced Encryption Service with WebCrypto API
 * Lazy-loaded for performance optimization
 */
@Injectable({
  providedIn: 'root'
})
export class AdvancedEncryptionService {
  private isInitialized = false;
  private cryptoKey: CryptoKey | null = null;
  private encryptionCache = new Map<string, EncryptionResult>();
  private metrics$ = new BehaviorSubject<EncryptionMetrics>(this.getInitialMetrics());

  private readonly config: EncryptionConfig = {
    algorithm: 'AES-GCM',
    keyLength: 256,
    ivLength: 12,
    tagLength: 16
  };

  private readonly CACHE_TTL = 5 * 60 * 1000; // 5 minutes
  private readonly MAX_CACHE_SIZE = 100;

  constructor() {
    this.initialize();
  }

  /**
   * Initialize encryption service
   */
  private async initialize(): Promise<void> {
    try {
      if (!window.crypto || !window.crypto.subtle) {
        throw new Error('WebCrypto API not available');
      }

      await this.generateOrLoadKey();
      this.isInitialized = true;
      this.setupPerformanceMonitoring();

      console.log('🔐 Advanced Encryption Service initialized with AES-256-GCM');
    } catch (error) {
      console.error('Failed to initialize encryption service:', error);
      throw error;
    }
  }

  /**
   * Generate or load encryption key
   */
  private async generateOrLoadKey(): Promise<void> {
    try {
      // Try to load existing key from secure storage
      const existingKey = await this.loadKeyFromStorage();

      if (existingKey) {
        this.cryptoKey = existingKey;
        return;
      }

      // Generate new key
      this.cryptoKey = await window.crypto.subtle.generateKey(
        {
          name: this.config.algorithm,
          length: this.config.keyLength
        },
        true, // extractable
        ['encrypt', 'decrypt']
      );

      // Store key securely
      await this.storeKeySecurely(this.cryptoKey);

    } catch (error) {
      console.error('Key generation failed:', error);
      throw error;
    }
  }

  /**
   * Encrypt data with performance monitoring
   */
  async encrypt(data: string, useCache: boolean = true): Promise<EncryptionResult> {
    const startTime = performance.now();

    try {
      if (!this.isInitialized || !this.cryptoKey) {
        await this.initialize();
      }

      // Check cache first
      if (useCache) {
        const cached = this.getCachedResult(data, 'encrypt');
        if (cached) {
          this.updateMetrics('cacheHit', performance.now() - startTime);
          return cached;
        }
      }

      // Generate random IV
      const iv = window.crypto.getRandomValues(new Uint8Array(this.config.ivLength));

      // Encrypt data
      const encodedData = new TextEncoder().encode(data);
      const encryptedBuffer = await window.crypto.subtle.encrypt(
        {
          name: this.config.algorithm,
          iv: iv
        },
        this.cryptoKey!,
        encodedData
      );

      const result: EncryptionResult = {
        data: this.arrayBufferToBase64(encryptedBuffer),
        iv: this.arrayBufferToBase64(iv),
        timestamp: Date.now()
      };

      // Cache result
      if (useCache && this.encryptionCache.size < this.MAX_CACHE_SIZE) {
        this.encryptionCache.set(this.createCacheKey(data, 'encrypt'), result);
      }

      const encryptTime = performance.now() - startTime;
      this.updateMetrics('encrypt', encryptTime);

      return result;

    } catch (error) {
      this.updateMetrics('error', performance.now() - startTime);
      console.error('Encryption failed:', error);
      throw error;
    }
  }

  /**
   * Decrypt data with performance monitoring
   */
  async decrypt(encryptedResult: EncryptionResult): Promise<string> {
    const startTime = performance.now();

    try {
      if (!this.isInitialized || !this.cryptoKey) {
        await this.initialize();
      }

      // Check cache
      const cacheKey = this.createCacheKey(encryptedResult.data, 'decrypt');
      const cached = this.encryptionCache.get(cacheKey);
      if (cached) {
        this.updateMetrics('cacheHit', performance.now() - startTime);
        return cached.data; // In this case, cached.data contains the decrypted string
      }

      // Decrypt data
      const encryptedData = this.base64ToArrayBuffer(encryptedResult.data);
      const iv = this.base64ToArrayBuffer(encryptedResult.iv);

      const decryptedBuffer = await window.crypto.subtle.decrypt(
        {
          name: this.config.algorithm,
          iv: iv
        },
        this.cryptoKey!,
        encryptedData
      );

      const decryptedText = new TextDecoder().decode(decryptedBuffer);

      const decryptTime = performance.now() - startTime;
      this.updateMetrics('decrypt', decryptTime);

      return decryptedText;

    } catch (error) {
      this.updateMetrics('error', performance.now() - startTime);
      console.error('Decryption failed:', error);
      throw error;
    }
  }

  /**
   * Generate secure hash
   */
  async generateHash(data: string, algorithm: 'SHA-256' | 'SHA-384' | 'SHA-512' = 'SHA-256'): Promise<string> {
    try {
      const encodedData = new TextEncoder().encode(data);
      const hashBuffer = await window.crypto.subtle.digest(algorithm, encodedData);
      return this.arrayBufferToBase64(hashBuffer);
    } catch (error) {
      console.error('Hash generation failed:', error);
      throw error;
    }
  }

  /**
   * Generate secure random bytes
   */
  generateRandomBytes(length: number): Uint8Array {
    return window.crypto.getRandomValues(new Uint8Array(length));
  }

  /**
   * Generate secure random string
   */
  generateRandomString(length: number): string {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    const randomBytes = this.generateRandomBytes(length);

    return Array.from(randomBytes)
      .map(byte => chars[byte % chars.length])
      .join('');
  }

  /**
   * Get encryption metrics
   */
  getMetrics(): Observable<EncryptionMetrics> {
    return this.metrics$.asObservable();
  }

  /**
   * Clear encryption cache
   */
  clearCache(): void {
    this.encryptionCache.clear();
    console.log('🧹 Encryption cache cleared');
  }

  /**
   * Get cache statistics
   */
  getCacheStats(): { size: number; maxSize: number; hitRate: number } {
    const currentMetrics = this.metrics$.value;
    const totalRequests = currentMetrics.cacheHits + currentMetrics.cacheMisses;
    const hitRate = totalRequests > 0 ? currentMetrics.cacheHits / totalRequests : 0;

    return {
      size: this.encryptionCache.size,
      maxSize: this.MAX_CACHE_SIZE,
      hitRate: hitRate
    };
  }

  // Private helper methods

  private getCachedResult(data: string, operation: 'encrypt' | 'decrypt'): EncryptionResult | null {
    const cacheKey = this.createCacheKey(data, operation);
    const cached = this.encryptionCache.get(cacheKey);

    if (cached && Date.now() - cached.timestamp < this.CACHE_TTL) {
      return cached;
    }

    if (cached) {
      this.encryptionCache.delete(cacheKey);
    }

    return null;
  }

  private createCacheKey(data: string, operation: string): string {
    // Create a simple hash for cache key
    let hash = 0;
    for (let i = 0; i < data.length; i++) {
      const char = data.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }
    return `${operation}_${hash.toString(36)}`;
  }

  private arrayBufferToBase64(buffer: ArrayBuffer): string {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary);
  }

  private base64ToArrayBuffer(base64: string): ArrayBuffer {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
      bytes[i] = binary.charCodeAt(i);
    }
    return bytes.buffer;
  }

  private async loadKeyFromStorage(): Promise<CryptoKey | null> {
    try {
      const keyData = localStorage.getItem('encryption_key');
      if (!keyData) return null;

      const keyBuffer = this.base64ToArrayBuffer(keyData);
      return await window.crypto.subtle.importKey(
        'raw',
        keyBuffer,
        { name: this.config.algorithm },
        true,
        ['encrypt', 'decrypt']
      );
    } catch (error) {
      console.warn('Failed to load key from storage:', error);
      return null;
    }
  }

  private async storeKeySecurely(key: CryptoKey): Promise<void> {
    try {
      const exportedKey = await window.crypto.subtle.exportKey('raw', key);
      const keyBase64 = this.arrayBufferToBase64(exportedKey);
      localStorage.setItem('encryption_key', keyBase64);
    } catch (error) {
      console.warn('Failed to store key securely:', error);
    }
  }

  private updateMetrics(operation: 'encrypt' | 'decrypt' | 'cacheHit' | 'error', duration: number): void {
    const currentMetrics = this.metrics$.value;

    const newMetrics: EncryptionMetrics = {
      ...currentMetrics,
      lastOperation: Date.now()
    };

    switch (operation) {
      case 'encrypt':
        newMetrics.operationsCount++;
        newMetrics.averageEncryptTime = this.calculateMovingAverage(
          currentMetrics.averageEncryptTime,
          duration,
          currentMetrics.operationsCount
        );
        break;

      case 'decrypt':
        newMetrics.operationsCount++;
        newMetrics.averageDecryptTime = this.calculateMovingAverage(
          currentMetrics.averageDecryptTime,
          duration,
          currentMetrics.operationsCount
        );
        break;

      case 'cacheHit':
        newMetrics.cacheHits++;
        break;

      case 'error':
        newMetrics.errorCount++;
        newMetrics.cacheMisses++;
        break;
    }

    this.metrics$.next(newMetrics);
  }

  private calculateMovingAverage(currentAvg: number, newValue: number, count: number): number {
    return ((currentAvg * (count - 1)) + newValue) / count;
  }

  private getInitialMetrics(): EncryptionMetrics {
    return {
      operationsCount: 0,
      averageEncryptTime: 0,
      averageDecryptTime: 0,
      cacheHits: 0,
      cacheMisses: 0,
      errorCount: 0,
      lastOperation: 0
    };
  }

  private setupPerformanceMonitoring(): void {
    // Monitor cache size and performance every 30 seconds
    setInterval(() => {
      const metrics = this.metrics$.value;
      const cacheStats = this.getCacheStats();

      if (metrics.operationsCount > 0) {
        console.log('🔐 Encryption Performance:', {
          operations: metrics.operationsCount,
          avgEncryptTime: `${metrics.averageEncryptTime.toFixed(2)}ms`,
          avgDecryptTime: `${metrics.averageDecryptTime.toFixed(2)}ms`,
          cacheHitRate: `${(cacheStats.hitRate * 100).toFixed(1)}%`,
          cacheSize: `${cacheStats.size}/${cacheStats.maxSize}`,
          errors: metrics.errorCount
        });
      }

      // Clean old cache entries
      this.cleanupCache();
    }, 30000);
  }

  private cleanupCache(): void {
    const now = Date.now();
    const keysToDelete: string[] = [];

    this.encryptionCache.forEach((value, key) => {
      if (now - value.timestamp > this.CACHE_TTL) {
        keysToDelete.push(key);
      }
    });

    keysToDelete.forEach(key => this.encryptionCache.delete(key));

    if (keysToDelete.length > 0) {
      console.log(`🧹 Cleaned ${keysToDelete.length} expired cache entries`);
    }
  }
}