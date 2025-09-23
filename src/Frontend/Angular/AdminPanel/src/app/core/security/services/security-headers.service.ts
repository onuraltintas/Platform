import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import {
  SecurityConfig,
  SecurityHeaders,
  CSPDirectives,
  CSPViolationReport,
  SecurityHeadersService as ISecurityHeadersService
} from '../interfaces/security-headers.interface';
import { getSecurityConfig } from '../config/security.config';
import { environment } from '../../../../environments/environment';

/**
 * Enterprise Security Headers Service
 * Implements OWASP security header best practices and CSP management
 */
@Injectable({
  providedIn: 'root'
})
export class SecurityHeadersService implements ISecurityHeadersService {
  private readonly http = inject(HttpClient);

  private currentConfig$ = new BehaviorSubject<SecurityConfig>(
    getSecurityConfig(environment.production ? 'production' : 'development')
  );

  private nonceCache = new Map<string, { nonce: string; timestamp: number }>();
  private readonly nonceLifetime = 5 * 60 * 1000; // 5 minutes

  /**
   * Get current security configuration
   */
  getCurrentConfig(): Observable<SecurityConfig> {
    return this.currentConfig$.asObservable();
  }

  /**
   * Update security configuration
   */
  updateConfig(config: SecurityConfig): void {
    this.currentConfig$.next(config);
  }

  /**
   * Generate Content Security Policy header value
   */
  generateCSP(config: SecurityConfig): string {
    if (!config.csp.enabled) {
      return '';
    }

    const directives: string[] = [];

    // Process each CSP directive
    Object.entries(config.csp.directives).forEach(([directive, values]) => {
      if (values && Array.isArray(values) && values.length > 0) {
        let directiveValue = values.join(' ');

        // Replace nonce placeholder with actual nonce
        if (config.csp.useNonces && directiveValue.includes('{NONCE}')) {
          const nonce = this.generateNonce();
          directiveValue = directiveValue.replace(/\{NONCE\}/g, nonce);
        }

        directives.push(`${directive} ${directiveValue}`);
      } else if (typeof values === 'boolean' && values) {
        // Handle boolean directives like upgrade-insecure-requests
        directives.push(directive);
      }
    });

    return directives.join('; ');
  }

  /**
   * Generate all security headers
   */
  generateSecurityHeaders(config: SecurityConfig): SecurityHeaders {
    if (!config.headers.enabled) {
      return {};
    }

    const headers: SecurityHeaders = {};

    // Generate CSP header
    if (config.csp.enabled) {
      const cspValue = this.generateCSP(config);
      if (cspValue) {
        const headerName = config.csp.reportOnly
          ? 'Content-Security-Policy-Report-Only'
          : 'Content-Security-Policy';
        headers[headerName] = cspValue;
      }
    }

    // Add custom headers
    Object.assign(headers, config.headers.custom);

    // Generate Permissions Policy if not custom defined
    if (!headers['Permissions-Policy'] && config.permissions) {
      headers['Permissions-Policy'] = this.generatePermissionsPolicy(config.permissions);
    }

    return headers;
  }

  /**
   * Validate CSP configuration
   */
  validateCSP(directives: Partial<CSPDirectives>): { valid: boolean; errors: string[] } {
    const errors: string[] = [];

    // Check for unsafe directives in production
    const currentConfig = this.currentConfig$.value;
    if (currentConfig.environment === 'production') {
      this.validateProductionCSP(directives, errors);
    }

    // Check for missing essential directives
    this.validateEssentialDirectives(directives, errors);

    // Check for conflicting directives
    this.validateDirectiveConflicts(directives, errors);

    return {
      valid: errors.length === 0,
      errors
    };
  }

  /**
   * Generate cryptographically secure nonce
   */
  generateNonce(): string {
    // Check cache first
    const cacheKey = 'current_nonce';
    const cached = this.nonceCache.get(cacheKey);

    if (cached && Date.now() - cached.timestamp < this.nonceLifetime) {
      return cached.nonce;
    }

    // Generate new nonce
    const array = new Uint8Array(16);
    crypto.getRandomValues(array);
    const nonce = btoa(String.fromCharCode(...array))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=/g, '');

    // Cache the nonce
    this.nonceCache.set(cacheKey, {
      nonce,
      timestamp: Date.now()
    });

    // Clean up old nonces
    this.cleanupNonceCache();

    return nonce;
  }

  /**
   * Report CSP violation
   */
  async reportViolation(report: CSPViolationReport): Promise<void> {
    try {
      const config = this.currentConfig$.value;

      if (!config.csp.reportUri) {
        console.warn('CSP violation reporting not configured');
        return;
      }

      // Log violation locally in development
      if (!environment.production) {
        console.warn('CSP Violation:', report);
      }

      // Send to reporting endpoint
      await this.http.post(config.csp.reportUri, {
        'csp-report': report,
        timestamp: new Date().toISOString(),
        userAgent: navigator.userAgent,
        url: window.location.href
      }).toPromise();

    } catch (error) {
      console.error('Failed to report CSP violation:', error);
    }
  }

  /**
   * Get security recommendations based on current config
   */
  getSecurityRecommendations(currentConfig: SecurityConfig): string[] {
    const recommendations: string[] = [];

    // CSP recommendations
    if (!currentConfig.csp.enabled) {
      recommendations.push('Enable Content Security Policy for XSS protection');
    }

    if (currentConfig.csp.reportOnly && currentConfig.environment === 'production') {
      recommendations.push('Disable CSP report-only mode in production');
    }

    if (!currentConfig.csp.useNonces && currentConfig.environment === 'production') {
      recommendations.push('Enable nonce-based CSP for better security');
    }

    // Check for unsafe directives
    const unsafeDirectives = this.findUnsafeDirectives(currentConfig.csp.directives);
    unsafeDirectives.forEach(directive => {
      recommendations.push(`Remove unsafe directive: ${directive}`);
    });

    // HTTPS recommendations
    if (!currentConfig.csp.directives['upgrade-insecure-requests']) {
      recommendations.push('Enable upgrade-insecure-requests directive');
    }

    // Header recommendations
    if (!currentConfig.headers.custom['Strict-Transport-Security']) {
      recommendations.push('Add HSTS header for transport security');
    }

    if (!currentConfig.headers.custom['X-Frame-Options']) {
      recommendations.push('Add X-Frame-Options header to prevent clickjacking');
    }

    return recommendations;
  }

  /**
   * Apply security headers to meta tags (for client-side enforcement)
   */
  applyMetaHeaders(config: SecurityConfig): void {
    const headers = this.generateSecurityHeaders(config);

    // Apply CSP via meta tag if needed
    if (headers['Content-Security-Policy']) {
      this.setMetaTag('Content-Security-Policy', headers['Content-Security-Policy']);
    }

    // Apply other compatible headers
    if (headers['Referrer-Policy']) {
      this.setMetaTag('referrer', headers['Referrer-Policy']);
    }
  }

  /**
   * Monitor CSP violations and provide real-time feedback
   */
  startViolationMonitoring(): void {
    if (typeof document !== 'undefined') {
      document.addEventListener('securitypolicyviolation', (event) => {
        const report: CSPViolationReport = {
          'document-uri': event.documentURI,
          'referrer': event.referrer,
          'blocked-uri': event.blockedURI,
          'violated-directive': event.violatedDirective,
          'effective-directive': event.effectiveDirective,
          'original-policy': event.originalPolicy,
          'disposition': event.disposition as 'enforce' | 'report',
          'status-code': event.statusCode,
          'script-sample': event.sample,
          'line-number': event.lineNumber,
          'column-number': event.columnNumber,
          'source-file': event.sourceFile
        };

        this.reportViolation(report);
      });
    }
  }

  /**
   * Get security metrics and statistics
   */
  getSecurityMetrics(): {
    cspEnabled: boolean;
    securityLevel: string;
    headersCount: number;
    violationsCount: number;
    lastViolation?: Date;
  } {
    const config = this.currentConfig$.value;
    const headers = this.generateSecurityHeaders(config);

    return {
      cspEnabled: config.csp.enabled,
      securityLevel: config.level,
      headersCount: Object.keys(headers).length,
      violationsCount: 0, // Would be tracked in real implementation
      lastViolation: undefined // Would be tracked in real implementation
    };
  }

  // Private helper methods

  private validateProductionCSP(directives: Partial<CSPDirectives>, errors: string[]): void {
    // Check for unsafe-inline and unsafe-eval
    ['script-src', 'style-src'].forEach(directive => {
      const values = directives[directive as keyof CSPDirectives] as string[];
      if (values?.includes("'unsafe-inline'")) {
        errors.push(`Production CSP should not include 'unsafe-inline' in ${directive}`);
      }
      if (values?.includes("'unsafe-eval'")) {
        errors.push(`Production CSP should not include 'unsafe-eval' in ${directive}`);
      }
    });
  }

  private validateEssentialDirectives(directives: Partial<CSPDirectives>, errors: string[]): void {
    const essential = ['default-src', 'script-src', 'style-src', 'object-src'];

    essential.forEach(directive => {
      if (!directives[directive as keyof CSPDirectives]) {
        errors.push(`Missing essential directive: ${directive}`);
      }
    });
  }

  private validateDirectiveConflicts(directives: Partial<CSPDirectives>, errors: string[]): void {
    // Check for conflicting directives
    if (directives['object-src'] && !directives['object-src']?.includes("'none'")) {
      errors.push("object-src should be set to 'none' for security");
    }
  }

  private findUnsafeDirectives(directives: Partial<CSPDirectives>): string[] {
    const unsafe: string[] = [];

    Object.entries(directives).forEach(([directive, values]) => {
      if (Array.isArray(values)) {
        values.forEach(value => {
          if (value.includes('unsafe-')) {
            unsafe.push(`${directive}: ${value}`);
          }
        });
      }
    });

    return unsafe;
  }

  private generatePermissionsPolicy(permissions: SecurityConfig['permissions']): string {
    const policies: string[] = [];

    Object.entries(permissions).forEach(([feature, policy]) => {
      if (policy === 'none') {
        policies.push(`${feature}=()`);
      } else if (policy === 'self') {
        policies.push(`${feature}=(self)`);
      } else if (Array.isArray(policy)) {
        const domains = policy.map(domain => `"${domain}"`).join(' ');
        policies.push(`${feature}=(${domains})`);
      }
    });

    return policies.join(', ');
  }

  private setMetaTag(name: string, content: string): void {
    if (typeof document === 'undefined') return;

    let metaTag = document.querySelector(`meta[http-equiv="${name}"]`) as HTMLMetaElement;

    if (!metaTag) {
      metaTag = document.createElement('meta');
      metaTag.httpEquiv = name;
      document.head.appendChild(metaTag);
    }

    metaTag.content = content;
  }

  private cleanupNonceCache(): void {
    const now = Date.now();

    for (const [key, value] of this.nonceCache.entries()) {
      if (now - value.timestamp > this.nonceLifetime) {
        this.nonceCache.delete(key);
      }
    }
  }
}