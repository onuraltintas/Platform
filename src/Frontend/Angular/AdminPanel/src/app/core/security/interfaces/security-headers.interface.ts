/**
 * Security Headers Configuration Interfaces
 * Implements OWASP security header best practices
 */

export interface CSPDirectives {
  /** Controls which scripts can be executed */
  'script-src'?: string[];
  /** Controls which stylesheets can be loaded */
  'style-src'?: string[];
  /** Controls which images can be loaded */
  'img-src'?: string[];
  /** Controls which fonts can be loaded */
  'font-src'?: string[];
  /** Controls which resources can be connected to */
  'connect-src'?: string[];
  /** Controls which media elements can be loaded */
  'media-src'?: string[];
  /** Controls which objects can be embedded */
  'object-src'?: string[];
  /** Controls which frames can be embedded */
  'frame-src'?: string[];
  /** Controls which workers can be loaded */
  'worker-src'?: string[];
  /** Controls which manifests can be loaded */
  'manifest-src'?: string[];
  /** Fallback directive for resource types not covered by other directives */
  'default-src'?: string[];
  /** Controls which URLs can be loaded in forms */
  'form-action'?: string[];
  /** Controls which URLs can be used as frame ancestors */
  'frame-ancestors'?: string[];
  /** Controls which plugins can be loaded */
  'plugin-types'?: string[];
  /** Controls where violation reports are sent */
  'report-uri'?: string[];
  /** Controls where violation reports are sent (newer syntax) */
  'report-to'?: string[];
  /** Controls navigation to URLs */
  'navigate-to'?: string[];
  /** Controls which URLs can be prefetched */
  'prefetch-src'?: string[];
  /** Base URI for relative URLs */
  'base-uri'?: string[];
  /** Controls which URLs can be loaded by child workers */
  'child-src'?: string[];
  /** Enables CSP sandbox */
  'sandbox'?: string[];
  /** Requires TLS for all requests */
  'upgrade-insecure-requests'?: boolean;
  /** Prevents MIME type sniffing */
  'block-all-mixed-content'?: boolean;
  /** Requires SRI for scripts and styles */
  'require-sri-for'?: string[];
  /** Enables trusted types */
  'require-trusted-types-for'?: string[];
  /** Defines trusted types policy */
  'trusted-types'?: string[];
}

export interface SecurityHeaders {
  /** Content Security Policy */
  'Content-Security-Policy'?: string;
  /** CSP Report Only mode */
  'Content-Security-Policy-Report-Only'?: string;
  /** Prevents MIME type sniffing */
  'X-Content-Type-Options'?: 'nosniff';
  /** Controls how much referrer information is included */
  'Referrer-Policy'?: ReferrerPolicyValue;
  /** Controls if site can be framed */
  'X-Frame-Options'?: 'DENY' | 'SAMEORIGIN' | string;
  /** Enables XSS filtering */
  'X-XSS-Protection'?: '0' | '1' | '1; mode=block' | string;
  /** HTTP Strict Transport Security */
  'Strict-Transport-Security'?: string;
  /** Permissions policy */
  'Permissions-Policy'?: string;
  /** Cross-Origin Embedder Policy */
  'Cross-Origin-Embedder-Policy'?: 'unsafe-none' | 'require-corp' | 'credentialless';
  /** Cross-Origin Opener Policy */
  'Cross-Origin-Opener-Policy'?: 'unsafe-none' | 'same-origin-allow-popups' | 'same-origin';
  /** Cross-Origin Resource Policy */
  'Cross-Origin-Resource-Policy'?: 'same-site' | 'same-origin' | 'cross-origin';
  /** Origin Agent Cluster */
  'Origin-Agent-Cluster'?: '?1';
  /** Server identification removal */
  'Server'?: string;
  /** X-Powered-By header removal */
  'X-Powered-By'?: string;
}

export type ReferrerPolicyValue =
  | 'no-referrer'
  | 'no-referrer-when-downgrade'
  | 'origin'
  | 'origin-when-cross-origin'
  | 'same-origin'
  | 'strict-origin'
  | 'strict-origin-when-cross-origin'
  | 'unsafe-url';

export type SecurityLevel = 'strict' | 'balanced' | 'permissive';

export type Environment = 'development' | 'staging' | 'production';

export interface SecurityConfig {
  /** Security level affects which policies are applied */
  level: SecurityLevel;
  /** Environment affects specific configurations */
  environment: Environment;
  /** CSP directives configuration */
  csp: {
    /** Enable CSP */
    enabled: boolean;
    /** Use report-only mode */
    reportOnly: boolean;
    /** Custom directives */
    directives: Partial<CSPDirectives>;
    /** Nonce generation for inline scripts/styles */
    useNonces: boolean;
    /** Report violations to this endpoint */
    reportUri?: string;
  };
  /** Security headers configuration */
  headers: {
    /** Enable security headers */
    enabled: boolean;
    /** Custom headers */
    custom: Partial<SecurityHeaders>;
  };
  /** Trusted domains for various resources */
  trustedDomains: {
    /** Domains for API calls */
    api: string[];
    /** Domains for CDN resources */
    cdn: string[];
    /** Domains for analytics */
    analytics: string[];
    /** Domains for external scripts */
    scripts: string[];
    /** Domains for fonts */
    fonts: string[];
    /** Domains for images */
    images: string[];
  };
  /** Feature policies */
  permissions: {
    /** Camera access */
    camera: 'self' | 'none' | string[];
    /** Microphone access */
    microphone: 'self' | 'none' | string[];
    /** Geolocation access */
    geolocation: 'self' | 'none' | string[];
    /** Notifications */
    notifications: 'self' | 'none' | string[];
    /** Payment request */
    payment: 'self' | 'none' | string[];
    /** USB access */
    usb: 'self' | 'none' | string[];
    /** Fullscreen */
    fullscreen: 'self' | 'none' | string[];
  };
}

export interface CSPViolationReport {
  /** Document URI where violation occurred */
  'document-uri': string;
  /** Referrer of the document */
  referrer: string;
  /** Blocked URI */
  'blocked-uri': string;
  /** Violated directive */
  'violated-directive': string;
  /** Effective directive */
  'effective-directive': string;
  /** Original policy */
  'original-policy': string;
  /** Disposition (enforce or report) */
  disposition: 'enforce' | 'report';
  /** Status code */
  'status-code': number;
  /** Script sample */
  'script-sample'?: string;
  /** Line number */
  'line-number'?: number;
  /** Column number */
  'column-number'?: number;
  /** Source file */
  'source-file'?: string;
}

export interface SecurityHeadersService {
  /** Generate CSP header value */
  generateCSP(config: SecurityConfig): string;

  /** Generate all security headers */
  generateSecurityHeaders(config: SecurityConfig): SecurityHeaders;

  /** Validate CSP configuration */
  validateCSP(directives: Partial<CSPDirectives>): { valid: boolean; errors: string[] };

  /** Generate nonce for inline scripts/styles */
  generateNonce(): string;

  /** Report CSP violation */
  reportViolation(report: CSPViolationReport): Promise<void>;

  /** Get security recommendations */
  getSecurityRecommendations(currentConfig: SecurityConfig): string[];
}