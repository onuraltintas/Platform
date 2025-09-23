import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, timer, interval } from 'rxjs';
import { map, catchError, timeout } from 'rxjs/operators';

export interface NetworkConnection {
  downlink: number; // Mbps
  effectiveType: '2g' | '3g' | '4g' | 'slow-2g' | 'unknown';
  rtt: number; // milliseconds
  saveData: boolean;
  type: 'bluetooth' | 'cellular' | 'ethernet' | 'none' | 'wifi' | 'wimax' | 'other' | 'unknown';
}

export interface NetworkLatency {
  timestamp: number;
  rtt: number;
  jitter: number;
  packetLoss: number;
}

export interface NetworkThroughput {
  timestamp: number;
  downloadSpeed: number; // Mbps
  uploadSpeed: number; // Mbps
  testDuration: number; // ms
}

export interface NetworkMetrics {
  connection: NetworkConnection;
  latency: NetworkLatency[];
  throughput: NetworkThroughput[];
  requestMetrics: {
    totalRequests: number;
    failedRequests: number;
    avgResponseTime: number;
    timeouts: number;
    slowRequests: number; // > 2 seconds
  };
  bandwidthUsage: {
    totalBytes: number;
    downloadBytes: number;
    uploadBytes: number;
    efficiency: number; // successful bytes / total bytes
  };
  connectionHistory: Array<{
    timestamp: number;
    isOnline: boolean;
    changeReason: string;
  }>;
}

export interface NetworkAlert {
  id: string;
  level: 'info' | 'warning' | 'critical';
  type: 'offline' | 'slow' | 'timeout' | 'packet-loss' | 'bandwidth';
  message: string;
  value?: number;
  threshold?: number;
  timestamp: number;
}

/**
 * Network Monitor Service
 * Comprehensive network performance monitoring
 */
@Injectable({
  providedIn: 'root'
})
export class NetworkMonitorService {
  private http = inject(HttpClient);

  private networkMetrics$ = new BehaviorSubject<NetworkMetrics>(this.getInitialMetrics());
  private networkAlerts$ = new BehaviorSubject<NetworkAlert[]>([]);
  private isOnline$ = new BehaviorSubject<boolean>(navigator.onLine);

  private readonly THRESHOLDS = {
    SLOW_RESPONSE: 2000, // ms
    HIGH_RTT: 500, // ms
    LOW_BANDWIDTH: 1, // Mbps
    PACKET_LOSS: 0.05, // 5%
    TIMEOUT_THRESHOLD: 10000 // ms
  };

  private readonly HISTORY_SIZE = 100;
  private readonly TEST_ENDPOINTS = [
    '/api/health',
    '/api/ping'
  ];

  private latencyHistory: NetworkLatency[] = [];
  private throughputHistory: NetworkThroughput[] = [];
  private connectionHistory: Array<{ timestamp: number; isOnline: boolean; changeReason: string }> = [];
  private requestMetrics = { totalRequests: 0, failedRequests: 0, totalResponseTime: 0, timeouts: 0, slowRequests: 0 };
  private bandwidthUsage = { totalBytes: 0, downloadBytes: 0, uploadBytes: 0, successfulBytes: 0 };

  constructor() {
    this.initializeNetworkMonitoring();
  }

  /**
   * Initialize network monitoring
   */
  private initializeNetworkMonitoring(): void {
    this.setupConnectionListeners();
    this.startLatencyMonitoring();
    this.startThroughputMonitoring();
    this.startMetricsCollection();

    console.log('🌐 Network Monitor Service initialized');
  }

  /**
   * Setup connection event listeners
   */
  private setupConnectionListeners(): void {
    // Online/offline events
    window.addEventListener('online', () => {
      this.isOnline$.next(true);
      this.recordConnectionChange(true, 'online-event');
      this.createAlert('info', 'offline', 'Connection restored');
    });

    window.addEventListener('offline', () => {
      this.isOnline$.next(false);
      this.recordConnectionChange(false, 'offline-event');
      this.createAlert('critical', 'offline', 'Connection lost');
    });

    // Connection change events
    if ('connection' in navigator) {
      const connection = (navigator as any).connection;
      connection.addEventListener('change', () => {
        this.recordConnectionChange(navigator.onLine, 'connection-change');
      });
    }
  }

  /**
   * Start latency monitoring
   */
  private startLatencyMonitoring(): void {
    // Test latency every 30 seconds
    timer(5000, 30000).subscribe(() => {
      if (navigator.onLine) {
        this.measureLatency();
      }
    });
  }

  /**
   * Start throughput monitoring
   */
  private startThroughputMonitoring(): void {
    // Test throughput every 2 minutes
    timer(60000, 120000).subscribe(() => {
      if (navigator.onLine) {
        this.measureThroughput();
      }
    });
  }

  /**
   * Start metrics collection
   */
  private startMetricsCollection(): void {
    // Update metrics every 10 seconds
    timer(10000, 10000).subscribe(() => {
      this.updateNetworkMetrics();
    });

    // Cleanup old data every 5 minutes
    timer(300000, 300000).subscribe(() => {
      this.cleanupOldData();
    });
  }

  /**
   * Measure network latency
   */
  private async measureLatency(): Promise<void> {
    const measurements: number[] = [];
    const endpoint = this.TEST_ENDPOINTS[0];

    // Take 5 measurements
    for (let i = 0; i < 5; i++) {
      try {
        const startTime = performance.now();

        await this.http.get(endpoint, {
          headers: { 'Cache-Control': 'no-cache' },
          responseType: 'text'
        }).pipe(
          timeout(this.THRESHOLDS.TIMEOUT_THRESHOLD),
          catchError(() => {
            this.requestMetrics.timeouts++;
            throw new Error('Timeout');
          })
        ).toPromise();

        const responseTime = performance.now() - startTime;
        measurements.push(responseTime);

        this.requestMetrics.totalRequests++;
        this.requestMetrics.totalResponseTime += responseTime;

        if (responseTime > this.THRESHOLDS.SLOW_RESPONSE) {
          this.requestMetrics.slowRequests++;
        }

        // Small delay between measurements
        await new Promise(resolve => setTimeout(resolve, 200));

      } catch (error) {
        this.requestMetrics.failedRequests++;
        measurements.push(this.THRESHOLDS.TIMEOUT_THRESHOLD);
      }
    }

    if (measurements.length > 0) {
      const avgRtt = measurements.reduce((sum, m) => sum + m, 0) / measurements.length;
      const jitter = this.calculateJitter(measurements);
      const packetLoss = measurements.filter(m => m >= this.THRESHOLDS.TIMEOUT_THRESHOLD).length / measurements.length;

      const latency: NetworkLatency = {
        timestamp: Date.now(),
        rtt: avgRtt,
        jitter,
        packetLoss
      };

      this.latencyHistory.push(latency);
      if (this.latencyHistory.length > this.HISTORY_SIZE) {
        this.latencyHistory.shift();
      }

      // Check for alerts
      this.checkLatencyAlerts(latency);
    }
  }

  /**
   * Calculate jitter from measurements
   */
  private calculateJitter(measurements: number[]): number {
    if (measurements.length < 2) return 0;

    const avg = measurements.reduce((sum, m) => sum + m, 0) / measurements.length;
    const variance = measurements.reduce((sum, m) => sum + Math.pow(m - avg, 2), 0) / measurements.length;

    return Math.sqrt(variance);
  }

  /**
   * Measure network throughput
   */
  private async measureThroughput(): Promise<void> {
    try {
      const startTime = performance.now();

      // Download test - fetch a test file or endpoint
      const response = await this.http.get('/api/network-test', {
        responseType: 'blob',
        headers: { 'Cache-Control': 'no-cache' }
      }).pipe(
        timeout(30000),
        catchError(() => {
          throw new Error('Throughput test failed');
        })
      ).toPromise();

      const endTime = performance.now();
      const duration = endTime - startTime;
      const bytes = response?.size || 0;

      // Calculate download speed in Mbps
      const downloadSpeed = (bytes * 8) / ((duration / 1000) * 1024 * 1024);

      const throughput: NetworkThroughput = {
        timestamp: Date.now(),
        downloadSpeed,
        uploadSpeed: 0, // Upload test could be added here
        testDuration: duration
      };

      this.throughputHistory.push(throughput);
      if (this.throughputHistory.length > this.HISTORY_SIZE) {
        this.throughputHistory.shift();
      }

      // Update bandwidth usage
      this.bandwidthUsage.downloadBytes += bytes;
      this.bandwidthUsage.totalBytes += bytes;
      this.bandwidthUsage.successfulBytes += bytes;

      // Check for alerts
      this.checkThroughputAlerts(throughput);

    } catch (error) {
      console.warn('Throughput measurement failed:', error);
    }
  }

  /**
   * Get current network connection info
   */
  private getNetworkConnection(): NetworkConnection {
    const defaultConnection: NetworkConnection = {
      downlink: 0,
      effectiveType: 'unknown',
      rtt: 0,
      saveData: false,
      type: 'unknown'
    };

    if ('connection' in navigator) {
      const connection = (navigator as any).connection;
      return {
        downlink: connection.downlink || 0,
        effectiveType: connection.effectiveType || 'unknown',
        rtt: connection.rtt || 0,
        saveData: connection.saveData || false,
        type: connection.type || 'unknown'
      };
    }

    return defaultConnection;
  }

  /**
   * Record connection state change
   */
  private recordConnectionChange(isOnline: boolean, reason: string): void {
    this.connectionHistory.push({
      timestamp: Date.now(),
      isOnline,
      changeReason: reason
    });

    // Keep only last 50 connection changes
    if (this.connectionHistory.length > 50) {
      this.connectionHistory.shift();
    }
  }

  /**
   * Update network metrics
   */
  private updateNetworkMetrics(): void {
    const connection = this.getNetworkConnection();
    const avgResponseTime = this.requestMetrics.totalRequests > 0
      ? this.requestMetrics.totalResponseTime / this.requestMetrics.totalRequests
      : 0;

    const efficiency = this.bandwidthUsage.totalBytes > 0
      ? this.bandwidthUsage.successfulBytes / this.bandwidthUsage.totalBytes
      : 1;

    const metrics: NetworkMetrics = {
      connection,
      latency: [...this.latencyHistory],
      throughput: [...this.throughputHistory],
      requestMetrics: {
        totalRequests: this.requestMetrics.totalRequests,
        failedRequests: this.requestMetrics.failedRequests,
        avgResponseTime,
        timeouts: this.requestMetrics.timeouts,
        slowRequests: this.requestMetrics.slowRequests
      },
      bandwidthUsage: {
        totalBytes: this.bandwidthUsage.totalBytes,
        downloadBytes: this.bandwidthUsage.downloadBytes,
        uploadBytes: this.bandwidthUsage.uploadBytes,
        efficiency
      },
      connectionHistory: [...this.connectionHistory]
    };

    this.networkMetrics$.next(metrics);
  }

  /**
   * Check for latency alerts
   */
  private checkLatencyAlerts(latency: NetworkLatency): void {
    if (latency.rtt > this.THRESHOLDS.HIGH_RTT) {
      this.createAlert('warning', 'slow',
        `High latency detected: ${latency.rtt.toFixed(0)}ms`,
        latency.rtt, this.THRESHOLDS.HIGH_RTT);
    }

    if (latency.packetLoss > this.THRESHOLDS.PACKET_LOSS) {
      this.createAlert('warning', 'packet-loss',
        `High packet loss: ${(latency.packetLoss * 100).toFixed(1)}%`,
        latency.packetLoss, this.THRESHOLDS.PACKET_LOSS);
    }
  }

  /**
   * Check for throughput alerts
   */
  private checkThroughputAlerts(throughput: NetworkThroughput): void {
    if (throughput.downloadSpeed < this.THRESHOLDS.LOW_BANDWIDTH) {
      this.createAlert('warning', 'bandwidth',
        `Low bandwidth detected: ${throughput.downloadSpeed.toFixed(2)} Mbps`,
        throughput.downloadSpeed, this.THRESHOLDS.LOW_BANDWIDTH);
    }
  }

  /**
   * Create network alert
   */
  private createAlert(
    level: 'info' | 'warning' | 'critical',
    type: 'offline' | 'slow' | 'timeout' | 'packet-loss' | 'bandwidth',
    message: string,
    value?: number,
    threshold?: number
  ): void {
    const alert: NetworkAlert = {
      id: crypto.randomUUID(),
      level,
      type,
      message,
      value,
      threshold,
      timestamp: Date.now()
    };

    const currentAlerts = this.networkAlerts$.value;
    const newAlerts = [alert, ...currentAlerts].slice(0, 50);
    this.networkAlerts$.next(newAlerts);
  }

  /**
   * Cleanup old data
   */
  private cleanupOldData(): void {
    const oneHourAgo = Date.now() - (60 * 60 * 1000);

    // Clean alerts
    const currentAlerts = this.networkAlerts$.value;
    const recentAlerts = currentAlerts.filter(alert => alert.timestamp > oneHourAgo);

    if (recentAlerts.length !== currentAlerts.length) {
      this.networkAlerts$.next(recentAlerts);
    }

    // Clean connection history
    this.connectionHistory = this.connectionHistory.filter(
      entry => entry.timestamp > oneHourAgo
    );
  }

  /**
   * Track API request for metrics
   */
  trackApiRequest(url: string, responseTime: number, success: boolean, bytes: number = 0): void {
    this.requestMetrics.totalRequests++;
    this.requestMetrics.totalResponseTime += responseTime;

    if (!success) {
      this.requestMetrics.failedRequests++;
    } else {
      this.bandwidthUsage.successfulBytes += bytes;
    }

    if (responseTime > this.THRESHOLDS.SLOW_RESPONSE) {
      this.requestMetrics.slowRequests++;
    }

    this.bandwidthUsage.totalBytes += bytes;
    this.bandwidthUsage.downloadBytes += bytes;
  }

  /**
   * Test specific endpoint connectivity
   */
  async testEndpoint(url: string): Promise<{ success: boolean; responseTime: number; error?: string }> {
    try {
      const startTime = performance.now();

      await this.http.get(url, {
        headers: { 'Cache-Control': 'no-cache' }
      }).pipe(
        timeout(10000)
      ).toPromise();

      const responseTime = performance.now() - startTime;

      return { success: true, responseTime };

    } catch (error) {
      return {
        success: false,
        responseTime: 0,
        error: error instanceof Error ? error.message : 'Unknown error'
      };
    }
  }

  /**
   * Get network quality score (0-1)
   */
  getNetworkQualityScore(): number {
    const metrics = this.networkMetrics$.value;

    if (metrics.requestMetrics.totalRequests === 0) return 1;

    const successRate = 1 - (metrics.requestMetrics.failedRequests / metrics.requestMetrics.totalRequests);
    const speedScore = Math.min(1, metrics.connection.downlink / 10); // Normalize to 10 Mbps
    const latencyScore = metrics.latency.length > 0
      ? Math.max(0, 1 - (metrics.latency[metrics.latency.length - 1].rtt / 1000))
      : 1;

    return (successRate * 0.5) + (speedScore * 0.3) + (latencyScore * 0.2);
  }

  // Public API

  /**
   * Get network metrics
   */
  getNetworkMetrics(): Observable<NetworkMetrics> {
    return this.networkMetrics$.asObservable();
  }

  /**
   * Get network alerts
   */
  getNetworkAlerts(): Observable<NetworkAlert[]> {
    return this.networkAlerts$.asObservable();
  }

  /**
   * Get online status
   */
  getOnlineStatus(): Observable<boolean> {
    return this.isOnline$.asObservable();
  }

  /**
   * Force network test
   */
  async runNetworkTest(): Promise<void> {
    console.log('🧪 Running network diagnostics...');

    await Promise.all([
      this.measureLatency(),
      this.measureThroughput()
    ]);

    console.log('🧪 Network diagnostics completed');
  }

  /**
   * Clear network data
   */
  clearNetworkData(): void {
    this.latencyHistory = [];
    this.throughputHistory = [];
    this.connectionHistory = [];
    this.requestMetrics = { totalRequests: 0, failedRequests: 0, totalResponseTime: 0, timeouts: 0, slowRequests: 0 };
    this.bandwidthUsage = { totalBytes: 0, downloadBytes: 0, uploadBytes: 0, successfulBytes: 0 };
    this.networkAlerts$.next([]);
    this.networkMetrics$.next(this.getInitialMetrics());
  }

  /**
   * Export network data
   */
  exportNetworkData(): string {
    return JSON.stringify({
      metrics: this.networkMetrics$.value,
      alerts: this.networkAlerts$.value,
      exportTime: Date.now()
    }, null, 2);
  }

  // Private helpers

  private getInitialMetrics(): NetworkMetrics {
    return {
      connection: {
        downlink: 0,
        effectiveType: 'unknown',
        rtt: 0,
        saveData: false,
        type: 'unknown'
      },
      latency: [],
      throughput: [],
      requestMetrics: {
        totalRequests: 0,
        failedRequests: 0,
        avgResponseTime: 0,
        timeouts: 0,
        slowRequests: 0
      },
      bandwidthUsage: {
        totalBytes: 0,
        downloadBytes: 0,
        uploadBytes: 0,
        efficiency: 1
      },
      connectionHistory: []
    };
  }
}