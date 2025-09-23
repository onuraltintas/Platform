import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, timer, combineLatest } from 'rxjs';
import { map, filter } from 'rxjs/operators';
import { PerformanceAnalyticsService } from './performance-analytics.service';
import { MemoryMonitorService } from './memory-monitor.service';
import { NetworkMonitorService } from './network-monitor.service';
import { UserExperienceMonitorService } from './user-experience-monitor.service';

export interface PerformanceAlert {
  id: string;
  level: 'info' | 'warning' | 'critical';
  category: 'performance' | 'memory' | 'network' | 'ux' | 'security' | 'system';
  metric: string;
  currentValue: number;
  threshold: number;
  message: string;
  description: string;
  timestamp: number;
  acknowledged: boolean;
  resolved: boolean;
  source: string;
  impact: 'low' | 'medium' | 'high' | 'critical';
  recommendedActions: string[];
  metadata?: any;
}

export interface AlertRule {
  id: string;
  name: string;
  category: 'performance' | 'memory' | 'network' | 'ux' | 'security' | 'system';
  metric: string;
  operator: 'gt' | 'lt' | 'eq' | 'gte' | 'lte';
  threshold: number;
  level: 'info' | 'warning' | 'critical';
  enabled: boolean;
  cooldownMs: number;
  description: string;
  lastTriggered?: number;
}

export interface AlertConfiguration {
  enableBrowserNotifications: boolean;
  enableToastNotifications: boolean;
  enableSoundAlerts: boolean;
  enableEmailAlerts: boolean;
  maxActiveAlerts: number;
  autoResolveAfterMs: number;
  categories: {
    [key: string]: {
      enabled: boolean;
      minLevel: 'info' | 'warning' | 'critical';
    };
  };
}

export interface AlertSummary {
  total: number;
  critical: number;
  warning: number;
  info: number;
  unacknowledged: number;
  unresolved: number;
  byCategory: { [category: string]: number };
  trends: {
    last24h: number;
    last1h: number;
    last15m: number;
  };
}

/**
 * Performance Alerts Service
 * Centralized alert management for all performance monitoring
 */
@Injectable({
  providedIn: 'root'
})
export class PerformanceAlertsService {
  private performanceAnalytics = inject(PerformanceAnalyticsService);
  private memoryMonitor = inject(MemoryMonitorService);
  private networkMonitor = inject(NetworkMonitorService);
  private uxMonitor = inject(UserExperienceMonitorService);

  private alerts$ = new BehaviorSubject<PerformanceAlert[]>([]);
  private alertRules$ = new BehaviorSubject<AlertRule[]>(this.getDefaultRules());
  private alertConfig$ = new BehaviorSubject<AlertConfiguration>(this.getDefaultConfig());

  private readonly MAX_ALERTS = 500;
  private alertIdCounter = 0;

  constructor() {
    this.initializeAlertSystem();
  }

  /**
   * Initialize alert system
   */
  private initializeAlertSystem(): void {
    this.setupAlertMonitoring();
    this.setupNotificationSystem();
    this.startAlertProcessing();

    console.log('🚨 Performance Alerts Service initialized');
  }

  /**
   * Setup alert monitoring from all sources
   */
  private setupAlertMonitoring(): void {
    // Monitor performance analytics alerts
    this.performanceAnalytics.getPerformanceAlerts().subscribe(alerts => {
      alerts.forEach(alert => {
        if (!alert.resolved) {
          this.processIncomingAlert('performance', alert);
        }
      });
    });

    // Monitor memory alerts
    this.memoryMonitor.getMemoryAlerts().subscribe(alerts => {
      alerts.forEach(alert => {
        if (!alert.acknowledged) {
          this.processMemoryAlert(alert);
        }
      });
    });

    // Monitor network alerts
    this.networkMonitor.getNetworkAlerts().subscribe(alerts => {
      alerts.forEach(alert => {
        this.processNetworkAlert(alert);
      });
    });

    // Monitor UX alerts
    this.uxMonitor.getUXAlerts().subscribe(alerts => {
      alerts.forEach(alert => {
        this.processUXAlert(alert);
      });
    });
  }

  /**
   * Setup notification system
   */
  private setupNotificationSystem(): void {
    // Request notification permission if not already granted
    if ('Notification' in window && Notification.permission === 'default') {
      Notification.requestPermission();
    }

    // Setup sound for critical alerts
    this.alerts$.pipe(
      filter(alerts => alerts.some(a => a.level === 'critical' && !a.acknowledged))
    ).subscribe(() => {
      const config = this.alertConfig$.value;
      if (config.enableSoundAlerts) {
        this.playAlertSound();
      }
    });
  }

  /**
   * Start alert processing
   */
  private startAlertProcessing(): void {
    // Process alert rules every 30 seconds
    timer(30000, 30000).subscribe(() => {
      this.evaluateAlertRules();
    });

    // Auto-resolve old alerts every 5 minutes
    timer(300000, 300000).subscribe(() => {
      this.autoResolveAlerts();
    });

    // Cleanup old alerts every hour
    timer(3600000, 3600000).subscribe(() => {
      this.cleanupOldAlerts();
    });
  }

  /**
   * Process incoming alert from performance analytics
   */
  private processIncomingAlert(category: string, sourceAlert: any): void {
    const alert: PerformanceAlert = {
      id: this.generateAlertId(),
      level: sourceAlert.level,
      category: category as any,
      metric: sourceAlert.metric,
      currentValue: sourceAlert.value,
      threshold: sourceAlert.threshold,
      message: sourceAlert.message,
      description: this.generateAlertDescription(sourceAlert),
      timestamp: sourceAlert.timestamp,
      acknowledged: false,
      resolved: false,
      source: 'performance-analytics',
      impact: this.calculateImpact(sourceAlert.level, sourceAlert.metric),
      recommendedActions: this.generateRecommendedActions(sourceAlert.metric, sourceAlert.level),
      metadata: sourceAlert
    };

    this.addAlert(alert);
  }

  /**
   * Process memory alert
   */
  private processMemoryAlert(memoryAlert: any): void {
    const alert: PerformanceAlert = {
      id: this.generateAlertId(),
      level: memoryAlert.level,
      category: 'memory',
      metric: 'memory-usage',
      currentValue: memoryAlert.usage,
      threshold: memoryAlert.threshold,
      message: memoryAlert.message,
      description: 'High memory usage detected. Monitor for memory leaks.',
      timestamp: memoryAlert.timestamp,
      acknowledged: false,
      resolved: false,
      source: 'memory-monitor',
      impact: memoryAlert.level === 'critical' ? 'critical' : 'high',
      recommendedActions: [
        'Check for memory leaks',
        'Clear caches if possible',
        'Monitor object retention',
        'Consider garbage collection'
      ],
      metadata: memoryAlert
    };

    this.addAlert(alert);
  }

  /**
   * Process network alert
   */
  private processNetworkAlert(networkAlert: any): void {
    const alert: PerformanceAlert = {
      id: this.generateAlertId(),
      level: networkAlert.level,
      category: 'network',
      metric: networkAlert.type,
      currentValue: networkAlert.value || 0,
      threshold: networkAlert.threshold || 0,
      message: networkAlert.message,
      description: this.generateNetworkDescription(networkAlert.type),
      timestamp: networkAlert.timestamp,
      acknowledged: false,
      resolved: false,
      source: 'network-monitor',
      impact: this.calculateNetworkImpact(networkAlert.type, networkAlert.level),
      recommendedActions: this.generateNetworkActions(networkAlert.type),
      metadata: networkAlert
    };

    this.addAlert(alert);
  }

  /**
   * Process UX alert
   */
  private processUXAlert(uxAlert: any): void {
    const alert: PerformanceAlert = {
      id: this.generateAlertId(),
      level: uxAlert.level,
      category: 'ux',
      metric: uxAlert.metric,
      currentValue: uxAlert.value,
      threshold: uxAlert.threshold,
      message: uxAlert.message,
      description: this.generateUXDescription(uxAlert.category, uxAlert.metric),
      timestamp: uxAlert.timestamp,
      acknowledged: false,
      resolved: false,
      source: 'ux-monitor',
      impact: uxAlert.userImpact,
      recommendedActions: this.generateUXActions(uxAlert.category, uxAlert.metric),
      metadata: uxAlert
    };

    this.addAlert(alert);
  }

  /**
   * Add alert to the system
   */
  private addAlert(alert: PerformanceAlert): void {
    const config = this.alertConfig$.value;

    // Check if category is enabled
    const categoryConfig = config.categories[alert.category];
    if (!categoryConfig?.enabled) return;

    // Check if level meets minimum threshold
    const levelPriority = { 'info': 1, 'warning': 2, 'critical': 3 };
    if (levelPriority[alert.level] < levelPriority[categoryConfig.minLevel]) return;

    const currentAlerts = this.alerts$.value;

    // Check for duplicate alerts (same metric within last 5 minutes)
    const isDuplicate = currentAlerts.some(existing =>
      existing.metric === alert.metric &&
      existing.category === alert.category &&
      (alert.timestamp - existing.timestamp) < 300000 && // 5 minutes
      !existing.resolved
    );

    if (isDuplicate) return;

    // Add alert
    const newAlerts = [alert, ...currentAlerts].slice(0, this.MAX_ALERTS);
    this.alerts$.next(newAlerts);

    // Send notifications
    this.sendNotification(alert);

    console.log(`🚨 New ${alert.level} alert:`, alert.message);
  }

  /**
   * Send notification for alert
   */
  private sendNotification(alert: PerformanceAlert): void {
    const config = this.alertConfig$.value;

    // Browser notification
    if (config.enableBrowserNotifications && 'Notification' in window && Notification.permission === 'granted') {
      const notification = new Notification(`${alert.level.toUpperCase()}: ${alert.metric}`, {
        body: alert.message,
        icon: this.getAlertIcon(alert.level),
        tag: alert.id
      });

      setTimeout(() => notification.close(), 5000);
    }

    // Toast notification would be handled by the UI component
    if (config.enableToastNotifications) {
      // Emit event for toast system
      this.notifyToastSystem(alert);
    }
  }

  /**
   * Play alert sound
   */
  private playAlertSound(): void {
    try {
      const audio = new Audio();
      audio.src = 'data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmwhBzSK1PXCfiMaIXS31/7dklMLmNrZB...'; // Truncated base64 sound
      audio.volume = 0.3;
      audio.play().catch(() => {
        // Ignore audio play errors
      });
    } catch (error) {
      // Ignore audio errors
    }
  }

  /**
   * Notify toast system
   */
  private notifyToastSystem(alert: PerformanceAlert): void {
    // This would integrate with your toast notification system
    // For now, just emit a custom event
    window.dispatchEvent(new CustomEvent('performance-alert', {
      detail: alert
    }));
  }

  /**
   * Evaluate alert rules
   */
  private evaluateAlertRules(): void {
    const rules = this.alertRules$.value.filter(rule => rule.enabled);
    const now = Date.now();

    rules.forEach(rule => {
      // Check cooldown
      if (rule.lastTriggered && (now - rule.lastTriggered) < rule.cooldownMs) {
        return;
      }

      // Get current metric value based on rule
      this.getCurrentMetricValue(rule).then(currentValue => {
        if (this.evaluateRule(rule, currentValue)) {
          this.triggerRule(rule, currentValue);
        }
      });
    });
  }

  /**
   * Get current metric value for rule evaluation
   */
  private async getCurrentMetricValue(rule: AlertRule): Promise<number> {
    // This would get real-time values from the appropriate service
    // Simplified implementation
    switch (rule.category) {
      case 'memory':
        return this.memoryMonitor.getCurrentMemoryUsage() * 100;
      case 'network':
        return 0; // Would get from network monitor
      case 'performance':
        return 0; // Would get from performance analytics
      case 'ux':
        return 0; // Would get from UX monitor
      default:
        return 0;
    }
  }

  /**
   * Evaluate if rule condition is met
   */
  private evaluateRule(rule: AlertRule, currentValue: number): boolean {
    switch (rule.operator) {
      case 'gt': return currentValue > rule.threshold;
      case 'gte': return currentValue >= rule.threshold;
      case 'lt': return currentValue < rule.threshold;
      case 'lte': return currentValue <= rule.threshold;
      case 'eq': return currentValue === rule.threshold;
      default: return false;
    }
  }

  /**
   * Trigger alert rule
   */
  private triggerRule(rule: AlertRule, currentValue: number): void {
    const alert: PerformanceAlert = {
      id: this.generateAlertId(),
      level: rule.level,
      category: rule.category,
      metric: rule.metric,
      currentValue,
      threshold: rule.threshold,
      message: `${rule.name}: ${rule.metric} is ${currentValue}`,
      description: rule.description,
      timestamp: Date.now(),
      acknowledged: false,
      resolved: false,
      source: 'alert-rule',
      impact: rule.level === 'critical' ? 'critical' : 'medium',
      recommendedActions: this.generateRecommendedActions(rule.metric, rule.level),
      metadata: { ruleId: rule.id }
    };

    this.addAlert(alert);

    // Update rule last triggered time
    const updatedRules = this.alertRules$.value.map(r =>
      r.id === rule.id ? { ...r, lastTriggered: Date.now() } : r
    );
    this.alertRules$.next(updatedRules);
  }

  /**
   * Auto-resolve old alerts
   */
  private autoResolveAlerts(): void {
    const config = this.alertConfig$.value;
    const cutoffTime = Date.now() - config.autoResolveAfterMs;

    const currentAlerts = this.alerts$.value;
    const updatedAlerts = currentAlerts.map(alert => {
      if (!alert.resolved && alert.timestamp < cutoffTime) {
        return { ...alert, resolved: true };
      }
      return alert;
    });

    if (updatedAlerts.some((alert, index) => alert.resolved !== currentAlerts[index].resolved)) {
      this.alerts$.next(updatedAlerts);
    }
  }

  /**
   * Cleanup old alerts
   */
  private cleanupOldAlerts(): void {
    const cutoffTime = Date.now() - (24 * 60 * 60 * 1000); // 24 hours

    const currentAlerts = this.alerts$.value;
    const recentAlerts = currentAlerts.filter(alert =>
      alert.timestamp > cutoffTime || (!alert.resolved && !alert.acknowledged)
    );

    if (recentAlerts.length !== currentAlerts.length) {
      this.alerts$.next(recentAlerts);
    }
  }

  // Helper methods for generating alert content

  private generateAlertDescription(sourceAlert: any): string {
    // Generate detailed description based on alert type
    return `Performance issue detected in ${sourceAlert.metric}. Current value: ${sourceAlert.value}, Threshold: ${sourceAlert.threshold}`;
  }

  private generateNetworkDescription(type: string): string {
    const descriptions = {
      'offline': 'Network connection has been lost',
      'slow': 'Network latency is higher than expected',
      'timeout': 'Network requests are timing out',
      'packet-loss': 'Network packet loss detected',
      'bandwidth': 'Network bandwidth is lower than expected'
    };
    return descriptions[type] || 'Network performance issue detected';
  }

  private generateUXDescription(category: string, metric: string): string {
    return `User experience issue in ${category}: ${metric} performance is below acceptable thresholds`;
  }

  private calculateImpact(level: string, metric: string): 'low' | 'medium' | 'high' | 'critical' {
    if (level === 'critical') return 'critical';
    if (level === 'warning') return 'medium';
    return 'low';
  }

  private calculateNetworkImpact(type: string, level: string): 'low' | 'medium' | 'high' | 'critical' {
    if (type === 'offline') return 'critical';
    if (level === 'critical') return 'high';
    return 'medium';
  }

  private generateRecommendedActions(metric: string, level: string): string[] {
    const actions = {
      'FCP': ['Optimize critical rendering path', 'Reduce server response time', 'Eliminate render-blocking resources'],
      'LCP': ['Optimize largest content element', 'Improve server response time', 'Preload important resources'],
      'FID': ['Reduce JavaScript execution time', 'Break up long tasks', 'Use web workers for heavy computation'],
      'CLS': ['Set size attributes on images and videos', 'Avoid inserting content above existing content', 'Use CSS transforms instead of changing layout properties'],
      'memory-usage': ['Check for memory leaks', 'Clear unnecessary caches', 'Optimize object retention'],
      'default': ['Monitor performance closely', 'Check system resources', 'Review recent changes']
    };

    return actions[metric] || actions['default'];
  }

  private generateNetworkActions(type: string): string[] {
    const actions = {
      'offline': ['Check network connection', 'Implement offline fallback', 'Cache critical resources'],
      'slow': ['Optimize request payload', 'Use CDN', 'Implement request compression'],
      'timeout': ['Increase timeout values', 'Implement retry logic', 'Check server health'],
      'packet-loss': ['Check network stability', 'Implement request retry', 'Monitor network quality'],
      'bandwidth': ['Optimize resource sizes', 'Implement lazy loading', 'Use adaptive streaming']
    };

    return actions[type] || ['Monitor network performance', 'Check connection quality'];
  }

  private generateUXActions(category: string, metric: string): string[] {
    const actions = {
      'performance': ['Optimize page load speed', 'Reduce JavaScript execution time', 'Implement performance budgets'],
      'usability': ['Improve interaction response time', 'Optimize user flows', 'Reduce cognitive load'],
      'accessibility': ['Fix accessibility violations', 'Improve keyboard navigation', 'Enhance screen reader support'],
      'engagement': ['Improve content relevance', 'Optimize user journey', 'Reduce bounce rate']
    };

    return actions[category] || ['Review user experience metrics', 'Conduct usability testing'];
  }

  private getAlertIcon(level: string): string {
    const icons = {
      'info': '📊',
      'warning': '⚠️',
      'critical': '🚨'
    };
    return icons[level] || '📊';
  }

  private generateAlertId(): string {
    return `alert-${Date.now()}-${++this.alertIdCounter}`;
  }

  // Public API

  /**
   * Get all alerts
   */
  getAlerts(): Observable<PerformanceAlert[]> {
    return this.alerts$.asObservable();
  }

  /**
   * Get alert summary
   */
  getAlertSummary(): Observable<AlertSummary> {
    return this.alerts$.pipe(
      map(alerts => {
        const now = Date.now();
        const last24h = now - (24 * 60 * 60 * 1000);
        const last1h = now - (60 * 60 * 1000);
        const last15m = now - (15 * 60 * 1000);

        const byCategory = alerts.reduce((acc, alert) => {
          acc[alert.category] = (acc[alert.category] || 0) + 1;
          return acc;
        }, {} as { [key: string]: number });

        return {
          total: alerts.length,
          critical: alerts.filter(a => a.level === 'critical').length,
          warning: alerts.filter(a => a.level === 'warning').length,
          info: alerts.filter(a => a.level === 'info').length,
          unacknowledged: alerts.filter(a => !a.acknowledged).length,
          unresolved: alerts.filter(a => !a.resolved).length,
          byCategory,
          trends: {
            last24h: alerts.filter(a => a.timestamp > last24h).length,
            last1h: alerts.filter(a => a.timestamp > last1h).length,
            last15m: alerts.filter(a => a.timestamp > last15m).length
          }
        };
      })
    );
  }

  /**
   * Acknowledge alert
   */
  acknowledgeAlert(alertId: string): void {
    const currentAlerts = this.alerts$.value;
    const updatedAlerts = currentAlerts.map(alert =>
      alert.id === alertId ? { ...alert, acknowledged: true } : alert
    );
    this.alerts$.next(updatedAlerts);
  }

  /**
   * Resolve alert
   */
  resolveAlert(alertId: string): void {
    const currentAlerts = this.alerts$.value;
    const updatedAlerts = currentAlerts.map(alert =>
      alert.id === alertId ? { ...alert, resolved: true } : alert
    );
    this.alerts$.next(updatedAlerts);
  }

  /**
   * Get alert configuration
   */
  getAlertConfiguration(): Observable<AlertConfiguration> {
    return this.alertConfig$.asObservable();
  }

  /**
   * Update alert configuration
   */
  updateAlertConfiguration(config: Partial<AlertConfiguration>): void {
    const currentConfig = this.alertConfig$.value;
    const updatedConfig = { ...currentConfig, ...config };
    this.alertConfig$.next(updatedConfig);
  }

  /**
   * Get alert rules
   */
  getAlertRules(): Observable<AlertRule[]> {
    return this.alertRules$.asObservable();
  }

  /**
   * Add alert rule
   */
  addAlertRule(rule: Omit<AlertRule, 'id'>): void {
    const newRule: AlertRule = {
      ...rule,
      id: `rule-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
    };

    const currentRules = this.alertRules$.value;
    this.alertRules$.next([...currentRules, newRule]);
  }

  /**
   * Update alert rule
   */
  updateAlertRule(ruleId: string, updates: Partial<AlertRule>): void {
    const currentRules = this.alertRules$.value;
    const updatedRules = currentRules.map(rule =>
      rule.id === ruleId ? { ...rule, ...updates } : rule
    );
    this.alertRules$.next(updatedRules);
  }

  /**
   * Delete alert rule
   */
  deleteAlertRule(ruleId: string): void {
    const currentRules = this.alertRules$.value;
    const updatedRules = currentRules.filter(rule => rule.id !== ruleId);
    this.alertRules$.next(updatedRules);
  }

  /**
   * Clear all alerts
   */
  clearAllAlerts(): void {
    this.alerts$.next([]);
  }

  // Default configurations

  private getDefaultRules(): AlertRule[] {
    return [
      {
        id: 'memory-high',
        name: 'High Memory Usage',
        category: 'memory',
        metric: 'memory-percentage',
        operator: 'gt',
        threshold: 80,
        level: 'warning',
        enabled: true,
        cooldownMs: 300000, // 5 minutes
        description: 'Memory usage exceeded 80%'
      },
      {
        id: 'memory-critical',
        name: 'Critical Memory Usage',
        category: 'memory',
        metric: 'memory-percentage',
        operator: 'gt',
        threshold: 90,
        level: 'critical',
        enabled: true,
        cooldownMs: 300000,
        description: 'Memory usage exceeded 90%'
      }
    ];
  }

  private getDefaultConfig(): AlertConfiguration {
    return {
      enableBrowserNotifications: true,
      enableToastNotifications: true,
      enableSoundAlerts: false,
      enableEmailAlerts: false,
      maxActiveAlerts: 100,
      autoResolveAfterMs: 3600000, // 1 hour
      categories: {
        performance: { enabled: true, minLevel: 'warning' },
        memory: { enabled: true, minLevel: 'warning' },
        network: { enabled: true, minLevel: 'warning' },
        ux: { enabled: true, minLevel: 'warning' },
        security: { enabled: true, minLevel: 'info' },
        system: { enabled: true, minLevel: 'warning' }
      }
    };
  }
}