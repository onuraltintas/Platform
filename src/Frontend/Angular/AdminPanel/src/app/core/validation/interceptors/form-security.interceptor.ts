import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import {
  SecurityRiskLevel,
  FormSecurityEvent
} from '../interfaces/validation.interface';
import { ValidationService } from '../services/validation.service';

/**
 * Form Security Interceptor
 * Monitors and validates form submissions for security threats
 */
@Injectable()
export class FormSecurityInterceptor implements HttpInterceptor {

  private readonly securityEventLog: FormSecurityEvent[] = [];
  private readonly rateLimitMap = new Map<string, { count: number; lastReset: number }>();
  private readonly suspiciousIPs = new Set<string>();

  constructor(private validationService: ValidationService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Only process form submissions and data mutations
    if (!this.shouldProcessRequest(req)) {
      return next.handle(req);
    }

    const startTime = performance.now();
    const requestId = this.generateRequestId();

    // Pre-process request for security
    const securityResult = this.analyzeRequestSecurity(req);

    // Block high-risk requests
    if (securityResult.shouldBlock) {
      this.logSecurityEvent({
        type: 'security_threat',
        timestamp: Date.now(),
        inputValue: '[BLOCKED REQUEST]',
        inputType: 'custom',
        riskLevel: securityResult.riskLevel,
        error: securityResult.reason,
        userContext: this.extractUserContext(req)
      });

      return throwError(() => new HttpErrorResponse({
        status: 403,
        statusText: 'Forbidden',
        error: {
          message: 'Request blocked due to security concerns',
          code: 'SECURITY_THREAT_DETECTED',
          requestId
        }
      }));
    }

    // Rate limiting check
    const rateLimitResult = this.checkRateLimit(req);
    if (!rateLimitResult.allowed) {
      this.logSecurityEvent({
        type: 'rate_limit',
        timestamp: Date.now(),
        inputValue: '[RATE LIMITED]',
        inputType: 'custom',
        riskLevel: 'medium',
        error: rateLimitResult.message,
        userContext: this.extractUserContext(req)
      });

      return throwError(() => new HttpErrorResponse({
        status: 429,
        statusText: 'Too Many Requests',
        error: {
          message: rateLimitResult.message,
          code: 'RATE_LIMIT_EXCEEDED',
          retryAfter: rateLimitResult.retryAfter,
          requestId
        }
      }));
    }

    // Process and sanitize request body
    const processedRequest = this.processRequestBody(req, securityResult);

    return next.handle(processedRequest).pipe(
      tap(event => {
        if (event instanceof HttpResponse) {
          this.handleSuccessfulResponse(event, req, startTime, requestId);
        }
      }),
      catchError(error => {
        this.handleErrorResponse(error, req, startTime, requestId);
        return throwError(() => error);
      }),
      finalize(() => {
        this.finalizeRequest(req, startTime, requestId);
      })
    );
  }

  /**
   * Determine if request should be processed by security interceptor
   */
  private shouldProcessRequest(req: HttpRequest<any>): boolean {
    // Process POST, PUT, PATCH requests with body
    if (['POST', 'PUT', 'PATCH'].includes(req.method) && req.body) {
      return true;
    }

    // Process requests to sensitive endpoints
    const sensitiveEndpoints = [
      '/api/auth',
      '/api/admin',
      '/api/user',
      '/api/upload',
      '/api/config'
    ];

    return sensitiveEndpoints.some(endpoint => req.url.includes(endpoint));
  }

  /**
   * Analyze request for security threats
   */
  private analyzeRequestSecurity(req: HttpRequest<any>): {
    shouldBlock: boolean;
    riskLevel: SecurityRiskLevel;
    reason?: string;
    sanitizedBody?: any;
  } {
    const result = {
      shouldBlock: false,
      riskLevel: 'none' as SecurityRiskLevel,
      sanitizedBody: req.body
    };

    // Check request headers for suspicious patterns
    const headerSecurity = this.analyzeHeaders(req);
    result.riskLevel = this.escalateRisk(result.riskLevel, headerSecurity.riskLevel);

    // Check URL for injection attempts
    const urlSecurity = this.analyzeUrl(req.url);
    result.riskLevel = this.escalateRisk(result.riskLevel, urlSecurity.riskLevel);

    // Analyze request body if present
    if (req.body) {
      const bodySecurity = this.analyzeRequestBody(req.body);
      result.riskLevel = this.escalateRisk(result.riskLevel, bodySecurity.riskLevel);
      result.sanitizedBody = bodySecurity.sanitizedBody;

      if (bodySecurity.shouldBlock) {
        result.shouldBlock = true;
        result.reason = bodySecurity.reason;
      }
    }

    // Check if IP is suspicious
    const userContext = this.extractUserContext(req);
    if (userContext.ipAddress && this.suspiciousIPs.has(userContext.ipAddress)) {
      result.riskLevel = this.escalateRisk(result.riskLevel, 'high');
    }

    // Block critical and high-risk requests in production
    if (this.isProductionEnvironment() &&
        (result.riskLevel === 'critical' || result.riskLevel === 'high')) {
      result.shouldBlock = true;
      result.reason = `Request blocked due to ${result.riskLevel} security risk`;
    }

    return result;
  }

  /**
   * Analyze request headers for security threats
   */
  private analyzeHeaders(req: HttpRequest<any>): { riskLevel: SecurityRiskLevel } {
    let riskLevel: SecurityRiskLevel = 'none';

    // Check User-Agent for suspicious patterns
    const userAgent = req.headers.get('User-Agent') || '';
    if (this.isSuspiciousUserAgent(userAgent)) {
      riskLevel = this.escalateRisk(riskLevel, 'medium');
    }

    // Check for injection in headers
    const headersToCheck = ['X-Forwarded-For', 'X-Real-IP', 'Referer', 'Origin'];
    for (const headerName of headersToCheck) {
      const headerValue = req.headers.get(headerName);
      if (headerValue && this.containsInjectionPatterns(headerValue)) {
        riskLevel = this.escalateRisk(riskLevel, 'high');
      }
    }

    // Check for missing security headers in sensitive requests
    if (this.isSensitiveEndpoint(req.url)) {
      if (!req.headers.has('X-Requested-With')) {
        riskLevel = this.escalateRisk(riskLevel, 'low');
      }
    }

    return { riskLevel };
  }

  /**
   * Analyze URL for security threats
   */
  private analyzeUrl(url: string): { riskLevel: SecurityRiskLevel } {
    let riskLevel: SecurityRiskLevel = 'none';

    // Check for path traversal
    if (url.includes('..') || url.includes('%2e%2e')) {
      riskLevel = this.escalateRisk(riskLevel, 'high');
    }

    // Check for SQL injection in URL parameters
    if (this.containsSqlInjectionPatterns(url)) {
      riskLevel = this.escalateRisk(riskLevel, 'critical');
    }

    // Check for XSS attempts in URL
    if (this.containsXssPatterns(url)) {
      riskLevel = this.escalateRisk(riskLevel, 'high');
    }

    // Check for command injection
    if (this.containsCommandInjectionPatterns(url)) {
      riskLevel = this.escalateRisk(riskLevel, 'critical');
    }

    return { riskLevel };
  }

  /**
   * Analyze and sanitize request body
   */
  private analyzeRequestBody(body: any): {
    riskLevel: SecurityRiskLevel;
    shouldBlock: boolean;
    reason?: string;
    sanitizedBody: any;
  } {
    const result = {
      riskLevel: 'none' as SecurityRiskLevel,
      shouldBlock: false,
      sanitizedBody: body
    };

    if (!body) return result;

    try {
      const bodyString = typeof body === 'string' ? body : JSON.stringify(body);

      // Check for various injection types
      if (this.containsSqlInjectionPatterns(bodyString)) {
        result.riskLevel = this.escalateRisk(result.riskLevel, 'critical');
        if (this.isProductionEnvironment()) {
          result.shouldBlock = true;
          result.reason = 'SQL injection patterns detected';
        }
      }

      if (this.containsXssPatterns(bodyString)) {
        result.riskLevel = this.escalateRisk(result.riskLevel, 'high');
      }

      if (this.containsCommandInjectionPatterns(bodyString)) {
        result.riskLevel = this.escalateRisk(result.riskLevel, 'critical');
        if (this.isProductionEnvironment()) {
          result.shouldBlock = true;
          result.reason = 'Command injection patterns detected';
        }
      }

      // Sanitize body if needed
      if (result.riskLevel !== 'none' && !result.shouldBlock) {
        result.sanitizedBody = this.sanitizeRequestBody(body);
      }

    } catch (error) {
      console.error('Error analyzing request body:', error);
      result.riskLevel = 'medium';
    }

    return result;
  }

  /**
   * Sanitize request body
   */
  private sanitizeRequestBody(body: any): any {
    if (!body) return body;

    try {
      if (typeof body === 'string') {
        return this.sanitizeString(body);
      }

      if (typeof body === 'object') {
        const sanitized: any = Array.isArray(body) ? [] : {};

        for (const key in body) {
          if (body.hasOwnProperty(key)) {
            const value = body[key];

            if (typeof value === 'string') {
              sanitized[key] = this.sanitizeString(value);
            } else if (typeof value === 'object' && value !== null) {
              sanitized[key] = this.sanitizeRequestBody(value);
            } else {
              sanitized[key] = value;
            }
          }
        }

        return sanitized;
      }

      return body;

    } catch (error) {
      console.error('Error sanitizing request body:', error);
      return body;
    }
  }

  /**
   * Sanitize string value
   */
  private sanitizeString(value: string): string {
    return this.validationService.sanitize(value, {
      html: true,
      sql: true,
      scripts: true,
      urls: true,
      custom: [],
      preserveWhitespace: false
    });
  }

  /**
   * Check rate limiting
   */
  private checkRateLimit(req: HttpRequest<any>): {
    allowed: boolean;
    message?: string;
    retryAfter?: number;
  } {
    const key = this.getRateLimitKey(req);
    const now = Date.now();
    const windowMs = 60 * 1000; // 1 minute window
    const maxRequests = this.getMaxRequestsForEndpoint(req);

    let limitData = this.rateLimitMap.get(key);
    if (!limitData) {
      limitData = { count: 0, lastReset: now };
      this.rateLimitMap.set(key, limitData);
    }

    // Reset window if needed
    if (now - limitData.lastReset > windowMs) {
      limitData.count = 0;
      limitData.lastReset = now;
    }

    limitData.count++;

    if (limitData.count > maxRequests) {
      const retryAfter = Math.ceil((windowMs - (now - limitData.lastReset)) / 1000);
      return {
        allowed: false,
        message: `Rate limit exceeded. Maximum ${maxRequests} requests per minute.`,
        retryAfter
      };
    }

    return { allowed: true };
  }

  /**
   * Process request body with security modifications
   */
  private processRequestBody(req: HttpRequest<any>, securityResult: any): HttpRequest<any> {
    if (securityResult.sanitizedBody && securityResult.sanitizedBody !== req.body) {
      // Create new request with sanitized body
      return req.clone({
        body: securityResult.sanitizedBody,
        setHeaders: {
          'X-Security-Sanitized': 'true'
        }
      });
    }

    return req;
  }

  /**
   * Handle successful response
   */
  private handleSuccessfulResponse(
    response: HttpResponse<any>,
    req: HttpRequest<any>,
    startTime: number,
    requestId: string
  ): void {
    const processingTime = performance.now() - startTime;

    // Log successful but sanitized requests
    if (req.headers.has('X-Security-Sanitized')) {
      this.logSecurityEvent({
        type: 'sanitization',
        timestamp: Date.now(),
        inputValue: '[SANITIZED]',
        inputType: 'custom',
        riskLevel: 'low',
        userContext: this.extractUserContext(req)
      });
    }

    // Monitor for unusually slow responses (potential DoS)
    if (processingTime > 5000) { // 5 seconds
      console.warn(`Slow request detected: ${req.url} took ${processingTime}ms`);
    }
  }

  /**
   * Handle error response
   */
  private handleErrorResponse(
    error: HttpErrorResponse,
    req: HttpRequest<any>,
    startTime: number,
    requestId: string
  ): void {
    const processingTime = performance.now() - startTime;

    // Log security-related errors
    if (error.status === 403 || error.status === 429) {
      this.logSecurityEvent({
        type: 'security_threat',
        timestamp: Date.now(),
        inputValue: '[ERROR RESPONSE]',
        inputType: 'custom',
        riskLevel: 'medium',
        error: error.message,
        userContext: this.extractUserContext(req)
      });
    }

    // Track suspicious IP addresses
    const userContext = this.extractUserContext(req);
    if (userContext.ipAddress && (error.status === 400 || error.status === 403)) {
      this.trackSuspiciousActivity(userContext.ipAddress);
    }
  }

  /**
   * Finalize request processing
   */
  private finalizeRequest(req: HttpRequest<any>, startTime: number, requestId: string): void {
    const processingTime = performance.now() - startTime;

    // Update security metrics
    this.updateSecurityMetrics({
      requestId,
      url: req.url,
      method: req.method,
      processingTime,
      timestamp: Date.now()
    });
  }

  // Helper methods

  private generateRequestId(): string {
    return Math.random().toString(36).substring(2) + Date.now().toString(36);
  }

  private getRateLimitKey(req: HttpRequest<any>): string {
    const userContext = this.extractUserContext(req);
    return `${userContext.ipAddress || 'unknown'}_${req.url.split('?')[0]}`;
  }

  private getMaxRequestsForEndpoint(req: HttpRequest<any>): number {
    // Different limits for different endpoints
    if (req.url.includes('/auth/login')) return 5;
    if (req.url.includes('/auth/')) return 10;
    if (req.url.includes('/upload')) return 20;
    if (req.url.includes('/admin')) return 30;

    return 60; // Default limit
  }

  private isSensitiveEndpoint(url: string): boolean {
    const sensitivePatterns = ['/admin', '/auth', '/config', '/user/profile'];
    return sensitivePatterns.some(pattern => url.includes(pattern));
  }

  private isSuspiciousUserAgent(userAgent: string): boolean {
    const suspiciousPatterns = [
      /curl/i,
      /wget/i,
      /python/i,
      /bot/i,
      /scanner/i,
      /script/i
    ];

    return suspiciousPatterns.some(pattern => pattern.test(userAgent));
  }

  private containsInjectionPatterns(value: string): boolean {
    return this.containsSqlInjectionPatterns(value) ||
           this.containsXssPatterns(value) ||
           this.containsCommandInjectionPatterns(value);
  }

  private containsSqlInjectionPatterns(value: string): boolean {
    const sqlPatterns = [
      /'\s*(or|and)\s*'?\d/i,
      /union\s+select/i,
      /insert\s+into/i,
      /delete\s+from/i,
      /drop\s+table/i,
      /exec\s*\(/i,
      /sp_/i,
      /xp_/i
    ];

    return sqlPatterns.some(pattern => pattern.test(value));
  }

  private containsXssPatterns(value: string): boolean {
    const xssPatterns = [
      /<script/i,
      /javascript:/i,
      /on\w+\s*=/i,
      /<iframe/i,
      /data:\s*text\/html/i,
      /vbscript:/i
    ];

    return xssPatterns.some(pattern => pattern.test(value));
  }

  private containsCommandInjectionPatterns(value: string): boolean {
    const commandPatterns = [
      /[;&|`$]/,
      /\|\s*\w/,
      /&&\s*\w/,
      /;\s*\w/,
      /`[^`]*`/
    ];

    return commandPatterns.some(pattern => pattern.test(value));
  }

  private extractUserContext(req: HttpRequest<any>): any {
    return {
      userAgent: req.headers.get('User-Agent'),
      ipAddress: req.headers.get('X-Forwarded-For') || req.headers.get('X-Real-IP'),
      referer: req.headers.get('Referer')
    };
  }

  private escalateRisk(current: SecurityRiskLevel, newRisk: SecurityRiskLevel): SecurityRiskLevel {
    const levels: SecurityRiskLevel[] = ['none', 'low', 'medium', 'high', 'critical'];
    const currentIndex = levels.indexOf(current);
    const newIndex = levels.indexOf(newRisk);
    return levels[Math.max(currentIndex, newIndex)];
  }

  private isProductionEnvironment(): boolean {
    return typeof window !== 'undefined' &&
           window.location.hostname !== 'localhost' &&
           !window.location.hostname.startsWith('127.0.0.1');
  }

  private logSecurityEvent(event: FormSecurityEvent): void {
    this.securityEventLog.push(event);

    // Keep only last 1000 events
    if (this.securityEventLog.length > 1000) {
      this.securityEventLog.splice(0, this.securityEventLog.length - 1000);
    }

    // Log critical events immediately
    if (event.riskLevel === 'critical' || event.riskLevel === 'high') {
      console.warn('Security event detected:', event);
    }
  }

  private trackSuspiciousActivity(ipAddress: string): void {
    // Add to suspicious IPs after multiple violations
    const recentEvents = this.securityEventLog.filter(
      event => event.userContext?.ipAddress === ipAddress &&
               Date.now() - event.timestamp < 300000 // 5 minutes
    );

    if (recentEvents.length > 5) {
      this.suspiciousIPs.add(ipAddress);
      console.warn(`IP address ${ipAddress} marked as suspicious`);
    }
  }

  private updateSecurityMetrics(metrics: any): void {
    // Update internal metrics
    // This could be expanded to send metrics to monitoring service
  }

  /**
   * Get security event log
   */
  getSecurityEvents(limit = 100): FormSecurityEvent[] {
    return this.securityEventLog.slice(-limit);
  }

  /**
   * Clear security event log
   */
  clearSecurityEvents(): void {
    this.securityEventLog.length = 0;
  }

  /**
   * Get suspicious IP addresses
   */
  getSuspiciousIPs(): string[] {
    return Array.from(this.suspiciousIPs);
  }

  /**
   * Clear suspicious IP list
   */
  clearSuspiciousIPs(): void {
    this.suspiciousIPs.clear();
  }
}