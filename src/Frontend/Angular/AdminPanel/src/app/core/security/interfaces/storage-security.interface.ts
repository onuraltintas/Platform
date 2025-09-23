/**
 * Secure Storage Interfaces for Token Management
 * Implements enterprise-grade security patterns for sensitive data storage
 */

export interface StorageOptions {
  /** Enable encryption for stored data */
  encrypted?: boolean;
  /** Enable compression for large data */
  compressed?: boolean;
  /** Storage expiration time in milliseconds */
  expiresIn?: number;
  /** Enable integrity checking */
  integrityCheck?: boolean;
  /** Fallback storage type if primary fails */
  fallbackStorage?: StorageType;
}

export interface StorageItem<T = any> {
  /** Stored data */
  data: T;
  /** Creation timestamp */
  createdAt: number;
  /** Expiration timestamp */
  expiresAt?: number;
  /** Data integrity hash */
  integrity?: string;
  /** Encryption metadata */
  encrypted?: boolean;
  /** Version for data migration */
  version: string;
}

export interface EncryptionResult {
  /** Encrypted data */
  data: string;
  /** Initialization vector */
  iv: string;
  /** Authentication tag */
  tag: string;
}

export interface SecurityMetrics {
  /** Storage access attempts */
  accessAttempts: number;
  /** Failed decryption attempts */
  decryptionFailures: number;
  /** Last access timestamp */
  lastAccess: number;
  /** Storage integrity violations */
  integrityViolations: number;
}

export type StorageType = 'memory' | 'sessionStorage' | 'localStorage' | 'indexedDB';

export type SecurityLevel = 'low' | 'medium' | 'high' | 'critical';

export interface StorageSecurityConfig {
  /** Default storage type */
  defaultStorage: StorageType;
  /** Security level for different data types */
  securityLevel: SecurityLevel;
  /** Enable automatic cleanup */
  autoCleanup: boolean;
  /** Cleanup interval in milliseconds */
  cleanupInterval: number;
  /** Maximum failed attempts before lockout */
  maxFailedAttempts: number;
  /** Lockout duration in milliseconds */
  lockoutDuration: number;
}

export interface ISecureStorage {
  /** Store data securely */
  setItem<T>(key: string, value: T, options?: StorageOptions): Promise<boolean>;

  /** Retrieve data securely */
  getItem<T>(key: string): Promise<T | null>;

  /** Remove specific item */
  removeItem(key: string): Promise<boolean>;

  /** Clear all stored data */
  clear(): Promise<boolean>;

  /** Check if key exists */
  hasItem(key: string): Promise<boolean>;

  /** Get storage metrics */
  getMetrics(): Promise<SecurityMetrics>;

  /** Validate storage integrity */
  validateIntegrity(): Promise<boolean>;

  /** Get available storage types */
  getAvailableStorageTypes(): StorageType[];
}

export interface ITokenStorage extends ISecureStorage {
  /** Store access token */
  setAccessToken(token: string, expiresIn?: number): Promise<boolean>;

  /** Store refresh token */
  setRefreshToken(token: string, expiresIn?: number): Promise<boolean>;

  /** Get access token */
  getAccessToken(): Promise<string | null>;

  /** Get refresh token */
  getRefreshToken(): Promise<string | null>;

  /** Clear all tokens */
  clearTokens(): Promise<boolean>;

  /** Check if tokens are valid */
  areTokensValid(): Promise<boolean>;

  /** Get token expiration info */
  getTokenExpiration(): Promise<{ accessToken?: number; refreshToken?: number }>;
}

export interface TokenMetadata {
  /** Token type */
  type: 'access' | 'refresh';
  /** Creation timestamp */
  createdAt: number;
  /** Expiration timestamp */
  expiresAt: number;
  /** Token hash for integrity */
  hash: string;
  /** Security level */
  securityLevel: SecurityLevel;
}