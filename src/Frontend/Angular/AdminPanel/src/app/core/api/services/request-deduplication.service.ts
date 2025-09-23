import { Injectable } from '@angular/core';
import { HttpRequest, HttpEvent } from '@angular/common/http';
import { Observable, Subject, share, timer } from 'rxjs';
import { finalize, tap } from 'rxjs/operators';

export interface PendingRequest {
  id: string;
  request: HttpRequest<any>;
  observable: Observable<HttpEvent<any>>;
  subscribers: Subject<HttpEvent<any>>[];
  startTime: number;
  timeout: number;
}

export interface DeduplicationConfig {
  enabled: boolean;
  keyGenerator?: (request: HttpRequest<any>) => string;
  timeoutMs: number;
  maxPendingRequests: number;
  excludePatterns: RegExp[];
  includeMethods: string[];
}

export interface DeduplicationStats {
  totalRequests: number;
  deduplicatedRequests: number;
  activePendingRequests: number;
  deduplicationRate: number;
  averageResponseTime: number;
  timeoutCount: number;
}

/**
 * Request Deduplication Service
 * Prevents duplicate HTTP requests and shares responses
 */
@Injectable({
  providedIn: 'root'
})
export class RequestDeduplicationService {
  private pendingRequests = new Map<string, PendingRequest>();
  private stats: DeduplicationStats = {
    totalRequests: 0,
    deduplicatedRequests: 0,
    activePendingRequests: 0,
    deduplicationRate: 0,
    averageResponseTime: 0,
    timeoutCount: 0
  };

  private responseTimes: number[] = [];
  private requestCounter = 0;

  private readonly defaultConfig: DeduplicationConfig = {
    enabled: true,
    keyGenerator: (req) => this.generateDefaultKey(req),
    timeoutMs: 30000, // 30 seconds
    maxPendingRequests: 100,
    excludePatterns: [
      /\/auth\/refresh$/,
      /\/upload$/,
      /\/download$/
    ],
    includeMethods: ['GET', 'POST', 'PUT', 'PATCH']
  };

  constructor() {
    this.setupCleanupTimer();
  }

  /**
   * Deduplicate HTTP request
   * Returns shared observable for identical requests
   */
  deduplicate<T>(
    request: HttpRequest<any>,
    executeRequest: () => Observable<HttpEvent<T>>,
    config: Partial<DeduplicationConfig> = {}
  ): Observable<HttpEvent<T>> {
    const finalConfig = { ...this.defaultConfig, ...config };

    this.stats.totalRequests++;

    // Check if deduplication should be applied
    if (!this.shouldDeduplicate(request, finalConfig)) {
      return executeRequest();
    }

    // Generate unique key for this request
    const requestKey = finalConfig.keyGenerator!(request);

    // Check if identical request is already pending
    const existingRequest = this.pendingRequests.get(requestKey);
    if (existingRequest) {
      return this.attachToExistingRequest<T>(existingRequest);
    }

    // Create new pending request
    return this.createNewPendingRequest<T>(requestKey, request, executeRequest, finalConfig);
  }

  /**
   * Check if request should be deduplicated
   */
  private shouldDeduplicate(request: HttpRequest<any>, config: DeduplicationConfig): boolean {
    if (!config.enabled) {
      return false;
    }

    // Check method inclusion
    if (!config.includeMethods.includes(request.method)) {
      return false;
    }

    // Check exclusion patterns
    if (config.excludePatterns.some(pattern => pattern.test(request.url))) {
      return false;
    }

    // Check pending requests limit
    if (this.pendingRequests.size >= config.maxPendingRequests) {
      console.warn('Request deduplication limit reached, allowing duplicate request');
      return false;
    }

    // Skip if request contains file uploads
    if (this.hasFileUpload(request)) {
      return false;
    }

    return true;
  }

  /**
   * Attach to existing pending request
   */
  private attachToExistingRequest<T>(existingRequest: PendingRequest): Observable<HttpEvent<T>> {
    this.stats.deduplicatedRequests++;
    this.updateDeduplicationRate();

    const subject = new Subject<HttpEvent<T>>();
    existingRequest.subscribers.push(subject as Subject<HttpEvent<any>>);

    // Set up timeout for this subscriber
    const timeout = timer(existingRequest.timeout).subscribe(() => {
      const index = existingRequest.subscribers.indexOf(subject as Subject<HttpEvent<any>>);
      if (index !== -1) {
        existingRequest.subscribers.splice(index, 1);
        subject.error(new Error('Request timeout while waiting for shared response'));
        this.stats.timeoutCount++;
      }
    });

    return subject.asObservable().pipe(
      finalize(() => timeout.unsubscribe())
    );
  }

  /**
   * Create new pending request
   */
  private createNewPendingRequest<T>(
    requestKey: string,
    request: HttpRequest<any>,
    executeRequest: () => Observable<HttpEvent<T>>,
    config: DeduplicationConfig
  ): Observable<HttpEvent<T>> {
    const startTime = Date.now();
    const requestId = this.generateRequestId();

    // Create shared observable
    const sharedObservable = executeRequest().pipe(
      tap((event) => {
        // Broadcast to all subscribers
        const pendingRequest = this.pendingRequests.get(requestKey);
        if (pendingRequest) {
          pendingRequest.subscribers.forEach(subject => {
            if (!subject.closed) {
              subject.next(event);
            }
          });
        }
      }),
      finalize(() => {
        // Clean up when request completes
        const pendingRequest = this.pendingRequests.get(requestKey);
        if (pendingRequest) {
          // Complete all subscribers
          pendingRequest.subscribers.forEach(subject => {
            if (!subject.closed) {
              subject.complete();
            }
          });

          // Record response time
          const responseTime = Date.now() - startTime;
          this.recordResponseTime(responseTime);

          // Remove from pending requests
          this.pendingRequests.delete(requestKey);
          this.updateActivePendingCount();
        }
      }),
      share() // Share the observable among multiple subscribers
    );

    // Create pending request entry
    const pendingRequest: PendingRequest = {
      id: requestId,
      request,
      observable: sharedObservable,
      subscribers: [],
      startTime,
      timeout: config.timeoutMs
    };

    this.pendingRequests.set(requestKey, pendingRequest);
    this.updateActivePendingCount();

    // Set up global timeout for the request
    const globalTimeout = timer(config.timeoutMs).subscribe(() => {
      const pending = this.pendingRequests.get(requestKey);
      if (pending) {
        pending.subscribers.forEach(subject => {
          if (!subject.closed) {
            subject.error(new Error('Request timeout'));
          }
        });
        this.pendingRequests.delete(requestKey);
        this.stats.timeoutCount++;
        this.updateActivePendingCount();
      }
    });

    // Return the shared observable
    return sharedObservable.pipe(
      finalize(() => globalTimeout.unsubscribe())
    ) as Observable<HttpEvent<T>>;
  }

  /**
   * Generate unique key for request deduplication
   */
  private generateDefaultKey(request: HttpRequest<any>): string {
    const components = [
      request.method,
      request.url,
      this.serializeParams(request.params),
      this.serializeHeaders(request.headers),
      this.serializeBody(request.body)
    ];

    // Create hash of the components
    return this.hashString(components.join('|'));
  }

  /**
   * Serialize URL parameters for key generation
   */
  private serializeParams(params: any): string {
    if (!params) return '';

    const keys = params.keys ? params.keys().sort() : [];
    return keys.map((key: string) => `${key}=${params.get(key)}`).join('&');
  }

  /**
   * Serialize relevant headers for key generation
   */
  private serializeHeaders(headers: any): string {
    if (!headers) return '';

    const relevantHeaders = ['content-type', 'accept', 'authorization'];
    const headerPairs: string[] = [];

    relevantHeaders.forEach(header => {
      const value = headers.get ? headers.get(header) : headers[header];
      if (value) {
        // Exclude dynamic parts like request IDs from authorization header
        if (header === 'authorization') {
          headerPairs.push(`${header}=Bearer:token`);
        } else {
          headerPairs.push(`${header}=${value}`);
        }
      }
    });

    return headerPairs.sort().join('&');
  }

  /**
   * Serialize request body for key generation
   */
  private serializeBody(body: any): string {
    if (!body) return '';

    if (typeof body === 'string') {
      return body;
    }

    if (body instanceof FormData) {
      // Don't deduplicate FormData requests
      return `formdata_${Date.now()}`;
    }

    try {
      return JSON.stringify(body);
    } catch {
      return 'unserializable_body';
    }
  }

  /**
   * Check if request contains file uploads
   */
  private hasFileUpload(request: HttpRequest<any>): boolean {
    if (request.body instanceof FormData) {
      return true;
    }

    const contentType = request.headers.get('content-type');
    return contentType?.includes('multipart/form-data') || false;
  }

  /**
   * Generate simple hash for string
   */
  private hashString(str: string): string {
    let hash = 0;
    if (str.length === 0) return hash.toString();

    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }

    return Math.abs(hash).toString(36);
  }

  /**
   * Generate unique request ID
   */
  private generateRequestId(): string {
    return `req_${Date.now()}_${++this.requestCounter}`;
  }

  /**
   * Record response time for statistics
   */
  private recordResponseTime(time: number): void {
    this.responseTimes.push(time);
    if (this.responseTimes.length > 100) {
      this.responseTimes.shift();
    }
    this.updateAverageResponseTime();
  }

  /**
   * Update deduplication rate
   */
  private updateDeduplicationRate(): void {
    if (this.stats.totalRequests > 0) {
      this.stats.deduplicationRate = this.stats.deduplicatedRequests / this.stats.totalRequests;
    }
  }

  /**
   * Update active pending requests count
   */
  private updateActivePendingCount(): void {
    this.stats.activePendingRequests = this.pendingRequests.size;
  }

  /**
   * Update average response time
   */
  private updateAverageResponseTime(): void {
    if (this.responseTimes.length > 0) {
      const sum = this.responseTimes.reduce((a, b) => a + b, 0);
      this.stats.averageResponseTime = sum / this.responseTimes.length;
    }
  }

  /**
   * Setup cleanup timer for stale requests
   */
  private setupCleanupTimer(): void {
    timer(0, 60000).subscribe(() => { // Every minute
      this.cleanupStaleRequests();
    });
  }

  /**
   * Clean up stale pending requests
   */
  private cleanupStaleRequests(): void {
    const now = Date.now();
    const staleThreshold = 60000; // 1 minute

    const staleKeys: string[] = [];

    this.pendingRequests.forEach((pending, key) => {
      if (now - pending.startTime > staleThreshold) {
        // Complete any remaining subscribers with error
        pending.subscribers.forEach(subject => {
          if (!subject.closed) {
            subject.error(new Error('Request cleanup - stale request removed'));
          }
        });
        staleKeys.push(key);
      }
    });

    staleKeys.forEach(key => this.pendingRequests.delete(key));

    if (staleKeys.length > 0) {
      console.log(`Cleaned up ${staleKeys.length} stale pending requests`);
      this.updateActivePendingCount();
    }
  }

  // Public API

  /**
   * Get current deduplication statistics
   */
  getStats(): DeduplicationStats {
    return { ...this.stats };
  }

  /**
   * Get list of currently pending request keys
   */
  getPendingRequestKeys(): string[] {
    return Array.from(this.pendingRequests.keys());
  }

  /**
   * Get detailed information about pending requests
   */
  getPendingRequestsInfo(): Array<{
    key: string;
    method: string;
    url: string;
    subscriberCount: number;
    ageMs: number;
  }> {
    const now = Date.now();
    return Array.from(this.pendingRequests.entries()).map(([key, pending]) => ({
      key,
      method: pending.request.method,
      url: pending.request.url,
      subscriberCount: pending.subscribers.length,
      ageMs: now - pending.startTime
    }));
  }

  /**
   * Clear all pending requests (emergency cleanup)
   */
  clearAllPendingRequests(): void {
    this.pendingRequests.forEach(pending => {
      pending.subscribers.forEach(subject => {
        if (!subject.closed) {
          subject.error(new Error('Service reset - all pending requests cleared'));
        }
      });
    });

    this.pendingRequests.clear();
    this.updateActivePendingCount();
    console.log('All pending requests cleared');
  }

  /**
   * Reset statistics
   */
  resetStats(): void {
    this.stats = {
      totalRequests: 0,
      deduplicatedRequests: 0,
      activePendingRequests: this.pendingRequests.size,
      deduplicationRate: 0,
      averageResponseTime: 0,
      timeoutCount: 0
    };
    this.responseTimes = [];
  }

  /**
   * Create custom key generator for specific use cases
   */
  createCustomKeyGenerator(options: {
    includeAuth?: boolean;
    includeQueryParams?: boolean;
    includeBody?: boolean;
    customFields?: string[];
  }): (request: HttpRequest<any>) => string {
    return (request: HttpRequest<any>) => {
      const components = [
        request.method,
        request.url.split('?')[0] // Base URL without query params
      ];

      if (options.includeQueryParams && request.url.includes('?')) {
        components.push(request.url.split('?')[1]);
      }

      if (options.includeAuth) {
        const auth = request.headers.get('authorization');
        if (auth) {
          components.push(`auth=${auth.substring(0, 20)}...`); // Partial auth for uniqueness
        }
      }

      if (options.includeBody && request.body) {
        components.push(this.serializeBody(request.body));
      }

      if (options.customFields) {
        options.customFields.forEach(field => {
          const value = request.headers.get(field);
          if (value) {
            components.push(`${field}=${value}`);
          }
        });
      }

      return this.hashString(components.join('|'));
    };
  }
}