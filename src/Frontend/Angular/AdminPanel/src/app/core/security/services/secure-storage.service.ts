import { Injectable, inject } from '@angular/core';
import {
  ISecureStorage,
  StorageOptions,
  SecurityMetrics,
  StorageType,
  StorageSecurityConfig,
  EncryptionResult
} from '../interfaces/storage-security.interface';
import { EncryptionService } from './encryption.service';
import { MemoryStorageProvider } from './storage-providers/memory-storage.provider';
import { SessionStorageProvider } from './storage-providers/session-storage.provider';

/**
 * Enterprise-grade secure storage service
 * Implements multiple storage layers with encryption and integrity checking
 */
@Injectable({
  providedIn: 'root'
})
export class SecureStorageService implements ISecureStorage {
  private readonly encryptionService = inject(EncryptionService);
  private readonly memoryStorage = inject(MemoryStorageProvider);
  private readonly sessionStorage = inject(SessionStorageProvider);

  private readonly defaultConfig: StorageSecurityConfig = {
    defaultStorage: 'memory',
    securityLevel: 'high',
    autoCleanup: true,
    cleanupInterval: 5 * 60 * 1000, // 5 minutes
    maxFailedAttempts: 5,
    lockoutDuration: 15 * 60 * 1000 // 15 minutes
  };

  private failedAttempts = 0;
  private lockoutUntil = 0;
  private cleanupTimer?: number;

  constructor() {
    this.initializeService();
  }

  /**
   * Initialize secure storage service
   */
  private async initializeService(): Promise<void> {
    // Start cleanup timer if auto cleanup is enabled
    if (this.defaultConfig.autoCleanup) {
      this.startCleanupTimer();
    }

    // Validate existing storage integrity
    await this.validateStorageIntegrity();
  }

  /**
   * Store data securely with encryption
   */
  async setItem<T>(key: string, value: T, options: StorageOptions = {}): Promise<boolean> {
    if (this.isLockedOut()) {
      console.warn('Storage is locked out due to security violations');
      return false;
    }

    try {
      const mergedOptions = this.mergeOptions(options);
      let dataToStore = value;

      // Encrypt data if required
      if (mergedOptions.encrypted && this.encryptionService.isReady()) {
        const serializedData = JSON.stringify(value);
        const encryptionResult = await this.encryptionService.encrypt(serializedData);
        dataToStore = encryptionResult as any;
      }

      // Add integrity hash if required
      if (mergedOptions.integrityCheck) {
        const dataString = JSON.stringify(dataToStore);
        const integrityHash = await this.encryptionService.generateIntegrityHash(dataString);
        dataToStore = {
          data: dataToStore,
          integrity: integrityHash
        } as any;
      }

      // Try primary storage
      const primaryStorage = this.getStorageProvider(mergedOptions.fallbackStorage || this.defaultConfig.defaultStorage);
      if (primaryStorage.setItem(key, dataToStore, mergedOptions)) {
        return true;
      }

      // Try fallback storage
      if (mergedOptions.fallbackStorage) {
        const fallbackStorage = this.getStorageProvider('memory');
        return fallbackStorage.setItem(key, dataToStore, mergedOptions);
      }

      return false;
    } catch (error) {
      console.error('SecureStorage setItem failed:', error);
      this.recordFailedAttempt();
      return false;
    }
  }

  /**
   * Retrieve data securely with decryption
   */
  async getItem<T>(key: string): Promise<T | null> {
    if (this.isLockedOut()) {
      console.warn('Storage is locked out due to security violations');
      return null;
    }

    try {
      // Try all available storage providers
      const storageTypes: StorageType[] = ['memory', 'sessionStorage'];

      for (const storageType of storageTypes) {
        const storageProvider = this.getStorageProvider(storageType);
        let storedData = storageProvider.getItem(key);

        if (storedData === null) {
          continue;
        }

        // Check integrity if present
        if (this.hasIntegrityCheck(storedData)) {
          const { data, integrity } = storedData as any;
          const dataString = JSON.stringify(data);
          const isValid = await this.encryptionService.verifyIntegrity(dataString, integrity);

          if (!isValid) {
            console.warn(`Integrity check failed for key: ${key}`);
            storageProvider.removeItem(key);
            this.recordFailedAttempt();
            continue;
          }

          storedData = data;
        }

        // Decrypt if encrypted
        if (this.isEncryptedData(storedData)) {
          try {
            const encryptionResult = storedData as EncryptionResult;
            const decryptedString = await this.encryptionService.decrypt(encryptionResult);
            return JSON.parse(decryptedString);
          } catch (error) {
            console.warn(`Decryption failed for key: ${key}`);
            storageProvider.removeItem(key);
            this.recordFailedAttempt();
            continue;
          }
        }

        return storedData as T;
      }

      return null;
    } catch (error) {
      console.error('SecureStorage getItem failed:', error);
      this.recordFailedAttempt();
      return null;
    }
  }

  /**
   * Remove item from all storage providers
   */
  async removeItem(key: string): Promise<boolean> {
    if (this.isLockedOut()) {
      return false;
    }

    try {
      let success = false;

      // Remove from all storage providers
      const storageTypes: StorageType[] = ['memory', 'sessionStorage'];
      for (const storageType of storageTypes) {
        const storageProvider = this.getStorageProvider(storageType);
        if (storageProvider.removeItem(key)) {
          success = true;
        }
      }

      return success;
    } catch (error) {
      console.error('SecureStorage removeItem failed:', error);
      return false;
    }
  }

  /**
   * Clear all stored data
   */
  async clear(): Promise<boolean> {
    if (this.isLockedOut()) {
      return false;
    }

    try {
      let success = true;

      // Clear all storage providers
      const storageTypes: StorageType[] = ['memory', 'sessionStorage'];
      for (const storageType of storageTypes) {
        const storageProvider = this.getStorageProvider(storageType);
        if (!storageProvider.clear()) {
          success = false;
        }
      }

      return success;
    } catch (error) {
      console.error('SecureStorage clear failed:', error);
      return false;
    }
  }

  /**
   * Check if key exists in any storage
   */
  async hasItem(key: string): Promise<boolean> {
    if (this.isLockedOut()) {
      return false;
    }

    const storageTypes: StorageType[] = ['memory', 'sessionStorage'];
    for (const storageType of storageTypes) {
      const storageProvider = this.getStorageProvider(storageType);
      if (storageProvider.hasItem(key)) {
        return true;
      }
    }

    return false;
  }

  /**
   * Get combined security metrics
   */
  async getMetrics(): Promise<SecurityMetrics> {
    const memoryMetrics = this.memoryStorage.getMetrics();
    const sessionMetrics = this.sessionStorage.getMetrics();

    return {
      accessAttempts: memoryMetrics.accessAttempts + sessionMetrics.accessAttempts,
      decryptionFailures: memoryMetrics.decryptionFailures + sessionMetrics.decryptionFailures,
      lastAccess: Math.max(memoryMetrics.lastAccess, sessionMetrics.lastAccess),
      integrityViolations: memoryMetrics.integrityViolations + sessionMetrics.integrityViolations
    };
  }

  /**
   * Validate integrity of all storage providers
   */
  async validateIntegrity(): Promise<boolean> {
    try {
      const memoryValid = this.memoryStorage.validateIntegrity();
      const sessionValid = this.sessionStorage.validateIntegrity();

      return memoryValid && sessionValid;
    } catch (error) {
      console.error('Storage integrity validation failed:', error);
      return false;
    }
  }

  /**
   * Get available storage types
   */
  getAvailableStorageTypes(): StorageType[] {
    const available: StorageType[] = [];

    if (MemoryStorageProvider.isAvailable()) {
      available.push('memory');
    }

    if (SessionStorageProvider.isAvailable()) {
      available.push('sessionStorage');
    }

    return available;
  }

  /**
   * Perform manual cleanup of expired items
   */
  async performCleanup(): Promise<{ memory: number; session: number }> {
    const memoryCleanedCount = this.memoryStorage.cleanup();
    const sessionCleanedCount = this.sessionStorage.cleanup();

    return {
      memory: memoryCleanedCount,
      session: sessionCleanedCount
    };
  }

  /**
   * Get storage provider by type
   */
  private getStorageProvider(type: StorageType): MemoryStorageProvider | SessionStorageProvider {
    switch (type) {
      case 'sessionStorage':
        return this.sessionStorage;
      case 'memory':
      default:
        return this.memoryStorage;
    }
  }

  /**
   * Merge options with defaults
   */
  private mergeOptions(options: StorageOptions): Required<StorageOptions> {
    return {
      encrypted: options.encrypted ?? true,
      compressed: options.compressed ?? false,
      expiresIn: options.expiresIn ?? 3600000,
      integrityCheck: options.integrityCheck ?? true,
      fallbackStorage: options.fallbackStorage ?? 'memory'
    };
  }

  /**
   * Check if data has integrity check
   */
  private hasIntegrityCheck(data: any): boolean {
    return data &&
           typeof data === 'object' &&
           'data' in data &&
           'integrity' in data &&
           typeof data.integrity === 'string';
  }

  /**
   * Check if data is encrypted
   */
  private isEncryptedData(data: any): boolean {
    return data &&
           typeof data === 'object' &&
           'data' in data &&
           'iv' in data &&
           'tag' in data &&
           typeof data.data === 'string' &&
           typeof data.iv === 'string' &&
           typeof data.tag === 'string';
  }

  /**
   * Record failed attempt and implement lockout
   */
  private recordFailedAttempt(): void {
    this.failedAttempts++;

    if (this.failedAttempts >= this.defaultConfig.maxFailedAttempts) {
      this.lockoutUntil = Date.now() + this.defaultConfig.lockoutDuration;
      console.warn(`Storage locked out for ${this.defaultConfig.lockoutDuration / 1000} seconds due to security violations`);
    }
  }

  /**
   * Check if storage is currently locked out
   */
  private isLockedOut(): boolean {
    if (this.lockoutUntil > Date.now()) {
      return true;
    }

    // Reset if lockout period has passed
    if (this.lockoutUntil > 0) {
      this.failedAttempts = 0;
      this.lockoutUntil = 0;
    }

    return false;
  }

  /**
   * Start automatic cleanup timer
   */
  private startCleanupTimer(): void {
    if (typeof window !== 'undefined') {
      this.cleanupTimer = window.setInterval(() => {
        this.performCleanup().catch(error => {
          console.error('Automatic cleanup failed:', error);
        });
      }, this.defaultConfig.cleanupInterval);
    }
  }

  /**
   * Validate storage integrity on initialization
   */
  private async validateStorageIntegrity(): Promise<void> {
    try {
      const isValid = await this.validateIntegrity();
      if (!isValid) {
        console.warn('Storage integrity validation failed, clearing potentially corrupted data');
        await this.clear();
      }
    } catch (error) {
      console.error('Storage integrity validation error:', error);
    }
  }

  /**
   * Cleanup resources
   */
  destroy(): void {
    if (this.cleanupTimer) {
      clearInterval(this.cleanupTimer);
    }
  }
}