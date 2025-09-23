import { Injectable } from '@angular/core';
import { HttpRequest } from '@angular/common/http';
import { Observable, Subject, BehaviorSubject, timer } from 'rxjs';

export interface QueuedRequest {
  id: string;
  request: HttpRequest<any>;
  timestamp: number;
  priority: 'low' | 'normal' | 'high' | 'critical';
  retryCount: number;
  maxRetries: number;
  timeout: number;
  subject: Subject<any>;
}

export interface RequestBatch {
  id: string;
  requests: QueuedRequest[];
  batchType: 'read' | 'write' | 'mixed';
  createdAt: number;
  estimatedSize: number;
}

export interface QueueStats {
  totalRequests: number;
  pendingRequests: number;
  completedRequests: number;
  failedRequests: number;
  averageProcessingTime: number;
  batchesProcessed: number;
  queueHealthScore: number;
}

/**
 * Advanced Request Queue Service
 * Optimizes HTTP requests with batching, prioritization, and smart processing
 */
@Injectable({
  providedIn: 'root'
})
export class RequestQueueService {
  private readonly queues = new Map<string, QueuedRequest[]>();
  private readonly processingQueues = new Set<string>();
  private readonly stats$ = new BehaviorSubject<QueueStats>(this.getInitialStats());

  private readonly QUEUE_TYPES = {
    HIGH_PRIORITY: 'high-priority',
    NORMAL: 'normal',
    BATCH_READ: 'batch-read',
    BATCH_WRITE: 'batch-write'
  };

  private readonly CONFIG = {
    MAX_BATCH_SIZE: 10,
    BATCH_TIMEOUT: 50, // ms
    MAX_CONCURRENT_BATCHES: 3,
    QUEUE_SIZE_LIMIT: 100,
    PROCESSING_TIMEOUT: 30000, // 30 seconds
    HEALTH_CHECK_INTERVAL: 5000 // 5 seconds
  };

  private processingTimes: number[] = [];
  private requestCounter = 0;

  constructor() {
    this.initializeQueues();
    this.setupBatchProcessor();
    this.setupHealthMonitoring();
  }

  /**
   * Add request to appropriate queue based on priority and type
   */
  enqueueRequest<T>(
    request: HttpRequest<any>,
    priority: 'low' | 'normal' | 'high' | 'critical' = 'normal',
    options?: {
      timeout?: number;
      maxRetries?: number;
      batchable?: boolean;
    }
  ): Observable<T> {
    const queuedRequest: QueuedRequest = {
      id: this.generateRequestId(),
      request,
      timestamp: Date.now(),
      priority,
      retryCount: 0,
      maxRetries: options?.maxRetries ?? 3,
      timeout: options?.timeout ?? 30000,
      subject: new Subject<T>()
    };

    // Determine queue type
    const queueType = this.determineQueueType(request, priority, options?.batchable);

    // Add to appropriate queue
    this.addToQueue(queueType, queuedRequest);

    // Trigger processing
    this.processQueue(queueType);

    return queuedRequest.subject.asObservable();
  }

  /**
   * Process requests with intelligent batching
   */
  private processQueue(queueType: string): void {
    if (this.processingQueues.has(queueType)) {
      return; // Already processing
    }

    const queue = this.queues.get(queueType);
    if (!queue || queue.length === 0) {
      return;
    }

    this.processingQueues.add(queueType);

    // Different processing strategies based on queue type
    switch (queueType) {
      case this.QUEUE_TYPES.HIGH_PRIORITY:
        this.processHighPriorityQueue(queue);
        break;
      case this.QUEUE_TYPES.BATCH_READ:
        this.processBatchReadQueue(queue);
        break;
      case this.QUEUE_TYPES.BATCH_WRITE:
        this.processBatchWriteQueue(queue);
        break;
      default:
        this.processNormalQueue(queue);
        break;
    }
  }

  /**
   * Process high priority requests immediately
   */
  private processHighPriorityQueue(queue: QueuedRequest[]): void {
    while (queue.length > 0) {
      const request = queue.shift()!;
      this.processSingleRequest(request);
    }
    this.processingQueues.delete(this.QUEUE_TYPES.HIGH_PRIORITY);
  }

  /**
   * Batch read requests for optimal performance
   */
  private processBatchReadQueue(queue: QueuedRequest[]): void {
    const batch = this.createBatch(queue, 'read');

    if (batch.requests.length > 0) {
      this.processBatch(batch);
    }

    // Schedule next batch processing
    setTimeout(() => {
      this.processingQueues.delete(this.QUEUE_TYPES.BATCH_READ);
      this.processQueue(this.QUEUE_TYPES.BATCH_READ);
    }, this.CONFIG.BATCH_TIMEOUT);
  }

  /**
   * Process write requests with careful ordering
   */
  private processBatchWriteQueue(queue: QueuedRequest[]): void {
    // Write operations are processed sequentially to maintain data consistency
    if (queue.length > 0) {
      const request = queue.shift()!;
      this.processSingleRequest(request).then(() => {
        this.processingQueues.delete(this.QUEUE_TYPES.BATCH_WRITE);
        this.processQueue(this.QUEUE_TYPES.BATCH_WRITE);
      });
    } else {
      this.processingQueues.delete(this.QUEUE_TYPES.BATCH_WRITE);
    }
  }

  /**
   * Process normal queue with moderate batching
   */
  private processNormalQueue(queue: QueuedRequest[]): void {
    const batchSize = Math.min(this.CONFIG.MAX_BATCH_SIZE / 2, queue.length);
    const requests = queue.splice(0, batchSize);

    Promise.all(requests.map(req => this.processSingleRequest(req))).then(() => {
      this.processingQueues.delete(this.QUEUE_TYPES.NORMAL);
      if (queue.length > 0) {
        setTimeout(() => this.processQueue(this.QUEUE_TYPES.NORMAL), 10);
      }
    });
  }

  /**
   * Create optimized batch from requests
   */
  private createBatch(queue: QueuedRequest[], type: 'read' | 'write' | 'mixed'): RequestBatch {
    const maxSize = type === 'read' ? this.CONFIG.MAX_BATCH_SIZE : Math.floor(this.CONFIG.MAX_BATCH_SIZE / 2);
    const requests = queue.splice(0, Math.min(maxSize, queue.length));

    return {
      id: this.generateBatchId(),
      requests,
      batchType: type,
      createdAt: Date.now(),
      estimatedSize: this.estimateBatchSize(requests)
    };
  }

  /**
   * Process batch with parallel execution
   */
  private async processBatch(batch: RequestBatch): Promise<void> {
    const startTime = Date.now();

    try {
      // Process requests in parallel for read operations
      if (batch.batchType === 'read') {
        await Promise.all(batch.requests.map(req => this.processSingleRequest(req)));
      } else {
        // Sequential processing for write operations
        for (const request of batch.requests) {
          await this.processSingleRequest(request);
        }
      }

      this.recordBatchSuccess(startTime);
    } catch (error) {
      console.error('Batch processing failed:', error);
      this.recordBatchFailure(batch, error);
    }
  }

  /**
   * Process individual request with error handling
   */
  private async processSingleRequest(queuedRequest: QueuedRequest): Promise<any> {
    const startTime = Date.now();

    try {
      // Simulate request processing
      // In real implementation, this would call the actual HTTP client
      await this.simulateHttpRequest(queuedRequest.request);

      const processingTime = Date.now() - startTime;
      this.recordProcessingTime(processingTime);

      queuedRequest.subject.next('success'); // Replace with actual response
      queuedRequest.subject.complete();

      this.updateStats('completed');
      return 'success';

    } catch (error) {
      this.handleRequestError(queuedRequest, error);
      throw error;
    }
  }

  /**
   * Handle request errors with smart retry logic
   */
  private handleRequestError(queuedRequest: QueuedRequest, error: any): void {
    queuedRequest.retryCount++;

    if (queuedRequest.retryCount <= queuedRequest.maxRetries) {
      // Exponential backoff retry
      const delay = Math.pow(2, queuedRequest.retryCount) * 1000;

      setTimeout(() => {
        const queueType = this.determineQueueType(queuedRequest.request, queuedRequest.priority);
        this.addToQueue(queueType, queuedRequest);
        this.processQueue(queueType);
      }, delay);
    } else {
      queuedRequest.subject.error(error);
      this.updateStats('failed');
    }
  }

  /**
   * Determine optimal queue type for request
   */
  private determineQueueType(
    request: HttpRequest<any>,
    priority: string,
    batchable?: boolean
  ): string {
    if (priority === 'critical' || priority === 'high') {
      return this.QUEUE_TYPES.HIGH_PRIORITY;
    }

    if (batchable !== false) {
      if (request.method === 'GET') {
        return this.QUEUE_TYPES.BATCH_READ;
      }
      if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method)) {
        return this.QUEUE_TYPES.BATCH_WRITE;
      }
    }

    return this.QUEUE_TYPES.NORMAL;
  }

  /**
   * Add request to queue with size limit
   */
  private addToQueue(queueType: string, request: QueuedRequest): void {
    const queue = this.queues.get(queueType)!;

    if (queue.length >= this.CONFIG.QUEUE_SIZE_LIMIT) {
      // Remove oldest low priority request if queue is full
      const oldestIndex = queue.findIndex(req => req.priority === 'low');
      if (oldestIndex !== -1) {
        const removed = queue.splice(oldestIndex, 1)[0];
        removed.subject.error(new Error('Queue overflow - request dropped'));
      }
    }

    queue.push(request);
    this.updateStats('pending');
  }

  /**
   * Initialize queue system
   */
  private initializeQueues(): void {
    Object.values(this.QUEUE_TYPES).forEach(queueType => {
      this.queues.set(queueType, []);
    });
  }

  /**
   * Setup batch processor with intelligent scheduling
   */
  private setupBatchProcessor(): void {
    // Process batches on a timer
    timer(0, this.CONFIG.BATCH_TIMEOUT).subscribe(() => {
      Object.values(this.QUEUE_TYPES).forEach(queueType => {
        if (!this.processingQueues.has(queueType)) {
          this.processQueue(queueType);
        }
      });
    });
  }

  /**
   * Setup health monitoring
   */
  private setupHealthMonitoring(): void {
    timer(0, this.CONFIG.HEALTH_CHECK_INTERVAL).subscribe(() => {
      this.updateHealthScore();
      this.cleanupStaleRequests();
    });
  }

  /**
   * Update queue health score
   */
  private updateHealthScore(): void {
    const stats = this.stats$.value;
    const totalQueued = Array.from(this.queues.values()).reduce((sum, queue) => sum + queue.length, 0);

    // Health score based on queue size, processing time, and success rate
    const queueHealthFactor = Math.max(0, 1 - (totalQueued / this.CONFIG.QUEUE_SIZE_LIMIT));
    const processingHealthFactor = stats.averageProcessingTime < 100 ? 1 : Math.max(0, 1 - (stats.averageProcessingTime / 1000));
    const successRate = stats.totalRequests > 0 ? stats.completedRequests / stats.totalRequests : 1;

    const healthScore = (queueHealthFactor + processingHealthFactor + successRate) / 3;

    this.stats$.next({
      ...stats,
      queueHealthScore: healthScore
    });
  }

  /**
   * Clean up stale requests
   */
  private cleanupStaleRequests(): void {
    const now = Date.now();

    this.queues.forEach((queue, _queueType) => {
      const staleRequests = queue.filter(req => now - req.timestamp > req.timeout);

      staleRequests.forEach(req => {
        req.subject.error(new Error('Request timeout'));
        const index = queue.indexOf(req);
        if (index !== -1) {
          queue.splice(index, 1);
        }
      });
    });
  }

  // Utility methods

  private generateRequestId(): string {
    return `req_${Date.now()}_${++this.requestCounter}`;
  }

  private generateBatchId(): string {
    return `batch_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  private estimateBatchSize(requests: QueuedRequest[]): number {
    return requests.reduce((size, req) => {
      const contentLength = req.request.body ? JSON.stringify(req.request.body).length : 0;
      return size + contentLength + 100; // 100 bytes overhead per request
    }, 0);
  }

  private async simulateHttpRequest(_request: HttpRequest<any>): Promise<any> {
    // Simulate network delay
    const delay = Math.random() * 100 + 50; // 50-150ms
    await new Promise(resolve => setTimeout(resolve, delay));
    return { success: true };
  }

  private recordProcessingTime(time: number): void {
    this.processingTimes.push(time);
    if (this.processingTimes.length > 100) {
      this.processingTimes.shift();
    }
  }

  private recordBatchSuccess(startTime: number): void {
    const processingTime = Date.now() - startTime;
    this.recordProcessingTime(processingTime);
    this.updateStats('batchCompleted');
  }

  private recordBatchFailure(batch: RequestBatch, error: any): void {
    console.error(`Batch ${batch.id} failed:`, error);
    batch.requests.forEach(req => {
      req.subject.error(error);
    });
    this.updateStats('batchFailed');
  }

  private updateStats(type: 'pending' | 'completed' | 'failed' | 'batchCompleted' | 'batchFailed'): void {
    const currentStats = this.stats$.value;

    switch (type) {
      case 'pending':
        this.stats$.next({
          ...currentStats,
          totalRequests: currentStats.totalRequests + 1,
          pendingRequests: currentStats.pendingRequests + 1
        });
        break;
      case 'completed':
        this.stats$.next({
          ...currentStats,
          completedRequests: currentStats.completedRequests + 1,
          pendingRequests: Math.max(0, currentStats.pendingRequests - 1),
          averageProcessingTime: this.calculateAverageProcessingTime()
        });
        break;
      case 'failed':
        this.stats$.next({
          ...currentStats,
          failedRequests: currentStats.failedRequests + 1,
          pendingRequests: Math.max(0, currentStats.pendingRequests - 1)
        });
        break;
      case 'batchCompleted':
        this.stats$.next({
          ...currentStats,
          batchesProcessed: currentStats.batchesProcessed + 1
        });
        break;
    }
  }

  private calculateAverageProcessingTime(): number {
    if (this.processingTimes.length === 0) return 0;
    return this.processingTimes.reduce((sum, time) => sum + time, 0) / this.processingTimes.length;
  }

  private getInitialStats(): QueueStats {
    return {
      totalRequests: 0,
      pendingRequests: 0,
      completedRequests: 0,
      failedRequests: 0,
      averageProcessingTime: 0,
      batchesProcessed: 0,
      queueHealthScore: 1.0
    };
  }

  // Public API

  getQueueStats(): Observable<QueueStats> {
    return this.stats$.asObservable();
  }

  getQueueStatus(): { [queueType: string]: number } {
    const status: { [queueType: string]: number } = {};
    this.queues.forEach((queue, queueType) => {
      status[queueType] = queue.length;
    });
    return status;
  }

  clearQueue(queueType?: string): void {
    if (queueType) {
      const queue = this.queues.get(queueType);
      if (queue) {
        queue.forEach(req => req.subject.error(new Error('Queue cleared')));
        queue.length = 0;
      }
    } else {
      this.queues.forEach(queue => {
        queue.forEach(req => req.subject.error(new Error('All queues cleared')));
        queue.length = 0;
      });
    }
  }
}