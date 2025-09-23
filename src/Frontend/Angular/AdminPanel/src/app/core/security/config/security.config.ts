import { SecurityConfig, SecurityLevel, Environment } from '../interfaces/security-headers.interface';

/**
 * Security Configuration for Different Environments
 * Implements OWASP security guidelines and industry best practices
 */

/**
 * Base security configuration with common settings
 */
const baseSecurityConfig: Partial<SecurityConfig> = {
  trustedDomains: {
    api: [
      'localhost:5001',
      'api.platformv1.com',
      '*.platformv1.com'
    ],
    cdn: [
      'cdn.jsdelivr.net',
      'fonts.googleapis.com',
      'fonts.gstatic.com'
    ],
    analytics: [
      'www.google-analytics.com',
      'analytics.google.com',
      'googletagmanager.com'
    ],
    scripts: [
      'accounts.google.com',
      'apis.google.com'
    ],
    fonts: [
      'fonts.googleapis.com',
      'fonts.gstatic.com'
    ],
    images: [
      'data:',
      'blob:',
      '*.platformv1.com',
      'www.gravatar.com'
    ]
  },
  permissions: {
    camera: 'none',
    microphone: 'none',
    geolocation: 'none',
    notifications: 'self',
    payment: 'none',
    usb: 'none',
    fullscreen: 'self'
  }
};

/**
 * Development environment security configuration
 * More permissive for development workflow
 */
export const developmentSecurityConfig: SecurityConfig = {
  level: 'permissive',
  environment: 'development',
  csp: {
    enabled: true,
    reportOnly: true, // Report-only mode for development
    useNonces: false, // Disabled for easier development
    reportUri: '/api/v1/security/csp-violations',
    directives: {
      'default-src': ["'self'"],
      'script-src': [
        "'self'",
        "'unsafe-inline'", // Allowed in development
        "'unsafe-eval'", // Allowed in development
        'localhost:*',
        '127.0.0.1:*',
        'accounts.google.com',
        'apis.google.com'
      ],
      'style-src': [
        "'self'",
        "'unsafe-inline'", // Allowed in development
        'fonts.googleapis.com'
      ],
      'font-src': [
        "'self'",
        'data:',
        'fonts.gstatic.com'
      ],
      'img-src': [
        "'self'",
        'data:',
        'blob:',
        'localhost:*',
        '127.0.0.1:*',
        'www.gravatar.com'
      ],
      'connect-src': [
        "'self'",
        'localhost:*',
        '127.0.0.1:*',
        'ws://localhost:*',
        'wss://localhost:*'
      ],
      'frame-src': [
        "'self'",
        'accounts.google.com'
      ],
      'object-src': ["'none'"],
      'base-uri': ["'self'"],
      'form-action': ["'self'"],
      'frame-ancestors': ["'none'"],
      'upgrade-insecure-requests': false // HTTP allowed in development
    }
  },
  headers: {
    enabled: true,
    custom: {
      'X-Frame-Options': 'SAMEORIGIN',
      'X-Content-Type-Options': 'nosniff',
      'Referrer-Policy': 'strict-origin-when-cross-origin',
      'X-XSS-Protection': '1; mode=block'
    }
  },
  ...baseSecurityConfig
};

/**
 * Staging environment security configuration
 * Balanced security for testing
 */
export const stagingSecurityConfig: SecurityConfig = {
  level: 'balanced',
  environment: 'staging',
  csp: {
    enabled: true,
    reportOnly: false,
    useNonces: true,
    reportUri: '/api/v1/security/csp-violations',
    directives: {
      'default-src': ["'self'"],
      'script-src': [
        "'self'",
        "'nonce-{NONCE}'", // Dynamic nonce
        'accounts.google.com',
        'apis.google.com'
      ],
      'style-src': [
        "'self'",
        "'nonce-{NONCE}'",
        'fonts.googleapis.com'
      ],
      'font-src': [
        "'self'",
        'data:',
        'fonts.gstatic.com'
      ],
      'img-src': [
        "'self'",
        'data:',
        'blob:',
        '*.platformv1-staging.com',
        'www.gravatar.com'
      ],
      'connect-src': [
        "'self'",
        'api.platformv1-staging.com',
        'wss://api.platformv1-staging.com'
      ],
      'frame-src': [
        "'self'",
        'accounts.google.com'
      ],
      'object-src': ["'none'"],
      'base-uri': ["'self'"],
      'form-action': ["'self'"],
      'frame-ancestors': ["'none'"],
      'upgrade-insecure-requests': true,
      'block-all-mixed-content': true
    }
  },
  headers: {
    enabled: true,
    custom: {
      'X-Frame-Options': 'DENY',
      'X-Content-Type-Options': 'nosniff',
      'Referrer-Policy': 'strict-origin-when-cross-origin',
      'X-XSS-Protection': '1; mode=block',
      'Strict-Transport-Security': 'max-age=31536000; includeSubDomains',
      'Cross-Origin-Opener-Policy': 'same-origin',
      'Cross-Origin-Embedder-Policy': 'require-corp',
      'Cross-Origin-Resource-Policy': 'same-site'
    }
  },
  ...baseSecurityConfig
};

/**
 * Production environment security configuration
 * Maximum security for production deployment
 */
export const productionSecurityConfig: SecurityConfig = {
  level: 'strict',
  environment: 'production',
  csp: {
    enabled: true,
    reportOnly: false,
    useNonces: true,
    reportUri: '/api/v1/security/csp-violations',
    directives: {
      'default-src': ["'none'"], // Most restrictive default
      'script-src': [
        "'self'",
        "'nonce-{NONCE}'", // Only nonce-based scripts
        'accounts.google.com',
        'apis.google.com'
      ],
      'style-src': [
        "'self'",
        "'nonce-{NONCE}'",
        'fonts.googleapis.com'
      ],
      'font-src': [
        "'self'",
        'fonts.gstatic.com'
      ],
      'img-src': [
        "'self'",
        'data:',
        'blob:',
        '*.platformv1.com',
        'www.gravatar.com'
      ],
      'connect-src': [
        "'self'",
        'api.platformv1.com',
        'wss://api.platformv1.com'
      ],
      'frame-src': [
        "'self'",
        'accounts.google.com'
      ],
      'worker-src': ["'self'"],
      'manifest-src': ["'self'"],
      'media-src': ["'self'"],
      'object-src': ["'none'"],
      'base-uri': ["'self'"],
      'form-action': ["'self'"],
      'frame-ancestors': ["'none'"],
      'upgrade-insecure-requests': true,
      'block-all-mixed-content': true,
      'require-sri-for': ['script', 'style'],
      'require-trusted-types-for': ["'script'"],
      'trusted-types': ['default', 'angular', 'angular#unsafe-inline']
    }
  },
  headers: {
    enabled: true,
    custom: {
      'X-Frame-Options': 'DENY',
      'X-Content-Type-Options': 'nosniff',
      'Referrer-Policy': 'strict-origin-when-cross-origin',
      'X-XSS-Protection': '1; mode=block',
      'Strict-Transport-Security': 'max-age=63072000; includeSubDomains; preload',
      'Permissions-Policy': [
        'camera=()',
        'microphone=()',
        'geolocation=()',
        'payment=()',
        'usb=()',
        'fullscreen=(self)',
        'notifications=(self)'
      ].join(', '),
      'Cross-Origin-Opener-Policy': 'same-origin',
      'Cross-Origin-Embedder-Policy': 'require-corp',
      'Cross-Origin-Resource-Policy': 'same-origin',
      'Origin-Agent-Cluster': '?1',
      'Server': '', // Remove server identification
      'X-Powered-By': '' // Remove X-Powered-By header
    }
  },
  permissions: {
    camera: 'none',
    microphone: 'none',
    geolocation: 'none',
    notifications: 'self',
    payment: 'none',
    usb: 'none',
    fullscreen: 'self'
  },
  ...baseSecurityConfig
};

/**
 * Get security configuration based on environment
 */
export function getSecurityConfig(environment: Environment): SecurityConfig {
  switch (environment) {
    case 'development':
      return developmentSecurityConfig;
    case 'staging':
      return stagingSecurityConfig;
    case 'production':
      return productionSecurityConfig;
    default:
      return developmentSecurityConfig;
  }
}

/**
 * Security configuration presets for quick setup
 */
export const securityPresets = {
  /** Minimal security for rapid development */
  minimal: {
    ...developmentSecurityConfig,
    csp: {
      ...developmentSecurityConfig.csp,
      enabled: false
    }
  },

  /** Enhanced security for testing */
  enhanced: {
    ...stagingSecurityConfig,
    level: 'strict' as SecurityLevel
  },

  /** Maximum security for high-security applications */
  maximum: {
    ...productionSecurityConfig,
    csp: {
      ...productionSecurityConfig.csp,
      directives: {
        ...productionSecurityConfig.csp.directives,
        'default-src': ["'none'"],
        'script-src': ["'self'"],
        'style-src': ["'self'"],
        'img-src': ["'self'", 'data:'],
        'connect-src': ["'self'"],
        'font-src': ["'self'"],
        'object-src': ["'none'"],
        'media-src': ["'none'"],
        'frame-src': ["'none'"]
      }
    }
  }
} as const;