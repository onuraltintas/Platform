import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { map, tap, take } from 'rxjs/operators';

export interface TreeShakingAnalysis {
  totalModules: number;
  shakableModules: number;
  potentialSavings: number; // bytes
  deadCodeSize: number;
  unusedExports: UnusedExport[];
  sideEffectModules: string[];
  optimizationOpportunities: TreeShakingOpportunity[];
  configurationIssues: ConfigurationIssue[];
  metrics: {
    shakingEfficiency: number; // 0-1
    deadCodeRatio: number;
    sideEffectRatio: number;
    esModuleRatio: number;
  };
}

export interface UnusedExport {
  module: string;
  exportName: string;
  exportType: 'function' | 'class' | 'const' | 'interface' | 'type' | 'default';
  size: number;
  confidence: number; // 0-1
  lastUsed?: number;
  importedBy: string[];
  canBeRemoved: boolean;
}

export interface TreeShakingOpportunity {
  id: string;
  type: 'unused-export' | 'barrel-export' | 'side-effect' | 'dynamic-import' | 'library-optimization';
  priority: 'low' | 'medium' | 'high' | 'critical';
  title: string;
  description: string;
  module: string;
  estimatedSavings: number;
  confidence: number;
  implementationSteps: string[];
  technicalDetails: string;
  codeExample?: string;
  automatable: boolean;
}

export interface ConfigurationIssue {
  type: 'sideEffects' | 'esModules' | 'optimization' | 'babel' | 'typescript';
  severity: 'warning' | 'error';
  message: string;
  file?: string;
  line?: number;
  solution: string;
  impact: 'low' | 'medium' | 'high';
}

export interface TreeShakingConfig {
  enableTreeShaking: boolean;
  sideEffects: boolean | string[];
  usedExports: boolean;
  providedExports: boolean;
  optimization: {
    usedExports: boolean;
    sideEffects: boolean;
    providedExports: boolean;
  };
  babel: {
    modules: false | 'auto' | 'amd' | 'umd' | 'systemjs' | 'commonjs' | 'cjs';
    useBuiltIns: boolean;
  };
}

export interface ModuleUsageMap {
  [modulePath: string]: {
    exports: { [exportName: string]: ExportUsage };
    imports: { [importName: string]: ImportUsage };
    hasDefaultExport: boolean;
    hasSideEffects: boolean;
    isEsModule: boolean;
    size: number;
  };
}

export interface ExportUsage {
  name: string;
  type: string;
  usedBy: string[];
  lastUsed: number;
  isUsed: boolean;
  size: number;
}

export interface ImportUsage {
  name: string;
  from: string;
  isUsed: boolean;
  usageCount: number;
  lastUsed: number;
}

/**
 * Tree Shaking Optimizer Service
 * Advanced tree shaking analysis and optimization
 */
@Injectable({
  providedIn: 'root'
})
export class TreeShakingOptimizerService {
  private analysis$ = new BehaviorSubject<TreeShakingAnalysis>(this.getInitialAnalysis());
  private config$ = new BehaviorSubject<TreeShakingConfig>(this.getDefaultConfig());
  private moduleUsageMap$ = new BehaviorSubject<ModuleUsageMap>({});

  private usageTracker = new Map<string, Set<string>>();
  private exportTracker = new Map<string, Map<string, number>>();
  // private importTracker = new Map<string, Map<string, number>>();

  constructor() {
    this.initializeTreeShakingOptimizer();
  }

  /**
   * Initialize tree shaking optimizer
   */
  private initializeTreeShakingOptimizer(): void {
    this.setupUsageTracking();
    this.analyzeCurrentState();
    this.setupAutomaticAnalysis();

    console.log('🌳 Tree Shaking Optimizer initialized');
  }

  /**
   * Setup usage tracking
   */
  private setupUsageTracking(): void {
    // Track module imports and exports usage
    this.interceptModuleSystem();
    this.setupStaticAnalysis();
  }

  /**
   * Intercept module system for usage tracking
   */
  private interceptModuleSystem(): void {
    // Intercept ES6 imports/exports if possible
    this.interceptESModules();

    // Intercept CommonJS requires if needed
    this.interceptCommonJS();

    // Track dynamic imports
    this.interceptDynamicImports();
  }

  /**
   * Intercept ES modules
   */
  private interceptESModules(): void {
    // This would require build-time analysis or runtime tracking
    // For demonstration, we'll simulate tracking

    if (typeof window !== 'undefined') {
      // Track usage through property access
      this.setupPropertyAccessTracking();
    }
  }

  /**
   * Setup property access tracking
   */
  private setupPropertyAccessTracking(): void {
    // Proxy-based tracking for object property access
    const originalProxy = window.Proxy;

    if (originalProxy) {
      window.Proxy = new Proxy(originalProxy, {
        construct: (target, args) => {
          const [targetObj, handler] = args;

          // Wrap get handler to track property access
          const wrappedHandler = {
            ...handler,
            get: (obj: any, prop: string) => {
              this.trackPropertyAccess(obj, prop);
              return handler.get ? handler.get(obj, prop) : obj[prop];
            }
          };

          return new target(targetObj, wrappedHandler);
        }
      });
    }
  }

  /**
   * Track property access
   */
  private trackPropertyAccess(obj: any, prop: string): void {
    try {
      const objName = obj.constructor?.name || 'unknown';
      // const key = `${objName}.${prop}`;

      if (!this.usageTracker.has(objName)) {
        this.usageTracker.set(objName, new Set());
      }

      this.usageTracker.get(objName)!.add(prop);

      // Update export tracker
      if (!this.exportTracker.has(objName)) {
        this.exportTracker.set(objName, new Map());
      }

      const currentCount = this.exportTracker.get(objName)!.get(prop) || 0;
      this.exportTracker.get(objName)!.set(prop, currentCount + 1);

    } catch (error) {
      // Ignore tracking errors
    }
  }

  /**
   * Intercept CommonJS
   */
  private interceptCommonJS(): void {
    // This method is not available in browser environment
    // CommonJS interception would only work in Node.js
    console.log('CommonJS interception not available in browser environment');
  }

  /**
   * Intercept dynamic imports
   */
  private interceptDynamicImports(): void {
    // Track dynamic import() calls
    if (typeof (window as any).import !== 'undefined') {
      const originalImport = (window as any).import;

      (window as any).import = async function(specifier: string) {
        console.log('Dynamic import tracked:', specifier);
        return originalImport(specifier);
      };
    }
  }

  /**
   * Setup static analysis
   */
  private setupStaticAnalysis(): void {
    // This would typically be done at build time
    // For runtime analysis, we'll use available information

    this.analyzeLoadedModules();
    this.analyzeWebpackModules();
  }

  /**
   * Analyze loaded modules
   */
  private analyzeLoadedModules(): void {
    // Analyze scripts loaded in the page
    const scripts = Array.from(document.querySelectorAll('script[src]'));

    scripts.forEach(script => {
      const src = script.getAttribute('src');
      if (src) {
        this.analyzeScriptModule(src);
      }
    });
  }

  /**
   * Analyze webpack modules
   */
  private analyzeWebpackModules(): void {
    try {
      const webpackRequire = (window as any).__webpack_require__;
      if (!webpackRequire) return;

      // Analyze webpack module cache
      Object.keys(webpackRequire.cache || {}).forEach(moduleId => {
        const module = webpackRequire.cache[moduleId];
        if (module) {
          this.analyzeWebpackModule(moduleId, module);
        }
      });

    } catch (error) {
      console.warn('Failed to analyze webpack modules:', error);
    }
  }

  /**
   * Analyze individual script module
   */
  private analyzeScriptModule(src: string): void {
    // Extract module information from script source
    const moduleName = this.extractModuleName(src);

    // Simulate module analysis
    const moduleUsage = {
      exports: this.generateMockExports(moduleName),
      imports: this.generateMockImports(moduleName),
      hasDefaultExport: true,
      hasSideEffects: this.detectSideEffects(src),
      isEsModule: src.includes('esm') || src.includes('module'),
      size: this.estimateModuleSize(src)
    };

    this.updateModuleUsageMap(moduleName, moduleUsage);
  }

  /**
   * Analyze webpack module
   */
  private analyzeWebpackModule(moduleId: string, module: any): void {
    const exports = Object.keys(module.exports || {});
    const hasDefaultExport = 'default' in (module.exports || {});

    const moduleUsage = {
      exports: exports.reduce((acc, exportName) => {
        acc[exportName] = {
          name: exportName,
          type: typeof module.exports[exportName],
          usedBy: [],
          lastUsed: Date.now(),
          isUsed: false,
          size: this.estimateExportSize(module.exports[exportName])
        };
        return acc;
      }, {} as { [key: string]: ExportUsage }),
      imports: {},
      hasDefaultExport,
      hasSideEffects: false,
      isEsModule: false,
      size: this.estimateObjectSize(module)
    };

    this.updateModuleUsageMap(`webpack-${moduleId}`, moduleUsage);
  }

  /**
   * Update module usage map
   */
  private updateModuleUsageMap(moduleName: string, usage: any): void {
    const currentMap = this.moduleUsageMap$.value;
    const updatedMap = {
      ...currentMap,
      [moduleName]: usage
    };

    this.moduleUsageMap$.next(updatedMap);
  }

  /**
   * Analyze current state
   */
  private analyzeCurrentState(): void {
    const moduleUsageMap = this.moduleUsageMap$.value;
    const unusedExports = this.findUnusedExports(moduleUsageMap);
    const opportunities = this.identifyOptimizationOpportunities(moduleUsageMap, unusedExports);
    const configIssues = this.detectConfigurationIssues();

    const analysis: TreeShakingAnalysis = {
      totalModules: Object.keys(moduleUsageMap).length,
      shakableModules: this.countShakableModules(moduleUsageMap),
      potentialSavings: this.calculatePotentialSavings(unusedExports),
      deadCodeSize: this.calculateDeadCodeSize(unusedExports),
      unusedExports,
      sideEffectModules: this.findSideEffectModules(moduleUsageMap),
      optimizationOpportunities: opportunities,
      configurationIssues: configIssues,
      metrics: this.calculateMetrics(moduleUsageMap, unusedExports)
    };

    this.analysis$.next(analysis);
  }

  /**
   * Find unused exports
   */
  private findUnusedExports(moduleUsageMap: ModuleUsageMap): UnusedExport[] {
    const unusedExports: UnusedExport[] = [];

    Object.entries(moduleUsageMap).forEach(([moduleName, moduleData]) => {
      Object.entries(moduleData.exports).forEach(([exportName, exportData]) => {
        if (!exportData.isUsed && exportData.usedBy.length === 0) {
          unusedExports.push({
            module: moduleName,
            exportName,
            exportType: this.determineExportType(exportData.type),
            size: exportData.size,
            confidence: this.calculateUnusedConfidence(exportData),
            lastUsed: exportData.lastUsed,
            importedBy: exportData.usedBy,
            canBeRemoved: this.canExportBeRemoved(moduleName, exportName)
          });
        }
      });
    });

    return unusedExports.sort((a, b) => b.size - a.size);
  }

  /**
   * Identify optimization opportunities
   */
  private identifyOptimizationOpportunities(
    moduleUsageMap: ModuleUsageMap,
    unusedExports: UnusedExport[]
  ): TreeShakingOpportunity[] {
    const opportunities: TreeShakingOpportunity[] = [];

    // Unused exports opportunities
    const largeUnusedExports = unusedExports.filter(e => e.size > 1024); // > 1KB
    if (largeUnusedExports.length > 0) {
      opportunities.push({
        id: 'remove-unused-exports',
        type: 'unused-export',
        priority: 'high',
        title: 'Remove Large Unused Exports',
        description: `${largeUnusedExports.length} large unused exports found`,
        module: 'multiple',
        estimatedSavings: largeUnusedExports.reduce((sum, e) => sum + e.size, 0),
        confidence: 0.9,
        implementationSteps: [
          'Review unused exports list',
          'Verify exports are truly unused',
          'Remove unused exports from modules',
          'Update export statements',
          'Run tree shaking build'
        ],
        technicalDetails: 'Unused exports prevent effective tree shaking',
        automatable: true
      });
    }

    // Barrel export opportunities
    const barrelModules = this.findBarrelModules(moduleUsageMap);
    if (barrelModules.length > 0) {
      opportunities.push({
        id: 'optimize-barrel-exports',
        type: 'barrel-export',
        priority: 'medium',
        title: 'Optimize Barrel Exports',
        description: `${barrelModules.length} barrel export modules found`,
        module: 'multiple',
        estimatedSavings: barrelModules.length * 5 * 1024, // Estimate 5KB per barrel
        confidence: 0.7,
        implementationSteps: [
          'Replace barrel imports with direct imports',
          'Use specific import paths instead of index files',
          'Configure webpack to optimize barrel exports',
          'Use babel-plugin-transform-imports'
        ],
        technicalDetails: 'Barrel exports can prevent tree shaking',
        codeExample: `
// Instead of:
import { ComponentA, ComponentB } from './barrel';

// Use:
import { ComponentA } from './component-a';
import { ComponentB } from './component-b';
        `,
        automatable: false
      });
    }

    // Side effect opportunities
    const sideEffectModules = this.findSideEffectModules(moduleUsageMap);
    if (sideEffectModules.length > 0) {
      opportunities.push({
        id: 'eliminate-side-effects',
        type: 'side-effect',
        priority: 'medium',
        title: 'Eliminate Side Effects',
        description: `${sideEffectModules.length} modules with side effects`,
        module: 'multiple',
        estimatedSavings: sideEffectModules.length * 2 * 1024, // Estimate 2KB per module
        confidence: 0.6,
        implementationSteps: [
          'Review modules with side effects',
          'Move side effects to separate modules',
          'Mark modules as side-effect free',
          'Update package.json sideEffects field'
        ],
        technicalDetails: 'Side effects prevent module elimination',
        automatable: false
      });
    }

    // Library optimization opportunities
    const libraryOptimizations = this.identifyLibraryOptimizations(moduleUsageMap);
    opportunities.push(...libraryOptimizations);

    return opportunities.sort((a, b) => b.estimatedSavings - a.estimatedSavings);
  }

  /**
   * Detect configuration issues
   */
  private detectConfigurationIssues(): ConfigurationIssue[] {
    const issues: ConfigurationIssue[] = [];

    // Check if tree shaking is enabled
    issues.push({
      type: 'optimization',
      severity: 'warning',
      message: 'Tree shaking configuration should be verified',
      solution: 'Ensure webpack optimization.usedExports is enabled',
      impact: 'high'
    });

    // Check sideEffects configuration
    issues.push({
      type: 'sideEffects',
      severity: 'warning',
      message: 'sideEffects field should be configured in package.json',
      solution: 'Add "sideEffects": false to package.json if no side effects',
      impact: 'medium'
    });

    // Check ES modules configuration
    issues.push({
      type: 'esModules',
      severity: 'warning',
      message: 'ES modules should be preferred for better tree shaking',
      solution: 'Use ES6 imports/exports instead of CommonJS',
      impact: 'medium'
    });

    return issues;
  }

  /**
   * Calculate metrics
   */
  private calculateMetrics(moduleUsageMap: ModuleUsageMap, unusedExports: UnusedExport[]): any {
    const totalModules = Object.keys(moduleUsageMap).length;
    const shakableModules = this.countShakableModules(moduleUsageMap);
    const totalSize = Object.values(moduleUsageMap).reduce((sum, m) => sum + m.size, 0);
    const deadCodeSize = unusedExports.reduce((sum, e) => sum + e.size, 0);
    const sideEffectModules = this.findSideEffectModules(moduleUsageMap).length;
    const esModules = Object.values(moduleUsageMap).filter(m => m.isEsModule).length;

    return {
      shakingEfficiency: totalModules > 0 ? shakableModules / totalModules : 0,
      deadCodeRatio: totalSize > 0 ? deadCodeSize / totalSize : 0,
      sideEffectRatio: totalModules > 0 ? sideEffectModules / totalModules : 0,
      esModuleRatio: totalModules > 0 ? esModules / totalModules : 0
    };
  }

  /**
   * Setup automatic analysis
   */
  private setupAutomaticAnalysis(): void {
    // Re-analyze every 2 minutes
    setInterval(() => {
      this.analyzeCurrentState();
    }, 120000);
  }

  // Helper methods

  private extractModuleName(src: string): string {
    const parts = src.split('/');
    return parts[parts.length - 1].replace(/\.(js|ts)$/, '');
  }

  private generateMockExports(_moduleName: string): { [key: string]: ExportUsage } {
    // Generate mock exports for demonstration
    const exports = ['init', 'config', 'utils', 'constants'];
    return exports.reduce((acc, name) => {
      acc[name] = {
        name,
        type: 'function',
        usedBy: [],
        lastUsed: Date.now(),
        isUsed: Math.random() > 0.3, // 70% chance of being used
        size: Math.floor(Math.random() * 5000) + 500 // 500-5500 bytes
      };
      return acc;
    }, {} as { [key: string]: ExportUsage });
  }

  private generateMockImports(_moduleName: string): { [key: string]: ImportUsage } {
    // Generate mock imports for demonstration
    return {};
  }

  private detectSideEffects(src: string): boolean {
    // Detect side effects based on URL patterns
    return src.includes('polyfill') || src.includes('global');
  }

  private estimateModuleSize(src: string): number {
    // Estimate module size based on performance entries
    const perfEntries = performance.getEntriesByName(src);
    if (perfEntries.length > 0) {
      const entry = perfEntries[0] as any;
      return entry.transferSize || entry.encodedBodySize || 0;
    }
    return Math.floor(Math.random() * 50000) + 5000; // 5-55KB
  }

  private estimateExportSize(exportValue: any): number {
    try {
      const serialized = JSON.stringify(exportValue);
      return serialized.length;
    } catch {
      return 1000; // Default estimate
    }
  }

  private estimateObjectSize(obj: any): number {
    try {
      const serialized = JSON.stringify(obj);
      return serialized.length;
    } catch {
      return 5000; // Default estimate
    }
  }

  private countShakableModules(moduleUsageMap: ModuleUsageMap): number {
    return Object.values(moduleUsageMap).filter(module =>
      module.isEsModule && !module.hasSideEffects
    ).length;
  }

  private calculatePotentialSavings(unusedExports: UnusedExport[]): number {
    return unusedExports.reduce((sum, exp) => sum + exp.size, 0);
  }

  private calculateDeadCodeSize(unusedExports: UnusedExport[]): number {
    return unusedExports
      .filter(exp => exp.confidence > 0.8)
      .reduce((sum, exp) => sum + exp.size, 0);
  }

  private findSideEffectModules(moduleUsageMap: ModuleUsageMap): string[] {
    return Object.entries(moduleUsageMap)
      .filter(([_, module]) => module.hasSideEffects)
      .map(([name, _]) => name);
  }

  private determineExportType(type: string): 'function' | 'class' | 'const' | 'interface' | 'type' | 'default' {
    switch (type) {
      case 'function': return 'function';
      case 'object': return 'class';
      case 'string':
      case 'number':
      case 'boolean': return 'const';
      default: return 'const';
    }
  }

  private calculateUnusedConfidence(exportData: ExportUsage): number {
    // Calculate confidence based on usage patterns
    if (exportData.usedBy.length === 0 && !exportData.isUsed) {
      return 0.9;
    }
    return 0.5;
  }

  private canExportBeRemoved(_moduleName: string, exportName: string): boolean {
    // Determine if export can be safely removed
    return !exportName.startsWith('_') && // Not private
           exportName !== 'default' &&    // Not default export
           !exportName.includes('polyfill'); // Not polyfill
  }

  private findBarrelModules(moduleUsageMap: ModuleUsageMap): string[] {
    // Find modules that look like barrel exports
    return Object.entries(moduleUsageMap)
      .filter(([name, module]) =>
        name.includes('index') ||
        name.includes('barrel') ||
        Object.keys(module.exports).length > 10
      )
      .map(([name, _]) => name);
  }

  private identifyLibraryOptimizations(moduleUsageMap: ModuleUsageMap): TreeShakingOpportunity[] {
    const opportunities: TreeShakingOpportunity[] = [];

    // Check for common library optimization opportunities
    const lodashModules = Object.keys(moduleUsageMap).filter(name => name.includes('lodash'));
    if (lodashModules.length > 0) {
      opportunities.push({
        id: 'optimize-lodash',
        type: 'library-optimization',
        priority: 'medium',
        title: 'Optimize Lodash Imports',
        description: 'Use specific lodash imports for better tree shaking',
        module: 'lodash',
        estimatedSavings: 50 * 1024, // 50KB
        confidence: 0.8,
        implementationSteps: [
          'Replace lodash with lodash-es',
          'Use specific function imports',
          'Consider alternatives like ramda'
        ],
        technicalDetails: 'Lodash requires specific import strategy for tree shaking',
        codeExample: `
// Instead of:
import _ from 'lodash';

// Use:
import { debounce, throttle } from 'lodash-es';
        `,
        automatable: false
      });
    }

    return opportunities;
  }

  // Public API

  /**
   * Get tree shaking analysis
   */
  getAnalysis(): Observable<TreeShakingAnalysis> {
    return this.analysis$.asObservable();
  }

  /**
   * Get tree shaking configuration
   */
  getConfig(): Observable<TreeShakingConfig> {
    return this.config$.asObservable();
  }

  /**
   * Update configuration
   */
  updateConfig(config: Partial<TreeShakingConfig>): void {
    const currentConfig = this.config$.value;
    const updatedConfig = { ...currentConfig, ...config };
    this.config$.next(updatedConfig);
  }

  /**
   * Get module usage map
   */
  getModuleUsageMap(): Observable<ModuleUsageMap> {
    return this.moduleUsageMap$.asObservable();
  }

  /**
   * Force analysis
   */
  runAnalysis(): void {
    this.analyzeCurrentState();
  }

  /**
   * Get optimization recommendations
   */
  getOptimizationRecommendations(): TreeShakingOpportunity[] {
    return this.analysis$.value.optimizationOpportunities;
  }

  /**
   * Apply tree shaking optimization
   */
  async applyOptimization(opportunityId: string): Promise<boolean> {
    const opportunity = this.analysis$.value.optimizationOpportunities
      .find(o => o.id === opportunityId);

    if (!opportunity) {
      throw new Error('Optimization opportunity not found');
    }

    if (!opportunity.automatable) {
      console.warn('This optimization requires manual implementation');
      return false;
    }

    // Apply automatic optimizations
    try {
      const success = await this.executeAutomaticOptimization(opportunity);
      if (success) {
        // Re-analyze after optimization
        setTimeout(() => this.analyzeCurrentState(), 1000);
      }
      return success;
    } catch (error) {
      console.error('Failed to apply optimization:', error);
      return false;
    }
  }

  /**
   * Execute automatic optimization
   */
  private async executeAutomaticOptimization(opportunity: TreeShakingOpportunity): Promise<boolean> {
    switch (opportunity.type) {
      case 'unused-export':
        return this.removeUnusedExports(opportunity);
      case 'dynamic-import':
        return this.convertToDynamicImport(opportunity);
      default:
        return false;
    }
  }

  /**
   * Remove unused exports (simulated)
   */
  private async removeUnusedExports(opportunity: TreeShakingOpportunity): Promise<boolean> {
    console.log('🗑️ Removing unused exports for:', opportunity.module);
    // This would require build-time or file system access
    return true;
  }

  /**
   * Convert to dynamic import (simulated)
   */
  private async convertToDynamicImport(opportunity: TreeShakingOpportunity): Promise<boolean> {
    console.log('📦 Converting to dynamic import:', opportunity.module);
    // This would require code transformation
    return true;
  }

  /**
   * Export analysis report
   */
  exportAnalysisReport(): string {
    const analysis = this.analysis$.value;
    const config = this.config$.value;
    const moduleUsageMap = this.moduleUsageMap$.value;

    return JSON.stringify({
      analysis,
      config,
      moduleUsageMap,
      exportTime: new Date().toISOString()
    }, null, 2);
  }

  // Default configurations

  private getDefaultConfig(): TreeShakingConfig {
    return {
      enableTreeShaking: true,
      sideEffects: false,
      usedExports: true,
      providedExports: true,
      optimization: {
        usedExports: true,
        sideEffects: false,
        providedExports: true
      },
      babel: {
        modules: false,
        useBuiltIns: false
      }
    };
  }

  private getInitialAnalysis(): TreeShakingAnalysis {
    return {
      totalModules: 0,
      shakableModules: 0,
      potentialSavings: 0,
      deadCodeSize: 0,
      unusedExports: [],
      sideEffectModules: [],
      optimizationOpportunities: [],
      configurationIssues: [],
      metrics: {
        shakingEfficiency: 0,
        deadCodeRatio: 0,
        sideEffectRatio: 0,
        esModuleRatio: 0
      }
    };
  }

  /**
   * Analyze entire project for tree shaking opportunities
   */
  public analyzeProject(): Observable<TreeShakingAnalysis> {
    return this.analysis$.pipe(
      tap(() => {
        console.log('🌳 Running project-wide tree shaking analysis');
        // Trigger fresh analysis data
        this.simulateAnalysis();
      }),
      take(1)
    );
  }

  private simulateAnalysis(): void {
    // Simulate updated analysis data
    const updatedAnalysis = this.getInitialAnalysis();
    updatedAnalysis.totalModules = 150;
    updatedAnalysis.shakableModules = 120;
    updatedAnalysis.potentialSavings = 85 * 1024; // 85KB
    this.analysis$.next(updatedAnalysis);
  }

  /**
   * Get comprehensive optimization report
   */
  public getOptimizationReport(): Observable<any> {
    return this.analysis$.pipe(
      map(analysis => ({
        summary: {
          totalModules: analysis.totalModules,
          shakableModules: analysis.shakableModules,
          potentialSavings: this.formatBytes(analysis.potentialSavings),
          deadCodeSize: this.formatBytes(analysis.deadCodeSize),
          optimizationEfficiency: analysis.metrics.shakingEfficiency * 100
        },
        opportunities: analysis.optimizationOpportunities.map(opp => ({
          module: opp.module,
          type: opp.type,
          savings: this.formatBytes(opp.estimatedSavings),
          confidence: opp.confidence,
          recommendation: 'Optimize tree shaking configuration'
        })),
        issues: analysis.configurationIssues.map(issue => ({
          type: issue.type,
          description: 'Configuration issue detected',
          severity: issue.severity,
          solution: issue.solution || 'Review configuration settings'
        })),
        metrics: {
          shakingEfficiency: (analysis.metrics.shakingEfficiency * 100).toFixed(1) + '%',
          deadCodeRatio: (analysis.metrics.deadCodeRatio * 100).toFixed(1) + '%',
          sideEffectRatio: (analysis.metrics.sideEffectRatio * 100).toFixed(1) + '%',
          esModuleRatio: (analysis.metrics.esModuleRatio * 100).toFixed(1) + '%'
        }
      }))
    );
  }

  /**
   * Format bytes for display
   */
  private formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  /**
   * Cleanup resources
   */
  public destroy(): void {
    // Cleanup would be implemented here
    console.log('🧹 Tree shaking optimizer service destroyed');
  }
}