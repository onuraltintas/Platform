import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, BehaviorSubject, from, of, combineLatest } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';

interface LazyModule {
  path: string;
  moduleName: string;
  loadChildren: string;
  estimatedSize: number;
  loadTime: number;
  accessFrequency: number;
  lastAccessed: Date;
  preloadStrategy: 'never' | 'ondemand' | 'hover' | 'viewport' | 'always';
  priority: 'high' | 'medium' | 'low';
}

interface PreloadingStrategy {
  name: string;
  description: string;
  trigger: 'immediate' | 'hover' | 'viewport' | 'idle' | 'network' | 'user-behavior';
  threshold?: number;
  conditions: string[];
  estimatedImprovement: string;
}

interface LazyLoadingMetrics {
  totalRoutes: number;
  lazyRoutes: number;
  eagerRoutes: number;
  averageLoadTime: number;
  preloadingEfficiency: number;
  cacheHitRate: number;
  userNavigationPatterns: NavigationPattern[];
}

interface NavigationPattern {
  fromRoute: string;
  toRoute: string;
  frequency: number;
  averageTime: number;
  likelihood: number;
}

interface OptimizationRecommendation {
  route: string;
  currentStrategy: string;
  recommendedStrategy: string;
  reason: string;
  impact: 'low' | 'medium' | 'high';
  implementation: string;
  estimatedImprovement: string;
}

@Injectable({
  providedIn: 'root'
})
export class LazyLoadingOptimizerService {
  private lazyModules = new BehaviorSubject<Map<string, LazyModule>>(new Map());
  private preloadingStrategies = new BehaviorSubject<PreloadingStrategy[]>([]);
  private navigationPatterns = new BehaviorSubject<NavigationPattern[]>([]);
  private optimizationRecommendations = new BehaviorSubject<OptimizationRecommendation[]>([]);
  private metrics = new BehaviorSubject<LazyLoadingMetrics>({
    totalRoutes: 0,
    lazyRoutes: 0,
    eagerRoutes: 0,
    averageLoadTime: 0,
    preloadingEfficiency: 0,
    cacheHitRate: 0,
    userNavigationPatterns: []
  });

  public lazyModules$ = this.lazyModules.asObservable();
  public strategies$ = this.preloadingStrategies.asObservable();
  public patterns$ = this.navigationPatterns.asObservable();
  public recommendations$ = this.optimizationRecommendations.asObservable();
  public metrics$ = this.metrics.asObservable();

  private routeAccessTracker = new Map<string, Date[]>();
  private preloadCache = new Map<string, Promise<any>>();
  private performanceObserver?: PerformanceObserver;

  constructor(private router: Router) {
    this.initializePreloadingStrategies();
    this.analyzeCurrentRoutes();
    this.initializeNavigationTracking();
    this.initializePerformanceTracking();
  }

  private initializePreloadingStrategies(): void {
    const strategies: PreloadingStrategy[] = [
      {
        name: 'NoPreloading',
        description: 'Load modules only when requested',
        trigger: 'immediate',
        conditions: ['User navigates to route'],
        estimatedImprovement: 'Minimal initial bundle'
      },
      {
        name: 'PreloadAllModules',
        description: 'Preload all lazy modules after initial load',
        trigger: 'immediate',
        conditions: ['Initial app load complete'],
        estimatedImprovement: 'Instant navigation'
      },
      {
        name: 'QuicklinkStrategy',
        description: 'Preload modules when links enter viewport',
        trigger: 'viewport',
        threshold: 0.5,
        conditions: ['Link visible in viewport', 'Network connection good'],
        estimatedImprovement: '70% faster navigation'
      },
      {
        name: 'HoverPreloadStrategy',
        description: 'Preload modules on link hover',
        trigger: 'hover',
        threshold: 200,
        conditions: ['User hovers over link for 200ms'],
        estimatedImprovement: '85% faster navigation'
      },
      {
        name: 'NetworkAwareStrategy',
        description: 'Adapt preloading based on network conditions',
        trigger: 'network',
        conditions: ['Fast network connection', 'Low data usage mode off'],
        estimatedImprovement: 'Smart resource usage'
      },
      {
        name: 'PredictiveStrategy',
        description: 'Preload based on user behavior patterns',
        trigger: 'user-behavior',
        conditions: ['Navigation pattern confidence > 70%'],
        estimatedImprovement: '90% navigation prediction accuracy'
      }
    ];

    this.preloadingStrategies.next(strategies);
  }

  private analyzeCurrentRoutes(): void {
    // Simulate route analysis (in real app, this would analyze router config)
    const mockLazyModules: LazyModule[] = [
      {
        path: 'users',
        moduleName: 'UserManagementModule',
        loadChildren: './features/user-management/user-management.module#UserManagementModule',
        estimatedSize: 250 * 1024, // 250KB
        loadTime: 150,
        accessFrequency: 8,
        lastAccessed: new Date(Date.now() - 2 * 60 * 60 * 1000), // 2 hours ago
        preloadStrategy: 'hover',
        priority: 'high'
      },
      {
        path: 'reports',
        moduleName: 'ReportsModule',
        loadChildren: './features/reports/reports.module#ReportsModule',
        estimatedSize: 450 * 1024, // 450KB
        loadTime: 280,
        accessFrequency: 3,
        lastAccessed: new Date(Date.now() - 24 * 60 * 60 * 1000), // 1 day ago
        preloadStrategy: 'viewport',
        priority: 'medium'
      },
      {
        path: 'settings',
        moduleName: 'SettingsModule',
        loadChildren: './features/settings/settings.module#SettingsModule',
        estimatedSize: 180 * 1024, // 180KB
        loadTime: 120,
        accessFrequency: 1,
        lastAccessed: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000), // 1 week ago
        preloadStrategy: 'never',
        priority: 'low'
      },
      {
        path: 'analytics',
        moduleName: 'AnalyticsModule',
        loadChildren: './features/analytics/analytics.module#AnalyticsModule',
        estimatedSize: 600 * 1024, // 600KB
        loadTime: 350,
        accessFrequency: 5,
        lastAccessed: new Date(Date.now() - 6 * 60 * 60 * 1000), // 6 hours ago
        preloadStrategy: 'ondemand',
        priority: 'medium'
      }
    ];

    const modulesMap = new Map<string, LazyModule>();
    mockLazyModules.forEach(module => modulesMap.set(module.path, module));
    this.lazyModules.next(modulesMap);

    this.generateOptimizationRecommendations();
    this.updateMetrics();
  }

  private initializeNavigationTracking(): void {
    // Track route changes to build navigation patterns
    this.router.events.subscribe(event => {
      if (event.constructor.name === 'NavigationEnd') {
        this.trackNavigation((event as any).url);
      }
    });

    // Simulate existing navigation patterns
    const mockPatterns: NavigationPattern[] = [
      {
        fromRoute: '/dashboard',
        toRoute: '/users',
        frequency: 15,
        averageTime: 2.5,
        likelihood: 0.8
      },
      {
        fromRoute: '/users',
        toRoute: '/reports',
        frequency: 8,
        averageTime: 4.2,
        likelihood: 0.6
      },
      {
        fromRoute: '/dashboard',
        toRoute: '/analytics',
        frequency: 5,
        averageTime: 3.1,
        likelihood: 0.4
      },
      {
        fromRoute: '/reports',
        toRoute: '/analytics',
        frequency: 12,
        averageTime: 1.8,
        likelihood: 0.7
      }
    ];

    this.navigationPatterns.next(mockPatterns);
  }

  private initializePerformanceTracking(): void {
    if (typeof window !== 'undefined' && 'PerformanceObserver' in window) {
      this.performanceObserver = new PerformanceObserver((entries) => {
        for (const entry of entries.getEntries()) {
          if (entry.entryType === 'navigation' || entry.entryType === 'resource') {
            this.trackModuleLoadPerformance(entry);
          }
        }
      });

      this.performanceObserver.observe({
        entryTypes: ['navigation', 'resource', 'measure']
      });
    }
  }

  private trackNavigation(url: string): void {
    const route = this.extractRouteFromUrl(url);
    const accessTimes = this.routeAccessTracker.get(route) || [];
    accessTimes.push(new Date());

    // Keep only last 50 accesses
    if (accessTimes.length > 50) {
      accessTimes.splice(0, accessTimes.length - 50);
    }

    this.routeAccessTracker.set(route, accessTimes);
    this.updateModuleAccessFrequency(route);
  }

  private extractRouteFromUrl(url: string): string {
    return url.split('/')[1] || 'dashboard';
  }

  private updateModuleAccessFrequency(route: string): void {
    const modules = this.lazyModules.value;
    const module = modules.get(route);

    if (module) {
      module.accessFrequency++;
      module.lastAccessed = new Date();
      modules.set(route, module);
      this.lazyModules.next(modules);
    }
  }

  private trackModuleLoadPerformance(entry: PerformanceEntry): void {
    if (entry.name.includes('chunk') || entry.name.includes('lazy')) {
      const moduleName = this.extractModuleNameFromEntry(entry.name);
      const modules = this.lazyModules.value;
      const module = modules.get(moduleName);

      if (module) {
        module.loadTime = entry.duration || 0;
        modules.set(moduleName, module);
        this.lazyModules.next(modules);
        this.updateMetrics();
      }
    }
  }

  private extractModuleNameFromEntry(name: string): string {
    const match = name.match(/\/([^\/]+)-chunk/);
    return match ? match[1] : name.split('/').pop()?.split('.')[0] || '';
  }

  private generateOptimizationRecommendations(): void {
    const modules = this.lazyModules.value;
    const patterns = this.navigationPatterns.value;
    const recommendations: OptimizationRecommendation[] = [];

    modules.forEach((module, route) => {
      // High-frequency modules should use aggressive preloading
      if (module.accessFrequency > 5 && module.preloadStrategy === 'never') {
        recommendations.push({
          route,
          currentStrategy: module.preloadStrategy,
          recommendedStrategy: 'hover',
          reason: 'High access frequency detected',
          impact: 'high',
          implementation: `RouterModule.forRoot(routes, { preloadingStrategy: HoverPreloadStrategy })`,
          estimatedImprovement: '80% faster navigation'
        });
      }

      // Large modules with low frequency should not preload
      if (module.estimatedSize > 400 * 1024 && module.accessFrequency < 3) {
        recommendations.push({
          route,
          currentStrategy: module.preloadStrategy,
          recommendedStrategy: 'never',
          reason: 'Large module with low usage',
          impact: 'medium',
          implementation: `Remove from preload list, load on-demand only`,
          estimatedImprovement: '400KB initial bundle reduction'
        });
      }

      // Modules with strong navigation patterns should use predictive loading
      const strongPattern = patterns.find(p => p.toRoute === route && p.likelihood > 0.7);
      if (strongPattern && module.preloadStrategy !== 'always') {
        recommendations.push({
          route,
          currentStrategy: module.preloadStrategy,
          recommendedStrategy: 'always',
          reason: `Strong navigation pattern from ${strongPattern.fromRoute}`,
          impact: 'high',
          implementation: `Implement predictive preloading based on ${strongPattern.fromRoute} visits`,
          estimatedImprovement: '90% prediction accuracy'
        });
      }

      // Fast-loading small modules can be preloaded more aggressively
      if (module.estimatedSize < 200 * 1024 && module.loadTime < 150) {
        recommendations.push({
          route,
          currentStrategy: module.preloadStrategy,
          recommendedStrategy: 'viewport',
          reason: 'Small, fast-loading module',
          impact: 'low',
          implementation: `Use QuicklinkStrategy for viewport-based preloading`,
          estimatedImprovement: 'Instant navigation with minimal overhead'
        });
      }
    });

    this.optimizationRecommendations.next(recommendations);
  }

  private updateMetrics(): void {
    const modules = this.lazyModules.value;
    const patterns = this.navigationPatterns.value;
    const modulesArray = Array.from(modules.values());

    const metrics: LazyLoadingMetrics = {
      totalRoutes: modulesArray.length + 3, // +3 for eager routes
      lazyRoutes: modulesArray.length,
      eagerRoutes: 3,
      averageLoadTime: modulesArray.reduce((sum, m) => sum + m.loadTime, 0) / modulesArray.length || 0,
      preloadingEfficiency: this.calculatePreloadingEfficiency(modulesArray),
      cacheHitRate: this.calculateCacheHitRate(),
      userNavigationPatterns: patterns
    };

    this.metrics.next(metrics);
  }

  private calculatePreloadingEfficiency(modules: LazyModule[]): number {
    const preloadedModules = modules.filter(m => m.preloadStrategy !== 'never');
    const accessedPreloaded = preloadedModules.filter(m => m.accessFrequency > 0);

    return preloadedModules.length > 0 ?
      (accessedPreloaded.length / preloadedModules.length) * 100 : 0;
  }

  private calculateCacheHitRate(): number {
    // Simulate cache hit rate calculation
    return 75 + Math.random() * 20; // 75-95%
  }

  public preloadModule(route: string): Observable<boolean> {
    const modules = this.lazyModules.value;
    const module = modules.get(route);

    if (!module) {
      return of(false);
    }

    if (this.preloadCache.has(route)) {
      return of(true); // Already preloaded
    }

    return from(this.executePreload(module)).pipe(
      tap(() => {
        console.log(`Preloaded module: ${module.moduleName}`);
      }),
      map(() => true),
      catchError(error => {
        console.error(`Failed to preload ${route}:`, error);
        return of(false);
      })
    );
  }

  private async executePreload(module: LazyModule): Promise<void> {
    const startTime = performance.now();

    try {
      // Simulate module loading
      const loadPromise = new Promise(resolve => {
        setTimeout(resolve, module.loadTime);
      });

      this.preloadCache.set(module.path, loadPromise);
      await loadPromise;

      const actualLoadTime = performance.now() - startTime;
      module.loadTime = actualLoadTime;

      const modules = this.lazyModules.value;
      modules.set(module.path, module);
      this.lazyModules.next(modules);

    } catch (error) {
      this.preloadCache.delete(module.path);
      throw error;
    }
  }

  public implementStrategy(strategyName: string): Observable<boolean> {
    const strategy = this.preloadingStrategies.value.find(s => s.name === strategyName);

    if (!strategy) {
      return of(false);
    }

    return from(this.applyPreloadingStrategy(strategy)).pipe(
      tap(() => {
        console.log(`Applied strategy: ${strategy.name}`);
        this.updateMetrics();
      }),
      map(() => true),
      catchError(error => {
        console.error(`Failed to apply strategy ${strategyName}:`, error);
        return of(false);
      })
    );
  }

  private async applyPreloadingStrategy(strategy: PreloadingStrategy): Promise<void> {
    const modules = this.lazyModules.value;

    switch (strategy.name) {
      case 'PreloadAllModules':
        modules.forEach(module => {
          module.preloadStrategy = 'always';
        });
        break;

      case 'QuicklinkStrategy':
        modules.forEach(module => {
          if (module.estimatedSize < 300 * 1024) {
            module.preloadStrategy = 'viewport';
          }
        });
        break;

      case 'HoverPreloadStrategy':
        modules.forEach(module => {
          if (module.accessFrequency > 3) {
            module.preloadStrategy = 'hover';
          }
        });
        break;

      case 'NetworkAwareStrategy':
        const connectionSpeed = this.getConnectionSpeed();
        modules.forEach(module => {
          if (connectionSpeed === 'fast' && module.estimatedSize < 500 * 1024) {
            module.preloadStrategy = 'viewport';
          } else if (connectionSpeed === 'slow') {
            module.preloadStrategy = 'never';
          }
        });
        break;

      case 'PredictiveStrategy':
        const patterns = this.navigationPatterns.value;
        modules.forEach(module => {
          const pattern = patterns.find(p => p.toRoute === module.path && p.likelihood > 0.7);
          if (pattern) {
            module.preloadStrategy = 'always';
          }
        });
        break;
    }

    this.lazyModules.next(modules);
  }

  private getConnectionSpeed(): 'fast' | 'medium' | 'slow' {
    if ('connection' in navigator) {
      const connection = (navigator as any).connection;
      const speed = connection.effectiveType;

      if (speed === '4g') return 'fast';
      if (speed === '3g') return 'medium';
      return 'slow';
    }
    return 'medium'; // Default assumption
  }

  public optimizeRoutePreloading(): Observable<string[]> {
    const recommendations = this.optimizationRecommendations.value;
    const highImpactRecs = recommendations.filter(r => r.impact === 'high');

    return from(Promise.all(
      highImpactRecs.map(rec => this.applyRecommendation(rec))
    )).pipe(
      map(results => results.filter(r => r.success).map(r => r.message)),
      tap(messages => {
        console.log('Applied optimizations:', messages);
        this.generateOptimizationRecommendations();
        this.updateMetrics();
      })
    );
  }

  private async applyRecommendation(recommendation: OptimizationRecommendation): Promise<{success: boolean, message: string}> {
    try {
      const modules = this.lazyModules.value;
      const module = modules.get(recommendation.route);

      if (module) {
        module.preloadStrategy = recommendation.recommendedStrategy as any;
        modules.set(recommendation.route, module);
        this.lazyModules.next(modules);

        return {
          success: true,
          message: `Applied ${recommendation.recommendedStrategy} strategy to ${recommendation.route}`
        };
      }

      return {
        success: false,
        message: `Module ${recommendation.route} not found`
      };

    } catch (error) {
      return {
        success: false,
        message: `Failed to apply recommendation for ${recommendation.route}: ${error}`
      };
    }
  }

  public generatePreloadingReport(): Observable<any> {
    return combineLatest([
      this.lazyModules$,
      this.metrics$,
      this.recommendations$
    ]).pipe(
      map(([modules, metrics, recommendations]) => {
        const modulesArray = Array.from(modules.values());

        return {
          summary: {
            totalModules: modulesArray.length,
            preloadedModules: modulesArray.filter(m => m.preloadStrategy !== 'never').length,
            averageLoadTime: metrics.averageLoadTime.toFixed(0) + 'ms',
            preloadingEfficiency: metrics.preloadingEfficiency.toFixed(1) + '%',
            cacheHitRate: metrics.cacheHitRate.toFixed(1) + '%'
          },
          moduleBreakdown: modulesArray.map(module => ({
            route: module.path,
            size: this.formatBytes(module.estimatedSize),
            loadTime: module.loadTime.toFixed(0) + 'ms',
            accessFrequency: module.accessFrequency,
            strategy: module.preloadStrategy,
            priority: module.priority
          })),
          navigationPatterns: metrics.userNavigationPatterns.map(pattern => ({
            route: `${pattern.fromRoute} → ${pattern.toRoute}`,
            frequency: pattern.frequency,
            likelihood: (pattern.likelihood * 100).toFixed(1) + '%',
            avgTime: pattern.averageTime.toFixed(1) + 's'
          })),
          optimizations: recommendations.map(rec => ({
            route: rec.route,
            currentStrategy: rec.currentStrategy,
            recommendedStrategy: rec.recommendedStrategy,
            reason: rec.reason,
            impact: rec.impact,
            improvement: rec.estimatedImprovement
          })),
          recommendations: this.generateGlobalLazyLoadingRecommendations(modulesArray, metrics)
        };
      })
    );
  }

  private generateGlobalLazyLoadingRecommendations(modules: LazyModule[], metrics: LazyLoadingMetrics): string[] {
    const recommendations: string[] = [];

    if (metrics.preloadingEfficiency < 60) {
      recommendations.push('Low preloading efficiency - review preloading strategies');
    }

    if (metrics.averageLoadTime > 300) {
      recommendations.push('High average load time - consider bundle splitting or CDN');
    }

    const largeModules = modules.filter(m => m.estimatedSize > 500 * 1024);
    if (largeModules.length > 0) {
      recommendations.push(`${largeModules.length} large modules detected - consider code splitting`);
    }

    const rarelyUsed = modules.filter(m => m.accessFrequency < 2);
    if (rarelyUsed.length > 0) {
      recommendations.push(`${rarelyUsed.length} rarely used modules - disable preloading`);
    }

    if (metrics.cacheHitRate < 70) {
      recommendations.push('Low cache hit rate - implement better caching strategy');
    }

    return recommendations;
  }

  private formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  public exportLazyLoadingConfig(): Observable<string> {
    return this.lazyModules$.pipe(
      map(modules => {
        const config = {
          preloadingStrategy: 'CustomPreloadingStrategy',
          modules: Array.from(modules.values()).map(module => ({
            path: module.path,
            loadChildren: module.loadChildren,
            preloadStrategy: module.preloadStrategy,
            priority: module.priority
          }))
        };

        return JSON.stringify(config, null, 2);
      })
    );
  }

  public destroy(): void {
    if (this.performanceObserver) {
      this.performanceObserver.disconnect();
    }
    this.preloadCache.clear();
    this.routeAccessTracker.clear();
  }
}