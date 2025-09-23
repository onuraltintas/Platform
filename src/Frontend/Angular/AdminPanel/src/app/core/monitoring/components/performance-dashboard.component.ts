import { Component, OnInit, OnDestroy, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { interval, Subject, combineLatest } from 'rxjs';
import { takeUntil, startWith } from 'rxjs/operators';

import { OptimizedTokenService } from '../../cache/services/optimized-token.service';
import { SmartRetryService } from '../../api/services/smart-retry.service';
import { RequestDeduplicationService } from '../../api/services/request-deduplication.service';
import { CircuitBreakerService, CircuitState } from '../../api/services/circuit-breaker.service';
import { RequestQueueService } from '../../api/services/request-queue.service';
import { LazySecurityLoaderService } from '../../security/services/lazy-security-loader.service';
import { SecurityBudgetMonitorService } from '../../security/services/security-budget-monitor.service';
import { ProgressiveSecurityEnhancementService } from '../../security/services/progressive-security-enhancement.service';

interface PerformanceMetrics {
  cache: {
    hitRate: number;
    avgResponseTime: number;
    totalRequests: number;
    cacheSize: number;
  };
  retry: {
    totalAttempts: number;
    successRate: number;
    avgRetryDelay: number;
    tokenRefreshCount: number;
  };
  deduplication: {
    deduplicationRate: number;
    activePendingRequests: number;
    timeoutCount: number;
  };
  circuits: {
    services: Array<{
      name: string;
      state: CircuitState;
      uptime: number;
      successRate: number;
    }>;
    overallHealth: number;
  };
  queue: {
    pendingRequests: number;
    avgProcessingTime: number;
    queueHealth: number;
  };
  security: {
    budget: {
      totalBudget: number;
      usedBudget: number;
      utilizationRate: number;
      efficiency: number;
    };
    loader: {
      loadedModules: number;
      loadingErrors: number;
      averageLoadTime: number;
    };
    enhancement: {
      securityLevel: string;
      trustScore: number;
      riskScore: number;
      userActions: number;
    };
  };
  memory: {
    usedJSHeapSize: number;
    totalJSHeapSize: number;
    jsHeapSizeLimit: number;
    memoryUsage: number;
  };
  network: {
    downlink: number;
    effectiveType: string;
    rtt: number;
    saveData: boolean;
  };
  userExperience: {
    pageLoadTime: number;
    firstContentfulPaint: number;
    largestContentfulPaint: number;
    cumulativeLayoutShift: number;
    firstInputDelay: number;
  };
}

@Component({
  selector: 'app-performance-dashboard',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="performance-dashboard">
      <!-- Header -->
      <div class="dashboard-header">
        <h2>🚀 Performance Monitoring Dashboard</h2>
        <div class="refresh-controls">
          <button class="btn btn-sm btn-outline-primary" (click)="refreshMetrics()">
            🔄 Refresh
          </button>
          <span class="last-update">
            Last update: {{ lastUpdate() | date:'HH:mm:ss' }}
          </span>
        </div>
      </div>

      <!-- System Health Overview -->
      <div class="health-overview">
        <div class="health-card" [class.healthy]="systemHealth() > 0.8"
             [class.warning]="systemHealth() > 0.6 && systemHealth() <= 0.8"
             [class.critical]="systemHealth() <= 0.6">
          <h3>System Health</h3>
          <div class="health-score">{{ (systemHealth() * 100).toFixed(1) }}%</div>
          <div class="health-status">{{ getHealthStatus() }}</div>
        </div>

        <div class="metrics-grid">
          <!-- Token Cache Metrics -->
          <div class="metric-card">
            <h4>🎯 Token Cache</h4>
            <div class="metrics">
              <div class="metric">
                <label>Hit Rate:</label>
                <span class="value">{{ (metrics().cache.hitRate * 100).toFixed(1) }}%</span>
              </div>
              <div class="metric">
                <label>Avg Response:</label>
                <span class="value">{{ metrics().cache.avgResponseTime.toFixed(2) }}ms</span>
              </div>
              <div class="metric">
                <label>Total Requests:</label>
                <span class="value">{{ metrics().cache.totalRequests }}</span>
              </div>
              <div class="metric">
                <label>Cache Size:</label>
                <span class="value">{{ metrics().cache.cacheSize }}</span>
              </div>
            </div>
          </div>

          <!-- Retry Metrics -->
          <div class="metric-card">
            <h4>🔄 Smart Retry</h4>
            <div class="metrics">
              <div class="metric">
                <label>Total Attempts:</label>
                <span class="value">{{ metrics().retry.totalAttempts }}</span>
              </div>
              <div class="metric">
                <label>Success Rate:</label>
                <span class="value">{{ (metrics().retry.successRate * 100).toFixed(1) }}%</span>
              </div>
              <div class="metric">
                <label>Avg Retry Delay:</label>
                <span class="value">{{ metrics().retry.avgRetryDelay.toFixed(0) }}ms</span>
              </div>
              <div class="metric">
                <label>Token Refreshes:</label>
                <span class="value">{{ metrics().retry.tokenRefreshCount }}</span>
              </div>
            </div>
          </div>

          <!-- Deduplication Metrics -->
          <div class="metric-card">
            <h4>🔗 Deduplication</h4>
            <div class="metrics">
              <div class="metric">
                <label>Dedup Rate:</label>
                <span class="value">{{ (metrics().deduplication.deduplicationRate * 100).toFixed(1) }}%</span>
              </div>
              <div class="metric">
                <label>Pending Requests:</label>
                <span class="value">{{ metrics().deduplication.activePendingRequests }}</span>
              </div>
              <div class="metric">
                <label>Timeouts:</label>
                <span class="value">{{ metrics().deduplication.timeoutCount }}</span>
              </div>
            </div>
          </div>

          <!-- Queue Metrics -->
          <div class="metric-card">
            <h4>📋 Request Queue</h4>
            <div class="metrics">
              <div class="metric">
                <label>Pending:</label>
                <span class="value">{{ metrics().queue.pendingRequests }}</span>
              </div>
              <div class="metric">
                <label>Avg Processing:</label>
                <span class="value">{{ metrics().queue.avgProcessingTime.toFixed(2) }}ms</span>
              </div>
              <div class="metric">
                <label>Queue Health:</label>
                <span class="value">{{ (metrics().queue.queueHealth * 100).toFixed(1) }}%</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Circuit Breaker Status -->
      <div class="circuit-section">
        <h3>🔌 Circuit Breaker Status</h3>
        <div class="circuit-grid">
          @for (service of metrics().circuits.services; track service.name) {
            <div class="circuit-card" [class.closed]="service.state === 'CLOSED'"
                 [class.open]="service.state === 'OPEN'"
                 [class.half-open]="service.state === 'HALF_OPEN'">
              <h4>{{ service.name }}</h4>
              <div class="circuit-state">{{ service.state }}</div>
              <div class="circuit-metrics">
                <div>Uptime: {{ formatUptime(service.uptime) }}</div>
                <div>Success: {{ (service.successRate * 100).toFixed(1) }}%</div>
              </div>
            </div>
          }
        </div>
      </div>

      <!-- Performance Recommendations -->
      <div class="recommendations">
        <h3>💡 Performance Recommendations</h3>
        <div class="recommendation-list">
          @for (recommendation of getRecommendations(); track recommendation.id) {
            <div class="recommendation" [class]="recommendation.type">
              <div class="rec-icon">{{ recommendation.icon }}</div>
              <div class="rec-content">
                <h4>{{ recommendation.title }}</h4>
                <p>{{ recommendation.description }}</p>
              </div>
            </div>
          }
        </div>
      </div>

      <!-- Debug Information -->
      @if (showDebugInfo()) {
        <div class="debug-section">
          <h3>🔍 Debug Information</h3>
          <details>
            <summary>Raw Metrics Data</summary>
            <pre>{{ debugData() | json }}</pre>
          </details>
        </div>
      }
    </div>
  `,
  styles: [`
    .performance-dashboard {
      padding: 20px;
      max-width: 1400px;
      margin: 0 auto;
    }

    .dashboard-header {
      display: flex;
      justify-content: between;
      align-items: center;
      margin-bottom: 30px;
    }

    .refresh-controls {
      display: flex;
      align-items: center;
      gap: 15px;
    }

    .last-update {
      font-size: 0.9em;
      color: #6c757d;
    }

    .health-overview {
      display: grid;
      grid-template-columns: 300px 1fr;
      gap: 20px;
      margin-bottom: 30px;
    }

    .health-card {
      padding: 30px;
      border-radius: 10px;
      text-align: center;
      border: 2px solid #e9ecef;
    }

    .health-card.healthy { border-color: #28a745; background: #f8fff9; }
    .health-card.warning { border-color: #ffc107; background: #fffef8; }
    .health-card.critical { border-color: #dc3545; background: #fff8f8; }

    .health-score {
      font-size: 3em;
      font-weight: bold;
      margin: 10px 0;
    }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 20px;
    }

    .metric-card {
      background: white;
      border: 1px solid #e9ecef;
      border-radius: 8px;
      padding: 20px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .metric-card h4 {
      margin: 0 0 15px 0;
      color: #495057;
    }

    .metric {
      display: flex;
      justify-content: space-between;
      margin-bottom: 8px;
    }

    .metric label {
      font-weight: 500;
    }

    .metric .value {
      font-weight: bold;
      color: #007bff;
    }

    .circuit-section {
      margin-bottom: 30px;
    }

    .circuit-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 15px;
    }

    .circuit-card {
      padding: 15px;
      border-radius: 8px;
      text-align: center;
      border: 2px solid;
    }

    .circuit-card.closed { border-color: #28a745; background: #f8fff9; }
    .circuit-card.open { border-color: #dc3545; background: #fff8f8; }
    .circuit-card.half-open { border-color: #ffc107; background: #fffef8; }

    .circuit-state {
      font-weight: bold;
      font-size: 1.2em;
      margin: 10px 0;
    }

    .circuit-metrics {
      font-size: 0.9em;
      color: #6c757d;
    }

    .recommendations {
      margin-bottom: 30px;
    }

    .recommendation-list {
      display: flex;
      flex-direction: column;
      gap: 15px;
    }

    .recommendation {
      display: flex;
      align-items: start;
      padding: 15px;
      border-radius: 8px;
      border-left: 4px solid;
    }

    .recommendation.info {
      border-color: #007bff;
      background: #f8f9ff;
    }
    .recommendation.warning {
      border-color: #ffc107;
      background: #fffef8;
    }
    .recommendation.error {
      border-color: #dc3545;
      background: #fff8f8;
    }

    .rec-icon {
      font-size: 1.5em;
      margin-right: 15px;
    }

    .rec-content h4 {
      margin: 0 0 5px 0;
    }

    .rec-content p {
      margin: 0;
      color: #6c757d;
    }

    .debug-section {
      margin-top: 30px;
      padding: 20px;
      background: #f8f9fa;
      border-radius: 8px;
    }

    .debug-section pre {
      background: white;
      padding: 15px;
      border-radius: 4px;
      overflow-x: auto;
      font-size: 0.8em;
    }

    @media (max-width: 768px) {
      .health-overview {
        grid-template-columns: 1fr;
      }

      .metrics-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class PerformanceDashboardComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();

  // Injected services
  private readonly tokenService = inject(OptimizedTokenService);
  private readonly retryService = inject(SmartRetryService);
  private readonly deduplicationService = inject(RequestDeduplicationService);
  private readonly circuitBreakerService = inject(CircuitBreakerService);
  private readonly queueService = inject(RequestQueueService);

  // Signals
  public readonly metrics = signal<PerformanceMetrics>(this.getInitialMetrics());
  public readonly lastUpdate = signal<Date>(new Date());
  public readonly showDebugInfo = signal<boolean>(false);
  public readonly systemHealth = computed(() => this.calculateSystemHealth());
  public readonly debugData = computed(() => this.getDebugData());

  ngOnInit(): void {
    this.startMetricsCollection();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Start collecting metrics at regular intervals
   */
  private startMetricsCollection(): void {
    // Collect metrics every 5 seconds
    interval(5000).pipe(
      startWith(0),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.collectMetrics();
    });
  }

  /**
   * Collect all performance metrics
   */
  private async collectMetrics(): Promise<void> {
    try {
      const [
        cacheStats,
        retryStats,
        deduplicationStats,
        circuitStats,
        queueStats
      ] = await Promise.all([
        this.tokenService.getCacheStats(),
        this.retryService.getRetryStats(),
        this.deduplicationService.getStats(),
        this.circuitBreakerService.getAllCircuitStats().pipe(takeUntil(this.destroy$)).toPromise(),
        this.queueService.getQueueStats().pipe(takeUntil(this.destroy$)).toPromise()
      ]);

      const newMetrics: PerformanceMetrics = {
        cache: {
          hitRate: cacheStats.hitRate,
          avgResponseTime: cacheStats.averageResponseTime,
          totalRequests: cacheStats.totalRequests,
          cacheSize: cacheStats.cacheSize
        },
        retry: {
          totalAttempts: retryStats.totalAttempts,
          successRate: retryStats.totalAttempts > 0 ? retryStats.successfulRetries / retryStats.totalAttempts : 1,
          avgRetryDelay: retryStats.averageRetryDelay,
          tokenRefreshCount: retryStats.tokenRefreshCount
        },
        deduplication: {
          deduplicationRate: deduplicationStats.deduplicationRate,
          activePendingRequests: deduplicationStats.activePendingRequests,
          timeoutCount: deduplicationStats.timeoutCount
        },
        circuits: {
          services: Object.entries(circuitStats || {}).map(([name, stats]) => ({
            name,
            state: stats.state,
            uptime: stats.uptime,
            successRate: stats.totalRequests > 0 ? stats.successCount / stats.totalRequests : 1
          })),
          overallHealth: this.circuitBreakerService.getResilienceScore()
        },
        queue: {
          pendingRequests: queueStats?.pendingRequests || 0,
          avgProcessingTime: queueStats?.averageProcessingTime || 0,
          queueHealth: queueStats?.queueHealthScore || 1
        }
      };

      this.metrics.set(newMetrics);
      this.lastUpdate.set(new Date());

    } catch (error) {
      console.error('Failed to collect performance metrics:', error);
    }
  }

  /**
   * Calculate overall system health score
   */
  private calculateSystemHealth(): number {
    const m = this.metrics();

    const cacheHealth = m.cache.hitRate * 0.3;
    const retryHealth = m.retry.successRate * 0.2;
    const deduplicationHealth = (1 - Math.min(m.deduplication.timeoutCount / 100, 1)) * 0.2;
    const circuitHealth = m.circuits.overallHealth * 0.2;
    const queueHealth = m.queue.queueHealth * 0.1;

    return Math.max(0, Math.min(1, cacheHealth + retryHealth + deduplicationHealth + circuitHealth + queueHealth));
  }

  /**
   * Get health status description
   */
  getHealthStatus(): string {
    const health = this.systemHealth();

    if (health > 0.8) return 'Excellent';
    if (health > 0.6) return 'Good';
    if (health > 0.4) return 'Fair';
    if (health > 0.2) return 'Poor';
    return 'Critical';
  }

  /**
   * Get performance recommendations
   */
  getRecommendations(): Array<{
    id: string;
    type: 'info' | 'warning' | 'error';
    icon: string;
    title: string;
    description: string;
  }> {
    const recommendations = [];
    const m = this.metrics();

    // Cache recommendations
    if (m.cache.hitRate < 0.8) {
      recommendations.push({
        id: 'cache-hit-rate',
        type: 'warning' as const,
        icon: '🎯',
        title: 'Low Cache Hit Rate',
        description: `Cache hit rate is ${(m.cache.hitRate * 100).toFixed(1)}%. Consider warming up cache or adjusting TTL settings.`
      });
    }

    // Response time recommendations
    if (m.cache.avgResponseTime > 5) {
      recommendations.push({
        id: 'response-time',
        type: 'warning' as const,
        icon: '⚡',
        title: 'Slow Response Times',
        description: `Average response time is ${m.cache.avgResponseTime.toFixed(2)}ms. Check for performance bottlenecks.`
      });
    }

    // Circuit breaker recommendations
    const openCircuits = m.circuits.services.filter(s => s.state === 'OPEN').length;
    if (openCircuits > 0) {
      recommendations.push({
        id: 'open-circuits',
        type: 'error' as const,
        icon: '🔌',
        title: 'Open Circuit Breakers',
        description: `${openCircuits} service(s) have open circuit breakers. Check service health.`
      });
    }

    // Queue recommendations
    if (m.queue.pendingRequests > 50) {
      recommendations.push({
        id: 'high-queue',
        type: 'warning' as const,
        icon: '📋',
        title: 'High Queue Load',
        description: `${m.queue.pendingRequests} requests pending. Consider scaling or load balancing.`
      });
    }

    // Default recommendation if everything is good
    if (recommendations.length === 0) {
      recommendations.push({
        id: 'all-good',
        type: 'info' as const,
        icon: '✅',
        title: 'System Running Optimally',
        description: 'All performance metrics are within acceptable ranges.'
      });
    }

    return recommendations;
  }

  /**
   * Format uptime duration
   */
  formatUptime(uptimeMs: number): string {
    const seconds = Math.floor(uptimeMs / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);

    if (hours > 0) return `${hours}h ${minutes % 60}m`;
    if (minutes > 0) return `${minutes}m ${seconds % 60}s`;
    return `${seconds}s`;
  }

  /**
   * Manually refresh metrics
   */
  refreshMetrics(): void {
    this.collectMetrics();
  }

  /**
   * Toggle debug information
   */
  toggleDebugInfo(): void {
    this.showDebugInfo.update(show => !show);
  }

  /**
   * Get debug data
   */
  private getDebugData(): any {
    return {
      metrics: this.metrics(),
      systemHealth: this.systemHealth(),
      lastUpdate: this.lastUpdate(),
      recommendations: this.getRecommendations()
    };
  }

  /**
   * Get initial metrics structure
   */
  private getInitialMetrics(): PerformanceMetrics {
    return {
      cache: {
        hitRate: 0,
        avgResponseTime: 0,
        totalRequests: 0,
        cacheSize: 0
      },
      retry: {
        totalAttempts: 0,
        successRate: 1,
        avgRetryDelay: 0,
        tokenRefreshCount: 0
      },
      deduplication: {
        deduplicationRate: 0,
        activePendingRequests: 0,
        timeoutCount: 0
      },
      circuits: {
        services: [],
        overallHealth: 1
      },
      queue: {
        pendingRequests: 0,
        avgProcessingTime: 0,
        queueHealth: 1
      }
    };
  }
}