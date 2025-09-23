import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SecurityHeadersService } from './security-headers.service';
import {
  SecurityConfig,
  CSPDirectives,
  CSPViolationReport
} from '../interfaces/security-headers.interface';
import { developmentSecurityConfig, productionSecurityConfig } from '../config/security.config';

/**
 * Comprehensive Security Headers Service Test Suite
 * Tests CSP generation, validation, and security features
 */
describe('SecurityHeadersService', () => {
  let service: SecurityHeadersService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SecurityHeadersService]
    });

    service = TestBed.inject(SecurityHeadersService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('CSP Generation', () => {
    it('should generate basic CSP header', () => {
      const config: SecurityConfig = {
        ...developmentSecurityConfig,
        csp: {
          enabled: true,
          reportOnly: false,
          useNonces: false,
          directives: {
            'default-src': ["'self'"],
            'script-src': ["'self'", "'unsafe-inline'"],
            'style-src': ["'self'", "'unsafe-inline'"]
          }
        }
      };

      const csp = service.generateCSP(config);
      expect(csp).toContain("default-src 'self'");
      expect(csp).toContain("script-src 'self' 'unsafe-inline'");
      expect(csp).toContain("style-src 'self' 'unsafe-inline'");
    });

    it('should generate nonce-based CSP when nonces are enabled', () => {
      const config: SecurityConfig = {
        ...productionSecurityConfig,
        csp: {
          enabled: true,
          reportOnly: false,
          useNonces: true,
          directives: {
            'script-src': ["'self'", "'nonce-{NONCE}'"],
            'style-src': ["'self'", "'nonce-{NONCE}'"]
          }
        }
      };

      const csp = service.generateCSP(config);
      expect(csp).toContain("script-src 'self' 'nonce-");
      expect(csp).toContain("style-src 'self' 'nonce-");
      expect(csp).not.toContain('{NONCE}'); // Should be replaced
    });

    it('should handle boolean directives correctly', () => {
      const config: SecurityConfig = {
        ...productionSecurityConfig,
        csp: {
          enabled: true,
          reportOnly: false,
          useNonces: false,
          directives: {
            'default-src': ["'self'"],
            'upgrade-insecure-requests': true,
            'block-all-mixed-content': true
          }
        }
      };

      const csp = service.generateCSP(config);
      expect(csp).toContain('upgrade-insecure-requests');
      expect(csp).toContain('block-all-mixed-content');
    });

    it('should return empty string when CSP is disabled', () => {
      const config: SecurityConfig = {
        ...developmentSecurityConfig,
        csp: {
          enabled: false,
          reportOnly: false,
          useNonces: false,
          directives: {}
        }
      };

      const csp = service.generateCSP(config);
      expect(csp).toBe('');
    });
  });

  describe('Security Headers Generation', () => {
    it('should generate comprehensive security headers for production', () => {
      const headers = service.generateSecurityHeaders(productionSecurityConfig);

      expect(headers['Content-Security-Policy']).toBeDefined();
      expect(headers['Strict-Transport-Security']).toContain('max-age=63072000');
      expect(headers['X-Frame-Options']).toBe('DENY');
      expect(headers['X-Content-Type-Options']).toBe('nosniff');
      expect(headers['Referrer-Policy']).toBe('strict-origin-when-cross-origin');
      expect(headers['Cross-Origin-Opener-Policy']).toBe('same-origin');
    });

    it('should generate development headers with relaxed policies', () => {
      const headers = service.generateSecurityHeaders(developmentSecurityConfig);

      expect(headers['Content-Security-Policy-Report-Only']).toBeDefined();
      expect(headers['X-Frame-Options']).toBe('SAMEORIGIN');
      expect(headers['Strict-Transport-Security']).toBeUndefined();
    });

    it('should generate Permissions Policy header', () => {
      const headers = service.generateSecurityHeaders(productionSecurityConfig);

      expect(headers['Permissions-Policy']).toBeDefined();
      expect(headers['Permissions-Policy']).toContain('camera=()');
      expect(headers['Permissions-Policy']).toContain('microphone=()');
      expect(headers['Permissions-Policy']).toContain('fullscreen=(self)');
    });
  });

  describe('CSP Validation', () => {
    it('should validate production CSP and flag unsafe directives', () => {
      const unsafeDirectives: Partial<CSPDirectives> = {
        'script-src': ["'self'", "'unsafe-inline'", "'unsafe-eval'"],
        'style-src': ["'self'", "'unsafe-inline'"]
      };

      // Set to production environment
      service.updateConfig(productionSecurityConfig);

      const validation = service.validateCSP(unsafeDirectives);
      expect(validation.valid).toBe(false);
      expect(validation.errors).toContain(
        "Production CSP should not include 'unsafe-inline' in script-src"
      );
      expect(validation.errors).toContain(
        "Production CSP should not include 'unsafe-eval' in script-src"
      );
    });

    it('should require essential directives', () => {
      const incompleteDirectives: Partial<CSPDirectives> = {
        'script-src': ["'self'"]
        // Missing default-src, style-src, object-src
      };

      const validation = service.validateCSP(incompleteDirectives);
      expect(validation.valid).toBe(false);
      expect(validation.errors).toContain('Missing essential directive: default-src');
      expect(validation.errors).toContain('Missing essential directive: style-src');
      expect(validation.errors).toContain('Missing essential directive: object-src');
    });

    it('should validate object-src security', () => {
      const insecureDirectives: Partial<CSPDirectives> = {
        'default-src': ["'self'"],
        'script-src': ["'self'"],
        'style-src': ["'self'"],
        'object-src': ["'self'", 'https://example.com'] // Should be 'none'
      };

      const validation = service.validateCSP(insecureDirectives);
      expect(validation.valid).toBe(false);
      expect(validation.errors).toContain("object-src should be set to 'none' for security");
    });

    it('should pass validation for secure CSP', () => {
      const secureDirectives: Partial<CSPDirectives> = {
        'default-src': ["'self'"],
        'script-src': ["'self'"],
        'style-src': ["'self'"],
        'object-src': ["'none'"]
      };

      const validation = service.validateCSP(secureDirectives);
      expect(validation.valid).toBe(true);
      expect(validation.errors).toHaveLength(0);
    });
  });

  describe('Nonce Generation', () => {
    it('should generate cryptographically secure nonces', () => {
      const nonce1 = service.generateNonce();
      const nonce2 = service.generateNonce();

      expect(nonce1).toBeTruthy();
      expect(nonce2).toBeTruthy();
      expect(nonce1.length).toBeGreaterThan(16);
      expect(nonce2.length).toBeGreaterThan(16);

      // Should be URL-safe base64
      expect(nonce1).toMatch(/^[A-Za-z0-9_-]+$/);
      expect(nonce2).toMatch(/^[A-Za-z0-9_-]+$/);
    });

    it('should cache nonces for performance', () => {
      const nonce1 = service.generateNonce();
      const nonce2 = service.generateNonce();

      // Should return same nonce within cache lifetime
      expect(nonce1).toBe(nonce2);
    });
  });

  describe('Violation Reporting', () => {
    it('should report CSP violations to endpoint', async () => {
      const mockReport: CSPViolationReport = {
        'document-uri': 'https://example.com/page',
        'referrer': 'https://example.com',
        'blocked-uri': 'https://malicious.com/script.js',
        'violated-directive': 'script-src',
        'effective-directive': 'script-src',
        'original-policy': "default-src 'self'",
        'disposition': 'enforce',
        'status-code': 200
      };

      const config = { ...productionSecurityConfig };
      config.csp.reportUri = '/api/v1/security/csp-violations';
      service.updateConfig(config);

      const reportPromise = service.reportViolation(mockReport);

      const req = httpTestingController.expectOne('/api/v1/security/csp-violations');
      expect(req.request.method).toBe('POST');
      expect(req.request.body['csp-report']).toEqual(mockReport);
      expect(req.request.body.timestamp).toBeDefined();
      expect(req.request.body.userAgent).toBeDefined();

      req.flush({});
      await reportPromise;
    });

    it('should handle reporting errors gracefully', async () => {
      const mockReport: CSPViolationReport = {
        'document-uri': 'https://example.com/page',
        'referrer': '',
        'blocked-uri': 'eval',
        'violated-directive': 'script-src',
        'effective-directive': 'script-src',
        'original-policy': "script-src 'self'",
        'disposition': 'enforce',
        'status-code': 200
      };

      const config = { ...productionSecurityConfig };
      config.csp.reportUri = '/api/v1/security/csp-violations';
      service.updateConfig(config);

      const reportPromise = service.reportViolation(mockReport);

      const req = httpTestingController.expectOne('/api/v1/security/csp-violations');
      req.error(new ErrorEvent('Network error'));

      // Should not throw error
      await expectAsync(reportPromise).toBeResolved();
    });
  });

  describe('Security Recommendations', () => {
    it('should provide recommendations for insecure configurations', () => {
      const insecureConfig: SecurityConfig = {
        ...developmentSecurityConfig,
        csp: {
          enabled: false,
          reportOnly: true,
          useNonces: false,
          directives: {
            'script-src': ["'self'", "'unsafe-inline'", "'unsafe-eval'"]
          }
        },
        headers: {
          enabled: true,
          custom: {}
        },
        environment: 'production'
      };

      const recommendations = service.getSecurityRecommendations(insecureConfig);

      expect(recommendations).toContain('Enable Content Security Policy for XSS protection');
      expect(recommendations).toContain('Disable CSP report-only mode in production');
      expect(recommendations).toContain('Enable nonce-based CSP for better security');
      expect(recommendations).toContain('Add HSTS header for transport security');
      expect(recommendations).toContain('Add X-Frame-Options header to prevent clickjacking');
    });

    it('should detect unsafe directives in recommendations', () => {
      const configWithUnsafeDirectives: SecurityConfig = {
        ...productionSecurityConfig,
        csp: {
          ...productionSecurityConfig.csp,
          directives: {
            'script-src': ["'self'", "'unsafe-inline'"],
            'style-src': ["'self'", "'unsafe-eval'"]
          }
        }
      };

      const recommendations = service.getSecurityRecommendations(configWithUnsafeDirectives);

      expect(recommendations.some(r => r.includes("Remove unsafe directive: script-src: 'unsafe-inline'"))).toBe(true);
      expect(recommendations.some(r => r.includes("Remove unsafe directive: style-src: 'unsafe-eval'"))).toBe(true);
    });
  });

  describe('Security Metrics', () => {
    it('should provide accurate security metrics', () => {
      service.updateConfig(productionSecurityConfig);
      const metrics = service.getSecurityMetrics();

      expect(metrics.cspEnabled).toBe(true);
      expect(metrics.securityLevel).toBe('strict');
      expect(metrics.headersCount).toBeGreaterThan(0);
      expect(metrics.violationsCount).toBeDefined();
    });
  });

  describe('Meta Tag Application', () => {
    it('should apply CSP via meta tag', () => {
      // Mock document if not available
      if (typeof document === 'undefined') {
        (global as any).document = {
          createElement: jasmine.createSpy('createElement').and.returnValue({
            setAttribute: jasmine.createSpy('setAttribute')
          }),
          head: {
            appendChild: jasmine.createSpy('appendChild')
          },
          querySelector: jasmine.createSpy('querySelector').and.returnValue(null)
        };
      }

      service.applyMetaHeaders(productionSecurityConfig);

      // Verify meta tag creation was attempted
      if (typeof document !== 'undefined') {
        expect(document.createElement).toHaveBeenCalledWith('meta');
      }
    });
  });
});

/**
 * Integration Tests for Security Headers
 */
describe('SecurityHeadersService Integration Tests', () => {
  let service: SecurityHeadersService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });
    service = TestBed.inject(SecurityHeadersService);
  });

  it('should generate production-ready security headers', () => {
    const headers = service.generateSecurityHeaders(productionSecurityConfig);

    // Verify all critical security headers are present
    expect(headers['Content-Security-Policy']).toBeDefined();
    expect(headers['Strict-Transport-Security']).toBeDefined();
    expect(headers['X-Frame-Options']).toBe('DENY');
    expect(headers['X-Content-Type-Options']).toBe('nosniff');
    expect(headers['Cross-Origin-Opener-Policy']).toBe('same-origin');
    expect(headers['Cross-Origin-Embedder-Policy']).toBe('require-corp');

    // Verify CSP includes essential directives
    const csp = headers['Content-Security-Policy']!;
    expect(csp).toContain("default-src 'none'");
    expect(csp).toContain("script-src 'self'");
    expect(csp).toContain("object-src 'none'");
    expect(csp).toContain('upgrade-insecure-requests');
    expect(csp).toContain('block-all-mixed-content');
  });

  it('should validate complete CSP policy workflow', () => {
    // Generate CSP
    const csp = service.generateCSP(productionSecurityConfig);

    // Parse directives back from CSP string
    const directives: Partial<CSPDirectives> = {};
    csp.split(';').forEach(directive => {
      const [key, ...values] = directive.trim().split(' ');
      if (values.length > 0) {
        directives[key as keyof CSPDirectives] = values as string[];
      } else if (key) {
        // Boolean directive
        (directives as any)[key] = true;
      }
    });

    // Validate the generated directives
    const validation = service.validateCSP(directives);
    expect(validation.valid).toBe(true);
  });

  it('should provide actionable security recommendations', () => {
    const recommendations = service.getSecurityRecommendations(developmentSecurityConfig);

    expect(recommendations.length).toBeGreaterThan(0);
    expect(recommendations.every(r => typeof r === 'string')).toBe(true);
    expect(recommendations.every(r => r.length > 10)).toBe(true); // Non-trivial recommendations
  });
});