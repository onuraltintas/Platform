import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject, from, of } from 'rxjs';
import { map, catchError, tap, switchMap } from 'rxjs/operators';

interface DynamicImportMetrics {
  importPath: string;
  bundleSize: number;
  loadTime: number;
  successRate: number;
  usageFrequency: number;
  lastUsed: Date;
  chunkName?: string;
  preloadPriority: 'high' | 'medium' | 'low';
}

interface ImportOptimization {
  path: string;
  suggestedStrategy: 'preload' | 'prefetch' | 'lazy' | 'eager';
  reason: string;
  estimatedImprovement: number;
  implementation: string;
}

interface ComponentAnalysis {
  component: string;
  staticImports: string[];
  dynamicImports: string[];
  conversionCandidates: string[];
  usagePattern: 'frequent' | 'occasional' | 'rare';
}

@Injectable({
  providedIn: 'root'
})
export class DynamicImportsOptimizerService {
  private metricsSubject = new BehaviorSubject<Map<string, DynamicImportMetrics>>(new Map());
  private optimizationsSubject = new BehaviorSubject<ImportOptimization[]>([]);
  private componentAnalysisSubject = new BehaviorSubject<ComponentAnalysis[]>([]);

  public metrics$ = this.metricsSubject.asObservable();
  public optimizations$ = this.optimizationsSubject.asObservable();
  public componentAnalysis$ = this.componentAnalysisSubject.asObservable();

  private performanceObserver?: PerformanceObserver;
  private importCache = new Map<string, any>();
  private loadingPromises = new Map<string, Promise<any>>();

  constructor() {
    this.initializePerformanceTracking();
    this.analyzeExistingImports();
  }

  private initializePerformanceTracking(): void {
    if (typeof window !== 'undefined' && 'PerformanceObserver' in window) {
      this.performanceObserver = new PerformanceObserver((entries) => {
        for (const entry of entries.getEntries()) {
          if (entry.entryType === 'navigation' || entry.entryType === 'resource') {
            this.trackImportPerformance(entry);
          }
        }
      });

      this.performanceObserver.observe({
        entryTypes: ['navigation', 'resource', 'measure']
      });
    }
  }

  private trackImportPerformance(entry: PerformanceEntry): void {
    const metrics = this.metricsSubject.value;

    if (entry.name.includes('chunk') || entry.name.includes('.js')) {
      const importPath = this.extractImportPath(entry.name);
      const existingMetric = metrics.get(importPath);

      const updatedMetric: DynamicImportMetrics = {
        importPath,
        bundleSize: this.estimateBundleSize(entry),
        loadTime: entry.duration || 0,
        successRate: existingMetric ? this.calculateSuccessRate(existingMetric, true) : 1,
        usageFrequency: (existingMetric?.usageFrequency || 0) + 1,
        lastUsed: new Date(),
        chunkName: this.extractChunkName(entry.name),
        preloadPriority: this.calculatePreloadPriority(importPath)
      };

      metrics.set(importPath, updatedMetric);
      this.metricsSubject.next(metrics);
      this.generateOptimizationRecommendations();
    }
  }

  private extractImportPath(name: string): string {
    const match = name.match(/\/([^\/]+)\.js$/);
    return match ? match[1] : name;
  }

  private estimateBundleSize(entry: PerformanceEntry): number {
    if ('transferSize' in entry) {
      return (entry as any).transferSize;
    }
    return entry.duration ? entry.duration * 100 : 0;
  }

  private calculateSuccessRate(existing: DynamicImportMetrics, success: boolean): number {
    const totalAttempts = existing.usageFrequency + 1;
    const successfulAttempts = existing.successRate * existing.usageFrequency + (success ? 1 : 0);
    return successfulAttempts / totalAttempts;
  }

  private extractChunkName(name: string): string {
    const match = name.match(/chunk[.-]([^.-]+)/);
    return match ? match[1] : 'unknown';
  }

  private calculatePreloadPriority(importPath: string): 'high' | 'medium' | 'low' {
    const metrics = this.metricsSubject.value.get(importPath);
    if (!metrics) return 'low';

    const daysSinceLastUse = (Date.now() - metrics.lastUsed.getTime()) / (1000 * 60 * 60 * 24);

    if (metrics.usageFrequency > 10 && daysSinceLastUse < 1) return 'high';
    if (metrics.usageFrequency > 5 && daysSinceLastUse < 7) return 'medium';
    return 'low';
  }

  public optimizeImport<T>(importPath: string, importFn: () => Promise<T>): Promise<T> {
    if (this.importCache.has(importPath)) {
      return Promise.resolve(this.importCache.get(importPath));
    }

    if (this.loadingPromises.has(importPath)) {
      return this.loadingPromises.get(importPath)!;
    }

    const startTime = performance.now();
    const loadingPromise = importFn()
      .then(module => {
        const loadTime = performance.now() - startTime;
        this.updateImportMetrics(importPath, loadTime, true);
        this.importCache.set(importPath, module);
        this.loadingPromises.delete(importPath);
        return module;
      })
      .catch(error => {
        this.updateImportMetrics(importPath, performance.now() - startTime, false);
        this.loadingPromises.delete(importPath);
        throw error;
      });

    this.loadingPromises.set(importPath, loadingPromise);
    return loadingPromise;
  }

  private updateImportMetrics(importPath: string, loadTime: number, success: boolean): void {
    const metrics = this.metricsSubject.value;
    const existing = metrics.get(importPath);

    const updatedMetric: DynamicImportMetrics = {
      importPath,
      bundleSize: existing?.bundleSize || 0,
      loadTime,
      successRate: existing ? this.calculateSuccessRate(existing, success) : (success ? 1 : 0),
      usageFrequency: (existing?.usageFrequency || 0) + 1,
      lastUsed: new Date(),
      chunkName: existing?.chunkName || this.extractChunkName(importPath),
      preloadPriority: this.calculatePreloadPriority(importPath)
    };

    metrics.set(importPath, updatedMetric);
    this.metricsSubject.next(metrics);
  }

  private analyzeExistingImports(): void {
    const components: ComponentAnalysis[] = [
      {
        component: 'UserManagementModule',
        staticImports: ['@angular/common', '@angular/forms', 'primeng/table'],
        dynamicImports: ['./user-details/user-details.component'],
        conversionCandidates: ['primeng/calendar', 'primeng/dropdown'],
        usagePattern: 'frequent'
      },
      {
        component: 'ReportsModule',
        staticImports: ['chart.js', 'date-fns', '@angular/material'],
        dynamicImports: [],
        conversionCandidates: ['chart.js', 'date-fns'],
        usagePattern: 'occasional'
      },
      {
        component: 'SettingsModule',
        staticImports: ['@angular/forms', 'primeng/inputtext'],
        dynamicImports: [],
        conversionCandidates: ['primeng/colorpicker', 'primeng/fileupload'],
        usagePattern: 'rare'
      }
    ];

    this.componentAnalysisSubject.next(components);
    this.generateOptimizationRecommendations();
  }

  private generateOptimizationRecommendations(): void {
    const optimizations: ImportOptimization[] = [];
    const metrics = this.metricsSubject.value;
    const components = this.componentAnalysisSubject.value;

    components.forEach(component => {
      component.conversionCandidates.forEach(candidate => {
        const metric = metrics.get(candidate);

        if (component.usagePattern === 'rare') {
          optimizations.push({
            path: candidate,
            suggestedStrategy: 'lazy',
            reason: 'Rarely used component, convert to dynamic import',
            estimatedImprovement: 15,
            implementation: `const ${this.getCamelCase(candidate)} = await import('${candidate}');`
          });
        } else if (component.usagePattern === 'frequent' && metric?.preloadPriority === 'high') {
          optimizations.push({
            path: candidate,
            suggestedStrategy: 'preload',
            reason: 'Frequently used, preload for instant access',
            estimatedImprovement: 25,
            implementation: `<link rel="preload" href="${candidate}" as="script">`
          });
        } else if (component.usagePattern === 'occasional') {
          optimizations.push({
            path: candidate,
            suggestedStrategy: 'prefetch',
            reason: 'Occasionally used, prefetch during idle time',
            estimatedImprovement: 10,
            implementation: `<link rel="prefetch" href="${candidate}">`
          });
        }
      });
    });

    this.optimizationsSubject.next(optimizations);
  }

  private getCamelCase(path: string): string {
    return path
      .split('/')
      .pop()!
      .replace(/[-._]/g, ' ')
      .replace(/\b\w/g, l => l.toUpperCase())
      .replace(/\s/g, '');
  }

  public convertToLazyRoute(modulePath: string): Observable<string> {
    return from(this.analyzeModuleForLazyLoading(modulePath)).pipe(
      map(analysis => this.generateLazyRouteCode(analysis)),
      catchError(error => {
        console.error('Failed to convert to lazy route:', error);
        return of('');
      })
    );
  }

  private async analyzeModuleForLazyLoading(modulePath: string): Promise<any> {
    return {
      moduleName: this.extractModuleName(modulePath),
      path: modulePath,
      dependencies: [],
      estimatedSize: 0
    };
  }

  private extractModuleName(path: string): string {
    const segments = path.split('/');
    return segments[segments.length - 1].replace('.module.ts', '');
  }

  private generateLazyRouteCode(analysis: any): string {
    return `{
  path: '${analysis.moduleName.toLowerCase()}',
  loadChildren: () => import('${analysis.path}').then(m => m.${this.getCamelCase(analysis.moduleName)}Module)
}`;
  }

  public preloadCriticalChunks(): Observable<void> {
    const metrics = this.metricsSubject.value;
    const criticalChunks = Array.from(metrics.values())
      .filter(m => m.preloadPriority === 'high')
      .sort((a, b) => b.usageFrequency - a.usageFrequency)
      .slice(0, 3);

    return from(Promise.all(
      criticalChunks.map(chunk => this.preloadChunk(chunk.importPath))
    )).pipe(
      map(() => void 0),
      tap(() => console.log('Critical chunks preloaded'))
    );
  }

  private preloadChunk(importPath: string): Promise<void> {
    return new Promise((resolve) => {
      const link = document.createElement('link');
      link.rel = 'preload';
      link.as = 'script';
      link.href = importPath;
      link.onload = () => resolve();
      link.onerror = () => resolve();
      document.head.appendChild(link);
    });
  }

  public getOptimizationReport(): Observable<any> {
    return this.metrics$.pipe(
      switchMap(metrics => {
        const totalBundleSize = Array.from(metrics.values())
          .reduce((sum, m) => sum + m.bundleSize, 0);

        const avgLoadTime = Array.from(metrics.values())
          .reduce((sum, m) => sum + m.loadTime, 0) / metrics.size;

        const optimizationsPotential = this.optimizationsSubject.value
          .reduce((sum, opt) => sum + opt.estimatedImprovement, 0);

        return of({
          totalImports: metrics.size,
          totalBundleSize,
          averageLoadTime: avgLoadTime,
          optimizationsPotential,
          recommendations: this.optimizationsSubject.value,
          criticalMetrics: this.getCriticalMetrics(metrics)
        });
      })
    );
  }

  private getCriticalMetrics(metrics: Map<string, DynamicImportMetrics>): any {
    const metricsArray = Array.from(metrics.values());

    return {
      slowestImports: metricsArray
        .sort((a, b) => b.loadTime - a.loadTime)
        .slice(0, 5)
        .map(m => ({ path: m.importPath, loadTime: m.loadTime })),

      largestBundles: metricsArray
        .sort((a, b) => b.bundleSize - a.bundleSize)
        .slice(0, 5)
        .map(m => ({ path: m.importPath, size: m.bundleSize })),

      mostUsed: metricsArray
        .sort((a, b) => b.usageFrequency - a.usageFrequency)
        .slice(0, 5)
        .map(m => ({ path: m.importPath, frequency: m.usageFrequency }))
    };
  }

  public optimizeComponentImports(componentCode: string): string {
    const staticImportRegex = /import\s+.*\s+from\s+['"]([^'"]+)['"];/g;
    const optimizations = this.optimizationsSubject.value;

    let optimizedCode = componentCode;
    let match;

    while ((match = staticImportRegex.exec(componentCode)) !== null) {
      const importPath = match[1];
      const optimization = optimizations.find(opt => opt.path.includes(importPath));

      if (optimization && optimization.suggestedStrategy === 'lazy') {
        const dynamicImport = `
  async load${this.getCamelCase(importPath)}() {
    const module = await import('${importPath}');
    return module;
  }`;

        optimizedCode = optimizedCode.replace(match[0], `// Converted to dynamic import${dynamicImport}`);
      }
    }

    return optimizedCode;
  }

  public destroy(): void {
    if (this.performanceObserver) {
      this.performanceObserver.disconnect();
    }
    this.importCache.clear();
    this.loadingPromises.clear();
  }
}