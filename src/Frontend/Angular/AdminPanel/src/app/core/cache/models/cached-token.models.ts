export interface CachedToken {
  value: string;
  type: 'access' | 'refresh';
  expiresAt: number;
  createdAt: number;
  refreshThreshold: number; // When to refresh (ms before expiry)
  isValid: boolean;
  source: 'memory' | 'session' | 'encrypted'; // Source of token
}

export interface TokenCacheOptions {
  ttl?: number; // Time to live in milliseconds
  refreshBuffer?: number; // Refresh buffer time in milliseconds
  persistToDisk?: boolean; // Whether to persist to disk storage
  encryptionLevel?: 'none' | 'basic' | 'advanced';
}

export interface TokenCacheStats {
  hitCount: number;
  missCount: number;
  hitRate: number;
  totalRequests: number;
  cacheSize: number;
  lastCleanup: number;
  averageResponseTime: number;
}

export interface TokenValidationResult {
  isValid: boolean;
  expiresIn: number; // Milliseconds until expiry
  shouldRefresh: boolean;
  source: 'cache' | 'storage' | 'network';
  responseTime: number; // Time taken to validate
}

export const TOKEN_CACHE_KEYS = {
  ACCESS_TOKEN: 'auth_access_token_cached',
  REFRESH_TOKEN: 'auth_refresh_token_cached',
  USER_INFO: 'auth_user_info_cached',
  PERMISSIONS: 'auth_permissions_cached'
} as const;

export const TOKEN_CACHE_DEFAULTS = {
  ACCESS_TOKEN_TTL: 15 * 60 * 1000, // 15 minutes
  REFRESH_TOKEN_TTL: 7 * 24 * 60 * 60 * 1000, // 7 days
  REFRESH_BUFFER: 2 * 60 * 1000, // 2 minutes before expiry
  MAX_CACHE_SIZE: 50,
  CLEANUP_INTERVAL: 5 * 60 * 1000 // 5 minutes
} as const;

export const CACHE_PERFORMANCE_TARGETS = {
  CACHE_HIT_RESPONSE_TIME: 1, // ms
  STORAGE_HIT_RESPONSE_TIME: 5, // ms
  NETWORK_RESPONSE_TIME: 100, // ms
  TARGET_HIT_RATE: 0.95 // 95%
} as const;