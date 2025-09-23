import { ValidationRule, InputType, SecurityContext } from '../interfaces/validation.interface';
import { CustomValidators } from '../validators/custom-validators';

/**
 * Enterprise Security Validation Rules
 * Predefined validation rules focused on security
 */
export class SecurityValidationRules {

  /**
   * Get default security rules for different input types
   */
  static getDefaultRules(inputType: InputType, securityContext: SecurityContext = 'public'): ValidationRule[] {
    const baseRules = this.getBaseSecurityRules();
    const typeSpecificRules = this.getTypeSpecificRules(inputType);
    const contextSpecificRules = this.getContextSpecificRules(securityContext);

    return [...baseRules, ...typeSpecificRules, ...contextSpecificRules];
  }

  /**
   * Base security rules applied to all inputs
   */
  private static getBaseSecurityRules(): ValidationRule[] {
    return [
      {
        name: 'xss-protection',
        description: 'Prevents Cross-Site Scripting (XSS) attacks',
        validator: CustomValidators.xss({
          allowedTags: [],
          stripTags: true,
          encodeEntities: true
        }),
        severity: 'error',
        securityImpact: 'high',
        enabled: true
      },
      {
        name: 'sql-injection-protection',
        description: 'Prevents SQL injection attacks',
        validator: CustomValidators.sqlInjection({
          blockKeywords: [
            'select', 'insert', 'update', 'delete', 'drop', 'create',
            'alter', 'execute', 'union', 'declare', 'exec', 'sp_',
            'xp_', 'merge', 'truncate', 'grant', 'revoke'
          ],
          checkFunctions: true,
          checkComments: true
        }),
        severity: 'error',
        securityImpact: 'critical',
        enabled: true
      },
      {
        name: 'command-injection-protection',
        description: 'Prevents command injection attacks',
        validator: CustomValidators.securePattern(
          /[;&|`$<>]/,
          'Command injection characters detected'
        ),
        severity: 'error',
        securityImpact: 'critical',
        enabled: true
      },
      {
        name: 'path-traversal-protection',
        description: 'Prevents path traversal attacks',
        validator: CustomValidators.securePattern(
          /\.\.[\/\\]/,
          'Path traversal patterns detected'
        ),
        severity: 'error',
        securityImpact: 'high',
        enabled: true
      },
      {
        name: 'null-byte-protection',
        description: 'Prevents null byte injection',
        validator: CustomValidators.securePattern(
          /\x00/,
          'Null bytes are not allowed'
        ),
        severity: 'error',
        securityImpact: 'high',
        enabled: true
      }
    ];
  }

  /**
   * Input type specific security rules
   */
  private static getTypeSpecificRules(inputType: InputType): ValidationRule[] {
    switch (inputType) {
      case 'email':
        return this.getEmailSecurityRules();
      case 'password':
        return this.getPasswordSecurityRules();
      case 'url':
        return this.getUrlSecurityRules();
      case 'phone':
        return this.getPhoneSecurityRules();
      case 'filename':
        return this.getFilenameSecurityRules();
      case 'html':
        return this.getHtmlSecurityRules();
      case 'json':
        return this.getJsonSecurityRules();
      case 'credit_card':
        return this.getCreditCardSecurityRules();
      case 'ssn':
        return this.getSsnSecurityRules();
      default:
        return this.getTextSecurityRules();
    }
  }

  /**
   * Security context specific rules
   */
  private static getContextSpecificRules(securityContext: SecurityContext): ValidationRule[] {
    switch (securityContext) {
      case 'admin':
        return this.getAdminSecurityRules();
      case 'system':
        return this.getSystemSecurityRules();
      case 'internal':
        return this.getInternalSecurityRules();
      default:
        return this.getPublicSecurityRules();
    }
  }

  // Email specific security rules
  private static getEmailSecurityRules(): ValidationRule[] {
    return [
      {
        name: 'email-format-validation',
        description: 'Validates email format and security',
        validator: CustomValidators.email({
          allowInternational: true,
          allowSubdomains: true,
          blockedDomains: [
            '10minutemail.com',
            'tempmail.org',
            'guerrillamail.com',
            'mailinator.com',
            'yopmail.com',
            'throwaway.email',
            'spam4.me',
            'getnada.com'
          ],
          maxLength: 320
        }),
        severity: 'error',
        securityImpact: 'medium',
        enabled: true
      },
      {
        name: 'email-domain-security',
        description: 'Checks for suspicious email domains',
        validator: CustomValidators.securePattern(
          /\.(tk|ml|cf|ga)$/i,
          'Suspicious domain detected'
        ),
        severity: 'warning',
        securityImpact: 'low',
        enabled: true
      }
    ];
  }

  // Password specific security rules
  private static getPasswordSecurityRules(): ValidationRule[] {
    return [
      {
        name: 'password-strength-validation',
        description: 'Enforces strong password requirements',
        validator: CustomValidators.password({
          minLength: 12,
          maxLength: 128,
          requireUppercase: true,
          requireLowercase: true,
          requireNumbers: true,
          requireSpecialChars: true,
          forbiddenPatterns: [
            'password', 'admin', 'user', 'root', 'guest',
            '123456', 'qwerty', 'abc123', 'password123'
          ],
          checkCommonPasswords: true,
          checkSequentialChars: true,
          maxRepeatingChars: 2
        }),
        severity: 'error',
        securityImpact: 'high',
        enabled: true
      },
      {
        name: 'password-personal-info-check',
        description: 'Prevents use of personal information in passwords',
        validator: CustomValidators.securePattern(
          /\b(admin|administrator|root|user|guest|password|pass|pwd)\b/i,
          'Password contains common or personal information'
        ),
        severity: 'warning',
        securityImpact: 'medium',
        enabled: true
      }
    ];
  }

  // URL specific security rules
  private static getUrlSecurityRules(): ValidationRule[] {
    return [
      {
        name: 'url-security-validation',
        description: 'Validates URL security and format',
        validator: CustomValidators.url({
          allowedProtocols: ['http', 'https'],
          blockedDomains: [
            'localhost',
            '127.0.0.1',
            '0.0.0.0',
            'internal',
            'admin',
            'api.internal'
          ],
          allowLocalhost: false,
          allowIP: false,
          maxLength: 2048
        }),
        severity: 'error',
        securityImpact: 'high',
        enabled: true
      },
      {
        name: 'url-shortener-detection',
        description: 'Detects potentially malicious URL shorteners',
        validator: CustomValidators.securePattern(
          /\b(bit\.ly|tinyurl|t\.co|short\.link|ow\.ly|is\.gd|buff\.ly)\b/i,
          'URL shorteners may pose security risks'
        ),
        severity: 'warning',
        securityImpact: 'medium',
        enabled: true
      }
    ];
  }

  // Phone specific security rules
  private static getPhoneSecurityRules(): ValidationRule[] {
    return [
      {
        name: 'phone-format-validation',
        description: 'Validates phone number format and security',
        validator: CustomValidators.phone({
          countries: ['US', 'TR', 'GB', 'DE', 'FR'],
          format: 'international',
          allowExtensions: false
        }),
        severity: 'error',
        securityImpact: 'low',
        enabled: true
      }
    ];
  }

  // Filename specific security rules
  private static getFilenameSecurityRules(): ValidationRule[] {
    return [
      {
        name: 'filename-security-validation',
        description: 'Validates filename security and format',
        validator: CustomValidators.filename({
          allowedExtensions: [
            'jpg', 'jpeg', 'png', 'gif', 'webp', 'svg',
            'pdf', 'doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx',
            'txt', 'csv', 'json', 'xml', 'zip', 'rar'
          ],
          blockedExtensions: [
            'exe', 'bat', 'cmd', 'scr', 'pif', 'com', 'vbs',
            'js', 'jar', 'app', 'deb', 'pkg', 'dmg', 'msi',
            'php', 'asp', 'jsp', 'py', 'rb', 'pl', 'sh'
          ],
          maxLength: 255,
          allowSpaces: false,
          allowUnicode: false
        }),
        severity: 'error',
        securityImpact: 'high',
        enabled: true
      }
    ];
  }

  // HTML specific security rules
  private static getHtmlSecurityRules(): ValidationRule[] {
    return [
      {
        name: 'html-sanitization',
        description: 'Sanitizes HTML content for security',
        validator: CustomValidators.xss({
          allowedTags: ['p', 'br', 'strong', 'em', 'u', 'i', 'b'],
          allowedAttributes: [],
          stripTags: true,
          encodeEntities: true
        }),
        severity: 'error',
        securityImpact: 'critical',
        enabled: true
      }
    ];
  }

  // JSON specific security rules
  private static getJsonSecurityRules(): ValidationRule[] {
    return [
      {
        name: 'json-security-validation',
        description: 'Validates JSON structure and security',
        validator: CustomValidators.json({
          maxDepth: 5,
          maxSize: 1024 * 100, // 100KB
          forbiddenKeys: [
            '__proto__',
            'constructor',
            'prototype',
            'eval',
            'function',
            'require',
            'import',
            'export'
          ]
        }),
        severity: 'error',
        securityImpact: 'high',
        enabled: true
      }
    ];
  }

  // Credit card specific security rules
  private static getCreditCardSecurityRules(): ValidationRule[] {
    return [
      {
        name: 'credit-card-validation',
        description: 'Validates credit card format with Luhn check',
        validator: CustomValidators.creditCard({
          allowedTypes: ['visa', 'mastercard', 'amex'],
          luhnCheck: true
        }),
        severity: 'error',
        securityImpact: 'high',
        enabled: true
      }
    ];
  }

  // SSN specific security rules
  private static getSsnSecurityRules(): ValidationRule[] {
    return [
      {
        name: 'ssn-format-validation',
        description: 'Validates SSN format',
        validator: CustomValidators.securePattern(
          /^\d{3}-?\d{2}-?\d{4}$/,
          'Invalid SSN format'
        ),
        severity: 'error',
        securityImpact: 'critical',
        enabled: true
      }
    ];
  }

  // Text specific security rules
  private static getTextSecurityRules(): ValidationRule[] {
    return [
      {
        name: 'text-length-limit',
        description: 'Limits text length for security',
        validator: CustomValidators.securePattern(
          /^.{0,10000}$/s,
          'Text exceeds maximum allowed length'
        ),
        severity: 'error',
        securityImpact: 'low',
        enabled: true
      },
      {
        name: 'control-character-check',
        description: 'Prevents control characters',
        validator: CustomValidators.securePattern(
          /[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]/,
          'Control characters are not allowed'
        ),
        severity: 'error',
        securityImpact: 'medium',
        enabled: true
      }
    ];
  }

  // Public context security rules
  private static getPublicSecurityRules(): ValidationRule[] {
    return [
      {
        name: 'public-content-filter',
        description: 'Filters content appropriate for public context',
        validator: CustomValidators.securePattern(
          /\b(password|secret|token|key|api|private)\b/i,
          'Potentially sensitive information detected'
        ),
        severity: 'warning',
        securityImpact: 'medium',
        enabled: true
      }
    ];
  }

  // Internal context security rules
  private static getInternalSecurityRules(): ValidationRule[] {
    return [
      {
        name: 'internal-data-validation',
        description: 'Additional validation for internal data',
        validator: CustomValidators.securePattern(
          /\b(admin|root|system|internal)\b/i,
          'Internal system references detected'
        ),
        severity: 'warning',
        securityImpact: 'low',
        enabled: true
      }
    ];
  }

  // Admin context security rules
  private static getAdminSecurityRules(): ValidationRule[] {
    return [
      {
        name: 'admin-privilege-check',
        description: 'Enhanced validation for admin context',
        validator: CustomValidators.securePattern(
          /[<>'";&|`$]/,
          'Special characters require additional validation in admin context'
        ),
        severity: 'error',
        securityImpact: 'high',
        enabled: true
      },
      {
        name: 'admin-command-protection',
        description: 'Prevents command execution in admin context',
        validator: CustomValidators.securePattern(
          /\b(exec|eval|system|shell|cmd|powershell|bash)\b/i,
          'Command execution keywords detected'
        ),
        severity: 'error',
        securityImpact: 'critical',
        enabled: true
      }
    ];
  }

  // System context security rules
  private static getSystemSecurityRules(): ValidationRule[] {
    return [
      {
        name: 'system-level-protection',
        description: 'Maximum security for system-level operations',
        validator: CustomValidators.securePattern(
          /[^\w\s\-._]/,
          'Only alphanumeric characters, spaces, hyphens, dots, and underscores allowed'
        ),
        severity: 'error',
        securityImpact: 'critical',
        enabled: true
      },
      {
        name: 'system-injection-protection',
        description: 'Prevents any form of injection in system context',
        validator: CustomValidators.securePattern(
          /[(){}[\];:,]/,
          'Structural characters not allowed in system context'
        ),
        severity: 'error',
        securityImpact: 'critical',
        enabled: true
      }
    ];
  }

  /**
   * Get OWASP Top 10 focused validation rules
   */
  static getOWASPValidationRules(): ValidationRule[] {
    return [
      // A03:2021 – Injection
      {
        name: 'owasp-injection-protection',
        description: 'OWASP A03: Injection protection',
        validator: CustomValidators.sqlInjection({
          blockKeywords: [
            'select', 'insert', 'update', 'delete', 'drop', 'create',
            'alter', 'execute', 'union', 'declare', 'exec'
          ]
        }),
        severity: 'error',
        securityImpact: 'critical',
        enabled: true
      },

      // A03:2021 – Cross-Site Scripting (XSS)
      {
        name: 'owasp-xss-protection',
        description: 'OWASP A03: Cross-Site Scripting protection',
        validator: CustomValidators.xss({
          allowedTags: [],
          stripTags: true,
          encodeEntities: true
        }),
        severity: 'error',
        securityImpact: 'high',
        enabled: true
      },

      // A07:2021 – Identification and Authentication Failures
      {
        name: 'owasp-weak-password-protection',
        description: 'OWASP A07: Weak password protection',
        validator: CustomValidators.password({
          minLength: 12,
          requireUppercase: true,
          requireLowercase: true,
          requireNumbers: true,
          requireSpecialChars: true,
          checkCommonPasswords: true
        }),
        severity: 'error',
        securityImpact: 'high',
        enabled: true
      },

      // A01:2021 – Broken Access Control
      {
        name: 'owasp-path-traversal-protection',
        description: 'OWASP A01: Path traversal protection',
        validator: CustomValidators.securePattern(
          /\.\.[\/\\]/,
          'Path traversal detected'
        ),
        severity: 'error',
        securityImpact: 'high',
        enabled: true
      },

      // A06:2021 – Vulnerable and Outdated Components
      {
        name: 'owasp-file-upload-protection',
        description: 'OWASP A06: File upload security',
        validator: CustomValidators.filename({
          blockedExtensions: [
            'exe', 'bat', 'cmd', 'scr', 'pif', 'com', 'vbs',
            'js', 'php', 'asp', 'jsp', 'py', 'rb', 'pl'
          ]
        }),
        severity: 'error',
        securityImpact: 'high',
        enabled: true
      }
    ];
  }

  /**
   * Get PCI DSS compliance validation rules
   */
  static getPCIDSSValidationRules(): ValidationRule[] {
    return [
      {
        name: 'pci-credit-card-validation',
        description: 'PCI DSS: Credit card validation',
        validator: CustomValidators.creditCard({
          allowedTypes: ['visa', 'mastercard', 'amex', 'discover'],
          luhnCheck: true
        }),
        severity: 'error',
        securityImpact: 'critical',
        enabled: true
      },
      {
        name: 'pci-data-protection',
        description: 'PCI DSS: Sensitive data protection',
        validator: CustomValidators.securePattern(
          /\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b/,
          'Credit card numbers must be properly protected'
        ),
        severity: 'error',
        securityImpact: 'critical',
        enabled: true
      }
    ];
  }

  /**
   * Get GDPR compliance validation rules
   */
  static getGDPRValidationRules(): ValidationRule[] {
    return [
      {
        name: 'gdpr-data-minimization',
        description: 'GDPR: Data minimization principle',
        validator: CustomValidators.securePattern(
          /^.{0,1000}$/s,
          'Input exceeds data minimization requirements'
        ),
        severity: 'warning',
        securityImpact: 'low',
        enabled: true
      },
      {
        name: 'gdpr-personal-data-protection',
        description: 'GDPR: Personal data protection',
        validator: CustomValidators.securePattern(
          /\b\d{3}-?\d{2}-?\d{4}\b/,
          'Personal identifiers require special handling under GDPR'
        ),
        severity: 'warning',
        securityImpact: 'medium',
        enabled: true
      }
    ];
  }
}