/**
 * Enterprise Security Alerting Service
 * Real-time alert management, notification dispatch, and escalation handling
 */

import { Injectable, inject, signal, computed, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subject, interval, timer } from 'rxjs';
import { debounceTime, takeUntil, tap, mergeMap, concatMap } from 'rxjs/operators';

import {
  SecurityAlert,
  NotificationChannel,
  NotificationTemplate,
  AlertSuppression,
  AlertSeverity,
  ActionResult,
  NotificationFilter,
  EscalationConfig
} from '../interfaces/security-monitoring.interface';

interface AlertingConfig {
  enabled: boolean;
  maxConcurrentNotifications: number;
  defaultRetryAttempts: number;
  defaultRetryDelay: number;
  escalationDelay: number;
  maxEscalationLevels: number;
  notificationBatchSize: number;
  rateLimitWindow: number;
  rateLimitThreshold: number;
  suppressionTtl: number;
  templateCacheSize: number;
  debugMode: boolean;
}

interface NotificationJob {
  id: string;
  alertId: string;
  channelId: string;
  templateId: string;
  priority: number;
  attempts: number;
  maxAttempts: number;
  nextRetry: Date;
  createdAt: Date;
  variables: Record<string, any>;
  status: 'pending' | 'processing' | 'completed' | 'failed' | 'cancelled';
  error?: string;
  result?: ActionResult;
}

interface EscalationJob {
  id: string;
  alertId: string;
  level: number;
  scheduledFor: Date;
  processed: boolean;
  escalationConfig: EscalationConfig;
  context: Record<string, any>;
}

interface NotificationMetrics {
  sent: number;
  failed: number;
  retried: number;
  suppressed: number;
  rateLimited: number;
  byChannel: Record<string, NotificationChannelMetrics>;
  byTemplate: Record<string, NotificationTemplateMetrics>;
  performance: NotificationPerformanceMetrics;
}

interface NotificationChannelMetrics {
  name: string;
  type: string;
  sent: number;
  failed: number;
  avgLatency: number;
  availability: number;
  errorRate: number;
  lastError?: string;
  lastSuccess?: Date;
}

interface NotificationTemplateMetrics {
  name: string;
  used: number;
  errors: number;
  avgRenderTime: number;
  variables: Record<string, number>;
}

interface NotificationPerformanceMetrics {
  avgLatency: number;
  maxLatency: number;
  throughput: number;
  queueDepth: number;
  processingRate: number;
  errorRate: number;
}

interface AlertingHealthStatus {
  status: 'healthy' | 'degraded' | 'unhealthy';
  channels: ChannelHealthStatus[];
  queue: QueueHealthStatus;
  escalation: EscalationHealthStatus;
  lastHealthCheck: Date;
  issues: AlertingIssue[];
}

interface ChannelHealthStatus {
  channelId: string;
  name: string;
  status: 'healthy' | 'degraded' | 'unhealthy';
  lastTest: Date;
  responseTime: number;
  availability: number;
  consecutiveFailures: number;
}

interface QueueHealthStatus {
  depth: number;
  maxDepth: number;
  processingRate: number;
  avgWaitTime: number;
  oldestJobAge: number;
  stalledJobs: number;
}

interface EscalationHealthStatus {
  pendingEscalations: number;
  overdueEscalations: number;
  avgEscalationTime: number;
  escalationRate: number;
}

interface AlertingIssue {
  id: string;
  type: 'channel_failure' | 'queue_overflow' | 'escalation_delay' | 'template_error' | 'rate_limit_exceeded';
  severity: AlertSeverity;
  description: string;
  details: Record<string, any>;
  firstSeen: Date;
  lastSeen: Date;
  count: number;
  resolved: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class SecurityAlertingService implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly destroy$ = new Subject<void>();

  // Configuration
  private readonly config = signal<AlertingConfig>({
    enabled: true,
    maxConcurrentNotifications: 50,
    defaultRetryAttempts: 3,
    defaultRetryDelay: 5000,
    escalationDelay: 300000, // 5 minutes
    maxEscalationLevels: 5,
    notificationBatchSize: 10,
    rateLimitWindow: 60000, // 1 minute
    rateLimitThreshold: 100,
    suppressionTtl: 3600000, // 1 hour
    templateCacheSize: 100,
    debugMode: false
  });

  // State management
  private readonly notificationChannels = signal<Map<string, NotificationChannel>>(new Map());
  private readonly notificationTemplates = signal<Map<string, NotificationTemplate>>(new Map());
  private readonly notificationQueue = signal<NotificationJob[]>([]);
  private readonly escalationQueue = signal<EscalationJob[]>([]);
  private readonly activeSuppressions = signal<Map<string, AlertSuppression>>(new Map());

  // Processing queues
  private readonly notificationSubject = new Subject<NotificationJob>();
  private readonly escalationSubject = new Subject<EscalationJob>();
  private readonly retrySubject = new Subject<NotificationJob>();

  // Metrics and health
  private readonly notificationMetrics = signal<NotificationMetrics>({
    sent: 0,
    failed: 0,
    retried: 0,
    suppressed: 0,
    rateLimited: 0,
    byChannel: {},
    byTemplate: {},
    performance: {
      avgLatency: 0,
      maxLatency: 0,
      throughput: 0,
      queueDepth: 0,
      processingRate: 0,
      errorRate: 0
    }
  });

  private readonly healthStatus = signal<AlertingHealthStatus>({
    status: 'healthy',
    channels: [],
    queue: {
      depth: 0,
      maxDepth: 1000,
      processingRate: 0,
      avgWaitTime: 0,
      oldestJobAge: 0,
      stalledJobs: 0
    },
    escalation: {
      pendingEscalations: 0,
      overdueEscalations: 0,
      avgEscalationTime: 0,
      escalationRate: 0
    },
    lastHealthCheck: new Date(),
    issues: []
  });

  // Rate limiting
  private readonly rateLimiters = new Map<string, { count: number; window: Date }>();
  private readonly templateCache = new Map<string, { template: NotificationTemplate; lastUsed: Date }>();

  // Performance tracking
  private notificationLatencies: number[] = [];
  private lastMetricsReset = new Date();

  // Computed properties
  readonly isHealthy = computed(() => this.healthStatus().status === 'healthy');
  readonly queueDepth = computed(() => this.notificationQueue().length);
  readonly channelCount = computed(() => this.notificationChannels().size);
  readonly activeChannels = computed(() =>
    Array.from(this.notificationChannels().values()).filter(c => c.enabled).length
  );
  readonly notificationRate = computed(() => this.notificationMetrics().performance.throughput);

  constructor() {
    this.initializeAlerting();
    this.startNotificationProcessing();
    this.startEscalationProcessing();
    this.startHealthMonitoring();
    this.startMetricsCollection();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Public API Methods

  /**
   * Send notifications for a security alert
   */
  async sendAlertNotifications(alert: SecurityAlert, channelIds?: string[]): Promise<void> {
    try {
      if (!this.config().enabled) {
        console.warn('Security alerting is disabled');
        return;
      }

      // Check if alert is suppressed
      if (this.isAlertSuppressed(alert)) {
        this.updateMetrics('suppressed');
        return;
      }

      // Determine target channels
      const channels = this.getTargetChannels(alert, channelIds);
      if (channels.length === 0) {
        console.warn('No notification channels configured for alert:', alert.id);
        return;
      }

      // Create notification jobs
      const jobs = await this.createNotificationJobs(alert, channels);

      // Add to queue
      this.enqueueNotifications(jobs);

      if (this.config().debugMode) {
        console.log(`Queued ${jobs.length} notification jobs for alert:`, alert.id);
      }
    } catch (error) {
      console.error('Failed to send alert notifications:', error);
      throw error;
    }
  }

  /**
   * Schedule alert escalation
   */
  async scheduleEscalation(alert: SecurityAlert, escalationConfig: EscalationConfig): Promise<void> {
    try {
      const escalationJob: EscalationJob = {
        id: this.generateId(),
        alertId: alert.id,
        level: 1,
        scheduledFor: new Date(Date.now() + escalationConfig.escalationDelay),
        processed: false,
        escalationConfig,
        context: {
          alertSeverity: alert.severity,
          alertType: alert.type,
          createdAt: alert.createdAt
        }
      };

      const queue = [...this.escalationQueue(), escalationJob];
      this.escalationQueue.set(queue);

      if (this.config().debugMode) {
        console.log('Scheduled escalation for alert:', alert.id, 'at level:', escalationJob.level);
      }
    } catch (error) {
      console.error('Failed to schedule escalation:', error);
      throw error;
    }
  }

  /**
   * Test notification channel
   */
  async testNotificationChannel(channelId: string): Promise<boolean> {
    try {
      const channel = this.notificationChannels().get(channelId);
      if (!channel) {
        throw new Error(`Channel not found: ${channelId}`);
      }

      const testAlert = this.createTestAlert();
      const template = this.getDefaultTemplate(channel.type);

      const success = await this.sendNotification(testAlert, channel, template);

      // Update channel health status
      this.updateChannelHealth(channelId, success, Date.now());

      return success;
    } catch (error) {
      console.error('Channel test failed:', error);
      this.updateChannelHealth(channelId, false, Date.now());
      return false;
    }
  }

  /**
   * Add notification channel
   */
  async addNotificationChannel(channel: NotificationChannel): Promise<void> {
    try {
      // Validate channel configuration
      this.validateChannelConfig(channel);

      const channels = new Map(this.notificationChannels());
      channels.set(channel.id, channel);
      this.notificationChannels.set(channels);

      // Test the channel
      await this.testNotificationChannel(channel.id);

      if (this.config().debugMode) {
        console.log('Added notification channel:', channel.name);
      }
    } catch (error) {
      console.error('Failed to add notification channel:', error);
      throw error;
    }
  }

  /**
   * Update notification channel
   */
  async updateNotificationChannel(channelId: string, updates: Partial<NotificationChannel>): Promise<void> {
    const channels = new Map(this.notificationChannels());
    const channel = channels.get(channelId);

    if (!channel) {
      throw new Error(`Channel not found: ${channelId}`);
    }

    const updatedChannel = { ...channel, ...updates };
    this.validateChannelConfig(updatedChannel);

    channels.set(channelId, updatedChannel);
    this.notificationChannels.set(channels);
  }

  /**
   * Remove notification channel
   */
  async removeNotificationChannel(channelId: string): Promise<void> {
    const channels = new Map(this.notificationChannels());
    channels.delete(channelId);
    this.notificationChannels.set(channels);

    // Cancel pending notifications for this channel
    this.cancelChannelNotifications(channelId);
  }

  /**
   * Suppress alert notifications
   */
  async suppressAlert(alertId: string, suppression: AlertSuppression): Promise<void> {
    const suppressions = new Map(this.activeSuppressions());
    suppressions.set(alertId, suppression);
    this.activeSuppressions.set(suppressions);

    // Cancel pending notifications for suppressed alert
    this.cancelAlertNotifications(alertId);

    // Schedule suppression expiry
    if (suppression.expiresAt) {
      timer(suppression.expiresAt.getTime() - Date.now()).subscribe(() => {
        this.removeSuppression(alertId);
      });
    }
  }

  /**
   * Get notification metrics
   */
  getNotificationMetrics(): NotificationMetrics {
    return this.notificationMetrics();
  }

  /**
   * Get health status
   */
  getHealthStatus(): AlertingHealthStatus {
    return this.healthStatus();
  }

  // Private Methods

  private initializeAlerting(): void {
    // Load configuration
    this.loadConfiguration();

    // Initialize default channels and templates
    this.initializeDefaultChannels();
    this.initializeDefaultTemplates();

    // Setup error handling
    this.setupErrorHandling();
  }

  private startNotificationProcessing(): void {
    // Process notification queue
    this.notificationSubject.pipe(
      takeUntil(this.destroy$),
      mergeMap(job => this.processNotificationJob(job), this.config().maxConcurrentNotifications)
    ).subscribe();

    // Process retry queue
    this.retrySubject.pipe(
      takeUntil(this.destroy$),
      debounceTime(1000),
      mergeMap(job => this.retryNotificationJob(job), 5)
    ).subscribe();

    // Queue processor
    interval(1000).pipe(
      takeUntil(this.destroy$),
      tap(() => this.processNotificationQueue())
    ).subscribe();
  }

  private startEscalationProcessing(): void {
    // Process escalation queue
    this.escalationSubject.pipe(
      takeUntil(this.destroy$),
      concatMap(job => this.processEscalationJob(job))
    ).subscribe();

    // Escalation scheduler
    interval(30000).pipe( // Check every 30 seconds
      takeUntil(this.destroy$),
      tap(() => this.processEscalationQueue())
    ).subscribe();
  }

  private startHealthMonitoring(): void {
    interval(60000).pipe( // Every minute
      takeUntil(this.destroy$),
      tap(() => this.performHealthCheck())
    ).subscribe();
  }

  private startMetricsCollection(): void {
    interval(300000).pipe( // Every 5 minutes
      takeUntil(this.destroy$),
      tap(() => this.collectMetrics())
    ).subscribe();
  }

  private async createNotificationJobs(alert: SecurityAlert, channels: NotificationChannel[]): Promise<NotificationJob[]> {
    const jobs: NotificationJob[] = [];

    for (const channel of channels) {
      // Check rate limits
      if (this.isRateLimited(channel.id)) {
        this.updateMetrics('rateLimited');
        continue;
      }

      // Apply filters
      if (!this.passesFilters(alert, channel.filters)) {
        continue;
      }

      // Get appropriate template
      const template = this.getTemplate(channel, alert);
      if (!template) {
        console.warn('No template found for channel:', channel.name);
        continue;
      }

      const job: NotificationJob = {
        id: this.generateId(),
        alertId: alert.id,
        channelId: channel.id,
        templateId: template.id,
        priority: this.calculatePriority(alert, channel),
        attempts: 0,
        maxAttempts: this.config().defaultRetryAttempts,
        nextRetry: new Date(),
        createdAt: new Date(),
        variables: this.prepareTemplateVariables(alert, channel),
        status: 'pending'
      };

      jobs.push(job);
    }

    return jobs.sort((a, b) => b.priority - a.priority);
  }

  private enqueueNotifications(jobs: NotificationJob[]): void {
    const queue = [...this.notificationQueue(), ...jobs];
    this.notificationQueue.set(queue);
  }

  private processNotificationQueue(): void {
    const queue = this.notificationQueue();
    const now = new Date();

    // Find jobs ready for processing
    const readyJobs = queue.filter(job =>
      job.status === 'pending' && job.nextRetry <= now
    );

    // Process ready jobs
    for (const job of readyJobs.slice(0, this.config().notificationBatchSize)) {
      this.notificationSubject.next(job);
      this.updateJobStatus(job.id, 'processing');
    }
  }

  private async processNotificationJob(job: NotificationJob): Promise<void> {
    const startTime = Date.now();

    try {
      const channel = this.notificationChannels().get(job.channelId);
      const template = this.getTemplateById(job.templateId);

      if (!channel || !template) {
        throw new Error('Channel or template not found');
      }

      // Get alert data (in real implementation, fetch from store)
      const alert = await this.getAlertById(job.alertId);
      if (!alert) {
        throw new Error('Alert not found');
      }

      // Send notification
      const success = await this.sendNotification(alert, channel, template);

      if (success) {
        this.updateJobStatus(job.id, 'completed');
        this.updateMetrics('sent');
        this.updateChannelMetrics(job.channelId, 'success', Date.now() - startTime);
      } else {
        throw new Error('Notification failed');
      }

    } catch (error) {
      this.handleNotificationError(job, error);
    } finally {
      const latency = Date.now() - startTime;
      this.recordLatency(latency);
    }
  }

  private async retryNotificationJob(job: NotificationJob): Promise<void> {
    job.attempts++;
    job.nextRetry = new Date(Date.now() + this.config().defaultRetryDelay * Math.pow(2, job.attempts - 1));

    if (job.attempts >= job.maxAttempts) {
      this.updateJobStatus(job.id, 'failed');
      this.updateMetrics('failed');
      console.error('Notification job failed after max retries:', job.id);
    } else {
      this.updateJobStatus(job.id, 'pending');
      this.updateMetrics('retried');
      if (this.config().debugMode) {
        console.log(`Retrying notification job ${job.id}, attempt ${job.attempts}`);
      }
    }
  }

  private processEscalationQueue(): void {
    const queue = this.escalationQueue();
    const now = new Date();

    const dueEscalations = queue.filter(job =>
      !job.processed && job.scheduledFor <= now
    );

    for (const job of dueEscalations) {
      this.escalationSubject.next(job);
    }
  }

  private async processEscalationJob(job: EscalationJob): Promise<void> {
    try {
      // Mark as processed
      job.processed = true;

      // Get escalation level configuration
      const levelConfig = job.escalationConfig.levels.find(l => l.level === job.level);
      if (!levelConfig) {
        console.warn('No escalation level configuration found:', job.level);
        return;
      }

      // Get alert (in real implementation, fetch from store)
      const alert = await this.getAlertById(job.alertId);
      if (!alert) {
        console.warn('Alert not found for escalation:', job.alertId);
        return;
      }

      // Send escalation notifications
      const channels = this.getChannelsByIds(levelConfig.channels);
      await this.sendAlertNotifications(alert, levelConfig.channels);

      // Schedule next escalation level if applicable
      if (job.level < this.config().maxEscalationLevels &&
          job.escalationConfig.levels.some(l => l.level === job.level + 1)) {

        const nextEscalation: EscalationJob = {
          ...job,
          id: this.generateId(),
          level: job.level + 1,
          scheduledFor: new Date(Date.now() + levelConfig.delay),
          processed: false
        };

        const queue = [...this.escalationQueue(), nextEscalation];
        this.escalationQueue.set(queue);
      }

      if (this.config().debugMode) {
        console.log(`Processed escalation for alert ${job.alertId} at level ${job.level}`);
      }

    } catch (error) {
      console.error('Failed to process escalation job:', error);
    }
  }

  private async sendNotification(alert: SecurityAlert, channel: NotificationChannel, template: NotificationTemplate): Promise<boolean> {
    try {
      const message = this.renderTemplate(template, alert, channel);

      switch (channel.type) {
        case 'email':
          return await this.sendEmailNotification(channel, message);
        case 'slack':
          return await this.sendSlackNotification(channel, message);
        case 'teams':
          return await this.sendTeamsNotification(channel, message);
        case 'webhook':
          return await this.sendWebhookNotification(channel, message);
        case 'sms':
          return await this.sendSmsNotification(channel, message);
        default:
          console.error('Unsupported channel type:', channel.type);
          return false;
      }
    } catch (error) {
      console.error('Failed to send notification:', error);
      return false;
    }
  }

  private renderTemplate(template: NotificationTemplate, alert: SecurityAlert, channel: NotificationChannel): any {
    // Simple template rendering (in real implementation, use a proper template engine)
    let subject = template.subject;
    let body = template.body;

    const variables = {
      alertId: alert.id,
      alertTitle: alert.title,
      alertDescription: alert.description,
      alertSeverity: alert.severity,
      alertStatus: alert.status,
      alertCreatedAt: alert.createdAt.toISOString(),
      eventCount: alert.events.length,
      channelName: channel.name,
      timestamp: new Date().toISOString()
    };

    // Replace variables
    for (const [key, value] of Object.entries(variables)) {
      const placeholder = `{{${key}}}`;
      subject = subject.replace(new RegExp(placeholder, 'g'), String(value));
      body = body.replace(new RegExp(placeholder, 'g'), String(value));
    }

    return {
      subject,
      body,
      variables,
      metadata: {
        alertId: alert.id,
        channelId: channel.id,
        templateId: template.id,
        timestamp: new Date().toISOString()
      }
    };
  }

  // Channel-specific notification methods
  private async sendEmailNotification(channel: NotificationChannel, message: any): Promise<boolean> {
    try {
      const response = await this.http.post(channel.config.endpoint!, {
        to: channel.config.credentials?.to,
        subject: message.subject,
        body: message.body,
        headers: {
          'Content-Type': 'text/html',
          ...channel.config.headers
        }
      }, {
        headers: {
          'Authorization': `Bearer ${channel.config.token}`,
          ...channel.config.headers
        },
        timeout: channel.config.timeout || 30000
      }).toPromise();

      return true;
    } catch (error) {
      console.error('Email notification failed:', error);
      return false;
    }
  }

  private async sendSlackNotification(channel: NotificationChannel, message: any): Promise<boolean> {
    try {
      const payload = {
        text: message.subject,
        blocks: [
          {
            type: 'section',
            text: {
              type: 'mrkdwn',
              text: message.body
            }
          }
        ],
        metadata: message.metadata
      };

      await this.http.post(channel.config.endpoint!, payload, {
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${channel.config.token}`,
          ...channel.config.headers
        },
        timeout: channel.config.timeout || 30000
      }).toPromise();

      return true;
    } catch (error) {
      console.error('Slack notification failed:', error);
      return false;
    }
  }

  private async sendTeamsNotification(channel: NotificationChannel, message: any): Promise<boolean> {
    try {
      const payload = {
        '@type': 'MessageCard',
        '@context': 'http://schema.org/extensions',
        summary: message.subject,
        themeColor: this.getSeverityColor(message.metadata.severity),
        sections: [
          {
            activityTitle: message.subject,
            activitySubtitle: message.metadata.timestamp,
            text: message.body,
            facts: [
              { name: 'Alert ID', value: message.metadata.alertId },
              { name: 'Severity', value: message.metadata.severity },
              { name: 'Timestamp', value: message.metadata.timestamp }
            ]
          }
        ]
      };

      await this.http.post(channel.config.endpoint!, payload, {
        headers: {
          'Content-Type': 'application/json',
          ...channel.config.headers
        },
        timeout: channel.config.timeout || 30000
      }).toPromise();

      return true;
    } catch (error) {
      console.error('Teams notification failed:', error);
      return false;
    }
  }

  private async sendWebhookNotification(channel: NotificationChannel, message: any): Promise<boolean> {
    try {
      await this.http.post(channel.config.endpoint!, message, {
        headers: {
          'Content-Type': 'application/json',
          ...channel.config.headers
        },
        timeout: channel.config.timeout || 30000
      }).toPromise();

      return true;
    } catch (error) {
      console.error('Webhook notification failed:', error);
      return false;
    }
  }

  private async sendSmsNotification(channel: NotificationChannel, message: any): Promise<boolean> {
    try {
      const payload = {
        to: channel.config.credentials?.to,
        message: `${message.subject}\n\n${message.body}`,
        from: channel.config.credentials?.from
      };

      await this.http.post(channel.config.endpoint!, payload, {
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${channel.config.token}`,
          ...channel.config.headers
        },
        timeout: channel.config.timeout || 30000
      }).toPromise();

      return true;
    } catch (error) {
      console.error('SMS notification failed:', error);
      return false;
    }
  }

  // Helper methods
  private isAlertSuppressed(alert: SecurityAlert): boolean {
    const suppression = this.activeSuppressions().get(alert.id);
    if (!suppression || !suppression.suppressed) return false;

    // Check if suppression has expired
    if (suppression.expiresAt && suppression.expiresAt < new Date()) {
      this.removeSuppression(alert.id);
      return false;
    }

    return true;
  }

  private removeSuppression(alertId: string): void {
    const suppressions = new Map(this.activeSuppressions());
    suppressions.delete(alertId);
    this.activeSuppressions.set(suppressions);
  }

  private getTargetChannels(alert: SecurityAlert, channelIds?: string[]): NotificationChannel[] {
    const allChannels = Array.from(this.notificationChannels().values());

    if (channelIds && channelIds.length > 0) {
      return allChannels.filter(c => c.enabled && channelIds.includes(c.id));
    }

    // Default channel selection based on alert severity
    return allChannels.filter(c =>
      c.enabled && this.isChannelApplicable(c, alert)
    );
  }

  private isChannelApplicable(channel: NotificationChannel, alert: SecurityAlert): boolean {
    // Implement channel applicability logic based on filters
    return this.passesFilters(alert, channel.filters);
  }

  private passesFilters(alert: SecurityAlert, filters: NotificationFilter[]): boolean {
    if (!filters || filters.length === 0) return true;

    return filters.every(filter => {
      if (!filter.enabled) return true;

      // Implement filter logic
      return filter.conditions.every(condition => {
        const alertValue = this.getAlertFieldValue(alert, condition.field);
        return this.evaluateCondition(alertValue, condition.operator, condition.value);
      });
    });
  }

  private getAlertFieldValue(alert: SecurityAlert, field: string): any {
    // Navigate nested object paths
    return field.split('.').reduce((obj, key) => obj?.[key], alert);
  }

  private evaluateCondition(value: any, operator: string, expectedValue: any): boolean {
    switch (operator) {
      case 'equals': return value === expectedValue;
      case 'not_equals': return value !== expectedValue;
      case 'contains': return String(value).includes(String(expectedValue));
      case 'not_contains': return !String(value).includes(String(expectedValue));
      case 'greater_than': return Number(value) > Number(expectedValue);
      case 'less_than': return Number(value) < Number(expectedValue);
      case 'in': return Array.isArray(expectedValue) && expectedValue.includes(value);
      case 'not_in': return Array.isArray(expectedValue) && !expectedValue.includes(value);
      default: return false;
    }
  }

  private isRateLimited(channelId: string): boolean {
    const limiter = this.rateLimiters.get(channelId);
    const now = new Date();

    if (!limiter) {
      this.rateLimiters.set(channelId, { count: 1, window: now });
      return false;
    }

    // Reset window if expired
    if (now.getTime() - limiter.window.getTime() > this.config().rateLimitWindow) {
      this.rateLimiters.set(channelId, { count: 1, window: now });
      return false;
    }

    // Check rate limit
    if (limiter.count >= this.config().rateLimitThreshold) {
      return true;
    }

    limiter.count++;
    return false;
  }

  private getTemplate(channel: NotificationChannel, alert: SecurityAlert): NotificationTemplate | null {
    // Find template by channel type and alert severity
    const templates = Array.from(this.notificationTemplates().values());
    return templates.find(t =>
      t.type === channel.type &&
      (t.name.includes(alert.severity) || t.name.includes('default'))
    ) || null;
  }

  private getTemplateById(templateId: string): NotificationTemplate | null {
    return this.notificationTemplates().get(templateId) || null;
  }

  private getDefaultTemplate(channelType: string): NotificationTemplate {
    return {
      id: `default-${channelType}`,
      name: `Default ${channelType} Template`,
      type: channelType as any,
      subject: 'Security Alert: {{alertTitle}}',
      body: 'Alert ID: {{alertId}}\nSeverity: {{alertSeverity}}\nDescription: {{alertDescription}}\nTimestamp: {{timestamp}}',
      variables: [],
      formatting: {
        dateFormat: 'ISO',
        timeFormat: '24h',
        timezone: 'UTC',
        numberFormat: 'en-US',
        truncateLength: 1000,
        htmlEscape: true
      }
    };
  }

  private calculatePriority(alert: SecurityAlert, channel: NotificationChannel): number {
    const severityWeights = { info: 1, low: 2, medium: 3, high: 4, critical: 5 };
    let priority = severityWeights[alert.severity] * 10;

    // Boost priority for certain channel types
    if (channel.type === 'sms' || channel.type === 'pagerduty') {
      priority += 20;
    }

    return priority;
  }

  private prepareTemplateVariables(alert: SecurityAlert, channel: NotificationChannel): Record<string, any> {
    return {
      alert,
      channel,
      timestamp: new Date().toISOString(),
      urgency: this.calculateUrgency(alert),
      context: this.getAlertContext(alert)
    };
  }

  private calculateUrgency(alert: SecurityAlert): string {
    const age = Date.now() - alert.createdAt.getTime();
    const ageInMinutes = age / (1000 * 60);

    if (alert.severity === 'critical' && ageInMinutes > 15) return 'overdue';
    if (alert.severity === 'high' && ageInMinutes > 60) return 'overdue';
    if (alert.severity === 'critical') return 'urgent';
    if (alert.severity === 'high') return 'high';
    return 'normal';
  }

  private getAlertContext(alert: SecurityAlert): Record<string, any> {
    return {
      eventCount: alert.events.length,
      affectedSystems: [...new Set(alert.events.map(e => e.source))],
      timeRange: {
        start: Math.min(...alert.events.map(e => e.timestamp.getTime())),
        end: Math.max(...alert.events.map(e => e.timestamp.getTime()))
      }
    };
  }

  // Status and metrics management
  private updateJobStatus(jobId: string, status: NotificationJob['status']): void {
    const queue = this.notificationQueue().map(job =>
      job.id === jobId ? { ...job, status } : job
    );
    this.notificationQueue.set(queue);
  }

  private updateMetrics(type: keyof Pick<NotificationMetrics, 'sent' | 'failed' | 'retried' | 'suppressed' | 'rateLimited'>): void {
    const metrics = this.notificationMetrics();
    const updated = { ...metrics };
    updated[type]++;
    this.notificationMetrics.set(updated);
  }

  private updateChannelMetrics(channelId: string, result: 'success' | 'failure', latency: number): void {
    const metrics = this.notificationMetrics();
    const channelMetrics = metrics.byChannel[channelId] || {
      name: '',
      type: '',
      sent: 0,
      failed: 0,
      avgLatency: 0,
      availability: 100,
      errorRate: 0
    };

    if (result === 'success') {
      channelMetrics.sent++;
      channelMetrics.lastSuccess = new Date();
    } else {
      channelMetrics.failed++;
      channelMetrics.lastError = 'Notification failed';
    }

    channelMetrics.avgLatency = (channelMetrics.avgLatency + latency) / 2;
    channelMetrics.availability = (channelMetrics.sent / (channelMetrics.sent + channelMetrics.failed)) * 100;
    channelMetrics.errorRate = (channelMetrics.failed / (channelMetrics.sent + channelMetrics.failed)) * 100;

    const updatedMetrics = { ...metrics };
    updatedMetrics.byChannel[channelId] = channelMetrics;
    this.notificationMetrics.set(updatedMetrics);
  }

  private recordLatency(latency: number): void {
    this.notificationLatencies.push(latency);

    // Keep only recent latencies
    if (this.notificationLatencies.length > 1000) {
      this.notificationLatencies = this.notificationLatencies.slice(-1000);
    }
  }

  private handleNotificationError(job: NotificationJob, error: any): void {
    console.error('Notification job failed:', job.id, error);

    job.error = error.message || 'Unknown error';
    job.result = {
      success: false,
      message: error.message,
      details: { error },
      duration: 0,
      sideEffects: []
    };

    // Schedule retry if attempts remaining
    if (job.attempts < job.maxAttempts) {
      this.retrySubject.next(job);
    } else {
      this.updateJobStatus(job.id, 'failed');
      this.updateMetrics('failed');
    }

    this.updateChannelMetrics(job.channelId, 'failure', 0);
  }

  private updateChannelHealth(channelId: string, success: boolean, responseTime: number): void {
    const health = this.healthStatus();
    const channelHealth = health.channels.find(c => c.channelId === channelId);

    if (channelHealth) {
      channelHealth.lastTest = new Date();
      channelHealth.responseTime = responseTime;

      if (success) {
        channelHealth.consecutiveFailures = 0;
        channelHealth.availability = Math.min(100, channelHealth.availability + 1);
        channelHealth.status = 'healthy';
      } else {
        channelHealth.consecutiveFailures++;
        channelHealth.availability = Math.max(0, channelHealth.availability - 5);

        if (channelHealth.consecutiveFailures >= 3) {
          channelHealth.status = 'unhealthy';
        } else if (channelHealth.consecutiveFailures >= 1) {
          channelHealth.status = 'degraded';
        }
      }
    }

    this.healthStatus.set({ ...health });
  }

  private performHealthCheck(): void {
    const health = this.healthStatus();
    const now = new Date();

    // Update queue health
    const queueDepth = this.notificationQueue().length;
    const stalledJobs = this.notificationQueue().filter(job =>
      job.status === 'processing' &&
      (now.getTime() - job.createdAt.getTime()) > 300000 // 5 minutes
    ).length;

    health.queue = {
      ...health.queue,
      depth: queueDepth,
      stalledJobs,
      oldestJobAge: queueDepth > 0 ?
        now.getTime() - Math.min(...this.notificationQueue().map(j => j.createdAt.getTime())) : 0
    };

    // Update escalation health
    const pendingEscalations = this.escalationQueue().filter(j => !j.processed).length;
    const overdueEscalations = this.escalationQueue().filter(j =>
      !j.processed && j.scheduledFor < now
    ).length;

    health.escalation = {
      ...health.escalation,
      pendingEscalations,
      overdueEscalations
    };

    // Determine overall health
    const unhealthyChannels = health.channels.filter(c => c.status === 'unhealthy').length;
    const degradedChannels = health.channels.filter(c => c.status === 'degraded').length;

    if (unhealthyChannels > 0 || stalledJobs > 10 || overdueEscalations > 5) {
      health.status = 'unhealthy';
    } else if (degradedChannels > 0 || stalledJobs > 5 || overdueEscalations > 2) {
      health.status = 'degraded';
    } else {
      health.status = 'healthy';
    }

    health.lastHealthCheck = now;
    this.healthStatus.set(health);
  }

  private collectMetrics(): void {
    const metrics = this.notificationMetrics();

    // Calculate performance metrics
    const avgLatency = this.notificationLatencies.length > 0 ?
      this.notificationLatencies.reduce((a, b) => a + b, 0) / this.notificationLatencies.length : 0;

    const maxLatency = this.notificationLatencies.length > 0 ?
      Math.max(...this.notificationLatencies) : 0;

    const timeSinceReset = Date.now() - this.lastMetricsReset.getTime();
    const throughput = (metrics.sent * 1000) / timeSinceReset; // per second

    metrics.performance = {
      avgLatency,
      maxLatency,
      throughput,
      queueDepth: this.notificationQueue().length,
      processingRate: throughput,
      errorRate: metrics.failed / (metrics.sent + metrics.failed) || 0
    };

    this.notificationMetrics.set(metrics);
  }

  // Utility methods
  private generateId(): string {
    return crypto.getRandomValues(new Uint32Array(1))[0].toString(16);
  }

  private getSeverityColor(severity: AlertSeverity): string {
    const colors = {
      info: '#0078d4',
      low: '#00bcf2',
      medium: '#ffb900',
      high: '#ff8c00',
      critical: '#d13438'
    };
    return colors[severity] || colors.medium;
  }

  // Placeholder methods for external dependencies
  private async getAlertById(alertId: string): Promise<SecurityAlert | null> {
    // In real implementation, fetch from alert store/service
    return null;
  }

  private getChannelsByIds(channelIds: string[]): NotificationChannel[] {
    return channelIds.map(id => this.notificationChannels().get(id)).filter(Boolean) as NotificationChannel[];
  }

  private createTestAlert(): SecurityAlert {
    return {
      id: 'test-alert',
      title: 'Test Alert',
      description: 'This is a test alert for channel validation',
      severity: 'info',
      status: 'new',
      type: 'test',
      scope: 'application',
      createdAt: new Date(),
      updatedAt: new Date(),
      events: [],
      indicators: [],
      timeline: [],
      actions: [],
      suppressions: [],
      escalations: [],
      tags: ['test'],
      priority: 1,
      confidence: 1,
      falsePositiveRisk: 0,
      impactAssessment: {} as any,
      remediationSteps: [],
      relatedAlerts: [],
      externalReferences: []
    };
  }

  private validateChannelConfig(channel: NotificationChannel): void {
    if (!channel.name || !channel.type) {
      throw new Error('Channel name and type are required');
    }

    if (!channel.config.endpoint && !['email', 'sms'].includes(channel.type)) {
      throw new Error('Endpoint is required for this channel type');
    }

    // Additional validation logic...
  }

  private cancelChannelNotifications(channelId: string): void {
    const queue = this.notificationQueue().map(job =>
      job.channelId === channelId && job.status === 'pending' ?
        { ...job, status: 'cancelled' as const } : job
    );
    this.notificationQueue.set(queue);
  }

  private cancelAlertNotifications(alertId: string): void {
    const queue = this.notificationQueue().map(job =>
      job.alertId === alertId && job.status === 'pending' ?
        { ...job, status: 'cancelled' as const } : job
    );
    this.notificationQueue.set(queue);
  }

  private loadConfiguration(): void {
    // Load from environment or API
  }

  private initializeDefaultChannels(): void {
    // Initialize default notification channels
  }

  private initializeDefaultTemplates(): void {
    // Initialize default notification templates
    const templates = new Map<string, NotificationTemplate>();

    // Email templates
    templates.set('email-critical', {
      id: 'email-critical',
      name: 'Critical Email Alert',
      type: 'email',
      subject: '🚨 CRITICAL SECURITY ALERT: {{alertTitle}}',
      body: `
        <h2 style="color: #d13438;">Critical Security Alert</h2>
        <p><strong>Alert ID:</strong> {{alertId}}</p>
        <p><strong>Severity:</strong> <span style="color: #d13438; font-weight: bold;">{{alertSeverity}}</span></p>
        <p><strong>Description:</strong> {{alertDescription}}</p>
        <p><strong>Event Count:</strong> {{eventCount}}</p>
        <p><strong>Timestamp:</strong> {{timestamp}}</p>
        <hr>
        <p style="color: #666;">This is an automated security alert. Please investigate immediately.</p>
      `,
      variables: [],
      formatting: {
        dateFormat: 'ISO',
        timeFormat: '24h',
        timezone: 'UTC',
        numberFormat: 'en-US',
        htmlEscape: true
      }
    });

    this.notificationTemplates.set(templates);
  }

  private setupErrorHandling(): void {
    // Setup global error handling for notification processing
  }
}