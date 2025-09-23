/**
 * Enterprise Input Validation Module
 * Exports all validation components, services, and utilities
 */

// Interfaces
export * from './interfaces/validation.interface';

// Services
export { ValidationService } from './services/validation.service';
export { SanitizationService } from './services/sanitization.service';

// Validators
export { CustomValidators } from './validators/custom-validators';

// Rules
export { SecurityValidationRules } from './rules/security-rules';

// Components
export { ValidationFeedbackComponent } from './components/validation-feedback.component';

// Directives
export { SecureInputDirective } from './directives/secure-input.directive';
export { SecureFormDirective } from './directives/secure-form.directive';

// Interceptors
export { FormSecurityInterceptor } from './interceptors/form-security.interceptor';

// Utility functions and constants
export const VALIDATION_CONFIG = {
  DEFAULT_DEBOUNCE_TIME: 300,
  DEFAULT_MAX_LENGTH: 10000,
  DEFAULT_SANITIZATION_CONFIG: {
    html: true,
    sql: true,
    scripts: true,
    urls: true,
    custom: [],
    preserveWhitespace: false,
    maxLength: 10000
  },
  SECURITY_RISK_LEVELS: ['none', 'low', 'medium', 'high', 'critical'] as const,
  INPUT_TYPES: [
    'email',
    'password',
    'text',
    'url',
    'phone',
    'html',
    'json',
    'sql',
    'filename',
    'number',
    'date',
    'credit_card',
    'ssn',
    'custom'
  ] as const,
  SECURITY_CONTEXTS: ['public', 'internal', 'admin', 'system'] as const
} as const;

// Type guards
export const isSecurityRiskLevel = (value: any): value is import('./interfaces/validation.interface').SecurityRiskLevel => {
  return VALIDATION_CONFIG.SECURITY_RISK_LEVELS.includes(value);
};

export const isInputType = (value: any): value is import('./interfaces/validation.interface').InputType => {
  return VALIDATION_CONFIG.INPUT_TYPES.includes(value);
};

export const isSecurityContext = (value: any): value is import('./interfaces/validation.interface').SecurityContext => {
  return VALIDATION_CONFIG.SECURITY_CONTEXTS.includes(value);
};

// Utility functions
export const createValidationRule = (
  name: string,
  description: string,
  validator: import('./interfaces/validation.interface').CustomValidatorFn,
  severity: 'error' | 'warning' | 'info' = 'error',
  securityImpact: import('./interfaces/validation.interface').SecurityRiskLevel = 'low',
  enabled = true,
  config?: Record<string, any>
): import('./interfaces/validation.interface').ValidationRule => ({
  name,
  description,
  validator,
  severity,
  securityImpact,
  enabled,
  config
});

export const escalateRiskLevel = (
  current: import('./interfaces/validation.interface').SecurityRiskLevel,
  newRisk: import('./interfaces/validation.interface').SecurityRiskLevel
): import('./interfaces/validation.interface').SecurityRiskLevel => {
  const levels = VALIDATION_CONFIG.SECURITY_RISK_LEVELS;
  const currentIndex = levels.indexOf(current);
  const newIndex = levels.indexOf(newRisk);
  return levels[Math.max(currentIndex, newIndex)];
};

export const sanitizeForLogging = (value: any, maxLength = 50): string => {
  if (!value) return '';

  const str = value.toString();
  const truncated = str.length > maxLength ? str.substring(0, maxLength) + '...' : str;

  // Remove potential sensitive data patterns
  return truncated
    .replace(/['"]/g, '')
    .replace(/<[^>]*>/g, '')
    .replace(/javascript:/gi, '')
    .replace(/data:/gi, '');
};

export const generateSecureId = (): string => {
  const array = new Uint8Array(16);
  crypto.getRandomValues(array);
  return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
};

export const isProductionEnvironment = (): boolean => {
  return typeof window !== 'undefined' &&
         window.location.hostname !== 'localhost' &&
         !window.location.hostname.startsWith('127.0.0.1');
};

// Validation helper functions
export const validateEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
};

export const validatePassword = (password: string, minLength = 8): boolean => {
  if (password.length < minLength) return false;

  const hasUppercase = /[A-Z]/.test(password);
  const hasLowercase = /[a-z]/.test(password);
  const hasNumbers = /\d/.test(password);
  const hasSpecialChars = /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?~`]/.test(password);

  return hasUppercase && hasLowercase && hasNumbers && hasSpecialChars;
};

export const validateUrl = (url: string): boolean => {
  try {
    const urlObj = new URL(url);
    return ['http:', 'https:'].includes(urlObj.protocol);
  } catch {
    return false;
  }
};

export const containsXSSPatterns = (value: string): boolean => {
  const xssPatterns = [
    /<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi,
    /javascript\s*:/gi,
    /on\w+\s*=/gi,
    /<iframe/gi,
    /data\s*:\s*text\/html/gi
  ];

  return xssPatterns.some(pattern => pattern.test(value));
};

export const containsSQLInjectionPatterns = (value: string): boolean => {
  const sqlPatterns = [
    /'\s*(or|and)\s*'?\d/gi,
    /union\s+select/gi,
    /insert\s+into/gi,
    /delete\s+from/gi,
    /drop\s+table/gi,
    /exec\s*\(/gi
  ];

  return sqlPatterns.some(pattern => pattern.test(value));
};

export const getPasswordStrength = (password: string): number => {
  let score = 0;

  if (password.length >= 8) score += 20;
  if (password.length >= 12) score += 10;
  if (password.length >= 16) score += 10;

  if (/[a-z]/.test(password)) score += 10;
  if (/[A-Z]/.test(password)) score += 10;
  if (/\d/.test(password)) score += 10;
  if (/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?~`]/.test(password)) score += 15;

  if (!/(.)\1{2,}/.test(password)) score += 10; // No repeating chars
  if (!/123|abc|qwe/i.test(password)) score += 15; // No sequences

  return Math.min(score, 100);
};

// Error message templates
export const ERROR_MESSAGES = {
  REQUIRED: 'This field is required',
  EMAIL_INVALID: 'Please enter a valid email address',
  EMAIL_TOO_LONG: 'Email address is too long',
  EMAIL_SECURITY: 'Email contains unsafe characters',
  EMAIL_DISPOSABLE: 'Disposable email addresses are not allowed',
  EMAIL_BLOCKED_DOMAIN: 'This email domain is not allowed',
  PASSWORD_TOO_SHORT: 'Password is too short',
  PASSWORD_TOO_LONG: 'Password is too long',
  PASSWORD_NO_UPPERCASE: 'Password must contain uppercase letters',
  PASSWORD_NO_LOWERCASE: 'Password must contain lowercase letters',
  PASSWORD_NO_NUMBERS: 'Password must contain numbers',
  PASSWORD_NO_SPECIAL_CHARS: 'Password must contain special characters',
  PASSWORD_SEQUENTIAL: 'Password contains sequential characters',
  PASSWORD_REPEATING: 'Password contains too many repeating characters',
  PASSWORD_COMMON: 'Password is too common',
  PASSWORD_PERSONAL_INFO: 'Password contains personal information',
  URL_INVALID: 'Please enter a valid URL',
  URL_TOO_LONG: 'URL is too long',
  URL_DANGEROUS: 'URL protocol is not allowed',
  URL_LOCALHOST: 'Localhost URLs are not allowed',
  URL_IP: 'IP addresses are not allowed',
  URL_BLOCKED: 'This domain is blocked',
  XSS_SCRIPT: 'Script tags are not allowed',
  XSS_EVENT_HANDLER: 'Event handlers are not allowed',
  XSS_JAVASCRIPT: 'JavaScript protocols are not allowed',
  XSS_HTML_TAGS: 'HTML tags are not allowed',
  SQL_KEYWORD: 'SQL keywords are not allowed',
  SQL_INJECTION: 'Potential SQL injection detected',
  SQL_FUNCTION: 'SQL functions are not allowed',
  SQL_COMMENT: 'SQL comments are not allowed',
  CREDIT_CARD_INVALID: 'Invalid credit card number',
  CREDIT_CARD_TYPE: 'Credit card type not allowed',
  CREDIT_CARD_LUHN: 'Credit card number failed validation',
  FILENAME_TOO_LONG: 'Filename is too long',
  FILENAME_DANGEROUS: 'Filename contains unsafe characters',
  FILENAME_TRAVERSAL: 'Path traversal detected in filename',
  FILENAME_SPACES: 'Spaces are not allowed in filename',
  FILENAME_UNICODE: 'Unicode characters are not allowed in filename',
  FILENAME_BLOCKED: 'File extension is not allowed',
  FILENAME_NOT_ALLOWED: 'File extension is not in allowed list',
  FILENAME_RESERVED: 'Filename is reserved',
  JSON_INVALID: 'Invalid JSON format',
  JSON_TOO_LARGE: 'JSON is too large',
  JSON_TOO_DEEP: 'JSON structure is too deep',
  JSON_FORBIDDEN_KEYS: 'JSON contains forbidden keys',
  JSON_INVALID_KEYS: 'JSON contains invalid keys',
  PHONE_TOO_SHORT: 'Phone number is too short',
  PHONE_INVALID: 'Invalid phone number format',
  PATTERN_MISMATCH: 'Input does not match required pattern',
  LENGTH_TOO_SHORT: 'Input is too short',
  LENGTH_TOO_LONG: 'Input is too long',
  WHITELIST_VIOLATION: 'Value is not in allowed list',
  BLACKLIST_VIOLATION: 'Value is not allowed',
  RATE_LIMIT_EXCEEDED: 'Too many requests. Please try again later.',
  SECURITY_THREAT_DETECTED: 'Security threat detected',
  BOT_ACTIVITY_DETECTED: 'Automated activity detected',
  CSRF_TOKEN_INVALID: 'Request authentication failed'
} as const;

// Pre-configured validation rule sets
export const COMMON_VALIDATION_RULES = {
  EMAIL: () => createValidationRule(
    'email-validation',
    'Email format and security validation',
    CustomValidators.email(),
    'error',
    'medium'
  ),

  STRONG_PASSWORD: () => createValidationRule(
    'strong-password',
    'Strong password requirements',
    CustomValidators.password({
      minLength: 12,
      requireUppercase: true,
      requireLowercase: true,
      requireNumbers: true,
      requireSpecialChars: true,
      checkCommonPasswords: true
    }),
    'error',
    'high'
  ),

  SECURE_URL: () => createValidationRule(
    'secure-url',
    'Secure URL validation',
    CustomValidators.url({
      allowedProtocols: ['https'],
      allowLocalhost: false,
      allowIP: false
    }),
    'error',
    'high'
  ),

  XSS_PROTECTION: () => createValidationRule(
    'xss-protection',
    'Cross-site scripting protection',
    CustomValidators.xss(),
    'error',
    'critical'
  ),

  SQL_INJECTION_PROTECTION: () => createValidationRule(
    'sql-injection-protection',
    'SQL injection protection',
    CustomValidators.sqlInjection(),
    'error',
    'critical'
  )
} as const;