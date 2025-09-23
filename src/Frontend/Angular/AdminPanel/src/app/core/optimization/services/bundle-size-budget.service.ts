import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject, interval, combineLatest } from 'rxjs';
import { map } from 'rxjs/operators';

interface BundleBudget {
  name: string;
  type: 'initial' | 'lazy' | 'vendor' | 'runtime' | 'polyfills';
  maxSize: number; // in bytes
  warningThreshold: number; // percentage
  currentSize: number;
  status: 'ok' | 'warning' | 'error';
  environment: 'development' | 'production' | 'all';
  enforced: boolean;
}

interface BudgetViolation {
  bundleName: string;
  budgetType: string;
  currentSize: number;
  maxSize: number;
  exceededBy: number;
  percentage: number;
  severity: 'warning' | 'error';
  timestamp: Date;
  suggestions: string[];
}

interface SizeTrend {
  date: Date;
  bundleName: string;
  size: number;
  changeFromPrevious: number;
  trend: 'increasing' | 'decreasing' | 'stable';
}

interface BudgetAlert {
  id: string;
  type: 'size_exceeded' | 'trend_warning' | 'budget_approaching';
  message: string;
  severity: 'info' | 'warning' | 'error';
  timestamp: Date;
  acknowledged: boolean;
  actionRequired: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class BundleSizeBudgetService {
  private budgets = new BehaviorSubject<Map<string, BundleBudget>>(new Map());
  private violations = new BehaviorSubject<BudgetViolation[]>([]);
  private sizeTrends = new BehaviorSubject<SizeTrend[]>([]);
  private alerts = new BehaviorSubject<BudgetAlert[]>([]);

  public budgets$ = this.budgets.asObservable();
  public violations$ = this.violations.asObservable();
  public sizeTrends$ = this.sizeTrends.asObservable();
  public alerts$ = this.alerts.asObservable();

  private performanceObserver?: PerformanceObserver;
  private sizeMeasurements = new Map<string, number[]>();

  constructor() {
    this.initializeDefaultBudgets();
    this.startBundleMonitoring();
    this.initializePerformanceTracking();
  }

  private initializeDefaultBudgets(): void {
    const defaultBudgets: BundleBudget[] = [
      {
        name: 'main',
        type: 'initial',
        maxSize: 500 * 1024, // 500KB
        warningThreshold: 80,
        currentSize: 0,
        status: 'ok',
        environment: 'production',
        enforced: true
      },
      {
        name: 'vendor',
        type: 'vendor',
        maxSize: 2 * 1024 * 1024, // 2MB
        warningThreshold: 85,
        currentSize: 0,
        status: 'ok',
        environment: 'production',
        enforced: true
      },
      {
        name: 'polyfills',
        type: 'polyfills',
        maxSize: 100 * 1024, // 100KB
        warningThreshold: 75,
        currentSize: 0,
        status: 'ok',
        environment: 'all',
        enforced: true
      },
      {
        name: 'runtime',
        type: 'runtime',
        maxSize: 50 * 1024, // 50KB
        warningThreshold: 70,
        currentSize: 0,
        status: 'ok',
        environment: 'all',
        enforced: true
      },
      {
        name: 'lazy-modules',
        type: 'lazy',
        maxSize: 200 * 1024, // 200KB per lazy chunk
        warningThreshold: 80,
        currentSize: 0,
        status: 'ok',
        environment: 'production',
        enforced: false
      }
    ];

    const budgetMap = new Map<string, BundleBudget>();
    defaultBudgets.forEach(budget => budgetMap.set(budget.name, budget));
    this.budgets.next(budgetMap);
  }

  private initializePerformanceTracking(): void {
    if (typeof window !== 'undefined' && 'PerformanceObserver' in window) {
      this.performanceObserver = new PerformanceObserver((entries) => {
        for (const entry of entries.getEntries()) {
          if (entry.entryType === 'resource' && entry.name.endsWith('.js')) {
            this.trackBundleSize(entry);
          }
        }
      });

      this.performanceObserver.observe({
        entryTypes: ['resource']
      });
    }
  }

  private trackBundleSize(entry: PerformanceEntry): void {
    const bundleName = this.extractBundleName(entry.name);
    const size = this.estimateBundleSize(entry);

    // Update current measurements
    if (!this.sizeMeasurements.has(bundleName)) {
      this.sizeMeasurements.set(bundleName, []);
    }
    this.sizeMeasurements.get(bundleName)!.push(size);

    // Update budget tracking
    this.updateBundleSize(bundleName, size);
    this.checkBudgetViolations();
    this.updateSizeTrends(bundleName, size);
  }

  private extractBundleName(url: string): string {
    const fileName = url.split('/').pop() || '';

    if (fileName.includes('main')) return 'main';
    if (fileName.includes('vendor')) return 'vendor';
    if (fileName.includes('polyfills')) return 'polyfills';
    if (fileName.includes('runtime')) return 'runtime';
    if (fileName.includes('chunk') || fileName.includes('lazy')) return 'lazy-modules';

    return fileName.replace('.js', '');
  }

  private estimateBundleSize(entry: PerformanceEntry): number {
    if ('transferSize' in entry) {
      return (entry as any).transferSize || 0;
    }
    // Fallback estimation based on duration
    return entry.duration ? Math.floor(entry.duration * 1000) : 0;
  }

  private updateBundleSize(bundleName: string, size: number): void {
    const budgets = this.budgets.value;
    const budget = budgets.get(bundleName);

    if (budget) {
      budget.currentSize = size;
      budget.status = this.calculateBudgetStatus(budget);
      budgets.set(bundleName, budget);
      this.budgets.next(budgets);
    }
  }

  private calculateBudgetStatus(budget: BundleBudget): 'ok' | 'warning' | 'error' {
    const percentage = (budget.currentSize / budget.maxSize) * 100;

    if (percentage >= 100) return 'error';
    if (percentage >= budget.warningThreshold) return 'warning';
    return 'ok';
  }

  private checkBudgetViolations(): void {
    const budgets = this.budgets.value;
    const currentViolations: BudgetViolation[] = [];

    budgets.forEach((budget, name) => {
      if (budget.currentSize > budget.maxSize * (budget.warningThreshold / 100)) {
        const exceededBy = budget.currentSize - budget.maxSize;
        const percentage = (budget.currentSize / budget.maxSize) * 100;

        currentViolations.push({
          bundleName: name,
          budgetType: budget.type,
          currentSize: budget.currentSize,
          maxSize: budget.maxSize,
          exceededBy: Math.max(0, exceededBy),
          percentage,
          severity: percentage >= 100 ? 'error' : 'warning',
          timestamp: new Date(),
          suggestions: this.generateOptimizationSuggestions(budget, percentage)
        });
      }
    });

    this.violations.next(currentViolations);
    this.generateBudgetAlerts(currentViolations);
  }

  private generateOptimizationSuggestions(budget: BundleBudget, percentage: number): string[] {
    const suggestions: string[] = [];

    switch (budget.type) {
      case 'initial':
        suggestions.push('Consider lazy loading non-critical features');
        suggestions.push('Review and remove unused dependencies');
        suggestions.push('Enable tree shaking for better dead code elimination');
        break;

      case 'vendor':
        suggestions.push('Audit third-party libraries for alternatives');
        suggestions.push('Use CDN for large libraries');
        suggestions.push('Enable vendor chunk splitting');
        break;

      case 'lazy':
        suggestions.push('Split large lazy modules into smaller chunks');
        suggestions.push('Use dynamic imports for component-level splitting');
        break;

      case 'polyfills':
        suggestions.push('Target modern browsers to reduce polyfill requirements');
        suggestions.push('Use differential loading for modern/legacy bundles');
        break;
    }

    if (percentage > 120) {
      suggestions.unshift('CRITICAL: Consider emergency optimization measures');
    }

    return suggestions;
  }

  private updateSizeTrends(bundleName: string, currentSize: number): void {
    const trends = this.sizeTrends.value;
    const recentTrends = trends.filter(t => t.bundleName === bundleName).slice(-9);

    const lastSize = recentTrends.length > 0 ? recentTrends[recentTrends.length - 1].size : 0;
    const changeFromPrevious = currentSize - lastSize;

    const newTrend: SizeTrend = {
      date: new Date(),
      bundleName,
      size: currentSize,
      changeFromPrevious,
      trend: this.calculateTrend(recentTrends, currentSize)
    };

    const updatedTrends = [...trends.filter(t => t.bundleName !== bundleName), ...recentTrends, newTrend];
    this.sizeTrends.next(updatedTrends);
  }

  private calculateTrend(recentTrends: SizeTrend[], currentSize: number): 'increasing' | 'decreasing' | 'stable' {
    if (recentTrends.length < 3) return 'stable';

    const avgChange = recentTrends.slice(-3).reduce((sum, t) => sum + t.changeFromPrevious, 0) / 3;

    if (avgChange > currentSize * 0.05) return 'increasing';
    if (avgChange < -currentSize * 0.05) return 'decreasing';
    return 'stable';
  }

  private generateBudgetAlerts(violations: BudgetViolation[]): void {
    const currentAlerts = this.alerts.value;
    const newAlerts: BudgetAlert[] = [];

    violations.forEach(violation => {
      const alertId = `${violation.bundleName}-${violation.severity}-${Date.now()}`;

      if (!currentAlerts.some(a => a.type === 'size_exceeded' && a.message.includes(violation.bundleName))) {
        newAlerts.push({
          id: alertId,
          type: 'size_exceeded',
          message: `Bundle '${violation.bundleName}' exceeded ${violation.severity} threshold (${violation.percentage.toFixed(1)}%)`,
          severity: violation.severity === 'error' ? 'error' : 'warning',
          timestamp: new Date(),
          acknowledged: false,
          actionRequired: violation.severity === 'error'
        });
      }
    });

    // Check for concerning trends
    this.sizeTrends.value.forEach(trend => {
      if (trend.trend === 'increasing' && trend.changeFromPrevious > trend.size * 0.1) {
        newAlerts.push({
          id: `trend-${trend.bundleName}-${Date.now()}`,
          type: 'trend_warning',
          message: `Bundle '${trend.bundleName}' showing concerning growth trend (+${(trend.changeFromPrevious / 1024).toFixed(1)}KB)`,
          severity: 'warning',
          timestamp: new Date(),
          acknowledged: false,
          actionRequired: false
        });
      }
    });

    this.alerts.next([...currentAlerts, ...newAlerts]);
  }

  private startBundleMonitoring(): void {
    // Periodic budget checking
    interval(60000).subscribe(() => { // Every minute
      this.checkBudgetViolations();
      this.cleanupOldAlerts();
    });

    // Simulated bundle size updates for demo
    interval(30000).subscribe(() => {
      this.simulateBundleSizeUpdates();
    });
  }

  private simulateBundleSizeUpdates(): void {
    const budgets = this.budgets.value;

    budgets.forEach((budget, name) => {
      // Simulate size fluctuation
      const baseSize = budget.maxSize * 0.7; // 70% of max as baseline
      const variation = (Math.random() - 0.5) * 0.2 * baseSize; // ±10% variation
      const newSize = Math.max(0, baseSize + variation);

      this.updateBundleSize(name, newSize);
    });
  }

  private cleanupOldAlerts(): void {
    const alerts = this.alerts.value;
    const oneDayAgo = new Date(Date.now() - 24 * 60 * 60 * 1000);

    const filteredAlerts = alerts.filter(alert =>
      alert.timestamp > oneDayAgo || !alert.acknowledged
    );

    if (filteredAlerts.length !== alerts.length) {
      this.alerts.next(filteredAlerts);
    }
  }

  public setBudget(name: string, budget: Partial<BundleBudget>): Observable<boolean> {
    const budgets = this.budgets.value;
    const existingBudget = budgets.get(name);

    if (existingBudget) {
      const updatedBudget = { ...existingBudget, ...budget };
      budgets.set(name, updatedBudget);
    } else {
      const newBudget: BundleBudget = {
        name,
        type: 'initial',
        maxSize: 500 * 1024,
        warningThreshold: 80,
        currentSize: 0,
        status: 'ok',
        environment: 'production',
        enforced: false,
        ...budget
      };
      budgets.set(name, newBudget);
    }

    this.budgets.next(budgets);
    this.checkBudgetViolations();

    return new Observable(observer => {
      observer.next(true);
      observer.complete();
    });
  }

  public getBudgetStatus(): Observable<any> {
    return combineLatest([
      this.budgets$,
      this.violations$,
      this.sizeTrends$
    ]).pipe(
      map(([budgets, violations, trends]) => {
        const budgetArray = Array.from(budgets.values());
        const totalBudget = budgetArray.reduce((sum, b) => sum + b.maxSize, 0);
        const totalCurrent = budgetArray.reduce((sum, b) => sum + b.currentSize, 0);

        return {
          totalBudgets: budgetArray.length,
          totalMaxSize: totalBudget,
          totalCurrentSize: totalCurrent,
          utilizationPercentage: (totalCurrent / totalBudget) * 100,
          violationsCount: violations.length,
          criticalViolations: violations.filter(v => v.severity === 'error').length,
          trendsStatus: {
            increasing: trends.filter(t => t.trend === 'increasing').length,
            decreasing: trends.filter(t => t.trend === 'decreasing').length,
            stable: trends.filter(t => t.trend === 'stable').length
          },
          worstOffenders: violations
            .sort((a, b) => b.percentage - a.percentage)
            .slice(0, 3)
            .map(v => ({
              bundle: v.bundleName,
              percentage: v.percentage,
              exceededBy: v.exceededBy
            }))
        };
      })
    );
  }

  public acknowledgeAlert(alertId: string): Observable<boolean> {
    const alerts = this.alerts.value;
    const alertIndex = alerts.findIndex(a => a.id === alertId);

    if (alertIndex !== -1) {
      alerts[alertIndex].acknowledged = true;
      this.alerts.next([...alerts]);
      return new Observable(observer => {
        observer.next(true);
        observer.complete();
      });
    }

    return new Observable(observer => {
      observer.next(false);
      observer.complete();
    });
  }

  public generateBudgetReport(): Observable<any> {
    return combineLatest([
      this.budgets$,
      this.violations$,
      this.sizeTrends$,
      this.alerts$
    ]).pipe(
      map(([budgets, violations, trends, alerts]) => {
        const budgetArray = Array.from(budgets.values());

        return {
          summary: {
            generatedAt: new Date(),
            totalBundles: budgetArray.length,
            budgetsEnforced: budgetArray.filter(b => b.enforced).length,
            currentViolations: violations.length,
            criticalAlerts: alerts.filter(a => a.severity === 'error' && !a.acknowledged).length
          },
          budgetDetails: budgetArray.map(budget => ({
            name: budget.name,
            type: budget.type,
            maxSize: this.formatBytes(budget.maxSize),
            currentSize: this.formatBytes(budget.currentSize),
            utilization: ((budget.currentSize / budget.maxSize) * 100).toFixed(1) + '%',
            status: budget.status,
            enforced: budget.enforced
          })),
          violations: violations.map(v => ({
            bundle: v.bundleName,
            severity: v.severity,
            exceeded: this.formatBytes(v.exceededBy),
            percentage: v.percentage.toFixed(1) + '%',
            suggestions: v.suggestions
          })),
          trends: this.analyzeTrends(trends),
          recommendations: this.generateGlobalRecommendations(budgetArray, violations)
        };
      })
    );
  }

  private formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  private analyzeTrends(trends: SizeTrend[]): any {
    const bundleNames = Array.from(new Set(trends.map(t => t.bundleName)));

    return bundleNames.map(bundleName => {
      const bundleTrends = trends
        .filter(t => t.bundleName === bundleName)
        .sort((a, b) => a.date.getTime() - b.date.getTime());

      const recent = bundleTrends.slice(-5);
      const avgGrowth = recent.length > 1 ?
        recent.reduce((sum, t) => sum + t.changeFromPrevious, 0) / recent.length : 0;

      return {
        bundle: bundleName,
        trend: recent[recent.length - 1]?.trend || 'stable',
        averageGrowth: this.formatBytes(avgGrowth),
        dataPoints: recent.length
      };
    });
  }

  private generateGlobalRecommendations(budgets: BundleBudget[], violations: BudgetViolation[]): string[] {
    const recommendations: string[] = [];

    const totalUtilization = budgets.reduce((sum, b) => sum + (b.currentSize / b.maxSize), 0) / budgets.length;

    if (totalUtilization > 0.8) {
      recommendations.push('Consider increasing budget limits or implementing aggressive optimization');
    }

    if (violations.length > budgets.length * 0.3) {
      recommendations.push('High violation rate detected - review bundle splitting strategy');
    }

    const vendorBudget = budgets.find(b => b.type === 'vendor');
    if (vendorBudget && vendorBudget.currentSize > vendorBudget.maxSize * 0.9) {
      recommendations.push('Vendor bundle approaching limit - audit dependencies');
    }

    if (violations.some(v => v.severity === 'error')) {
      recommendations.push('URGENT: Critical budget violations require immediate attention');
    }

    return recommendations;
  }

  public exportBudgetConfig(): Observable<string> {
    return this.budgets$.pipe(
      map(budgets => {
        const config = {
          budgets: Array.from(budgets.values()).map(budget => ({
            type: budget.type,
            name: budget.name,
            maximumWarning: this.formatBytes(budget.maxSize * (budget.warningThreshold / 100)),
            maximumError: this.formatBytes(budget.maxSize)
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
    this.sizeMeasurements.clear();
  }
}