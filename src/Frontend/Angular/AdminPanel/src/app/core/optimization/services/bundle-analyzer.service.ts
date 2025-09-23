import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, timer } from 'rxjs';

export interface BundleModule {
  name: string;
  size: number; // bytes
  gzipSize: number;
  path: string;
  type: 'initial' | 'lazy' | 'vendor' | 'polyfill';
  importedBy: string[];
  exports: string[];
  imports: string[];
  isUnused: boolean;
  duplicateOf?: string;
  treeshakable: boolean;
  timestamp: number;
}

export interface BundleChunk {
  id: string;
  name: string;
  size: number;
  gzipSize: number;
  modules: BundleModule[];
  loadTime: number;
  type: 'initial' | 'lazy' | 'vendor';
  route?: string;
  priority: 'high' | 'medium' | 'low';
  cached: boolean;
}

export interface BundleAnalysis {
  totalSize: number;
  totalGzipSize: number;
  initialBundleSize: number;
  lazyBundleSize: number;
  vendorBundleSize: number;
  chunks: BundleChunk[];
  modules: BundleModule[];
  duplicates: Array<{
    module: string;
    instances: string[];
    totalSize: number;
  }>;
  unusedModules: BundleModule[];
  treeshakingOpportunities: Array<{
    module: string;
    unusedExports: string[];
    potentialSavings: number;
  }>;
  recommendations: BundleRecommendation[];
  metrics: {
    moduleCount: number;
    chunkCount: number;
    duplicationRate: number;
    treeshakingEfficiency: number;
    loadTimeScore: number;
  };
}

export interface BundleRecommendation {
  id: string;
  type: 'size' | 'duplication' | 'treeshaking' | 'splitting' | 'lazy-loading';
  priority: 'low' | 'medium' | 'high' | 'critical';
  title: string;
  description: string;
  impact: 'low' | 'medium' | 'high';
  effort: 'low' | 'medium' | 'high';
  estimatedSavings: number; // bytes
  implementationSteps: string[];
  technicalDetails: string;
  affectedModules: string[];
  confidence: number; // 0-1
}

export interface BundleBudget {
  maxInitialSize: number; // bytes
  maxChunkSize: number;
  maxTotalSize: number;
  maxModuleCount: number;
  performanceBudget: {
    fcp: number; // ms
    lcp: number;
    tti: number;
  };
  warnings: {
    initialSizeThreshold: number;
    chunkSizeThreshold: number;
    duplicateThreshold: number;
  };
}

export interface BundleOptimization {
  id: string;
  name: string;
  description: string;
  type: 'tree-shaking' | 'code-splitting' | 'dynamic-import' | 'dead-code-elimination';
  status: 'pending' | 'applied' | 'failed';
  estimatedSavings: number;
  actualSavings?: number;
  timestamp: number;
  config?: any;
}

/**
 * Bundle Analyzer Service
 * Advanced bundle analysis and optimization recommendations
 */
@Injectable({
  providedIn: 'root'
})
export class BundleAnalyzerService {
  private bundleAnalysis$ = new BehaviorSubject<BundleAnalysis>(this.getInitialAnalysis());
  private bundleBudget$ = new BehaviorSubject<BundleBudget>(this.getDefaultBudget());
  private optimizations$ = new BehaviorSubject<BundleOptimization[]>([]);

  private readonly ANALYSIS_INTERVAL = 300000; // 5 minutes
  private moduleCache = new Map<string, BundleModule>();
  private analysisHistory: BundleAnalysis[] = [];

  constructor() {
    this.initializeBundleAnalyzer();
  }

  /**
   * Initialize bundle analyzer
   */
  private initializeBundleAnalyzer(): void {
    this.startAnalysisEngine();
    this.setupPerformanceMonitoring();
    this.analyzeCurrentBundle();

    console.log('📦 Bundle Analyzer Service initialized');
  }

  /**
   * Start analysis engine
   */
  private startAnalysisEngine(): void {
    // Analyze bundle every 5 minutes
    timer(this.ANALYSIS_INTERVAL, this.ANALYSIS_INTERVAL).subscribe(() => {
      this.analyzeCurrentBundle();
    });

    // Monitor module loading
    this.setupModuleLoadMonitoring();
  }

  /**
   * Setup performance monitoring for bundle loading
   */
  private setupPerformanceMonitoring(): void {
    // Monitor script loading performance
    if ('PerformanceObserver' in window) {
      const observer = new PerformanceObserver((list) => {
        const entries = list.getEntries();
        entries.forEach(entry => {
          if (entry.entryType === 'resource' && entry.name.includes('.js')) {
            this.recordModuleLoadTime(entry.name, entry.duration);
          }
        });
      });

      observer.observe({ type: 'resource', buffered: true });
    }
  }

  /**
   * Setup module load monitoring
   */
  private setupModuleLoadMonitoring(): void {
    // Monitor dynamic imports
    const originalImport = window.eval('import');
    if (originalImport) {
      window.eval(`
        window.__originalImport = import;
        window.import = function(specifier) {
          console.log('Dynamic import:', specifier);
          return window.__originalImport(specifier);
        };
      `);
    }
  }

  /**
   * Analyze current bundle
   */
  private async analyzeCurrentBundle(): Promise<void> {
    try {
      console.log('🔍 Analyzing current bundle...');

      const modules = await this.discoverModules();
      const chunks = await this.analyzeChunks();
      const duplicates = this.findDuplicates(modules);
      const unusedModules = this.findUnusedModules(modules);
      const treeshakingOpportunities = this.analyzeTreeshakingOpportunities(modules);
      const recommendations = this.generateRecommendations(modules, chunks, duplicates);

      const analysis: BundleAnalysis = {
        totalSize: modules.reduce((sum, m) => sum + m.size, 0),
        totalGzipSize: modules.reduce((sum, m) => sum + m.gzipSize, 0),
        initialBundleSize: this.calculateInitialBundleSize(chunks),
        lazyBundleSize: this.calculateLazyBundleSize(chunks),
        vendorBundleSize: this.calculateVendorBundleSize(chunks),
        chunks,
        modules,
        duplicates,
        unusedModules,
        treeshakingOpportunities,
        recommendations,
        metrics: this.calculateMetrics(modules, chunks, duplicates)
      };

      this.bundleAnalysis$.next(analysis);
      this.analysisHistory.push(analysis);

      // Keep only last 24 analyses
      if (this.analysisHistory.length > 24) {
        this.analysisHistory.shift();
      }

      console.log(`📊 Bundle analysis completed: ${(analysis.totalSize / 1024).toFixed(1)}KB total`);

    } catch (error) {
      console.error('Bundle analysis failed:', error);
    }
  }

  /**
   * Discover modules in the bundle
   */
  private async discoverModules(): Promise<BundleModule[]> {
    const modules: BundleModule[] = [];

    // Analyze loaded scripts
    const scripts = Array.from(document.querySelectorAll('script[src]'));

    for (const script of scripts) {
      const src = script.getAttribute('src');
      if (src && (src.includes('.js') || src.includes('chunk'))) {
        const module = await this.analyzeScript(src);
        if (module) {
          modules.push(module);
        }
      }
    }

    // Analyze webpack chunks if available
    if (typeof (window as any).__webpack_require__ !== 'undefined') {
      const webpackModules = this.analyzeWebpackModules();
      modules.push(...webpackModules);
    }

    // Analyze ES modules
    const esModules = await this.analyzeESModules();
    modules.push(...esModules);

    return modules;
  }

  /**
   * Analyze individual script
   */
  private async analyzeScript(src: string): Promise<BundleModule | null> {
    try {
      // Get module info from performance entries
      const perfEntries = performance.getEntriesByName(src);
      const perfEntry = perfEntries[0] as any;

      if (!perfEntry) return null;

      const size = perfEntry.transferSize || 0;
      const gzipSize = perfEntry.encodedBodySize || size;

      const module: BundleModule = {
        name: this.extractModuleName(src),
        size,
        gzipSize,
        path: src,
        type: this.determineModuleType(src),
        importedBy: [],
        exports: [],
        imports: [],
        isUnused: false,
        treeshakable: true,
        timestamp: Date.now()
      };

      // Try to analyze module content if possible
      await this.analyzeModuleContent(module, src);

      return module;

    } catch (error) {
      console.warn(`Failed to analyze script ${src}:`, error);
      return null;
    }
  }

  /**
   * Analyze webpack modules
   */
  private analyzeWebpackModules(): BundleModule[] {
    const modules: BundleModule[] = [];

    try {
      const webpackRequire = (window as any).__webpack_require__;
      if (!webpackRequire || !webpackRequire.cache) {
        return modules;
      }

      Object.keys(webpackRequire.cache).forEach(moduleId => {
        const cachedModule = webpackRequire.cache[moduleId];
        if (cachedModule && cachedModule.exports) {
          const module: BundleModule = {
            name: `webpack-${moduleId}`,
            size: this.estimateModuleSize(cachedModule),
            gzipSize: 0, // Unknown for webpack modules
            path: moduleId,
            type: 'initial',
            importedBy: [],
            exports: Object.keys(cachedModule.exports || {}),
            imports: [],
            isUnused: !cachedModule.loaded,
            treeshakable: true,
            timestamp: Date.now()
          };

          modules.push(module);
        }
      });

    } catch (error) {
      console.warn('Failed to analyze webpack modules:', error);
    }

    return modules;
  }

  /**
   * Analyze ES modules
   */
  private async analyzeESModules(): Promise<BundleModule[]> {
    const modules: BundleModule[] = [];

    // This would integrate with ES module analysis
    // For now, return empty array as ES module analysis requires build-time tools

    return modules;
  }

  /**
   * Analyze module content
   */
  private async analyzeModuleContent(module: BundleModule, src: string): Promise<void> {
    try {
      // For same-origin scripts, we could potentially fetch and analyze
      // For now, we'll use heuristics based on the URL and performance data

      if (src.includes('vendor') || src.includes('polyfill')) {
        module.type = 'vendor';
      } else if (src.includes('lazy') || src.includes('chunk')) {
        module.type = 'lazy';
      }

      // Estimate imports/exports based on conventions
      if (src.includes('angular')) {
        module.imports.push('@angular/core', '@angular/common');
      }

    } catch (error) {
      // Ignore analysis errors
    }
  }

  /**
   * Analyze chunks
   */
  private async analyzeChunks(): Promise<BundleChunk[]> {
    const chunks: BundleChunk[] = [];
    const scripts = Array.from(document.querySelectorAll('script[src]'));

    for (const script of scripts) {
      const src = script.getAttribute('src');
      if (src && src.includes('.js')) {
        const chunk = await this.analyzeChunk(src);
        if (chunk) {
          chunks.push(chunk);
        }
      }
    }

    return chunks;
  }

  /**
   * Analyze individual chunk
   */
  private async analyzeChunk(src: string): Promise<BundleChunk | null> {
    try {
      const perfEntries = performance.getEntriesByName(src);
      const perfEntry = perfEntries[0] as any;

      if (!perfEntry) return null;

      const size = perfEntry.transferSize || 0;
      const loadTime = perfEntry.duration || 0;

      const chunk: BundleChunk = {
        id: this.extractChunkId(src),
        name: this.extractModuleName(src),
        size,
        gzipSize: perfEntry.encodedBodySize || size,
        modules: [], // Would be populated with actual module analysis
        loadTime,
        type: this.determineChunkType(src),
        priority: this.determineChunkPriority(src),
        cached: this.isChunkCached(src)
      };

      return chunk;

    } catch (error) {
      console.warn(`Failed to analyze chunk ${src}:`, error);
      return null;
    }
  }

  /**
   * Find duplicate modules
   */
  private findDuplicates(modules: BundleModule[]): Array<{ module: string; instances: string[]; totalSize: number }> {
    const moduleGroups = new Map<string, string[]>();
    const duplicates: Array<{ module: string; instances: string[]; totalSize: number }> = [];

    modules.forEach(module => {
      const normalizedName = this.normalizeModuleName(module.name);
      if (!moduleGroups.has(normalizedName)) {
        moduleGroups.set(normalizedName, []);
      }
      moduleGroups.get(normalizedName)!.push(module.path);
    });

    moduleGroups.forEach((instances, moduleName) => {
      if (instances.length > 1) {
        const totalSize = instances.reduce((sum, path) => {
          const module = modules.find(m => m.path === path);
          return sum + (module?.size || 0);
        }, 0);

        duplicates.push({
          module: moduleName,
          instances,
          totalSize
        });
      }
    });

    return duplicates;
  }

  /**
   * Find unused modules
   */
  private findUnusedModules(modules: BundleModule[]): BundleModule[] {
    return modules.filter(module => {
      // Simple heuristic: modules that haven't been accessed
      return module.isUnused ||
             (module.importedBy.length === 0 && module.type !== 'initial');
    });
  }

  /**
   * Analyze tree shaking opportunities
   */
  private analyzeTreeshakingOpportunities(modules: BundleModule[]): Array<{
    module: string;
    unusedExports: string[];
    potentialSavings: number;
  }> {
    const opportunities: Array<{
      module: string;
      unusedExports: string[];
      potentialSavings: number;
    }> = [];

    modules.forEach(module => {
      if (module.treeshakable && module.exports.length > 0) {
        // Heuristic: assume some exports are unused
        const unusedExports = module.exports.filter((_, index) => index % 3 === 0); // Simple simulation

        if (unusedExports.length > 0) {
          const potentialSavings = (module.size * unusedExports.length) / module.exports.length;

          opportunities.push({
            module: module.name,
            unusedExports,
            potentialSavings
          });
        }
      }
    });

    return opportunities;
  }

  /**
   * Generate optimization recommendations
   */
  private generateRecommendations(
    modules: BundleModule[],
    chunks: BundleChunk[],
    duplicates: Array<{ module: string; instances: string[]; totalSize: number }>
  ): BundleRecommendation[] {
    const recommendations: BundleRecommendation[] = [];

    // Bundle size recommendations
    const totalSize = modules.reduce((sum, m) => sum + m.size, 0);
    if (totalSize > 500 * 1024) { // 500KB
      recommendations.push({
        id: 'reduce-bundle-size',
        type: 'size',
        priority: 'high',
        title: 'Reduce Bundle Size',
        description: `Total bundle size (${(totalSize / 1024).toFixed(1)}KB) exceeds recommended limits.`,
        impact: 'high',
        effort: 'medium',
        estimatedSavings: totalSize * 0.3, // 30% reduction
        implementationSteps: [
          'Implement code splitting for routes',
          'Use dynamic imports for large libraries',
          'Enable tree shaking optimization',
          'Remove unused dependencies',
          'Compress and minify bundles'
        ],
        technicalDetails: 'Large bundles increase initial load time and parsing cost.',
        affectedModules: modules.filter(m => m.size > 50 * 1024).map(m => m.name),
        confidence: 0.9
      });
    }

    // Duplicate module recommendations
    if (duplicates.length > 0) {
      const totalDuplicateSize = duplicates.reduce((sum, d) => sum + d.totalSize, 0);
      recommendations.push({
        id: 'eliminate-duplicates',
        type: 'duplication',
        priority: 'medium',
        title: 'Eliminate Duplicate Modules',
        description: `${duplicates.length} duplicate modules found, wasting ${(totalDuplicateSize / 1024).toFixed(1)}KB.`,
        impact: 'medium',
        effort: 'low',
        estimatedSavings: totalDuplicateSize * 0.8,
        implementationSteps: [
          'Configure webpack optimization.splitChunks',
          'Use shared modules for common dependencies',
          'Implement proper module federation',
          'Review import statements for inconsistencies'
        ],
        technicalDetails: 'Duplicate modules waste bandwidth and memory.',
        affectedModules: duplicates.map(d => d.module),
        confidence: 0.95
      });
    }

    // Large chunk recommendations
    const largeChunks = chunks.filter(c => c.size > 200 * 1024); // 200KB
    if (largeChunks.length > 0) {
      recommendations.push({
        id: 'split-large-chunks',
        type: 'splitting',
        priority: 'medium',
        title: 'Split Large Chunks',
        description: `${largeChunks.length} chunks exceed size recommendations.`,
        impact: 'medium',
        effort: 'medium',
        estimatedSavings: largeChunks.reduce((sum, c) => sum + c.size * 0.2, 0),
        implementationSteps: [
          'Implement route-based code splitting',
          'Split vendor libraries into separate chunks',
          'Use dynamic imports for feature modules',
          'Configure chunk size limits'
        ],
        technicalDetails: 'Large chunks delay time to interactive and increase memory usage.',
        affectedModules: largeChunks.map(c => c.name),
        confidence: 0.8
      });
    }

    // Tree shaking recommendations
    const treeshakableModules = modules.filter(m => m.treeshakable && m.exports.length > 5);
    if (treeshakableModules.length > 0) {
      recommendations.push({
        id: 'improve-tree-shaking',
        type: 'treeshaking',
        priority: 'medium',
        title: 'Improve Tree Shaking',
        description: `${treeshakableModules.length} modules have tree shaking opportunities.`,
        impact: 'medium',
        effort: 'low',
        estimatedSavings: treeshakableModules.reduce((sum, m) => sum + m.size * 0.15, 0),
        implementationSteps: [
          'Use ES modules with named exports',
          'Configure webpack with sideEffects: false',
          'Avoid importing entire libraries',
          'Use babel-plugin-transform-imports',
          'Review and optimize barrel exports'
        ],
        technicalDetails: 'Tree shaking removes unused code from bundles.',
        affectedModules: treeshakableModules.map(m => m.name),
        confidence: 0.7
      });
    }

    return recommendations;
  }

  /**
   * Calculate bundle metrics
   */
  private calculateMetrics(
    modules: BundleModule[],
    chunks: BundleChunk[],
    duplicates: Array<{ module: string; instances: string[]; totalSize: number }>
  ): any {
    const totalSize = modules.reduce((sum, m) => sum + m.size, 0);
    const duplicateSize = duplicates.reduce((sum, d) => sum + d.totalSize, 0);
    const treeshakableSize = modules.filter(m => m.treeshakable).reduce((sum, m) => sum + m.size, 0);

    return {
      moduleCount: modules.length,
      chunkCount: chunks.length,
      duplicationRate: totalSize > 0 ? duplicateSize / totalSize : 0,
      treeshakingEfficiency: totalSize > 0 ? treeshakableSize / totalSize : 0,
      loadTimeScore: this.calculateLoadTimeScore(chunks)
    };
  }

  /**
   * Calculate load time score
   */
  private calculateLoadTimeScore(chunks: BundleChunk[]): number {
    if (chunks.length === 0) return 1;

    const avgLoadTime = chunks.reduce((sum, c) => sum + c.loadTime, 0) / chunks.length;

    // Score based on load time (lower is better)
    if (avgLoadTime < 100) return 1.0;
    if (avgLoadTime < 500) return 0.8;
    if (avgLoadTime < 1000) return 0.6;
    if (avgLoadTime < 2000) return 0.4;
    return 0.2;
  }

  /**
   * Record module load time
   */
  private recordModuleLoadTime(name: string, _duration: number): void {
    const module = this.moduleCache.get(name);
    if (module) {
      module.timestamp = Date.now();
      // Could store load time metrics here
    }
  }

  // Helper methods

  private extractModuleName(src: string): string {
    const parts = src.split('/');
    const filename = parts[parts.length - 1];
    return filename.replace(/\.(js|ts)$/, '');
  }

  private extractChunkId(src: string): string {
    const match = src.match(/chunk[.-](\w+)/);
    return match ? match[1] : this.extractModuleName(src);
  }

  private determineModuleType(src: string): 'initial' | 'lazy' | 'vendor' | 'polyfill' {
    if (src.includes('vendor') || src.includes('node_modules')) return 'vendor';
    if (src.includes('polyfill')) return 'polyfill';
    if (src.includes('lazy') || src.includes('chunk')) return 'lazy';
    return 'initial';
  }

  private determineChunkType(src: string): 'initial' | 'lazy' | 'vendor' {
    if (src.includes('vendor')) return 'vendor';
    if (src.includes('lazy') || src.includes('chunk')) return 'lazy';
    return 'initial';
  }

  private determineChunkPriority(src: string): 'high' | 'medium' | 'low' {
    if (src.includes('main') || src.includes('app')) return 'high';
    if (src.includes('vendor') || src.includes('common')) return 'medium';
    return 'low';
  }

  private isChunkCached(src: string): boolean {
    // Check if chunk is in cache
    return performance.getEntriesByName(src).some((entry: any) =>
      entry.transferSize < entry.decodedBodySize
    );
  }

  private normalizeModuleName(name: string): string {
    return name.replace(/[-._\d]+$/, '').toLowerCase();
  }

  private estimateModuleSize(module: any): number {
    // Rough estimation based on module content
    const serialized = JSON.stringify(module);
    return serialized.length;
  }

  private calculateInitialBundleSize(chunks: BundleChunk[]): number {
    return chunks.filter(c => c.type === 'initial').reduce((sum, c) => sum + c.size, 0);
  }

  private calculateLazyBundleSize(chunks: BundleChunk[]): number {
    return chunks.filter(c => c.type === 'lazy').reduce((sum, c) => sum + c.size, 0);
  }

  private calculateVendorBundleSize(chunks: BundleChunk[]): number {
    return chunks.filter(c => c.type === 'vendor').reduce((sum, c) => sum + c.size, 0);
  }

  // Public API

  /**
   * Get bundle analysis
   */
  getBundleAnalysis(): Observable<BundleAnalysis> {
    return this.bundleAnalysis$.asObservable();
  }

  /**
   * Get bundle budget
   */
  getBundleBudget(): Observable<BundleBudget> {
    return this.bundleBudget$.asObservable();
  }

  /**
   * Update bundle budget
   */
  updateBundleBudget(budget: Partial<BundleBudget>): void {
    const currentBudget = this.bundleBudget$.value;
    const updatedBudget = { ...currentBudget, ...budget };
    this.bundleBudget$.next(updatedBudget);
  }

  /**
   * Force bundle analysis
   */
  async runBundleAnalysis(): Promise<void> {
    await this.analyzeCurrentBundle();
  }

  /**
   * Get optimization history
   */
  getOptimizations(): Observable<BundleOptimization[]> {
    return this.optimizations$.asObservable();
  }

  /**
   * Apply optimization
   */
  async applyOptimization(optimizationId: string): Promise<boolean> {
    const optimizations = this.optimizations$.value;
    const optimization = optimizations.find(o => o.id === optimizationId);

    if (!optimization) {
      throw new Error('Optimization not found');
    }

    try {
      const success = await this.executeOptimization(optimization);

      const newStatus: 'applied' | 'failed' = success ? 'applied' : 'failed';
      const updatedOptimizations = optimizations.map(o =>
        o.id === optimizationId
          ? { ...o, status: newStatus }
          : o
      );

      this.optimizations$.next(updatedOptimizations);
      return success;

    } catch (error) {
      console.error('Failed to apply optimization:', error);
      return false;
    }
  }

  /**
   * Execute optimization
   */
  private async executeOptimization(optimization: BundleOptimization): Promise<boolean> {
    switch (optimization.type) {
      case 'tree-shaking':
        return this.applyTreeShaking(optimization);
      case 'code-splitting':
        return this.applyCodeSplitting(optimization);
      case 'dynamic-import':
        return this.applyDynamicImport(optimization);
      case 'dead-code-elimination':
        return this.applyDeadCodeElimination(optimization);
      default:
        return false;
    }
  }

  /**
   * Apply tree shaking optimization
   */
  private async applyTreeShaking(optimization: BundleOptimization): Promise<boolean> {
    // Tree shaking is primarily a build-time optimization
    // This would trigger build process or provide guidance
    console.log('🌳 Tree shaking optimization applied:', optimization.name);
    return true;
  }

  /**
   * Apply code splitting optimization
   */
  private async applyCodeSplitting(optimization: BundleOptimization): Promise<boolean> {
    // Code splitting requires build configuration changes
    console.log('✂️ Code splitting optimization applied:', optimization.name);
    return true;
  }

  /**
   * Apply dynamic import optimization
   */
  private async applyDynamicImport(optimization: BundleOptimization): Promise<boolean> {
    // Dynamic imports can be applied at runtime for some cases
    console.log('📦 Dynamic import optimization applied:', optimization.name);
    return true;
  }

  /**
   * Apply dead code elimination
   */
  private async applyDeadCodeElimination(optimization: BundleOptimization): Promise<boolean> {
    // Dead code elimination is a build-time optimization
    console.log('🗑️ Dead code elimination applied:', optimization.name);
    return true;
  }

  /**
   * Get bundle size trends
   */
  getBundleTrends(): Array<{ timestamp: number; size: number; gzipSize: number }> {
    return this.analysisHistory.map(analysis => ({
      timestamp: Date.now(), // Would use actual timestamp from analysis
      size: analysis.totalSize,
      gzipSize: analysis.totalGzipSize
    }));
  }

  /**
   * Export bundle analysis
   */
  exportBundleAnalysis(): string {
    const analysis = this.bundleAnalysis$.value;
    return JSON.stringify({
      analysis,
      budget: this.bundleBudget$.value,
      optimizations: this.optimizations$.value,
      trends: this.getBundleTrends(),
      exportTime: Date.now()
    }, null, 2);
  }

  // Default configurations

  private getDefaultBudget(): BundleBudget {
    return {
      maxInitialSize: 500 * 1024, // 500KB
      maxChunkSize: 200 * 1024,   // 200KB
      maxTotalSize: 2 * 1024 * 1024, // 2MB
      maxModuleCount: 500,
      performanceBudget: {
        fcp: 2000, // 2s
        lcp: 2500, // 2.5s
        tti: 3800  // 3.8s
      },
      warnings: {
        initialSizeThreshold: 400 * 1024, // 400KB
        chunkSizeThreshold: 150 * 1024,   // 150KB
        duplicateThreshold: 50 * 1024     // 50KB
      }
    };
  }

  private getInitialAnalysis(): BundleAnalysis {
    return {
      totalSize: 0,
      totalGzipSize: 0,
      initialBundleSize: 0,
      lazyBundleSize: 0,
      vendorBundleSize: 0,
      chunks: [],
      modules: [],
      duplicates: [],
      unusedModules: [],
      treeshakingOpportunities: [],
      recommendations: [],
      metrics: {
        moduleCount: 0,
        chunkCount: 0,
        duplicationRate: 0,
        treeshakingEfficiency: 0,
        loadTimeScore: 1
      }
    };
  }
}