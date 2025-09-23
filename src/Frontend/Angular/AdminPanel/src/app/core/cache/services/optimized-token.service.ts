import { Injectable, inject } from '@angular/core';
import { jwtDecode } from 'jwt-decode';
import { TokenPayload } from '../../auth/models/auth.models';
import { SecureTokenStorageService } from '../../security/services/secure-token-storage.service';
import { LRUCacheService } from './lru-cache.service';
import {
  CachedToken,
  TokenCacheStats,
  TokenValidationResult,
  TOKEN_CACHE_KEYS,
  TOKEN_CACHE_DEFAULTS,
  CACHE_PERFORMANCE_TARGETS
} from '../models/cached-token.models';

/**
 * Optimized Token Service with Memory-First Architecture
 * Performance targets: <1ms for cache hits, <5ms for storage hits
 */
@Injectable({
  providedIn: 'root'
})
export class OptimizedTokenService {
  private readonly secureTokenStorage = inject(SecureTokenStorageService);
  private readonly cache = inject(LRUCacheService<CachedToken>);

  // Performance tracking
  private stats: TokenCacheStats = {
    hitCount: 0,
    missCount: 0,
    hitRate: 0,
    totalRequests: 0,
    cacheSize: 0,
    lastCleanup: Date.now(),
    averageResponseTime: 0
  };

  private performanceTimes: number[] = [];

  constructor() {
    this.setupPeriodicCleanup();
  }

  /**
   * Get access token with memory-first strategy
   * Target: <1ms for cache hits
   */
  getAccessToken(): string | null {
    const startTime = performance.now();

    try {
      this.stats.totalRequests++;

      // STEP 1: Memory cache check (0-1ms)
      const cached = this.cache.get(TOKEN_CACHE_KEYS.ACCESS_TOKEN);
      if (cached && this.isTokenValid(cached)) {
        this.recordCacheHit(startTime);
        return cached.value;
      }

      // STEP 2: Cache miss - return null for now, load async
      this.recordCacheMiss(startTime);
      this.loadTokenFromStorage(TOKEN_CACHE_KEYS.ACCESS_TOKEN);
      return null;

    } catch (error) {
      console.error('Token retrieval failed:', error);
      this.recordCacheMiss(startTime);
      return null;
    }
  }

  /**
   * Get access token synchronously (for immediate use)
   * Falls back to basic validation if cache miss
   */
  getAccessTokenSync(): string | null {
    const cached = this.cache.get(TOKEN_CACHE_KEYS.ACCESS_TOKEN);

    if (cached && this.isTokenValidBasic(cached.value)) {
      return cached.value;
    }

    return null;
  }

  /**
   * Get refresh token with same strategy
   */
  getRefreshToken(): string | null {
    const startTime = performance.now();

    try {
      this.stats.totalRequests++;

      const cached = this.cache.get(TOKEN_CACHE_KEYS.REFRESH_TOKEN);
      if (cached && this.isTokenValid(cached)) {
        this.recordCacheHit(startTime);
        return cached.value;
      }

      this.recordCacheMiss(startTime);
      this.loadTokenFromStorage(TOKEN_CACHE_KEYS.REFRESH_TOKEN);
      return null;

    } catch (error) {
      console.error('Refresh token retrieval failed:', error);
      this.recordCacheMiss(startTime);
      return null;
    }
  }

  /**
   * Set tokens with intelligent caching
   */
  async setTokens(accessToken: string, refreshToken: string, expiresIn?: number): Promise<boolean> {
    try {
      const now = Date.now();
      const accessTokenExpiry = expiresIn ? now + (expiresIn * 1000) : now + TOKEN_CACHE_DEFAULTS.ACCESS_TOKEN_TTL;

      // Create cached tokens
      const cachedAccessToken: CachedToken = {
        value: accessToken,
        type: 'access',
        expiresAt: accessTokenExpiry,
        createdAt: now,
        refreshThreshold: accessTokenExpiry - TOKEN_CACHE_DEFAULTS.REFRESH_BUFFER,
        isValid: true,
        source: 'memory'
      };

      const cachedRefreshToken: CachedToken = {
        value: refreshToken,
        type: 'refresh',
        expiresAt: now + TOKEN_CACHE_DEFAULTS.REFRESH_TOKEN_TTL,
        createdAt: now,
        refreshThreshold: now + TOKEN_CACHE_DEFAULTS.REFRESH_TOKEN_TTL - TOKEN_CACHE_DEFAULTS.REFRESH_BUFFER,
        isValid: true,
        source: 'memory'
      };

      // STEP 1: Immediate memory cache (0-1ms)
      this.cache.set(TOKEN_CACHE_KEYS.ACCESS_TOKEN, cachedAccessToken, {
        ttl: TOKEN_CACHE_DEFAULTS.ACCESS_TOKEN_TTL
      });

      this.cache.set(TOKEN_CACHE_KEYS.REFRESH_TOKEN, cachedRefreshToken, {
        ttl: TOKEN_CACHE_DEFAULTS.REFRESH_TOKEN_TTL
      });

      // STEP 2: Background storage (non-blocking)
      this.persistToStorageAsync(accessToken, refreshToken, expiresIn);

      return true;
    } catch (error) {
      console.error('Failed to set tokens:', error);
      return false;
    }
  }

  /**
   * Clear all tokens
   */
  async clearTokens(): Promise<boolean> {
    try {
      // Clear memory cache immediately
      this.cache.delete(TOKEN_CACHE_KEYS.ACCESS_TOKEN);
      this.cache.delete(TOKEN_CACHE_KEYS.REFRESH_TOKEN);
      this.cache.delete(TOKEN_CACHE_KEYS.USER_INFO);
      this.cache.delete(TOKEN_CACHE_KEYS.PERMISSIONS);

      // Clear storage in background
      this.clearStorageAsync();

      return true;
    } catch (error) {
      console.error('Failed to clear tokens:', error);
      return false;
    }
  }

  /**
   * Validate token with performance optimization
   */
  validateToken(token: string): TokenValidationResult {
    const startTime = performance.now();

    try {
      // Quick format check
      if (!token || token.split('.').length !== 3) {
        return {
          isValid: false,
          expiresIn: 0,
          shouldRefresh: false,
          source: 'cache',
          responseTime: performance.now() - startTime
        };
      }

      // Decode and validate
      const payload = this.decodeToken(token);
      if (!payload || !payload.exp) {
        return {
          isValid: false,
          expiresIn: 0,
          shouldRefresh: false,
          source: 'cache',
          responseTime: performance.now() - startTime
        };
      }

      const now = Date.now();
      const expirationTime = payload.exp * 1000;
      const expiresIn = expirationTime - now;
      const bufferTime = TOKEN_CACHE_DEFAULTS.REFRESH_BUFFER;

      return {
        isValid: expiresIn > 0,
        expiresIn: Math.max(0, expiresIn),
        shouldRefresh: expiresIn < bufferTime && expiresIn > 0,
        source: 'cache',
        responseTime: performance.now() - startTime
      };

    } catch (error) {
      return {
        isValid: false,
        expiresIn: 0,
        shouldRefresh: false,
        source: 'cache',
        responseTime: performance.now() - startTime
      };
    }
  }

  /**
   * Check if token should be refreshed soon
   */
  shouldRefreshToken(): boolean {
    const cached = this.cache.get(TOKEN_CACHE_KEYS.ACCESS_TOKEN);
    if (!cached) return false;

    return Date.now() > cached.refreshThreshold;
  }

  /**
   * Get cache statistics for monitoring
   */
  getCacheStats(): TokenCacheStats {
    const cacheStats = this.cache.getStats();

    return {
      ...this.stats,
      hitRate: this.stats.totalRequests > 0 ? this.stats.hitCount / this.stats.totalRequests : 0,
      cacheSize: cacheStats.size,
      averageResponseTime: this.performanceTimes.length > 0
        ? this.performanceTimes.reduce((a, b) => a + b, 0) / this.performanceTimes.length
        : 0
    };
  }

  /**
   * Warm up cache from storage
   */
  async warmUpCache(): Promise<void> {
    try {
      const [accessToken, refreshToken] = await Promise.all([
        this.secureTokenStorage.getAccessToken(),
        this.secureTokenStorage.getRefreshToken()
      ]);

      if (accessToken) {
        const validation = this.validateToken(accessToken);
        if (validation.isValid) {
          const cachedToken: CachedToken = {
            value: accessToken,
            type: 'access',
            expiresAt: Date.now() + validation.expiresIn,
            createdAt: Date.now(),
            refreshThreshold: Date.now() + validation.expiresIn - TOKEN_CACHE_DEFAULTS.REFRESH_BUFFER,
            isValid: true,
            source: 'encrypted'
          };

          this.cache.set(TOKEN_CACHE_KEYS.ACCESS_TOKEN, cachedToken);
        }
      }

      if (refreshToken) {
        const cachedToken: CachedToken = {
          value: refreshToken,
          type: 'refresh',
          expiresAt: Date.now() + TOKEN_CACHE_DEFAULTS.REFRESH_TOKEN_TTL,
          createdAt: Date.now(),
          refreshThreshold: Date.now() + TOKEN_CACHE_DEFAULTS.REFRESH_TOKEN_TTL - TOKEN_CACHE_DEFAULTS.REFRESH_BUFFER,
          isValid: true,
          source: 'encrypted'
        };

        this.cache.set(TOKEN_CACHE_KEYS.REFRESH_TOKEN, cachedToken);
      }
    } catch (error) {
      console.error('Cache warm-up failed:', error);
    }
  }

  // Private methods

  private isTokenValid(cachedToken: CachedToken): boolean {
    const now = Date.now();
    return cachedToken.isValid && now < cachedToken.expiresAt;
  }

  private isTokenValidBasic(token: string): boolean {
    try {
      const payload = this.decodeToken(token);
      if (!payload || !payload.exp) return false;

      return Date.now() < (payload.exp * 1000);
    } catch {
      return false;
    }
  }

  private decodeToken(token: string): TokenPayload | null {
    try {
      return jwtDecode<TokenPayload>(token);
    } catch {
      return null;
    }
  }

  private async loadTokenFromStorage(key: string): Promise<void> {
    try {
      const token = key === TOKEN_CACHE_KEYS.ACCESS_TOKEN
        ? await this.secureTokenStorage.getAccessToken()
        : await this.secureTokenStorage.getRefreshToken();

      if (token) {
        const validation = this.validateToken(token);
        if (validation.isValid) {
          const cachedToken: CachedToken = {
            value: token,
            type: key === TOKEN_CACHE_KEYS.ACCESS_TOKEN ? 'access' : 'refresh',
            expiresAt: Date.now() + validation.expiresIn,
            createdAt: Date.now(),
            refreshThreshold: Date.now() + validation.expiresIn - TOKEN_CACHE_DEFAULTS.REFRESH_BUFFER,
            isValid: true,
            source: 'encrypted'
          };

          this.cache.set(key, cachedToken);
        }
      }
    } catch (error) {
      console.error('Failed to load token from storage:', error);
    }
  }

  private async persistToStorageAsync(accessToken: string, refreshToken: string, expiresIn?: number): Promise<void> {
    // Non-blocking storage operation
    setTimeout(async () => {
      try {
        await Promise.all([
          this.secureTokenStorage.setAccessToken(accessToken, expiresIn),
          this.secureTokenStorage.setRefreshToken(refreshToken)
        ]);
      } catch (error) {
        console.error('Background storage failed:', error);
      }
    }, 0);
  }

  private async clearStorageAsync(): Promise<void> {
    setTimeout(async () => {
      try {
        await this.secureTokenStorage.clearTokens();
      } catch (error) {
        console.error('Background storage clear failed:', error);
      }
    }, 0);
  }

  private recordCacheHit(startTime: number): void {
    const responseTime = performance.now() - startTime;
    this.stats.hitCount++;
    this.recordPerformance(responseTime);
  }

  private recordCacheMiss(startTime: number): void {
    const responseTime = performance.now() - startTime;
    this.stats.missCount++;
    this.recordPerformance(responseTime);
  }

  private recordPerformance(responseTime: number): void {
    this.performanceTimes.push(responseTime);

    // Keep only last 100 measurements
    if (this.performanceTimes.length > 100) {
      this.performanceTimes.shift();
    }

    // Performance alert if too slow
    if (responseTime > CACHE_PERFORMANCE_TARGETS.CACHE_HIT_RESPONSE_TIME) {
      console.warn(`Token cache performance degraded: ${responseTime.toFixed(2)}ms`);
    }
  }

  private setupPeriodicCleanup(): void {
    setInterval(() => {
      const cleaned = this.cache.cleanup();
      this.stats.lastCleanup = Date.now();

      if (cleaned > 0) {
        console.log(`Cache cleanup: removed ${cleaned} expired entries`);
      }
    }, TOKEN_CACHE_DEFAULTS.CLEANUP_INTERVAL);
  }
}