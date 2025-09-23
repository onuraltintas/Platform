import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, timer, fromEvent } from 'rxjs';
import { map, throttleTime, startWith } from 'rxjs/operators';

export interface PerformanceEntry {
  timestamp: number;
  type: 'navigation' | 'resource' | 'measure' | 'paint' | 'longtask';
  name: string;
  duration: number;
  startTime: number;
  entryType: string;
  details?: any;
}

export interface PerformanceReport {
  timestamp: number;
  pageLoadMetrics: {
    domContentLoaded: number;
    loadComplete: number;
    firstContentfulPaint: number;
    largestContentfulPaint: number;
    firstInputDelay: number;
    cumulativeLayoutShift: number;
  };
  resourceMetrics: {
    totalRequests: number;
    totalBytes: number;
    avgRequestTime: number;
    slowestRequests: Array<{ name: string; duration: number }>;
  };
  memoryMetrics: {
    usedJSHeapSize: number;
    totalJSHeapSize: number;
    jsHeapSizeLimit: number;
    usagePercentage: number;
  };
  longTasks: Array<{
    startTime: number;
    duration: number;
    attribution?: any;
  }>;
  userInteractions: {
    clicks: number;
    scrolls: number;
    keystrokes: number;
    avgResponseTime: number;
  };
}

export interface PerformanceTrend {
  metric: string;
  values: Array<{ timestamp: number; value: number }>;
  trend: 'improving' | 'degrading' | 'stable';
  changePercentage: number;
}

export interface PerformanceAlert {
  id: string;
  level: 'info' | 'warning' | 'critical';
  metric: string;
  value: number;
  threshold: number;
  message: string;
  timestamp: number;
  resolved: boolean;
}

/**
 * Performance Analytics Service
 * Comprehensive performance monitoring and analysis
 */
@Injectable({
  providedIn: 'root'
})
export class PerformanceAnalyticsService {
  private performanceEntries$ = new BehaviorSubject<PerformanceEntry[]>([]);
  private performanceReports$ = new BehaviorSubject<PerformanceReport[]>([]);
  private performanceTrends$ = new BehaviorSubject<PerformanceTrend[]>([]);
  private performanceAlerts$ = new BehaviorSubject<PerformanceAlert[]>([]);

  private readonly MAX_ENTRIES = 1000;
  private readonly MAX_REPORTS = 100;
  private readonly THRESHOLDS = {
    LONG_TASK: 50, // ms
    SLOW_REQUEST: 1000, // ms
    HIGH_MEMORY: 0.8, // 80% of heap limit
    FCP_THRESHOLD: 2000, // ms
    LCP_THRESHOLD: 2500, // ms
    FID_THRESHOLD: 100, // ms
    CLS_THRESHOLD: 0.1
  };

  private userInteractionCount = { clicks: 0, scrolls: 0, keystrokes: 0 };
  private interactionResponseTimes: number[] = [];
  private observer?: PerformanceObserver;
  private longTaskObserver?: PerformanceObserver;

  constructor() {
    this.initializePerformanceMonitoring();
  }

  /**
   * Initialize performance monitoring
   */
  private initializePerformanceMonitoring(): void {
    this.setupPerformanceObservers();
    this.setupUserInteractionTracking();
    this.setupReportGeneration();
    this.collectInitialMetrics();

    console.log('📊 Performance Analytics Service initialized');
  }

  /**
   * Setup performance observers
   */
  private setupPerformanceObservers(): void {
    if ('PerformanceObserver' in window) {
      try {
        // Main performance observer
        this.observer = new PerformanceObserver((list) => {
          const entries = list.getEntries();
          this.processPerformanceEntries(entries);
        });

        this.observer.observe({
          type: 'navigation',
          buffered: true
        });

        this.observer.observe({
          type: 'resource',
          buffered: true
        });

        this.observer.observe({
          type: 'paint',
          buffered: true
        });

        this.observer.observe({
          type: 'measure',
          buffered: true
        });

        // Long task observer
        this.longTaskObserver = new PerformanceObserver((list) => {
          const entries = list.getEntries();
          this.processLongTasks(entries);
        });

        this.longTaskObserver.observe({
          type: 'longtask',
          buffered: true
        });

      } catch (error) {
        console.warn('Performance Observer not fully supported:', error);
      }
    }
  }

  /**
   * Setup user interaction tracking
   */
  private setupUserInteractionTracking(): void {
    // Track clicks
    fromEvent(document, 'click').pipe(
      throttleTime(100)
    ).subscribe(() => {
      this.userInteractionCount.clicks++;
      this.recordInteractionResponseTime();
    });

    // Track scrolls
    fromEvent(window, 'scroll').pipe(
      throttleTime(100)
    ).subscribe(() => {
      this.userInteractionCount.scrolls++;
    });

    // Track keystrokes
    fromEvent(document, 'keydown').pipe(
      throttleTime(50)
    ).subscribe(() => {
      this.userInteractionCount.keystrokes++;
      this.recordInteractionResponseTime();
    });
  }

  /**
   * Setup report generation
   */
  private setupReportGeneration(): void {
    // Generate reports every 30 seconds
    timer(30000, 30000).subscribe(() => {
      this.generatePerformanceReport();
    });

    // Analyze trends every 2 minutes
    timer(120000, 120000).subscribe(() => {
      this.analyzeTrends();
    });

    // Check for alerts every 10 seconds
    timer(10000, 10000).subscribe(() => {
      this.checkPerformanceAlerts();
    });
  }

  /**
   * Collect initial metrics
   */
  private collectInitialMetrics(): void {
    // Collect existing performance entries
    if ('performance' in window) {
      const entries = performance.getEntries();
      this.processPerformanceEntries(entries);
    }
  }

  /**
   * Process performance entries
   */
  private processPerformanceEntries(entries: PerformanceEntry[]): void {
    const currentEntries = this.performanceEntries$.value;
    const newEntries = entries.map(entry => ({
      timestamp: Date.now(),
      type: entry.entryType as any,
      name: entry.name,
      duration: entry.duration,
      startTime: entry.startTime,
      entryType: entry.entryType,
      details: this.extractEntryDetails(entry)
    }));

    const allEntries = [...currentEntries, ...newEntries]
      .slice(-this.MAX_ENTRIES);

    this.performanceEntries$.next(allEntries);
  }

  /**
   * Process long tasks
   */
  private processLongTasks(entries: PerformanceEntry[]): void {
    entries.forEach(entry => {
      if (entry.duration > this.THRESHOLDS.LONG_TASK) {
        this.createAlert('warning', 'longtask', entry.duration, this.THRESHOLDS.LONG_TASK,
          `Long task detected: ${entry.duration.toFixed(2)}ms`);
      }
    });
  }

  /**
   * Extract details from performance entry
   */
  private extractEntryDetails(entry: PerformanceEntry): any {
    const details: any = {};

    if (entry.entryType === 'navigation') {
      const navEntry = entry as any;
      details.domContentLoaded = navEntry.domContentLoadedEventEnd - navEntry.domContentLoadedEventStart;
      details.loadComplete = navEntry.loadEventEnd - navEntry.loadEventStart;
      details.dnsLookup = navEntry.domainLookupEnd - navEntry.domainLookupStart;
      details.tcpConnect = navEntry.connectEnd - navEntry.connectStart;
      details.serverResponse = navEntry.responseEnd - navEntry.requestStart;
    }

    if (entry.entryType === 'resource') {
      const resourceEntry = entry as any;
      details.transferSize = resourceEntry.transferSize;
      details.encodedBodySize = resourceEntry.encodedBodySize;
      details.decodedBodySize = resourceEntry.decodedBodySize;
      details.initiatorType = resourceEntry.initiatorType;
    }

    if (entry.entryType === 'paint') {
      details.paintType = entry.name;
    }

    return details;
  }

  /**
   * Record interaction response time
   */
  private recordInteractionResponseTime(): void {
    const startTime = performance.now();

    // Use requestAnimationFrame to measure response time
    requestAnimationFrame(() => {
      const responseTime = performance.now() - startTime;
      this.interactionResponseTimes.push(responseTime);

      // Keep only last 100 measurements
      if (this.interactionResponseTimes.length > 100) {
        this.interactionResponseTimes.shift();
      }
    });
  }

  /**
   * Generate performance report
   */
  private generatePerformanceReport(): void {
    const entries = this.performanceEntries$.value;
    const navigationEntry = entries.find(e => e.type === 'navigation');
    const paintEntries = entries.filter(e => e.type === 'paint');
    const resourceEntries = entries.filter(e => e.type === 'resource');

    const report: PerformanceReport = {
      timestamp: Date.now(),
      pageLoadMetrics: this.calculatePageLoadMetrics(navigationEntry, paintEntries),
      resourceMetrics: this.calculateResourceMetrics(resourceEntries),
      memoryMetrics: this.getMemoryMetrics(),
      longTasks: this.getLongTasks(),
      userInteractions: this.getUserInteractionMetrics()
    };

    const currentReports = this.performanceReports$.value;
    const newReports = [report, ...currentReports].slice(0, this.MAX_REPORTS);
    this.performanceReports$.next(newReports);
  }

  /**
   * Calculate page load metrics
   */
  private calculatePageLoadMetrics(navigationEntry?: PerformanceEntry, paintEntries?: PerformanceEntry[]): any {
    const fcp = paintEntries?.find(e => e.name === 'first-contentful-paint')?.startTime || 0;

    return {
      domContentLoaded: navigationEntry?.details?.domContentLoaded || 0,
      loadComplete: navigationEntry?.details?.loadComplete || 0,
      firstContentfulPaint: fcp,
      largestContentfulPaint: this.getLargestContentfulPaint(),
      firstInputDelay: this.getFirstInputDelay(),
      cumulativeLayoutShift: this.getCumulativeLayoutShift()
    };
  }

  /**
   * Calculate resource metrics
   */
  private calculateResourceMetrics(resourceEntries: PerformanceEntry[]): any {
    const totalBytes = resourceEntries.reduce((sum, entry) =>
      sum + (entry.details?.transferSize || 0), 0);

    const avgRequestTime = resourceEntries.length > 0
      ? resourceEntries.reduce((sum, entry) => sum + entry.duration, 0) / resourceEntries.length
      : 0;

    const slowestRequests = resourceEntries
      .filter(entry => entry.duration > 100)
      .sort((a, b) => b.duration - a.duration)
      .slice(0, 5)
      .map(entry => ({ name: entry.name, duration: entry.duration }));

    return {
      totalRequests: resourceEntries.length,
      totalBytes,
      avgRequestTime,
      slowestRequests
    };
  }

  /**
   * Get memory metrics
   */
  private getMemoryMetrics(): any {
    if ('memory' in performance) {
      const memory = (performance as any).memory;
      return {
        usedJSHeapSize: memory.usedJSHeapSize,
        totalJSHeapSize: memory.totalJSHeapSize,
        jsHeapSizeLimit: memory.jsHeapSizeLimit,
        usagePercentage: memory.usedJSHeapSize / memory.jsHeapSizeLimit
      };
    }

    return {
      usedJSHeapSize: 0,
      totalJSHeapSize: 0,
      jsHeapSizeLimit: 0,
      usagePercentage: 0
    };
  }

  /**
   * Get long tasks
   */
  private getLongTasks(): any[] {
    const entries = this.performanceEntries$.value;
    return entries
      .filter(entry => entry.type === 'longtask')
      .map(entry => ({
        startTime: entry.startTime,
        duration: entry.duration,
        attribution: entry.details?.attribution
      }));
  }

  /**
   * Get user interaction metrics
   */
  private getUserInteractionMetrics(): any {
    const avgResponseTime = this.interactionResponseTimes.length > 0
      ? this.interactionResponseTimes.reduce((sum, time) => sum + time, 0) / this.interactionResponseTimes.length
      : 0;

    return {
      clicks: this.userInteractionCount.clicks,
      scrolls: this.userInteractionCount.scrolls,
      keystrokes: this.userInteractionCount.keystrokes,
      avgResponseTime
    };
  }

  /**
   * Analyze trends
   */
  private analyzeTrends(): void {
    const reports = this.performanceReports$.value;
    if (reports.length < 3) return;

    const trends: PerformanceTrend[] = [];

    // Analyze key metrics trends
    const metrics = [
      'firstContentfulPaint',
      'largestContentfulPaint',
      'avgRequestTime',
      'usagePercentage'
    ];

    metrics.forEach(metric => {
      const values = reports.slice(0, 10).reverse().map(report => ({
        timestamp: report.timestamp,
        value: this.getMetricValue(report, metric)
      }));

      if (values.length >= 3) {
        const trend = this.calculateTrend(values);
        trends.push(trend);
      }
    });

    this.performanceTrends$.next(trends);
  }

  /**
   * Get metric value from report
   */
  private getMetricValue(report: PerformanceReport, metric: string): number {
    switch (metric) {
      case 'firstContentfulPaint':
        return report.pageLoadMetrics.firstContentfulPaint;
      case 'largestContentfulPaint':
        return report.pageLoadMetrics.largestContentfulPaint;
      case 'avgRequestTime':
        return report.resourceMetrics.avgRequestTime;
      case 'usagePercentage':
        return report.memoryMetrics.usagePercentage;
      default:
        return 0;
    }
  }

  /**
   * Calculate trend direction
   */
  private calculateTrend(values: Array<{ timestamp: number; value: number }>): PerformanceTrend {
    const firstValue = values[0].value;
    const lastValue = values[values.length - 1].value;
    const changePercentage = ((lastValue - firstValue) / firstValue) * 100;

    let trend: 'improving' | 'degrading' | 'stable' = 'stable';
    if (Math.abs(changePercentage) > 5) {
      trend = changePercentage < 0 ? 'improving' : 'degrading';
    }

    return {
      metric: 'trend',
      values,
      trend,
      changePercentage
    };
  }

  /**
   * Check for performance alerts
   */
  private checkPerformanceAlerts(): void {
    const latestReport = this.performanceReports$.value[0];
    if (!latestReport) return;

    const alerts: PerformanceAlert[] = [];

    // Check FCP threshold
    if (latestReport.pageLoadMetrics.firstContentfulPaint > this.THRESHOLDS.FCP_THRESHOLD) {
      alerts.push(this.createAlert('warning', 'FCP',
        latestReport.pageLoadMetrics.firstContentfulPaint, this.THRESHOLDS.FCP_THRESHOLD,
        'First Contentful Paint is slower than expected'
      ));
    }

    // Check LCP threshold
    if (latestReport.pageLoadMetrics.largestContentfulPaint > this.THRESHOLDS.LCP_THRESHOLD) {
      alerts.push(this.createAlert('warning', 'LCP',
        latestReport.pageLoadMetrics.largestContentfulPaint, this.THRESHOLDS.LCP_THRESHOLD,
        'Largest Contentful Paint is slower than expected'
      ));
    }

    // Check memory usage
    if (latestReport.memoryMetrics.usagePercentage > this.THRESHOLDS.HIGH_MEMORY) {
      alerts.push(this.createAlert('critical', 'memory',
        latestReport.memoryMetrics.usagePercentage, this.THRESHOLDS.HIGH_MEMORY,
        'High memory usage detected'
      ));
    }

    if (alerts.length > 0) {
      const currentAlerts = this.performanceAlerts$.value;
      const newAlerts = [...alerts, ...currentAlerts].slice(0, 50);
      this.performanceAlerts$.next(newAlerts);
    }
  }

  /**
   * Create performance alert
   */
  private createAlert(
    level: 'info' | 'warning' | 'critical',
    metric: string,
    value: number,
    threshold: number,
    message: string
  ): PerformanceAlert {
    return {
      id: crypto.randomUUID(),
      level,
      metric,
      value,
      threshold,
      message,
      timestamp: Date.now(),
      resolved: false
    };
  }

  // Web Vitals helpers (simplified implementations)
  private getLargestContentfulPaint(): number {
    const entries = performance.getEntriesByType('largest-contentful-paint') as any[];
    return entries.length > 0 ? entries[entries.length - 1].startTime : 0;
  }

  private getFirstInputDelay(): number {
    const entries = performance.getEntriesByType('first-input') as any[];
    return entries.length > 0 ? entries[0].processingStart - entries[0].startTime : 0;
  }

  private getCumulativeLayoutShift(): number {
    const entries = performance.getEntriesByType('layout-shift') as any[];
    return entries.reduce((sum: number, entry: any) => {
      if (!entry.hadRecentInput) {
        return sum + entry.value;
      }
      return sum;
    }, 0);
  }

  // Public API

  /**
   * Get performance entries
   */
  getPerformanceEntries(): Observable<PerformanceEntry[]> {
    return this.performanceEntries$.asObservable();
  }

  /**
   * Get performance reports
   */
  getPerformanceReports(): Observable<PerformanceReport[]> {
    return this.performanceReports$.asObservable();
  }

  /**
   * Get performance trends
   */
  getPerformanceTrends(): Observable<PerformanceTrend[]> {
    return this.performanceTrends$.asObservable();
  }

  /**
   * Get performance alerts
   */
  getPerformanceAlerts(): Observable<PerformanceAlert[]> {
    return this.performanceAlerts$.asObservable();
  }

  /**
   * Get current performance snapshot
   */
  getCurrentPerformanceSnapshot(): PerformanceReport | null {
    return this.performanceReports$.value[0] || null;
  }

  /**
   * Clear performance data
   */
  clearPerformanceData(): void {
    this.performanceEntries$.next([]);
    this.performanceReports$.next([]);
    this.performanceTrends$.next([]);
    this.performanceAlerts$.next([]);
    this.userInteractionCount = { clicks: 0, scrolls: 0, keystrokes: 0 };
    this.interactionResponseTimes = [];
  }

  /**
   * Export performance data
   */
  exportPerformanceData(): string {
    return JSON.stringify({
      entries: this.performanceEntries$.value,
      reports: this.performanceReports$.value,
      trends: this.performanceTrends$.value,
      alerts: this.performanceAlerts$.value
    }, null, 2);
  }

  /**
   * Mark alert as resolved
   */
  resolveAlert(alertId: string): void {
    const currentAlerts = this.performanceAlerts$.value;
    const updatedAlerts = currentAlerts.map(alert =>
      alert.id === alertId ? { ...alert, resolved: true } : alert
    );
    this.performanceAlerts$.next(updatedAlerts);
  }
}