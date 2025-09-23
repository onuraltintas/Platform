import { Injectable, inject } from '@angular/core';
import { Observable, BehaviorSubject, of, timer, EMPTY } from 'rxjs';
import { switchMap, catchError, tap, shareReplay, timeout } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';

export interface SecurityModuleConfig {
  name: string;
  priority: 'critical' | 'high' | 'medium' | 'low';
  loadTrigger: 'immediate' | 'first-use' | 'background' | 'user-action';
  timeout: number;
  fallback?: () => any;
  dependencies?: string[];
}

export interface SecurityLoadResult {
  moduleName: string;
  loaded: boolean;
  loadTime: number;
  error?: string;
  fallbackUsed: boolean;
}

export interface SecurityBudget {
  totalBudget: number; // ms
  usedBudget: number;
  remainingBudget: number;
  modules: Array<{
    name: string;
    budgetUsed: number;
    isWithinBudget: boolean;
  }>;
}

/**
 * Lazy Security Loader Service
 * Intelligently loads security modules based on environment and usage patterns
 */
@Injectable({
  providedIn: 'root'
})
export class LazySecurityLoaderService {
  private loadedModules = new Map<string, any>();
  private loadingPromises = new Map<string, Promise<any>>();
  private loadResults = new Map<string, SecurityLoadResult>();

  private securityBudget$ = new BehaviorSubject<SecurityBudget>(this.getInitialBudget());
  private loadOrder: string[] = [];

  private readonly moduleConfigs: Record<string, SecurityModuleConfig> = {
    'encryption': {
      name: 'encryption',
      priority: 'high',
      loadTrigger: environment.production ? 'first-use' : 'background',
      timeout: 2000,
      fallback: () => this.createBasicEncryption(),
      dependencies: []
    },
    'integrity-check': {
      name: 'integrity-check',
      priority: 'medium',
      loadTrigger: environment.production ? 'first-use' : 'background',
      timeout: 1500,
      fallback: () => this.createBasicIntegrityCheck(),
      dependencies: ['encryption']
    },
    'audit-logging': {
      name: 'audit-logging',
      priority: 'low',
      loadTrigger: 'background',
      timeout: 3000,
      fallback: () => this.createNoOpAuditLogger(),
      dependencies: []
    },
    'advanced-validation': {
      name: 'advanced-validation',
      priority: 'medium',
      loadTrigger: 'user-action',
      timeout: 2500,
      fallback: () => this.createBasicValidation(),
      dependencies: ['encryption']
    },
    'secure-storage': {
      name: 'secure-storage',
      priority: 'critical',
      loadTrigger: 'immediate',
      timeout: 1000,
      fallback: () => this.createFallbackStorage(),
      dependencies: []
    }
  };

  private readonly PERFORMANCE_BUDGET = {
    development: 50, // 50ms total budget
    staging: 100,    // 100ms total budget
    production: 200  // 200ms total budget
  };

  constructor() {
    this.initializeSecurityLoading();
  }

  /**
   * Initialize security loading based on environment and triggers
   */
  private initializeSecurityLoading(): void {
    // Load critical modules immediately
    this.loadCriticalModules();

    // Schedule background loading
    this.scheduleBackgroundLoading();

    // Setup performance monitoring
    this.setupPerformanceMonitoring();
  }

  /**
   * Load critical security modules immediately
   */
  private loadCriticalModules(): void {
    const criticalModules = Object.values(this.moduleConfigs)
      .filter(config => config.priority === 'critical' || config.loadTrigger === 'immediate')
      .sort((a, b) => this.getPriorityWeight(a.priority) - this.getPriorityWeight(b.priority));

    criticalModules.forEach(config => {
      this.loadModule(config.name, true);
    });
  }

  /**
   * Schedule background loading of non-critical modules
   */
  private scheduleBackgroundLoading(): void {
    // Wait for page to be interactive
    if (typeof requestIdleCallback !== 'undefined') {
      requestIdleCallback(() => {
        this.loadBackgroundModules();
      }, { timeout: 5000 });
    } else {
      // Fallback for browsers without requestIdleCallback
      setTimeout(() => {
        this.loadBackgroundModules();
      }, 2000);
    }
  }

  /**
   * Load modules marked for background loading
   */
  private loadBackgroundModules(): void {
    const backgroundModules = Object.values(this.moduleConfigs)
      .filter(config => config.loadTrigger === 'background')
      .sort((a, b) => this.getPriorityWeight(a.priority) - this.getPriorityWeight(b.priority));

    // Load modules with delay to avoid blocking
    backgroundModules.forEach((config, index) => {
      setTimeout(() => {
        this.loadModule(config.name, false);
      }, index * 500); // 500ms delay between each module
    });
  }

  /**
   * Load a specific security module
   */
  async loadModule(moduleName: string, isBlocking: boolean = false): Promise<any> {
    const startTime = performance.now();

    // Check if already loaded
    if (this.loadedModules.has(moduleName)) {
      return this.loadedModules.get(moduleName);
    }

    // Check if already loading
    if (this.loadingPromises.has(moduleName)) {
      return this.loadingPromises.get(moduleName);
    }

    const config = this.moduleConfigs[moduleName];
    if (!config) {
      throw new Error(`Unknown security module: ${moduleName}`);
    }

    // Check dependencies
    await this.loadDependencies(config.dependencies || []);

    // Create loading promise
    const loadingPromise = this.createLoadingPromise(moduleName, config, isBlocking);
    this.loadingPromises.set(moduleName, loadingPromise);

    try {
      const module = await loadingPromise;
      const loadTime = performance.now() - startTime;

      this.loadedModules.set(moduleName, module);
      this.recordLoadResult(moduleName, true, loadTime, false);
      this.updateSecurityBudget(moduleName, loadTime);

      console.log(`🔒 Security module '${moduleName}' loaded in ${loadTime.toFixed(2)}ms`);
      return module;

    } catch (error) {
      const loadTime = performance.now() - startTime;
      console.warn(`⚠️ Failed to load security module '${moduleName}':`, error);

      // Use fallback if available
      if (config.fallback) {
        const fallbackModule = config.fallback();
        this.loadedModules.set(moduleName, fallbackModule);
        this.recordLoadResult(moduleName, true, loadTime, true);
        console.log(`🔄 Using fallback for '${moduleName}'`);
        return fallbackModule;
      }

      this.recordLoadResult(moduleName, false, loadTime, false, error.message);
      throw error;

    } finally {
      this.loadingPromises.delete(moduleName);
    }
  }

  /**
   * Create loading promise with timeout and error handling
   */
  private createLoadingPromise(
    moduleName: string,
    config: SecurityModuleConfig,
    isBlocking: boolean
  ): Promise<any> {
    const loadPromise = this.dynamicImportModule(moduleName);

    if (!isBlocking) {
      // Non-blocking: use timeout and fallback
      return Promise.race([
        loadPromise,
        this.createTimeoutPromise(config.timeout)
      ]).catch(error => {
        if (config.fallback) {
          return config.fallback();
        }
        throw error;
      });
    }

    return loadPromise;
  }

  /**
   * Dynamic import for security modules
   */
  private async dynamicImportModule(moduleName: string): Promise<any> {
    switch (moduleName) {
      case 'encryption':
        const { AdvancedEncryptionService } = await import('./advanced-encryption.service');
        return new AdvancedEncryptionService();

      case 'integrity-check':
        const { IntegrityCheckService } = await import('./integrity-check.service');
        return new IntegrityCheckService();

      case 'audit-logging':
        const { AuditLoggingService } = await import('./audit-logging.service');
        return new AuditLoggingService();

      case 'advanced-validation':
        const { AdvancedValidationService } = await import('./advanced-validation.service');
        return new AdvancedValidationService();

      case 'secure-storage':
        const { SecureStorageService } = await import('./secure-storage.service');
        return new SecureStorageService();

      default:
        throw new Error(`Unknown module: ${moduleName}`);
    }
  }

  /**
   * Load module dependencies
   */
  private async loadDependencies(dependencies: string[]): Promise<void> {
    for (const dep of dependencies) {
      if (!this.loadedModules.has(dep)) {
        await this.loadModule(dep, false);
      }
    }
  }

  /**
   * Create timeout promise
   */
  private createTimeoutPromise(timeoutMs: number): Promise<never> {
    return new Promise((_, reject) => {
      setTimeout(() => {
        reject(new Error(`Module load timeout after ${timeoutMs}ms`));
      }, timeoutMs);
    });
  }

  /**
   * Get security module with automatic loading
   */
  async getSecurityModule<T = any>(moduleName: string): Promise<T> {
    if (this.loadedModules.has(moduleName)) {
      return this.loadedModules.get(moduleName);
    }

    return this.loadModule(moduleName, false);
  }

  /**
   * Get security module synchronously (returns null if not loaded)
   */
  getSecurityModuleSync<T = any>(moduleName: string): T | null {
    return this.loadedModules.get(moduleName) || null;
  }

  /**
   * Preload modules based on user behavior
   */
  preloadModules(moduleNames: string[]): void {
    moduleNames.forEach(name => {
      if (!this.loadedModules.has(name) && !this.loadingPromises.has(name)) {
        // Load with low priority
        setTimeout(() => {
          this.loadModule(name, false).catch(error => {
            console.warn(`Preload failed for ${name}:`, error);
          });
        }, 100);
      }
    });
  }

  /**
   * Get security budget information
   */
  getSecurityBudget(): Observable<SecurityBudget> {
    return this.securityBudget$.asObservable();
  }

  /**
   * Get load results for monitoring
   */
  getLoadResults(): Array<SecurityLoadResult> {
    return Array.from(this.loadResults.values());
  }

  /**
   * Check if all critical modules are loaded
   */
  areCriticalModulesLoaded(): boolean {
    const criticalModules = Object.values(this.moduleConfigs)
      .filter(config => config.priority === 'critical')
      .map(config => config.name);

    return criticalModules.every(name => this.loadedModules.has(name));
  }

  // Fallback implementations

  private createBasicEncryption(): any {
    return {
      encrypt: (data: string) => btoa(data),
      decrypt: (data: string) => atob(data),
      generateHash: (data: string) => data.length.toString()
    };
  }

  private createBasicIntegrityCheck(): any {
    return {
      generateChecksum: (data: string) => data.length.toString(),
      verifyChecksum: () => true
    };
  }

  private createNoOpAuditLogger(): any {
    return {
      log: () => {}, // No-op
      getAuditTrail: () => []
    };
  }

  private createBasicValidation(): any {
    return {
      validateToken: () => true,
      validateRequest: () => true
    };
  }

  private createFallbackStorage(): any {
    return {
      setItem: (key: string, value: any) => localStorage.setItem(key, JSON.stringify(value)),
      getItem: (key: string) => JSON.parse(localStorage.getItem(key) || 'null'),
      removeItem: (key: string) => localStorage.removeItem(key)
    };
  }

  // Utility methods

  private getPriorityWeight(priority: string): number {
    const weights = { critical: 1, high: 2, medium: 3, low: 4 };
    return weights[priority] || 5;
  }

  private recordLoadResult(
    moduleName: string,
    loaded: boolean,
    loadTime: number,
    fallbackUsed: boolean,
    error?: string
  ): void {
    const result: SecurityLoadResult = {
      moduleName,
      loaded,
      loadTime,
      fallbackUsed,
      error
    };

    this.loadResults.set(moduleName, result);
    this.loadOrder.push(moduleName);
  }

  private updateSecurityBudget(moduleName: string, loadTime: number): void {
    const currentBudget = this.securityBudget$.value;
    const totalBudget = this.getTotalBudget();

    const newUsedBudget = currentBudget.usedBudget + loadTime;
    const newRemainingBudget = Math.max(0, totalBudget - newUsedBudget);

    const moduleEntry = {
      name: moduleName,
      budgetUsed: loadTime,
      isWithinBudget: loadTime <= (totalBudget * 0.2) // Each module should use max 20% of budget
    };

    const updatedModules = [...currentBudget.modules.filter(m => m.name !== moduleName), moduleEntry];

    this.securityBudget$.next({
      totalBudget,
      usedBudget: newUsedBudget,
      remainingBudget: newRemainingBudget,
      modules: updatedModules
    });
  }

  private getTotalBudget(): number {
    const env = environment.production ? 'production' : 'development';
    return this.PERFORMANCE_BUDGET[env] || this.PERFORMANCE_BUDGET.development;
  }

  private getInitialBudget(): SecurityBudget {
    const totalBudget = this.getTotalBudget();
    return {
      totalBudget,
      usedBudget: 0,
      remainingBudget: totalBudget,
      modules: []
    };
  }

  private setupPerformanceMonitoring(): void {
    // Monitor budget every 30 seconds
    timer(0, 30000).subscribe(() => {
      const budget = this.securityBudget$.value;

      if (budget.usedBudget > budget.totalBudget * 0.8) {
        console.warn(`⚠️ Security budget usage high: ${budget.usedBudget.toFixed(2)}ms / ${budget.totalBudget}ms`);
      }

      // Check for budget violations
      const violatingModules = budget.modules.filter(m => !m.isWithinBudget);
      if (violatingModules.length > 0) {
        console.warn('⚠️ Modules exceeding budget:', violatingModules.map(m => m.name));
      }
    });
  }
}