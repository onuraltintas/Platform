/**
 * Enterprise Input Validation Interfaces
 * Implements OWASP input validation best practices and security standards
 */

import { AbstractControl, ValidationErrors } from '@angular/forms';
import { Observable } from 'rxjs';

export interface ValidationResult {
  /** Validation passed */
  valid: boolean;
  /** Error messages */
  errors: string[];
  /** Warnings (non-blocking) */
  warnings: string[];
  /** Sanitized value */
  sanitizedValue?: any;
  /** Security risk level */
  riskLevel: SecurityRiskLevel;
  /** Validation metadata */
  metadata?: ValidationMetadata;
}

export interface ValidationMetadata {
  /** Validator that performed the check */
  validator: string;
  /** Input type being validated */
  inputType: InputType;
  /** Validation timestamp */
  timestamp: number;
  /** Processing time in milliseconds */
  processingTime: number;
  /** Security context */
  securityContext: SecurityContext;
}

export type SecurityRiskLevel = 'none' | 'low' | 'medium' | 'high' | 'critical';

export type InputType =
  | 'email'
  | 'password'
  | 'text'
  | 'url'
  | 'phone'
  | 'html'
  | 'json'
  | 'sql'
  | 'filename'
  | 'number'
  | 'date'
  | 'credit_card'
  | 'ssn'
  | 'custom';

export type SecurityContext = 'public' | 'internal' | 'admin' | 'system';

export interface ValidationRule {
  /** Rule identifier */
  name: string;
  /** Rule description */
  description: string;
  /** Validation function */
  validator: CustomValidatorFn;
  /** Rule severity */
  severity: 'error' | 'warning' | 'info';
  /** Security implications */
  securityImpact: SecurityRiskLevel;
  /** Enable/disable rule */
  enabled: boolean;
  /** Rule configuration */
  config?: Record<string, any>;
}

export interface ValidationConfig {
  /** Enable validation */
  enabled: boolean;
  /** Validation mode */
  mode: ValidationMode;
  /** Security level */
  securityLevel: SecurityLevel;
  /** Input sanitization */
  sanitization: SanitizationConfig;
  /** Rate limiting */
  rateLimiting: RateLimitConfig;
  /** Real-time validation */
  realTimeValidation: boolean;
  /** Custom rules */
  customRules: ValidationRule[];
  /** Whitelisted patterns */
  whitelist: string[];
  /** Blacklisted patterns */
  blacklist: string[];
}

export interface SanitizationConfig {
  /** Enable HTML sanitization */
  html: boolean;
  /** Enable SQL injection protection */
  sql: boolean;
  /** Enable script tag removal */
  scripts: boolean;
  /** Enable URL sanitization */
  urls: boolean;
  /** Custom sanitization rules */
  custom: SanitizationRule[];
  /** Preserve whitespace */
  preserveWhitespace: boolean;
  /** Maximum input length */
  maxLength?: number;
}

export interface SanitizationRule {
  /** Rule name */
  name: string;
  /** Input pattern to match */
  pattern: RegExp;
  /** Replacement value */
  replacement: string;
  /** Apply globally */
  global: boolean;
}

export interface RateLimitConfig {
  /** Enable rate limiting */
  enabled: boolean;
  /** Requests per minute */
  requestsPerMinute: number;
  /** Burst limit */
  burstLimit: number;
  /** Lockout duration (minutes) */
  lockoutDuration: number;
}

export type ValidationMode = 'strict' | 'balanced' | 'permissive';
export type SecurityLevel = 'low' | 'medium' | 'high' | 'critical';

export interface CustomValidatorFn {
  (control: AbstractControl): ValidationErrors | null;
}

export interface AsyncValidatorFn {
  (control: AbstractControl): Promise<ValidationErrors | null> | Observable<ValidationErrors | null>;
}

export interface ValidationContext {
  /** Form control being validated */
  control: AbstractControl;
  /** Input type */
  inputType: InputType;
  /** Security context */
  securityContext: SecurityContext;
  /** User role/permissions */
  userRole?: string;
  /** Additional metadata */
  metadata?: Record<string, any>;
}

export interface ValidatorFactory {
  /** Create email validator */
  email(options?: EmailValidatorOptions): CustomValidatorFn;

  /** Create password validator */
  password(options?: PasswordValidatorOptions): CustomValidatorFn;

  /** Create URL validator */
  url(options?: UrlValidatorOptions): CustomValidatorFn;

  /** Create phone number validator */
  phone(options?: PhoneValidatorOptions): CustomValidatorFn;

  /** Create credit card validator */
  creditCard(options?: CreditCardValidatorOptions): CustomValidatorFn;

  /** Create filename validator */
  filename(options?: FilenameValidatorOptions): CustomValidatorFn;

  /** Create JSON validator */
  json(options?: JsonValidatorOptions): CustomValidatorFn;

  /** Create SQL injection validator */
  sqlInjection(options?: SqlValidatorOptions): CustomValidatorFn;

  /** Create XSS validator */
  xss(options?: XssValidatorOptions): CustomValidatorFn;

  /** Create custom pattern validator */
  pattern(pattern: RegExp, message?: string): CustomValidatorFn;

  /** Create length validator */
  length(min?: number, max?: number): CustomValidatorFn;

  /** Create whitelist validator */
  whitelist(allowedValues: string[]): CustomValidatorFn;

  /** Create blacklist validator */
  blacklist(forbiddenValues: string[]): CustomValidatorFn;
}

export interface EmailValidatorOptions {
  allowInternational?: boolean;
  allowSubdomains?: boolean;
  blockedDomains?: string[];
  requiredDomains?: string[];
  maxLength?: number;
}

export interface PasswordValidatorOptions {
  minLength?: number;
  maxLength?: number;
  requireUppercase?: boolean;
  requireLowercase?: boolean;
  requireNumbers?: boolean;
  requireSpecialChars?: boolean;
  forbiddenPatterns?: string[];
  checkCommonPasswords?: boolean;
  checkSequentialChars?: boolean;
  maxRepeatingChars?: number;
}

export interface UrlValidatorOptions {
  allowedProtocols?: string[];
  allowedDomains?: string[];
  blockedDomains?: string[];
  allowLocalhost?: boolean;
  allowIP?: boolean;
  maxLength?: number;
}

export interface PhoneValidatorOptions {
  countries?: string[];
  format?: 'international' | 'national' | 'e164' | 'rfc3966';
  allowExtensions?: boolean;
}

export interface CreditCardValidatorOptions {
  allowedTypes?: CreditCardType[];
  luhnCheck?: boolean;
}

export interface FilenameValidatorOptions {
  allowedExtensions?: string[];
  blockedExtensions?: string[];
  maxLength?: number;
  allowSpaces?: boolean;
  allowUnicode?: boolean;
}

export interface JsonValidatorOptions {
  maxDepth?: number;
  maxSize?: number;
  allowedKeys?: string[];
  forbiddenKeys?: string[];
}

export interface SqlValidatorOptions {
  blockKeywords?: string[];
  allowKeywords?: string[];
  checkFunctions?: boolean;
  checkComments?: boolean;
}

export interface XssValidatorOptions {
  allowedTags?: string[];
  allowedAttributes?: string[];
  stripTags?: boolean;
  encodeEntities?: boolean;
}

export type CreditCardType = 'visa' | 'mastercard' | 'amex' | 'discover' | 'diners' | 'jcb';

export interface ValidationService {
  /** Validate input value */
  validate(value: any, rules: ValidationRule[], context?: ValidationContext): Promise<ValidationResult>;

  /** Sanitize input value */
  sanitize(value: any, config: SanitizationConfig): string;

  /** Check for security threats */
  checkSecurity(value: any, context: SecurityContext): SecurityRiskLevel;

  /** Get validator factory */
  getValidatorFactory(): ValidatorFactory;

  /** Register custom validator */
  registerValidator(name: string, validator: CustomValidatorFn): void;

  /** Get validation config */
  getConfig(): ValidationConfig;

  /** Update validation config */
  updateConfig(config: Partial<ValidationConfig>): void;
}

export interface ValidationMetrics {
  /** Total validations performed */
  totalValidations: number;

  /** Failed validations */
  failedValidations: number;

  /** Security threats detected */
  securityThreats: number;

  /** Average processing time */
  averageProcessingTime: number;

  /** Most common errors */
  commonErrors: { error: string; count: number }[];

  /** Validation by input type */
  validationsByType: Record<InputType, number>;

  /** Risk level distribution */
  riskLevelDistribution: Record<SecurityRiskLevel, number>;
}

export interface FormSecurityEvent {
  /** Event type */
  type: 'validation_failed' | 'security_threat' | 'sanitization' | 'rate_limit';

  /** Event timestamp */
  timestamp: number;

  /** Input value (sanitized for logging) */
  inputValue: string;

  /** Input type */
  inputType: InputType;

  /** Security risk level */
  riskLevel: SecurityRiskLevel;

  /** Error details */
  error?: string;

  /** User context */
  userContext?: {
    userAgent?: string;
    ipAddress?: string;
    sessionId?: string;
  };
}