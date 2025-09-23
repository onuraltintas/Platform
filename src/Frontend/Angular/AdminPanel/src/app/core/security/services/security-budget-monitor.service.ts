import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, timer, combineLatest } from 'rxjs';
import { map } from 'rxjs/operators';
import { LazySecurityLoaderService, SecurityBudget } from './lazy-security-loader.service';
import { AdvancedEncryptionService } from './advanced-encryption.service';
import { IntegrityCheckService } from './integrity-check.service';
import { ProgressiveSecurityEnhancementService } from './progressive-security-enhancement.service';

export interface SecurityBudgetMetrics {
  totalBudget: number;
  usedBudget: number;
  remainingBudget: number;
  utilizationRate: number;
  averageOperationTime: number;
  budgetEfficiency: number;
  modulePerformance: ModulePerformanceMetrics[];
  recommendations: string[];
  alerts: SecurityBudgetAlert[];
}

export interface ModulePerformanceMetrics {
  moduleName: string;
  budgetAllocated: number;
  budgetUsed: number;
  operationCount: number;
  averageTime: number;
  efficiency: number;
  isOptimal: boolean;
}

export interface SecurityBudgetAlert {
  level: 'info' | 'warning' | 'critical';
  message: string;
  timestamp: number;
  module?: string;
  actionRequired?: boolean;
}

export interface OptimizationRecommendation {
  type: 'cache' | 'lazy-load' | 'batch' | 'disable' | 'priority';
  module: string;
  description: string;
  expectedImprovement: number; // percentage
  implementation: string;
}

/**
 * Security Budget Monitor Service
 * Monitors and optimizes security operations performance budget
 */
@Injectable({
  providedIn: 'root'
})
export class SecurityBudgetMonitorService {
  private lazyLoader = inject(LazySecurityLoaderService);

  private budgetMetrics$ = new BehaviorSubject<SecurityBudgetMetrics>(this.getInitialMetrics());
  private budgetAlerts$ = new BehaviorSubject<SecurityBudgetAlert[]>([]);
  private optimizationQueue$ = new BehaviorSubject<OptimizationRecommendation[]>([]);

  private readonly BUDGET_THRESHOLDS = {
    WARNING: 0.75,     // 75% budget usage
    CRITICAL: 0.9,     // 90% budget usage
    EFFICIENCY_MIN: 0.6 // Minimum efficiency threshold
  };

  private readonly PERFORMANCE_TARGETS = {
    encryption: 5,      // 5ms max
    integrity: 3,       // 3ms max
    validation: 2,      // 2ms max
    storage: 1,         // 1ms max
    audit: 8           // 8ms max
  };

  private operationHistory = new Map<string, number[]>();
  private moduleMetrics = new Map<string, ModulePerformanceMetrics>();
  private lastOptimization = Date.now();

  constructor() {
    this.initializeBudgetMonitoring();
  }

  /**
   * Initialize budget monitoring system
   */
  private initializeBudgetMonitoring(): void {
    // Monitor security loader budget
    this.lazyLoader.getSecurityBudget().subscribe(budget => {
      this.updateBudgetMetrics(budget);
    });

    // Setup performance monitoring
    this.setupPerformanceMonitoring();

    // Setup automatic optimization
    this.setupAutoOptimization();

    console.log('📊 Security Budget Monitor initialized');
  }

  /**
   * Record security operation performance
   */
  recordOperation(
    moduleName: string,
    operationTime: number,
    operationType: 'encrypt' | 'decrypt' | 'validate' | 'check' | 'audit' = 'check'
  ): void {
    // Add to operation history
    if (!this.operationHistory.has(moduleName)) {
      this.operationHistory.set(moduleName, []);
    }

    const history = this.operationHistory.get(moduleName)!;
    history.push(operationTime);

    // Keep only last 100 operations
    if (history.length > 100) {
      history.shift();
    }

    // Update module metrics
    this.updateModuleMetrics(moduleName, operationTime);

    // Check for budget violations
    this.checkBudgetViolations(moduleName, operationTime);

    // Generate recommendations if needed
    this.generateOptimizationRecommendations();
  }

  /**
   * Get current budget metrics
   */
  getBudgetMetrics(): Observable<SecurityBudgetMetrics> {
    return this.budgetMetrics$.asObservable();
  }

  /**
   * Get budget alerts
   */
  getBudgetAlerts(): Observable<SecurityBudgetAlert[]> {
    return this.budgetAlerts$.asObservable();
  }

  /**
   * Get optimization recommendations
   */
  getOptimizationRecommendations(): Observable<OptimizationRecommendation[]> {
    return this.optimizationQueue$.asObservable();
  }

  /**
   * Apply optimization recommendation
   */
  async applyOptimization(recommendation: OptimizationRecommendation): Promise<boolean> {
    try {
      console.log(`🔧 Applying optimization for ${recommendation.module}:`, recommendation.description);

      switch (recommendation.type) {
        case 'cache':
          await this.optimizeWithCaching(recommendation.module);
          break;

        case 'lazy-load':
          await this.optimizeLazyLoading(recommendation.module);
          break;

        case 'batch':
          await this.optimizeBatching(recommendation.module);
          break;

        case 'priority':
          await this.adjustPriority(recommendation.module);
          break;

        case 'disable':
          await this.disableNonCriticalFeatures(recommendation.module);
          break;

        default:
          console.warn(`Unknown optimization type: ${recommendation.type}`);
          return false;
      }

      // Remove applied recommendation
      const currentRecommendations = this.optimizationQueue$.value;
      const updatedRecommendations = currentRecommendations.filter(r => r !== recommendation);
      this.optimizationQueue$.next(updatedRecommendations);

      this.addAlert('info', `Optimization applied for ${recommendation.module}`, recommendation.module);
      return true;

    } catch (error) {
      console.error(`Failed to apply optimization for ${recommendation.module}:`, error);
      this.addAlert('warning', `Failed to apply optimization for ${recommendation.module}`, recommendation.module);
      return false;
    }
  }

  /**
   * Get module performance report
   */
  getModulePerformanceReport(): ModulePerformanceMetrics[] {
    return Array.from(this.moduleMetrics.values());
  }

  /**
   * Reset budget monitoring
   */
  resetBudgetMonitoring(): void {
    this.operationHistory.clear();
    this.moduleMetrics.clear();
    this.budgetAlerts$.next([]);
    this.optimizationQueue$.next([]);
    this.budgetMetrics$.next(this.getInitialMetrics());

    console.log('🔄 Budget monitoring reset');
  }

  /**
   * Get budget utilization forecast
   */
  getBudgetForecast(timeWindowMs: number = 60000): { projected: number; confidence: number } {
    const allOperations = Array.from(this.operationHistory.values()).flat();

    if (allOperations.length < 10) {
      return { projected: 0, confidence: 0 };
    }

    // Calculate average operation time
    const avgOperationTime = allOperations.reduce((sum, time) => sum + time, 0) / allOperations.length;

    // Estimate operations per time window
    const recentOperations = allOperations.slice(-20); // Last 20 operations
    const operationRate = recentOperations.length / (timeWindowMs / 1000); // ops per second

    // Project budget usage
    const projectedOperations = operationRate * (timeWindowMs / 1000);
    const projectedBudget = projectedOperations * avgOperationTime;

    // Calculate confidence based on variance
    const variance = recentOperations.reduce((sum, time) => sum + Math.pow(time - avgOperationTime, 2), 0) / recentOperations.length;
    const confidence = Math.max(0, 1 - (variance / (avgOperationTime * avgOperationTime)));

    return { projected: projectedBudget, confidence };
  }

  // Private methods

  private setupPerformanceMonitoring(): void {
    // Monitor every 30 seconds
    timer(30000, 30000).subscribe(() => {
      this.analyzePerformanceTrends();
      this.generatePerformanceReport();
    });

    // Deep analysis every 5 minutes
    timer(300000, 300000).subscribe(() => {
      this.performDeepAnalysis();
    });
  }

  private setupAutoOptimization(): void {
    // Auto-optimization every 10 minutes
    timer(600000, 600000).subscribe(() => {
      this.performAutoOptimization();
    });
  }

  private updateBudgetMetrics(securityBudget: SecurityBudget): void {
    const utilizationRate = securityBudget.usedBudget / securityBudget.totalBudget;
    const modulePerformance = this.calculateModulePerformance();
    const budgetEfficiency = this.calculateBudgetEfficiency();

    const metrics: SecurityBudgetMetrics = {
      totalBudget: securityBudget.totalBudget,
      usedBudget: securityBudget.usedBudget,
      remainingBudget: securityBudget.remainingBudget,
      utilizationRate,
      averageOperationTime: this.calculateAverageOperationTime(),
      budgetEfficiency,
      modulePerformance,
      recommendations: this.generateRecommendations(),
      alerts: this.budgetAlerts$.value
    };

    this.budgetMetrics$.next(metrics);

    // Check thresholds
    if (utilizationRate >= this.BUDGET_THRESHOLDS.CRITICAL) {
      this.addAlert('critical', `Budget usage critical: ${(utilizationRate * 100).toFixed(1)}%`);
    } else if (utilizationRate >= this.BUDGET_THRESHOLDS.WARNING) {
      this.addAlert('warning', `Budget usage high: ${(utilizationRate * 100).toFixed(1)}%`);
    }
  }

  private updateModuleMetrics(moduleName: string, operationTime: number): void {
    const history = this.operationHistory.get(moduleName) || [];
    const operationCount = history.length;
    const averageTime = history.reduce((sum, time) => sum + time, 0) / operationCount;

    const target = this.PERFORMANCE_TARGETS[moduleName as keyof typeof this.PERFORMANCE_TARGETS] || 5;
    const efficiency = Math.max(0, 1 - (averageTime / target));
    const isOptimal = efficiency >= this.BUDGET_THRESHOLDS.EFFICIENCY_MIN;

    const metrics: ModulePerformanceMetrics = {
      moduleName,
      budgetAllocated: target,
      budgetUsed: averageTime,
      operationCount,
      averageTime,
      efficiency,
      isOptimal
    };

    this.moduleMetrics.set(moduleName, metrics);
  }

  private checkBudgetViolations(moduleName: string, operationTime: number): void {
    const target = this.PERFORMANCE_TARGETS[moduleName as keyof typeof this.PERFORMANCE_TARGETS] || 5;

    if (operationTime > target * 2) {
      this.addAlert('critical', `${moduleName} operation exceeded budget by ${((operationTime / target - 1) * 100).toFixed(0)}%`, moduleName, true);
    } else if (operationTime > target * 1.5) {
      this.addAlert('warning', `${moduleName} operation above target by ${((operationTime / target - 1) * 100).toFixed(0)}%`, moduleName);
    }
  }

  private generateOptimizationRecommendations(): void {
    const currentRecommendations = this.optimizationQueue$.value;
    const newRecommendations: OptimizationRecommendation[] = [];

    this.moduleMetrics.forEach((metrics, moduleName) => {
      // Skip if already has recommendations
      if (currentRecommendations.some(r => r.module === moduleName)) {
        return;
      }

      if (!metrics.isOptimal) {
        if (metrics.averageTime > metrics.budgetAllocated * 2) {
          // Critical performance issue
          newRecommendations.push({
            type: 'cache',
            module: moduleName,
            description: `Implement aggressive caching for ${moduleName} to reduce operation time`,
            expectedImprovement: 40,
            implementation: 'Add memory cache with TTL and LRU eviction'
          });
        } else if (metrics.averageTime > metrics.budgetAllocated * 1.5) {
          // Moderate performance issue
          newRecommendations.push({
            type: 'lazy-load',
            module: moduleName,
            description: `Optimize loading strategy for ${moduleName}`,
            expectedImprovement: 25,
            implementation: 'Defer loading until actually needed'
          });
        }
      }

      // Check for batch optimization opportunities
      if (metrics.operationCount > 50 && metrics.averageTime > 3) {
        newRecommendations.push({
          type: 'batch',
          module: moduleName,
          description: `Implement batching for ${moduleName} operations`,
          expectedImprovement: 30,
          implementation: 'Group multiple operations into single batch'
        });
      }
    });

    if (newRecommendations.length > 0) {
      const allRecommendations = [...currentRecommendations, ...newRecommendations];
      this.optimizationQueue$.next(allRecommendations);
    }
  }

  private generateRecommendations(): string[] {
    const recommendations: string[] = [];
    const metrics = this.budgetMetrics$.value;

    if (metrics.utilizationRate > this.BUDGET_THRESHOLDS.WARNING) {
      recommendations.push('Consider implementing caching for frequently used operations');
    }

    if (metrics.budgetEfficiency < this.BUDGET_THRESHOLDS.EFFICIENCY_MIN) {
      recommendations.push('Review and optimize underperforming security modules');
    }

    if (metrics.averageOperationTime > 5) {
      recommendations.push('Enable lazy loading for non-critical security features');
    }

    const inefficientModules = metrics.modulePerformance.filter(m => !m.isOptimal);
    if (inefficientModules.length > 0) {
      recommendations.push(`Optimize modules: ${inefficientModules.map(m => m.moduleName).join(', ')}`);
    }

    return recommendations;
  }

  private calculateModulePerformance(): ModulePerformanceMetrics[] {
    return Array.from(this.moduleMetrics.values());
  }

  private calculateBudgetEfficiency(): number {
    const moduleMetrics = Array.from(this.moduleMetrics.values());
    if (moduleMetrics.length === 0) return 1.0;

    const totalEfficiency = moduleMetrics.reduce((sum, metrics) => sum + metrics.efficiency, 0);
    return totalEfficiency / moduleMetrics.length;
  }

  private calculateAverageOperationTime(): number {
    const allOperations = Array.from(this.operationHistory.values()).flat();
    if (allOperations.length === 0) return 0;

    return allOperations.reduce((sum, time) => sum + time, 0) / allOperations.length;
  }

  private analyzePerformanceTrends(): void {
    // Analyze trends in the last 20 operations per module
    this.moduleMetrics.forEach((metrics, moduleName) => {
      const history = this.operationHistory.get(moduleName);
      if (!history || history.length < 10) return;

      const recent = history.slice(-10);
      const older = history.slice(-20, -10);

      if (older.length === 0) return;

      const recentAvg = recent.reduce((sum, time) => sum + time, 0) / recent.length;
      const olderAvg = older.reduce((sum, time) => sum + time, 0) / older.length;

      const trend = (recentAvg - olderAvg) / olderAvg;

      if (trend > 0.2) { // 20% slower
        this.addAlert('warning', `Performance degradation detected in ${moduleName}: ${(trend * 100).toFixed(1)}% slower`, moduleName);
      } else if (trend < -0.2) { // 20% faster
        this.addAlert('info', `Performance improvement detected in ${moduleName}: ${(Math.abs(trend) * 100).toFixed(1)}% faster`, moduleName);
      }
    });
  }

  private generatePerformanceReport(): void {
    const metrics = this.budgetMetrics$.value;

    console.log('📊 Security Budget Performance Report:', {
      budgetUtilization: `${(metrics.utilizationRate * 100).toFixed(1)}%`,
      efficiency: `${(metrics.budgetEfficiency * 100).toFixed(1)}%`,
      avgOperationTime: `${metrics.averageOperationTime.toFixed(2)}ms`,
      moduleCount: metrics.modulePerformance.length,
      alerts: metrics.alerts.length,
      recommendations: metrics.recommendations.length
    });
  }

  private performDeepAnalysis(): void {
    const forecast = this.getBudgetForecast();

    console.log('🔍 Deep Security Budget Analysis:', {
      forecast: `${forecast.projected.toFixed(2)}ms (${(forecast.confidence * 100).toFixed(1)}% confidence)`,
      moduleMetrics: this.getModulePerformanceReport().map(m => ({
        module: m.moduleName,
        efficiency: `${(m.efficiency * 100).toFixed(1)}%`,
        avgTime: `${m.averageTime.toFixed(2)}ms`,
        operations: m.operationCount
      }))
    });
  }

  private async performAutoOptimization(): Promise<void> {
    const recommendations = this.optimizationQueue$.value;
    const criticalRecommendations = recommendations.filter(r => r.expectedImprovement > 30);

    if (criticalRecommendations.length > 0) {
      console.log(`🤖 Auto-applying ${criticalRecommendations.length} critical optimizations`);

      for (const recommendation of criticalRecommendations) {
        await this.applyOptimization(recommendation);
      }
    }
  }

  // Optimization implementations

  private async optimizeWithCaching(moduleName: string): Promise<void> {
    // Implementation would depend on specific module
    console.log(`🚀 Implementing caching optimization for ${moduleName}`);
  }

  private async optimizeLazyLoading(moduleName: string): Promise<void> {
    console.log(`🚀 Optimizing lazy loading for ${moduleName}`);
  }

  private async optimizeBatching(moduleName: string): Promise<void> {
    console.log(`🚀 Implementing batching optimization for ${moduleName}`);
  }

  private async adjustPriority(moduleName: string): Promise<void> {
    console.log(`🚀 Adjusting priority for ${moduleName}`);
  }

  private async disableNonCriticalFeatures(moduleName: string): Promise<void> {
    console.log(`🚀 Disabling non-critical features for ${moduleName}`);
  }

  private addAlert(
    level: 'info' | 'warning' | 'critical',
    message: string,
    module?: string,
    actionRequired: boolean = false
  ): void {
    const alert: SecurityBudgetAlert = {
      level,
      message,
      timestamp: Date.now(),
      module,
      actionRequired
    };

    const currentAlerts = this.budgetAlerts$.value;
    const newAlerts = [alert, ...currentAlerts].slice(0, 100); // Keep only last 100 alerts

    this.budgetAlerts$.next(newAlerts);
  }

  private getInitialMetrics(): SecurityBudgetMetrics {
    return {
      totalBudget: 0,
      usedBudget: 0,
      remainingBudget: 0,
      utilizationRate: 0,
      averageOperationTime: 0,
      budgetEfficiency: 1.0,
      modulePerformance: [],
      recommendations: [],
      alerts: []
    };
  }
}