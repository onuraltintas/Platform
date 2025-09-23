import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, timer } from 'rxjs';

export interface MemoryUsage {
  usedJSHeapSize: number;
  totalJSHeapSize: number;
  jsHeapSizeLimit: number;
  usagePercentage: number;
  trend: 'increasing' | 'decreasing' | 'stable';
  timestamp: number;
}

export interface MemoryAlert {
  id: string;
  level: 'warning' | 'critical';
  message: string;
  usage: number;
  threshold: number;
  timestamp: number;
  acknowledged: boolean;
}

export interface MemoryMetrics {
  current: MemoryUsage;
  history: MemoryUsage[];
  alerts: MemoryAlert[];
  statistics: {
    average: number;
    peak: number;
    minimum: number;
    growthRate: number;
  };
  recommendations: string[];
}

export interface LeakDetection {
  isLeakDetected: boolean;
  suspiciousGrowth: boolean;
  growthRate: number; // MB/minute
  confidence: number; // 0-1
  recommendations: string[];
}

/**
 * Memory Monitor Service
 * Advanced memory usage monitoring and leak detection
 */
@Injectable({
  providedIn: 'root'
})
export class MemoryMonitorService {
  private memoryMetrics$ = new BehaviorSubject<MemoryMetrics>(this.getInitialMetrics());
  private memoryAlerts$ = new BehaviorSubject<MemoryAlert[]>([]);
  private leakDetection$ = new BehaviorSubject<LeakDetection>(this.getInitialLeakDetection());

  private readonly THRESHOLDS = {
    WARNING: 0.7,    // 70% of heap limit
    CRITICAL: 0.85,  // 85% of heap limit
    GROWTH_RATE: 5   // MB/minute growth rate alert
  };

  private readonly HISTORY_SIZE = 100;
  private readonly MONITORING_INTERVAL = 5000; // 5 seconds
  private readonly LEAK_DETECTION_WINDOW = 20; // number of samples for leak detection

  private memoryHistory: MemoryUsage[] = [];
  private startTime = Date.now();

  constructor() {
    this.initializeMemoryMonitoring();
  }

  /**
   * Initialize memory monitoring
   */
  private initializeMemoryMonitoring(): void {
    // Monitor memory every 5 seconds
    timer(0, this.MONITORING_INTERVAL).subscribe(() => {
      this.collectMemoryMetrics();
    });

    // Analyze for memory leaks every 30 seconds
    timer(30000, 30000).subscribe(() => {
      this.analyzeMemoryLeaks();
    });

    // Cleanup old data every 5 minutes
    timer(300000, 300000).subscribe(() => {
      this.cleanupOldData();
    });

    console.log('🧠 Memory Monitor Service initialized');
  }

  /**
   * Collect current memory metrics
   */
  private collectMemoryMetrics(): void {
    const memoryInfo = this.getMemoryInfo();
    if (!memoryInfo) return;

    const usage: MemoryUsage = {
      usedJSHeapSize: memoryInfo.usedJSHeapSize,
      totalJSHeapSize: memoryInfo.totalJSHeapSize,
      jsHeapSizeLimit: memoryInfo.jsHeapSizeLimit,
      usagePercentage: memoryInfo.usedJSHeapSize / memoryInfo.jsHeapSizeLimit,
      trend: this.calculateTrend(memoryInfo.usedJSHeapSize),
      timestamp: Date.now()
    };

    // Add to history
    this.memoryHistory.push(usage);
    if (this.memoryHistory.length > this.HISTORY_SIZE) {
      this.memoryHistory.shift();
    }

    // Update metrics
    const metrics = this.calculateMetrics(usage);
    this.memoryMetrics$.next(metrics);

    // Check for alerts
    this.checkMemoryAlerts(usage);
  }

  /**
   * Get memory information
   */
  private getMemoryInfo(): any {
    if ('memory' in performance) {
      return (performance as any).memory;
    }
    return null;
  }

  /**
   * Calculate memory usage trend
   */
  private calculateTrend(currentUsage: number): 'increasing' | 'decreasing' | 'stable' {
    if (this.memoryHistory.length < 5) return 'stable';

    const recent = this.memoryHistory.slice(-5);
    const avgOld = recent.slice(0, 2).reduce((sum, item) => sum + item.usedJSHeapSize, 0) / 2;
    const avgNew = recent.slice(-2).reduce((sum, item) => sum + item.usedJSHeapSize, 0) / 2;

    const changePercent = ((avgNew - avgOld) / avgOld) * 100;

    if (changePercent > 5) return 'increasing';
    if (changePercent < -5) return 'decreasing';
    return 'stable';
  }

  /**
   * Calculate comprehensive metrics
   */
  private calculateMetrics(current: MemoryUsage): MemoryMetrics {
    const statistics = this.calculateStatistics();
    const recommendations = this.generateRecommendations(current);

    return {
      current,
      history: [...this.memoryHistory],
      alerts: this.memoryAlerts$.value,
      statistics,
      recommendations
    };
  }

  /**
   * Calculate memory statistics
   */
  private calculateStatistics(): any {
    if (this.memoryHistory.length === 0) {
      return { average: 0, peak: 0, minimum: 0, growthRate: 0 };
    }

    const usages = this.memoryHistory.map(h => h.usedJSHeapSize);
    const average = usages.reduce((sum, usage) => sum + usage, 0) / usages.length;
    const peak = Math.max(...usages);
    const minimum = Math.min(...usages);

    // Calculate growth rate (MB per minute)
    const growthRate = this.calculateGrowthRate();

    return {
      average: average / (1024 * 1024), // Convert to MB
      peak: peak / (1024 * 1024),
      minimum: minimum / (1024 * 1024),
      growthRate
    };
  }

  /**
   * Calculate memory growth rate
   */
  private calculateGrowthRate(): number {
    if (this.memoryHistory.length < 10) return 0;

    const recent = this.memoryHistory.slice(-10);
    const timespan = recent[recent.length - 1].timestamp - recent[0].timestamp;
    const memoryChange = recent[recent.length - 1].usedJSHeapSize - recent[0].usedJSHeapSize;

    if (timespan === 0) return 0;

    // Convert to MB per minute
    const growthRatePerMs = memoryChange / timespan;
    return (growthRatePerMs * 60 * 1000) / (1024 * 1024);
  }

  /**
   * Generate memory optimization recommendations
   */
  private generateRecommendations(current: MemoryUsage): string[] {
    const recommendations: string[] = [];

    if (current.usagePercentage > this.THRESHOLDS.WARNING) {
      recommendations.push('Memory usage is high. Consider clearing caches or optimizing data structures.');
    }

    if (current.trend === 'increasing') {
      recommendations.push('Memory usage is increasing. Monitor for potential memory leaks.');
    }

    const growthRate = this.calculateGrowthRate();
    if (growthRate > this.THRESHOLDS.GROWTH_RATE) {
      recommendations.push(`High memory growth rate (${growthRate.toFixed(2)} MB/min). Check for memory leaks.`);
    }

    if (this.memoryHistory.length > 50) {
      const recentPeak = Math.max(...this.memoryHistory.slice(-20).map(h => h.usedJSHeapSize));
      const overallPeak = Math.max(...this.memoryHistory.map(h => h.usedJSHeapSize));

      if (recentPeak > overallPeak * 0.9) {
        recommendations.push('Recent memory usage approaching historical peak. Monitor closely.');
      }
    }

    if (recommendations.length === 0) {
      recommendations.push('Memory usage is within normal parameters.');
    }

    return recommendations;
  }

  /**
   * Check for memory alerts
   */
  private checkMemoryAlerts(usage: MemoryUsage): void {
    const alerts: MemoryAlert[] = [];

    // Critical usage alert
    if (usage.usagePercentage > this.THRESHOLDS.CRITICAL) {
      alerts.push({
        id: `critical-${Date.now()}`,
        level: 'critical',
        message: `Critical memory usage: ${(usage.usagePercentage * 100).toFixed(1)}%`,
        usage: usage.usagePercentage,
        threshold: this.THRESHOLDS.CRITICAL,
        timestamp: Date.now(),
        acknowledged: false
      });
    }
    // Warning usage alert
    else if (usage.usagePercentage > this.THRESHOLDS.WARNING) {
      alerts.push({
        id: `warning-${Date.now()}`,
        level: 'warning',
        message: `High memory usage: ${(usage.usagePercentage * 100).toFixed(1)}%`,
        usage: usage.usagePercentage,
        threshold: this.THRESHOLDS.WARNING,
        timestamp: Date.now(),
        acknowledged: false
      });
    }

    // Growth rate alert
    const growthRate = this.calculateGrowthRate();
    if (growthRate > this.THRESHOLDS.GROWTH_RATE) {
      alerts.push({
        id: `growth-${Date.now()}`,
        level: 'warning',
        message: `High memory growth rate: ${growthRate.toFixed(2)} MB/min`,
        usage: growthRate,
        threshold: this.THRESHOLDS.GROWTH_RATE,
        timestamp: Date.now(),
        acknowledged: false
      });
    }

    if (alerts.length > 0) {
      const currentAlerts = this.memoryAlerts$.value;
      const newAlerts = [...alerts, ...currentAlerts].slice(0, 50);
      this.memoryAlerts$.next(newAlerts);
    }
  }

  /**
   * Analyze for memory leaks
   */
  private analyzeMemoryLeaks(): void {
    if (this.memoryHistory.length < this.LEAK_DETECTION_WINDOW) {
      return;
    }

    const recentSamples = this.memoryHistory.slice(-this.LEAK_DETECTION_WINDOW);
    const growthRate = this.calculateGrowthRate();

    // Detect consistent growth pattern
    const growthPoints = recentSamples.reduce((count, sample, index) => {
      if (index === 0) return count;
      return sample.usedJSHeapSize > recentSamples[index - 1].usedJSHeapSize ? count + 1 : count;
    }, 0);

    const growthRatio = growthPoints / (recentSamples.length - 1);
    const suspiciousGrowth = growthRatio > 0.7; // 70% of samples showing growth
    const isLeakDetected = suspiciousGrowth && growthRate > this.THRESHOLDS.GROWTH_RATE;

    const confidence = Math.min(1, (growthRatio * 0.7) + (Math.min(growthRate / this.THRESHOLDS.GROWTH_RATE, 1) * 0.3));

    const recommendations: string[] = [];
    if (isLeakDetected) {
      recommendations.push('Potential memory leak detected. Monitor object creation and disposal.');
      recommendations.push('Check for unclosed resources, event listeners, or cached objects.');
      recommendations.push('Consider running garbage collection manually or optimizing data retention.');
    }

    const leakDetection: LeakDetection = {
      isLeakDetected,
      suspiciousGrowth,
      growthRate,
      confidence,
      recommendations
    };

    this.leakDetection$.next(leakDetection);

    if (isLeakDetected) {
      console.warn('🚨 Potential memory leak detected:', {
        growthRate: `${growthRate.toFixed(2)} MB/min`,
        confidence: `${(confidence * 100).toFixed(1)}%`,
        suspiciousGrowth
      });
    }
  }

  /**
   * Cleanup old data
   */
  private cleanupOldData(): void {
    // Remove alerts older than 1 hour
    const oneHourAgo = Date.now() - (60 * 60 * 1000);
    const currentAlerts = this.memoryAlerts$.value;
    const recentAlerts = currentAlerts.filter(alert =>
      alert.timestamp > oneHourAgo || !alert.acknowledged
    );

    if (recentAlerts.length !== currentAlerts.length) {
      this.memoryAlerts$.next(recentAlerts);
    }
  }

  /**
   * Force garbage collection (if available)
   */
  forceGarbageCollection(): boolean {
    if ('gc' in window) {
      try {
        (window as any).gc();
        console.log('🗑️ Manual garbage collection triggered');
        return true;
      } catch (error) {
        console.warn('Failed to trigger garbage collection:', error);
      }
    }
    return false;
  }

  /**
   * Get memory usage snapshot for a specific time range
   */
  getMemorySnapshot(startTime: number, endTime: number): MemoryUsage[] {
    return this.memoryHistory.filter(usage =>
      usage.timestamp >= startTime && usage.timestamp <= endTime
    );
  }

  /**
   * Export memory data for analysis
   */
  exportMemoryData(): string {
    return JSON.stringify({
      history: this.memoryHistory,
      alerts: this.memoryAlerts$.value,
      leakDetection: this.leakDetection$.value,
      exportTime: Date.now()
    }, null, 2);
  }

  // Public API

  /**
   * Get memory metrics
   */
  getMemoryMetrics(): Observable<MemoryMetrics> {
    return this.memoryMetrics$.asObservable();
  }

  /**
   * Get memory alerts
   */
  getMemoryAlerts(): Observable<MemoryAlert[]> {
    return this.memoryAlerts$.asObservable();
  }

  /**
   * Get leak detection results
   */
  getLeakDetection(): Observable<LeakDetection> {
    return this.leakDetection$.asObservable();
  }

  /**
   * Acknowledge memory alert
   */
  acknowledgeAlert(alertId: string): void {
    const currentAlerts = this.memoryAlerts$.value;
    const updatedAlerts = currentAlerts.map(alert =>
      alert.id === alertId ? { ...alert, acknowledged: true } : alert
    );
    this.memoryAlerts$.next(updatedAlerts);
  }

  /**
   * Clear all memory data
   */
  clearMemoryData(): void {
    this.memoryHistory = [];
    this.memoryAlerts$.next([]);
    this.leakDetection$.next(this.getInitialLeakDetection());
    this.memoryMetrics$.next(this.getInitialMetrics());
  }

  /**
   * Get current memory usage percentage
   */
  getCurrentMemoryUsage(): number {
    const memoryInfo = this.getMemoryInfo();
    if (!memoryInfo) return 0;
    return memoryInfo.usedJSHeapSize / memoryInfo.jsHeapSizeLimit;
  }

  // Private helpers

  private getInitialMetrics(): MemoryMetrics {
    return {
      current: {
        usedJSHeapSize: 0,
        totalJSHeapSize: 0,
        jsHeapSizeLimit: 0,
        usagePercentage: 0,
        trend: 'stable',
        timestamp: Date.now()
      },
      history: [],
      alerts: [],
      statistics: {
        average: 0,
        peak: 0,
        minimum: 0,
        growthRate: 0
      },
      recommendations: []
    };
  }

  private getInitialLeakDetection(): LeakDetection {
    return {
      isLeakDetected: false,
      suspiciousGrowth: false,
      growthRate: 0,
      confidence: 0,
      recommendations: []
    };
  }
}