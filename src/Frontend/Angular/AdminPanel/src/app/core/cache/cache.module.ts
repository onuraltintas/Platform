import { NgModule, APP_INITIALIZER } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LRUCacheService } from './services/lru-cache.service';
import { OptimizedTokenService } from './services/optimized-token.service';
import { environment } from '../../../environments/environment';

/**
 * Cache initialization factory
 * Warms up token cache on application startup
 */
export function cacheInitializerFactory(tokenService: OptimizedTokenService) {
  return (): Promise<void> => {
    if (environment.performance.cacheWarmupOnInit) {
      console.log('🚀 Warming up token cache...');
      return tokenService.warmUpCache().then(() => {
        console.log('✅ Token cache warmed up successfully');
      }).catch((error) => {
        console.warn('⚠️ Token cache warmup failed:', error);
      });
    }
    return Promise.resolve();
  };
}

/**
 * Cache Module with Performance Optimization
 * Provides memory-first caching services
 */
@NgModule({
  imports: [CommonModule],
  providers: [
    LRUCacheService,
    OptimizedTokenService,
    {
      provide: APP_INITIALIZER,
      useFactory: cacheInitializerFactory,
      deps: [OptimizedTokenService],
      multi: true
    }
  ]
})
export class CacheModule {
  constructor() {
    if (environment.performance.performanceMonitoring) {
      this.setupPerformanceMonitoring();
    }
  }

  private setupPerformanceMonitoring(): void {
    // Log cache performance metrics every 30 seconds
    setInterval(() => {
      const tokenService = new OptimizedTokenService();
      const stats = tokenService.getCacheStats();

      if (stats.totalRequests > 0) {
        console.log('📊 Cache Performance:', {
          hitRate: `${(stats.hitRate * 100).toFixed(1)}%`,
          avgResponseTime: `${stats.averageResponseTime.toFixed(2)}ms`,
          totalRequests: stats.totalRequests,
          cacheSize: stats.cacheSize
        });
      }
    }, 30000);
  }
}