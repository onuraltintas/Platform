import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, timer, fromEvent } from 'rxjs';
import { throttleTime, map, distinctUntilChanged } from 'rxjs/operators';

export interface CoreWebVitals {
  firstContentfulPaint: number; // FCP
  largestContentfulPaint: number; // LCP
  firstInputDelay: number; // FID
  cumulativeLayoutShift: number; // CLS
  timeToInteractive: number; // TTI
  totalBlockingTime: number; // TBT
}

export interface UserInteraction {
  type: 'click' | 'scroll' | 'input' | 'navigation' | 'resize';
  timestamp: number;
  responseTime: number;
  element?: string;
  value?: any;
}

export interface PerformanceScore {
  overall: number; // 0-100
  performance: number;
  accessibility: number;
  bestPractices: number;
  seo: number;
  progressiveWebApp: number;
}

export interface UserExperienceMetrics {
  vitals: CoreWebVitals;
  interactions: UserInteraction[];
  performanceScore: PerformanceScore;
  userJourney: {
    sessionDuration: number;
    pageViews: number;
    bounceRate: number;
    engagementTime: number;
    errorRate: number;
  };
  accessibility: {
    violations: number;
    warnings: number;
    score: number;
  };
  deviceMetrics: {
    deviceType: 'mobile' | 'tablet' | 'desktop';
    screenSize: { width: number; height: number };
    pixelRatio: number;
    orientation: 'portrait' | 'landscape';
    connection: string;
  };
  frustrationIndicators: {
    rapidClicks: number;
    backNavigations: number;
    pageReloads: number;
    longTasks: number;
    errors: number;
  };
}

export interface UXAlert {
  id: string;
  level: 'info' | 'warning' | 'critical';
  category: 'performance' | 'usability' | 'accessibility' | 'engagement';
  metric: string;
  value: number;
  threshold: number;
  message: string;
  timestamp: number;
  userImpact: 'low' | 'medium' | 'high';
}

/**
 * User Experience Monitor Service
 * Comprehensive UX metrics and Core Web Vitals monitoring
 */
@Injectable({
  providedIn: 'root'
})
export class UserExperienceMonitorService {
  private uxMetrics$ = new BehaviorSubject<UserExperienceMetrics>(this.getInitialMetrics());
  private uxAlerts$ = new BehaviorSubject<UXAlert[]>([]);

  private readonly THRESHOLDS = {
    FCP_GOOD: 1800, // ms
    FCP_POOR: 3000,
    LCP_GOOD: 2500,
    LCP_POOR: 4000,
    FID_GOOD: 100,
    FID_POOR: 300,
    CLS_GOOD: 0.1,
    CLS_POOR: 0.25,
    TTI_GOOD: 3800,
    TTI_POOR: 7300,
    TBT_GOOD: 200,
    TBT_POOR: 600
  };

  private sessionStartTime = Date.now();
  private pageViews = 1;
  private interactions: UserInteraction[] = [];
  private rapidClickCount = 0;
  private lastClickTime = 0;
  private frustrationMetrics = {
    rapidClicks: 0,
    backNavigations: 0,
    pageReloads: 0,
    longTasks: 0,
    errors: 0
  };

  private vitalObserver?: PerformanceObserver;
  private interactionObserver?: PerformanceObserver;

  constructor() {
    this.initializeUXMonitoring();
  }

  /**
   * Initialize UX monitoring
   */
  private initializeUXMonitoring(): void {
    this.setupWebVitalsObservers();
    this.setupUserInteractionTracking();
    this.setupDeviceMetrics();
    this.startMetricsCollection();

    console.log('👤 User Experience Monitor initialized');
  }

  /**
   * Setup Web Vitals observers
   */
  private setupWebVitalsObservers(): void {
    if ('PerformanceObserver' in window) {
      try {
        // LCP Observer
        this.vitalObserver = new PerformanceObserver((list) => {
          const entries = list.getEntries();
          entries.forEach((entry) => {
            this.processWebVitalEntry(entry);
          });
        });

        // Observe different vital types
        ['largest-contentful-paint', 'first-input', 'layout-shift'].forEach(type => {
          try {
            this.vitalObserver?.observe({ type, buffered: true } as any);
          } catch (error) {
            console.warn(`Cannot observe ${type}:`, error);
          }
        });

        // Long task observer for TBT calculation
        this.interactionObserver = new PerformanceObserver((list) => {
          const entries = list.getEntries();
          entries.forEach((entry) => {
            if (entry.duration > 50) {
              this.frustrationMetrics.longTasks++;
              this.updateTotalBlockingTime(entry.duration);
            }
          });
        });

        this.interactionObserver.observe({ type: 'longtask', buffered: true } as any);

      } catch (error) {
        console.warn('Performance Observer setup failed:', error);
      }
    }

    // Fallback for older browsers
    this.setupFallbackVitals();
  }

  /**
   * Setup fallback vitals measurement
   */
  private setupFallbackVitals(): void {
    // FCP measurement
    const paintEntries = performance.getEntriesByType('paint');
    const fcpEntry = paintEntries.find(entry => entry.name === 'first-contentful-paint');

    if (fcpEntry) {
      this.updateVital('FCP', fcpEntry.startTime);
    }

    // TTI estimation
    this.estimateTimeToInteractive();
  }

  /**
   * Process Web Vital entries
   */
  private processWebVitalEntry(entry: PerformanceEntry): void {
    switch (entry.entryType) {
      case 'largest-contentful-paint':
        this.updateVital('LCP', entry.startTime);
        break;

      case 'first-input':
        const fidEntry = entry as any;
        this.updateVital('FID', fidEntry.processingStart - fidEntry.startTime);
        break;

      case 'layout-shift':
        const clsEntry = entry as any;
        if (!clsEntry.hadRecentInput) {
          this.updateVital('CLS', clsEntry.value);
        }
        break;
    }
  }

  /**
   * Update Web Vital metric
   */
  private updateVital(vital: string, value: number): void {
    const currentMetrics = this.uxMetrics$.value;
    const updatedVitals = { ...currentMetrics.vitals };

    switch (vital) {
      case 'FCP':
        updatedVitals.firstContentfulPaint = value;
        this.checkVitalThreshold('FCP', value, this.THRESHOLDS.FCP_GOOD, this.THRESHOLDS.FCP_POOR);
        break;
      case 'LCP':
        updatedVitals.largestContentfulPaint = value;
        this.checkVitalThreshold('LCP', value, this.THRESHOLDS.LCP_GOOD, this.THRESHOLDS.LCP_POOR);
        break;
      case 'FID':
        updatedVitals.firstInputDelay = value;
        this.checkVitalThreshold('FID', value, this.THRESHOLDS.FID_GOOD, this.THRESHOLDS.FID_POOR);
        break;
      case 'CLS':
        updatedVitals.cumulativeLayoutShift += value;
        this.checkVitalThreshold('CLS', updatedVitals.cumulativeLayoutShift, this.THRESHOLDS.CLS_GOOD, this.THRESHOLDS.CLS_POOR);
        break;
      case 'TTI':
        updatedVitals.timeToInteractive = value;
        this.checkVitalThreshold('TTI', value, this.THRESHOLDS.TTI_GOOD, this.THRESHOLDS.TTI_POOR);
        break;
    }

    const updatedMetrics = { ...currentMetrics, vitals: updatedVitals };
    this.uxMetrics$.next(updatedMetrics);
  }

  /**
   * Update Total Blocking Time
   */
  private updateTotalBlockingTime(duration: number): void {
    const currentMetrics = this.uxMetrics$.value;
    const blockingTime = Math.max(0, duration - 50); // Only time above 50ms counts

    const updatedVitals = {
      ...currentMetrics.vitals,
      totalBlockingTime: currentMetrics.vitals.totalBlockingTime + blockingTime
    };

    this.checkVitalThreshold('TBT', updatedVitals.totalBlockingTime, this.THRESHOLDS.TBT_GOOD, this.THRESHOLDS.TBT_POOR);

    const updatedMetrics = { ...currentMetrics, vitals: updatedVitals };
    this.uxMetrics$.next(updatedMetrics);
  }

  /**
   * Check vital thresholds and create alerts
   */
  private checkVitalThreshold(vital: string, value: number, goodThreshold: number, poorThreshold: number): void {
    let level: 'info' | 'warning' | 'critical' = 'info';
    let impact: 'low' | 'medium' | 'high' = 'low';

    if (value > poorThreshold) {
      level = 'critical';
      impact = 'high';
    } else if (value > goodThreshold) {
      level = 'warning';
      impact = 'medium';
    }

    if (level !== 'info') {
      this.createUXAlert(level, 'performance', vital, value, goodThreshold,
        `${vital} is ${level === 'critical' ? 'poor' : 'needs improvement'}: ${value.toFixed(2)}${vital === 'CLS' ? '' : 'ms'}`,
        impact);
    }
  }

  /**
   * Setup user interaction tracking
   */
  private setupUserInteractionTracking(): void {
    // Click tracking
    fromEvent(document, 'click').pipe(
      throttleTime(10) // Allow rapid clicks to be detected
    ).subscribe((event: Event) => {
      const currentTime = Date.now();
      const responseTime = this.measureInteractionResponseTime();

      // Detect rapid clicks (frustration indicator)
      if (currentTime - this.lastClickTime < 500) {
        this.rapidClickCount++;
        if (this.rapidClickCount >= 3) {
          this.frustrationMetrics.rapidClicks++;
        }
      } else {
        this.rapidClickCount = 0;
      }

      this.lastClickTime = currentTime;

      const target = event.target as HTMLElement;
      this.recordInteraction('click', responseTime, target?.tagName || 'unknown');
    });

    // Scroll tracking
    fromEvent(window, 'scroll').pipe(
      throttleTime(100)
    ).subscribe(() => {
      const responseTime = this.measureInteractionResponseTime();
      this.recordInteraction('scroll', responseTime);
    });

    // Input tracking
    fromEvent(document, 'input').pipe(
      throttleTime(50)
    ).subscribe((event: Event) => {
      const responseTime = this.measureInteractionResponseTime();
      const target = event.target as HTMLInputElement;
      this.recordInteraction('input', responseTime, target?.type || 'text');
    });

    // Navigation tracking
    window.addEventListener('beforeunload', () => {
      this.recordInteraction('navigation', 0);
    });

    // Resize tracking
    fromEvent(window, 'resize').pipe(
      throttleTime(250),
      distinctUntilChanged()
    ).subscribe(() => {
      const responseTime = this.measureInteractionResponseTime();
      this.recordInteraction('resize', responseTime);
    });

    // Back/forward navigation
    window.addEventListener('popstate', () => {
      this.frustrationMetrics.backNavigations++;
    });

    // Error tracking
    window.addEventListener('error', () => {
      this.frustrationMetrics.errors++;
    });
  }

  /**
   * Measure interaction response time
   */
  private measureInteractionResponseTime(): number {
    const startTime = performance.now();

    return new Promise<number>((resolve) => {
      requestAnimationFrame(() => {
        resolve(performance.now() - startTime);
      });
    }) as any; // Simplified for synchronous use
  }

  /**
   * Record user interaction
   */
  private recordInteraction(
    type: 'click' | 'scroll' | 'input' | 'navigation' | 'resize',
    responseTime: number,
    element?: string
  ): void {
    const interaction: UserInteraction = {
      type,
      timestamp: Date.now(),
      responseTime,
      element
    };

    this.interactions.push(interaction);

    // Keep only last 100 interactions
    if (this.interactions.length > 100) {
      this.interactions.shift();
    }

    // Check for slow interactions
    if (responseTime > 100) {
      this.createUXAlert('warning', 'usability', 'interaction-delay', responseTime, 100,
        `Slow ${type} interaction: ${responseTime.toFixed(2)}ms`, 'medium');
    }
  }

  /**
   * Setup device metrics
   */
  private setupDeviceMetrics(): void {
    // Initial device metrics are collected in getDeviceMetrics()
    // This could be extended to track orientation changes, etc.

    window.addEventListener('orientationchange', () => {
      setTimeout(() => this.updateMetrics(), 100);
    });
  }

  /**
   * Get device metrics
   */
  private getDeviceMetrics(): any {
    const width = window.innerWidth;
    const height = window.innerHeight;

    let deviceType: 'mobile' | 'tablet' | 'desktop' = 'desktop';
    if (width < 768) {
      deviceType = 'mobile';
    } else if (width < 1024) {
      deviceType = 'tablet';
    }

    const orientation = width > height ? 'landscape' : 'portrait';

    return {
      deviceType,
      screenSize: { width, height },
      pixelRatio: window.devicePixelRatio || 1,
      orientation,
      connection: this.getConnectionType()
    };
  }

  /**
   * Get connection type
   */
  private getConnectionType(): string {
    if ('connection' in navigator) {
      const connection = (navigator as any).connection;
      return connection.effectiveType || 'unknown';
    }
    return 'unknown';
  }

  /**
   * Start metrics collection
   */
  private startMetricsCollection(): void {
    // Update metrics every 30 seconds
    timer(30000, 30000).subscribe(() => {
      this.updateMetrics();
    });

    // Cleanup old data every 10 minutes
    timer(600000, 600000).subscribe(() => {
      this.cleanupOldData();
    });
  }

  /**
   * Update UX metrics
   */
  private updateMetrics(): void {
    const currentMetrics = this.uxMetrics$.value;
    const sessionDuration = Date.now() - this.sessionStartTime;
    const engagementTime = this.calculateEngagementTime();

    const updatedMetrics: UserExperienceMetrics = {
      ...currentMetrics,
      interactions: [...this.interactions],
      performanceScore: this.calculatePerformanceScore(),
      userJourney: {
        sessionDuration,
        pageViews: this.pageViews,
        bounceRate: this.calculateBounceRate(),
        engagementTime,
        errorRate: this.frustrationMetrics.errors / Math.max(this.interactions.length, 1)
      },
      accessibility: this.assessAccessibility(),
      deviceMetrics: this.getDeviceMetrics(),
      frustrationIndicators: { ...this.frustrationMetrics }
    };

    this.uxMetrics$.next(updatedMetrics);
  }

  /**
   * Calculate performance score (0-100)
   */
  private calculatePerformanceScore(): PerformanceScore {
    const vitals = this.uxMetrics$.value.vitals;

    // Performance score based on Core Web Vitals
    const fcpScore = this.getVitalScore(vitals.firstContentfulPaint, this.THRESHOLDS.FCP_GOOD, this.THRESHOLDS.FCP_POOR);
    const lcpScore = this.getVitalScore(vitals.largestContentfulPaint, this.THRESHOLDS.LCP_GOOD, this.THRESHOLDS.LCP_POOR);
    const fidScore = this.getVitalScore(vitals.firstInputDelay, this.THRESHOLDS.FID_GOOD, this.THRESHOLDS.FID_POOR);
    const clsScore = this.getVitalScore(vitals.cumulativeLayoutShift, this.THRESHOLDS.CLS_GOOD, this.THRESHOLDS.CLS_POOR);
    const ttiScore = this.getVitalScore(vitals.timeToInteractive, this.THRESHOLDS.TTI_GOOD, this.THRESHOLDS.TTI_POOR);

    const performance = Math.round((fcpScore + lcpScore + fidScore + clsScore + ttiScore) / 5);

    return {
      overall: performance, // Simplified - would normally include other factors
      performance,
      accessibility: this.assessAccessibility().score,
      bestPractices: 85, // Placeholder - would need actual analysis
      seo: 90, // Placeholder
      progressiveWebApp: 80 // Placeholder
    };
  }

  /**
   * Get score for individual vital (0-100)
   */
  private getVitalScore(value: number, good: number, poor: number): number {
    if (value === 0) return 100; // Not measured yet

    if (value <= good) return 100;
    if (value >= poor) return 0;

    // Linear interpolation between good and poor
    return Math.round(100 - ((value - good) / (poor - good)) * 100);
  }

  /**
   * Calculate engagement time
   */
  private calculateEngagementTime(): number {
    // Simplified engagement calculation based on interactions
    const activeInteractions = this.interactions.filter(i =>
      ['click', 'input', 'scroll'].includes(i.type)
    );

    return activeInteractions.length * 2000; // Rough estimate: 2s per interaction
  }

  /**
   * Calculate bounce rate
   */
  private calculateBounceRate(): number {
    const sessionDuration = Date.now() - this.sessionStartTime;
    const hasEngagement = this.interactions.length > 3;

    // Simple bounce rate calculation
    if (sessionDuration > 30000 && hasEngagement) {
      return 0; // Not a bounce
    }

    return sessionDuration < 10000 ? 1 : 0.5; // Likely bounce / Maybe bounce
  }

  /**
   * Assess accessibility
   */
  private assessAccessibility(): any {
    // Simplified accessibility assessment
    // In a real implementation, this would integrate with axe-core or similar

    const violations = 0; // Would be calculated by accessibility scanner
    const warnings = 0;
    const score = 95; // Placeholder score

    return { violations, warnings, score };
  }

  /**
   * Estimate Time to Interactive
   */
  private estimateTimeToInteractive(): void {
    // Simplified TTI estimation
    const domContentLoaded = performance.getEntriesByType('navigation')[0] as any;
    if (domContentLoaded && domContentLoaded.domContentLoadedEventEnd) {
      const tti = domContentLoaded.domContentLoadedEventEnd;
      this.updateVital('TTI', tti);
    }
  }

  /**
   * Create UX alert
   */
  private createUXAlert(
    level: 'info' | 'warning' | 'critical',
    category: 'performance' | 'usability' | 'accessibility' | 'engagement',
    metric: string,
    value: number,
    threshold: number,
    message: string,
    userImpact: 'low' | 'medium' | 'high'
  ): void {
    const alert: UXAlert = {
      id: crypto.randomUUID(),
      level,
      category,
      metric,
      value,
      threshold,
      message,
      timestamp: Date.now(),
      userImpact
    };

    const currentAlerts = this.uxAlerts$.value;
    const newAlerts = [alert, ...currentAlerts].slice(0, 50);
    this.uxAlerts$.next(newAlerts);
  }

  /**
   * Cleanup old data
   */
  private cleanupOldData(): void {
    const oneHourAgo = Date.now() - (60 * 60 * 1000);

    // Clean alerts
    const currentAlerts = this.uxAlerts$.value;
    const recentAlerts = currentAlerts.filter(alert => alert.timestamp > oneHourAgo);

    if (recentAlerts.length !== currentAlerts.length) {
      this.uxAlerts$.next(recentAlerts);
    }

    // Clean interactions
    this.interactions = this.interactions.filter(
      interaction => interaction.timestamp > oneHourAgo
    );
  }

  // Public API

  /**
   * Get UX metrics
   */
  getUXMetrics(): Observable<UserExperienceMetrics> {
    return this.uxMetrics$.asObservable();
  }

  /**
   * Get UX alerts
   */
  getUXAlerts(): Observable<UXAlert[]> {
    return this.uxAlerts$.asObservable();
  }

  /**
   * Get Core Web Vitals snapshot
   */
  getCoreWebVitals(): CoreWebVitals {
    return this.uxMetrics$.value.vitals;
  }

  /**
   * Get performance score
   */
  getPerformanceScore(): PerformanceScore {
    return this.uxMetrics$.value.performanceScore;
  }

  /**
   * Track custom user action
   */
  trackUserAction(action: string, details?: any): void {
    this.recordInteraction('click' as any, 0, action);
  }

  /**
   * Track page view
   */
  trackPageView(): void {
    this.pageViews++;
    this.recordInteraction('navigation', 0, 'page-view');
  }

  /**
   * Export UX data
   */
  exportUXData(): string {
    return JSON.stringify({
      metrics: this.uxMetrics$.value,
      alerts: this.uxAlerts$.value,
      exportTime: Date.now()
    }, null, 2);
  }

  /**
   * Reset UX tracking
   */
  resetUXTracking(): void {
    this.sessionStartTime = Date.now();
    this.pageViews = 1;
    this.interactions = [];
    this.frustrationMetrics = {
      rapidClicks: 0,
      backNavigations: 0,
      pageReloads: 0,
      longTasks: 0,
      errors: 0
    };
    this.uxAlerts$.next([]);
    this.uxMetrics$.next(this.getInitialMetrics());
  }

  // Private helpers

  private getInitialMetrics(): UserExperienceMetrics {
    return {
      vitals: {
        firstContentfulPaint: 0,
        largestContentfulPaint: 0,
        firstInputDelay: 0,
        cumulativeLayoutShift: 0,
        timeToInteractive: 0,
        totalBlockingTime: 0
      },
      interactions: [],
      performanceScore: {
        overall: 100,
        performance: 100,
        accessibility: 100,
        bestPractices: 100,
        seo: 100,
        progressiveWebApp: 100
      },
      userJourney: {
        sessionDuration: 0,
        pageViews: 1,
        bounceRate: 0,
        engagementTime: 0,
        errorRate: 0
      },
      accessibility: {
        violations: 0,
        warnings: 0,
        score: 100
      },
      deviceMetrics: this.getDeviceMetrics(),
      frustrationIndicators: {
        rapidClicks: 0,
        backNavigations: 0,
        pageReloads: 0,
        longTasks: 0,
        errors: 0
      }
    };
  }
}