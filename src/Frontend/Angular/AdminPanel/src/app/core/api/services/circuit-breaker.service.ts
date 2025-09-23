import { Injectable } from '@angular/core';
import { HttpRequest, HttpEvent } from '@angular/common/http';
import { Observable, throwError, timer, BehaviorSubject } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

export interface CircuitBreakerConfig {
  failureThreshold: number;
  timeoutMs: number;
  resetTimeoutMs: number;
  monitoringPeriodMs: number;
  healthCheckUrl?: string;
  healthCheckInterval?: number;
}

export enum CircuitState {
  CLOSED = 'CLOSED',     // Normal operation
  OPEN = 'OPEN',         // Failing, blocking requests
  HALF_OPEN = 'HALF_OPEN' // Testing if service recovered
}

export interface CircuitStats {
  state: CircuitState;
  failureCount: number;
  successCount: number;
  timeouts: number;
  lastFailureTime: number;
  lastSuccessTime: number;
  totalRequests: number;
  blockedRequests: number;
  uptime: number;
}

export interface ServiceHealth {
  serviceName: string;
  isHealthy: boolean;
  responseTime: number;
  lastCheck: number;
  consecutiveFailures: number;
}

/**
 * Circuit Breaker Service for API Resilience
 * Implements circuit breaker pattern to handle service failures gracefully
 */
@Injectable({
  providedIn: 'root'
})
export class CircuitBreakerService {
  private circuits = new Map<string, CircuitBreakerInstance>();
  private serviceHealth = new Map<string, ServiceHealth>();
  private globalStats$ = new BehaviorSubject<{ [serviceName: string]: CircuitStats }>({});

  private readonly DEFAULT_CONFIG: CircuitBreakerConfig = {
    failureThreshold: 5,
    timeoutMs: 10000,
    resetTimeoutMs: 60000,
    monitoringPeriodMs: 30000,
    healthCheckInterval: 15000
  };

  constructor() {
    this.setupGlobalMonitoring();
  }

  /**
   * Execute request through circuit breaker
   */
  execute<T>(
    serviceName: string,
    _request: HttpRequest<any>,
    executeRequest: () => Observable<HttpEvent<T>>,
    config: Partial<CircuitBreakerConfig> = {}
  ): Observable<HttpEvent<T>> {

    const circuit = this.getOrCreateCircuit(serviceName, config);

    return circuit.execute(executeRequest);
  }

  /**
   * Get or create circuit breaker for service
   */
  private getOrCreateCircuit(
    serviceName: string,
    config: Partial<CircuitBreakerConfig>
  ): CircuitBreakerInstance {

    if (!this.circuits.has(serviceName)) {
      const finalConfig = { ...this.DEFAULT_CONFIG, ...config };
      const circuit = new CircuitBreakerInstance(serviceName, finalConfig);

      // Subscribe to circuit state changes
      circuit.getStats().subscribe(stats => {
        this.updateGlobalStats(serviceName, stats);
      });

      this.circuits.set(serviceName, circuit);
    }

    return this.circuits.get(serviceName)!;
  }

  /**
   * Update global statistics
   */
  private updateGlobalStats(serviceName: string, stats: CircuitStats): void {
    const currentStats = this.globalStats$.value;
    this.globalStats$.next({
      ...currentStats,
      [serviceName]: stats
    });
  }

  /**
   * Setup global monitoring for all circuits
   */
  private setupGlobalMonitoring(): void {
    timer(0, 30000).subscribe(() => { // Every 30 seconds
      this.performHealthChecks();
      this.cleanupInactiveCircuits();
      this.logSystemHealth();
    });
  }

  /**
   * Perform health checks on all services
   */
  private performHealthChecks(): void {
    this.circuits.forEach((circuit, serviceName) => {
      const health = circuit.getHealthStatus();
      this.serviceHealth.set(serviceName, health);
    });
  }

  /**
   * Clean up inactive circuits
   */
  private cleanupInactiveCircuits(): void {
    const now = Date.now();
    const inactiveThreshold = 5 * 60 * 1000; // 5 minutes

    this.circuits.forEach((circuit, serviceName) => {
      const stats = circuit.getCurrentStats();
      if (now - Math.max(stats.lastSuccessTime, stats.lastFailureTime) > inactiveThreshold) {
        console.log(`Cleaning up inactive circuit: ${serviceName}`);
        this.circuits.delete(serviceName);
        this.serviceHealth.delete(serviceName);
      }
    });
  }

  /**
   * Log system health summary
   */
  private logSystemHealth(): void {
    const healthSummary = Array.from(this.serviceHealth.entries()).map(([service, health]) => ({
      service,
      healthy: health.isHealthy,
      responseTime: health.responseTime,
      failures: health.consecutiveFailures
    }));

    if (healthSummary.length > 0) {
      console.log('🏥 Service Health Summary:', healthSummary);
    }
  }

  // Public API

  /**
   * Get circuit stats for specific service
   */
  getCircuitStats(serviceName: string): CircuitStats | null {
    const circuit = this.circuits.get(serviceName);
    return circuit ? circuit.getCurrentStats() : null;
  }

  /**
   * Get all circuit statistics
   */
  getAllCircuitStats(): Observable<{ [serviceName: string]: CircuitStats }> {
    return this.globalStats$.asObservable();
  }

  /**
   * Get service health information
   */
  getServiceHealth(serviceName: string): ServiceHealth | null {
    return this.serviceHealth.get(serviceName) || null;
  }

  /**
   * Get all service health information
   */
  getAllServiceHealth(): ServiceHealth[] {
    return Array.from(this.serviceHealth.values());
  }

  /**
   * Force circuit state change (for testing/debugging)
   */
  forceCircuitState(serviceName: string, state: CircuitState): boolean {
    const circuit = this.circuits.get(serviceName);
    if (circuit) {
      circuit.forceState(state);
      return true;
    }
    return false;
  }

  /**
   * Reset circuit statistics
   */
  resetCircuit(serviceName: string): boolean {
    const circuit = this.circuits.get(serviceName);
    if (circuit) {
      circuit.reset();
      return true;
    }
    return false;
  }

  /**
   * Get system resilience score
   */
  getResilienceScore(): number {
    const services = Array.from(this.serviceHealth.values());
    if (services.length === 0) return 1.0;

    const healthyServices = services.filter(s => s.isHealthy).length;
    const baseScore = healthyServices / services.length;

    // Adjust score based on response times
    const avgResponseTime = services.reduce((sum, s) => sum + s.responseTime, 0) / services.length;
    const responseTimeScore = Math.max(0, 1 - (avgResponseTime / 5000)); // 5s baseline

    return (baseScore + responseTimeScore) / 2;
  }
}

/**
 * Individual Circuit Breaker Instance
 */
class CircuitBreakerInstance {
  private state = CircuitState.CLOSED;
  private failureCount = 0;
  private successCount = 0;
  private timeouts = 0;
  private lastFailureTime = 0;
  private lastSuccessTime = 0;
  private totalRequests = 0;
  private blockedRequests = 0;
  private createdAt = Date.now();

  private stats$ = new BehaviorSubject<CircuitStats>(this.getCurrentStats());

  constructor(
    private serviceName: string,
    private config: CircuitBreakerConfig
  ) {}

  /**
   * Execute request through this circuit
   */
  execute<T>(executeRequest: () => Observable<HttpEvent<T>>): Observable<HttpEvent<T>> {
    this.totalRequests++;

    // Check circuit state
    switch (this.state) {
      case CircuitState.OPEN:
        return this.handleOpenState();

      case CircuitState.HALF_OPEN:
        return this.handleHalfOpenState(executeRequest);

      case CircuitState.CLOSED:
      default:
        return this.handleClosedState(executeRequest);
    }
  }

  /**
   * Handle requests when circuit is OPEN (blocking)
   */
  private handleOpenState<T>(): Observable<HttpEvent<T>> {
    this.blockedRequests++;

    // Check if enough time has passed to try again
    if (Date.now() - this.lastFailureTime > this.config.resetTimeoutMs) {
      this.transitionToHalfOpen();
      return throwError(() => new Error(`Circuit breaker OPEN -> HALF_OPEN transition for ${this.serviceName}`));
    }

    return throwError(() => new Error(`Circuit breaker OPEN for ${this.serviceName} - request blocked`));
  }

  /**
   * Handle requests when circuit is HALF_OPEN (testing)
   */
  private handleHalfOpenState<T>(executeRequest: () => Observable<HttpEvent<T>>): Observable<HttpEvent<T>> {
    const startTime = Date.now();

    return executeRequest().pipe(
      tap(() => {
        // Success - transition to CLOSED
        this.onSuccess(Date.now() - startTime);
        this.transitionToClosed();
      }),
      catchError(error => {
        // Failure - transition back to OPEN
        this.onFailure();
        this.transitionToOpen();
        return throwError(() => error);
      })
    );
  }

  /**
   * Handle requests when circuit is CLOSED (normal operation)
   */
  private handleClosedState<T>(executeRequest: () => Observable<HttpEvent<T>>): Observable<HttpEvent<T>> {
    const startTime = Date.now();

    return executeRequest().pipe(
      tap(() => {
        this.onSuccess(Date.now() - startTime);
      }),
      catchError(error => {
        this.onFailure();

        // Check if we should open the circuit
        if (this.failureCount >= this.config.failureThreshold) {
          this.transitionToOpen();
        }

        return throwError(() => error);
      })
    );
  }

  /**
   * Handle successful request
   */
  private onSuccess(_responseTime: number): void {
    this.successCount++;
    this.lastSuccessTime = Date.now();
    this.failureCount = 0; // Reset failure count on success
    this.updateStats();
  }

  /**
   * Handle failed request
   */
  private onFailure(): void {
    this.failureCount++;
    this.lastFailureTime = Date.now();
    this.updateStats();
  }

  /**
   * Transition to OPEN state
   */
  private transitionToOpen(): void {
    console.warn(`🔴 Circuit breaker OPEN for ${this.serviceName} (failures: ${this.failureCount})`);
    this.state = CircuitState.OPEN;
    this.updateStats();
  }

  /**
   * Transition to HALF_OPEN state
   */
  private transitionToHalfOpen(): void {
    console.log(`🟡 Circuit breaker HALF_OPEN for ${this.serviceName} - testing service`);
    this.state = CircuitState.HALF_OPEN;
    this.updateStats();
  }

  /**
   * Transition to CLOSED state
   */
  private transitionToClosed(): void {
    console.log(`🟢 Circuit breaker CLOSED for ${this.serviceName} - service recovered`);
    this.state = CircuitState.CLOSED;
    this.failureCount = 0;
    this.updateStats();
  }

  /**
   * Force circuit to specific state
   */
  forceState(state: CircuitState): void {
    console.log(`Forcing circuit ${this.serviceName} to state: ${state}`);
    this.state = state;
    this.updateStats();
  }

  /**
   * Reset all statistics
   */
  reset(): void {
    this.state = CircuitState.CLOSED;
    this.failureCount = 0;
    this.successCount = 0;
    this.timeouts = 0;
    this.totalRequests = 0;
    this.blockedRequests = 0;
    this.updateStats();
  }

  /**
   * Get current statistics
   */
  getCurrentStats(): CircuitStats {
    return {
      state: this.state,
      failureCount: this.failureCount,
      successCount: this.successCount,
      timeouts: this.timeouts,
      lastFailureTime: this.lastFailureTime,
      lastSuccessTime: this.lastSuccessTime,
      totalRequests: this.totalRequests,
      blockedRequests: this.blockedRequests,
      uptime: Date.now() - this.createdAt
    };
  }

  /**
   * Get statistics observable
   */
  getStats(): Observable<CircuitStats> {
    return this.stats$.asObservable();
  }

  /**
   * Get health status
   */
  getHealthStatus(): ServiceHealth {
    const now = Date.now();
    const isHealthy = this.state === CircuitState.CLOSED && this.failureCount < this.config.failureThreshold;

    // Calculate average response time from recent successes
    const responseTime = this.lastSuccessTime > 0 ? 100 : 5000; // Placeholder logic

    return {
      serviceName: this.serviceName,
      isHealthy,
      responseTime,
      lastCheck: now,
      consecutiveFailures: this.failureCount
    };
  }

  /**
   * Update statistics subject
   */
  private updateStats(): void {
    this.stats$.next(this.getCurrentStats());
  }
}