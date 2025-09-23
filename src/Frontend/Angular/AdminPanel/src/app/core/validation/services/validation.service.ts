import { Injectable, signal, computed, effect } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { Observable, of, catchError, map, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  ValidationResult,
  ValidationRule,
  ValidationContext,
  ValidationConfig,
  ValidationMetrics,
  FormSecurityEvent,
  SecurityRiskLevel,
  InputType,
  SecurityContext,
  CustomValidatorFn,
  ValidationService as IValidationService
} from '../interfaces/validation.interface';
import { SanitizationService } from './sanitization.service';
import { CustomValidators } from '../validators/custom-validators';

/**
 * Enterprise Validation Service
 * Implements comprehensive validation with security monitoring
 */
@Injectable({
  providedIn: 'root'
})
export class ValidationService implements IValidationService {

  private readonly configSignal = signal<ValidationConfig>({
    enabled: true,
    mode: 'balanced',
    securityLevel: 'medium',
    sanitization: {
      html: true,
      sql: true,
      scripts: true,
      urls: true,
      custom: [],
      preserveWhitespace: false,
      maxLength: 10000
    },
    rateLimiting: {
      enabled: true,
      requestsPerMinute: 60,
      burstLimit: 10,
      lockoutDuration: 5
    },
    realTimeValidation: true,
    customRules: [],
    whitelist: [],
    blacklist: []
  });

  private readonly metricsSignal = signal<ValidationMetrics>({
    totalValidations: 0,
    failedValidations: 0,
    securityThreats: 0,
    averageProcessingTime: 0,
    commonErrors: [],
    validationsByType: {} as Record<InputType, number>,
    riskLevelDistribution: {
      none: 0,
      low: 0,
      medium: 0,
      high: 0,
      critical: 0
    }
  });

  private readonly customValidators = new Map<string, CustomValidatorFn>();
  private readonly rateLimitMap = new Map<string, { count: number; lastReset: number; locked: boolean }>();
  private readonly securityEvents: FormSecurityEvent[] = [];

  // Computed signals
  readonly config = computed(() => this.configSignal());
  readonly metrics = computed(() => this.metricsSignal());
  readonly isHighSecurity = computed(() =>
    this.config().securityLevel === 'high' || this.config().securityLevel === 'critical'
  );

  constructor(private sanitizationService: SanitizationService) {
    // Initialize default security rules based on environment
    this.initializeSecurityRules();

    // Setup metrics monitoring
    effect(() => {
      const currentMetrics = this.metrics();
      if (currentMetrics.securityThreats > 0) {
        console.warn(`Security threats detected: ${currentMetrics.securityThreats}`);
      }
    });
  }

  /**
   * Validate input value with comprehensive security checks
   */
  async validate(
    value: any,
    rules: ValidationRule[],
    context?: ValidationContext
  ): Promise<ValidationResult> {
    const startTime = performance.now();

    try {
      // Rate limiting check
      if (context && !this.checkRateLimit(context)) {
        return {
          valid: false,
          errors: ['Rate limit exceeded. Please try again later.'],
          warnings: [],
          riskLevel: 'high',
          metadata: {
            validator: 'rate-limiter',
            inputType: context.inputType,
            timestamp: Date.now(),
            processingTime: performance.now() - startTime,
            securityContext: context.securityContext
          }
        };
      }

      const config = this.config();
      const result: ValidationResult = {
        valid: true,
        errors: [],
        warnings: [],
        riskLevel: 'none',
        metadata: {
          validator: 'validation-service',
          inputType: context?.inputType || 'text',
          timestamp: Date.now(),
          processingTime: 0,
          securityContext: context?.securityContext || 'public'
        }
      };

      // Sanitization first
      if (config.sanitization && context?.inputType) {
        const sanitizationResult = this.sanitizationService.sanitize(
          value,
          context.inputType,
          config.sanitization
        );

        result.sanitizedValue = sanitizationResult.sanitizedValue;
        result.warnings.push(...sanitizationResult.warnings);
        result.riskLevel = this.escalateRisk(result.riskLevel, sanitizationResult.riskLevel);
      }

      // Apply validation rules
      const valuesToValidate = result.sanitizedValue !== undefined ? result.sanitizedValue : value;

      for (const rule of rules) {
        if (!rule.enabled) continue;

        try {
          const ruleResult = rule.validator(
            context?.control || ({ value: valuesToValidate } as AbstractControl)
          );

          if (ruleResult) {
            const errorMessages = Object.keys(ruleResult).map(key =>
              this.getErrorMessage(key, ruleResult[key], rule)
            );

            if (rule.severity === 'error') {
              result.valid = false;
              result.errors.push(...errorMessages);
            } else if (rule.severity === 'warning') {
              result.warnings.push(...errorMessages);
            }

            result.riskLevel = this.escalateRisk(result.riskLevel, rule.securityImpact);
          }
        } catch (error) {
          console.error(`Validation rule ${rule.name} failed:`, error);
          result.warnings.push(`Validation rule ${rule.name} encountered an error`);
        }
      }

      // Security context validation
      if (context) {
        const securityResult = this.validateSecurityContext(valuesToValidate, context);
        result.valid = result.valid && securityResult.valid;
        result.errors.push(...securityResult.errors);
        result.warnings.push(...securityResult.warnings);
        result.riskLevel = this.escalateRisk(result.riskLevel, securityResult.riskLevel);
      }

      // Update metrics
      const processingTime = performance.now() - startTime;
      result.metadata!.processingTime = processingTime;
      this.updateMetrics(result, context);

      // Log security events
      if (result.riskLevel !== 'none' || !result.valid) {
        this.logSecurityEvent(result, context, valuesToValidate);
      }

      return result;

    } catch (error) {
      console.error('Validation service error:', error);
      return {
        valid: false,
        errors: ['Validation service encountered an internal error'],
        warnings: [],
        riskLevel: 'medium',
        metadata: {
          validator: 'validation-service',
          inputType: context?.inputType || 'text',
          timestamp: Date.now(),
          processingTime: performance.now() - startTime,
          securityContext: context?.securityContext || 'public'
        }
      };
    }
  }

  /**
   * Sanitize input value
   */
  sanitize(value: any, config: any): string {
    if (!value) return '';

    const sanitizationResult = this.sanitizationService.sanitize(
      value,
      'text',
      config
    );

    return sanitizationResult.sanitizedValue;
  }

  /**
   * Check for security threats
   */
  checkSecurity(value: any, context: SecurityContext): SecurityRiskLevel {
    if (!value) return 'none';

    let maxRisk: SecurityRiskLevel = 'none';
    const stringValue = value.toString();

    // XSS patterns
    const xssPatterns = [
      /<script/i,
      /javascript:/i,
      /on\w+\s*=/i,
      /<iframe/i,
      /data:\s*text\/html/i
    ];

    for (const pattern of xssPatterns) {
      if (pattern.test(stringValue)) {
        maxRisk = this.escalateRisk(maxRisk, 'high');
        break;
      }
    }

    // SQL injection patterns
    const sqlPatterns = [
      /'\s*(or|and)\s*'?\d/i,
      /union\s+select/i,
      /insert\s+into/i,
      /delete\s+from/i,
      /drop\s+table/i
    ];

    for (const pattern of sqlPatterns) {
      if (pattern.test(stringValue)) {
        maxRisk = this.escalateRisk(maxRisk, 'critical');
        break;
      }
    }

    // Path traversal
    if (stringValue.includes('../') || stringValue.includes('..\\')) {
      maxRisk = this.escalateRisk(maxRisk, 'high');
    }

    // Command injection
    const commandPatterns = [
      /[;&|`$]/,
      /\|\s*\w/,
      /&&\s*\w/,
      /;\s*\w/
    ];

    for (const pattern of commandPatterns) {
      if (pattern.test(stringValue)) {
        maxRisk = this.escalateRisk(maxRisk, context === 'system' ? 'critical' : 'high');
        break;
      }
    }

    return maxRisk;
  }

  /**
   * Get validator factory
   */
  getValidatorFactory() {
    return {
      email: (options?: any) => CustomValidators.email(options),
      password: (options?: any) => CustomValidators.password(options),
      url: (options?: any) => CustomValidators.url(options),
      phone: (options?: any) => CustomValidators.phone(options),
      creditCard: (options?: any) => CustomValidators.creditCard(options),
      filename: (options?: any) => CustomValidators.filename(options),
      json: (options?: any) => CustomValidators.json(options),
      sqlInjection: (options?: any) => CustomValidators.sqlInjection(options),
      xss: (options?: any) => CustomValidators.xss(options),
      pattern: (pattern: RegExp, message?: string) => CustomValidators.securePattern(pattern, message),
      length: (min?: number, max?: number) => this.createLengthValidator(min, max),
      whitelist: (allowedValues: string[]) => this.createWhitelistValidator(allowedValues),
      blacklist: (forbiddenValues: string[]) => this.createBlacklistValidator(forbiddenValues)
    };
  }

  /**
   * Register custom validator
   */
  registerValidator(name: string, validator: CustomValidatorFn): void {
    this.customValidators.set(name, validator);
  }

  /**
   * Get validation config
   */
  getConfig(): ValidationConfig {
    return this.config();
  }

  /**
   * Update validation config
   */
  updateConfig(config: Partial<ValidationConfig>): void {
    this.configSignal.update(current => ({ ...current, ...config }));
  }

  /**
   * Get validation metrics
   */
  getValidationMetrics(): ValidationMetrics {
    return this.metrics();
  }

  /**
   * Get security events
   */
  getSecurityEvents(limit = 100): FormSecurityEvent[] {
    return this.securityEvents.slice(-limit);
  }

  /**
   * Clear security events
   */
  clearSecurityEvents(): void {
    this.securityEvents.length = 0;
  }

  /**
   * Create real-time validator observable
   */
  createRealTimeValidator(
    rules: ValidationRule[],
    context?: ValidationContext,
    debounceMs = 300
  ): (control: AbstractControl) => Observable<ValidationResult> {
    return (control: AbstractControl) => {
      return of(control.value).pipe(
        debounceTime(debounceMs),
        distinctUntilChanged(),
        map(async value => {
          const validationContext = context ? { ...context, control } : { control, inputType: 'text' as InputType, securityContext: 'public' as SecurityContext };
          return await this.validate(value, rules, validationContext);
        }),
        catchError(error => {
          console.error('Real-time validation error:', error);
          return of({
            valid: false,
            errors: ['Real-time validation failed'],
            warnings: [],
            riskLevel: 'medium' as SecurityRiskLevel
          });
        })
      );
    };
  }

  // Private methods

  private initializeSecurityRules(): void {
    const isProduction = typeof window !== 'undefined' &&
      (window.location.hostname !== 'localhost' && !window.location.hostname.startsWith('127.0.0.1'));

    if (isProduction) {
      this.updateConfig({
        mode: 'strict',
        securityLevel: 'high',
        sanitization: {
          ...this.config().sanitization,
          html: true,
          sql: true,
          scripts: true,
          urls: true
        }
      });
    }
  }

  private validateSecurityContext(
    value: any,
    context: ValidationContext
  ): { valid: boolean; errors: string[]; warnings: string[]; riskLevel: SecurityRiskLevel } {

    const result = {
      valid: true,
      errors: [] as string[],
      warnings: [] as string[],
      riskLevel: 'none' as SecurityRiskLevel
    };

    const securityRisk = this.checkSecurity(value, context.securityContext);
    result.riskLevel = securityRisk;

    // High security contexts have stricter validation
    if (context.securityContext === 'admin' || context.securityContext === 'system') {
      if (securityRisk === 'high' || securityRisk === 'critical') {
        result.valid = false;
        result.errors.push('Input contains security threats and cannot be processed in this context');
      } else if (securityRisk === 'medium') {
        result.warnings.push('Input flagged for security review');
      }
    }

    return result;
  }

  private checkRateLimit(context: ValidationContext): boolean {
    const config = this.config();
    if (!config.rateLimiting.enabled) return true;

    const key = this.getRateLimitKey(context);
    const now = Date.now();
    const windowMs = 60 * 1000; // 1 minute

    let limitData = this.rateLimitMap.get(key);
    if (!limitData) {
      limitData = { count: 0, lastReset: now, locked: false };
      this.rateLimitMap.set(key, limitData);
    }

    // Check if locked
    if (limitData.locked) {
      const lockDuration = config.rateLimiting.lockoutDuration * 60 * 1000;
      if (now - limitData.lastReset < lockDuration) {
        return false;
      } else {
        // Unlock
        limitData.locked = false;
        limitData.count = 0;
        limitData.lastReset = now;
      }
    }

    // Reset window if needed
    if (now - limitData.lastReset > windowMs) {
      limitData.count = 0;
      limitData.lastReset = now;
    }

    limitData.count++;

    // Check limits
    if (limitData.count > config.rateLimiting.burstLimit) {
      limitData.locked = true;
      return false;
    }

    if (limitData.count > config.rateLimiting.requestsPerMinute) {
      return false;
    }

    return true;
  }

  private getRateLimitKey(context: ValidationContext): string {
    // Use input type and security context for rate limiting key
    return `${context.inputType}-${context.securityContext}`;
  }

  private updateMetrics(result: ValidationResult, context?: ValidationContext): void {
    this.metricsSignal.update(current => {
      const updated = { ...current };

      updated.totalValidations++;

      if (!result.valid) {
        updated.failedValidations++;
      }

      if (result.riskLevel !== 'none') {
        updated.securityThreats++;
        updated.riskLevelDistribution[result.riskLevel]++;
      }

      // Update processing time average
      if (result.metadata) {
        const totalTime = current.averageProcessingTime * (current.totalValidations - 1) + result.metadata.processingTime;
        updated.averageProcessingTime = totalTime / current.totalValidations;

        // Update validations by type
        const inputType = result.metadata.inputType;
        updated.validationsByType[inputType] = (updated.validationsByType[inputType] || 0) + 1;
      }

      // Update common errors
      for (const error of result.errors) {
        const existingError = updated.commonErrors.find(e => e.error === error);
        if (existingError) {
          existingError.count++;
        } else {
          updated.commonErrors.push({ error, count: 1 });
        }
      }

      // Keep only top 10 common errors
      updated.commonErrors.sort((a, b) => b.count - a.count).splice(10);

      return updated;
    });
  }

  private logSecurityEvent(
    result: ValidationResult,
    context?: ValidationContext,
    inputValue?: any
  ): void {
    const event: FormSecurityEvent = {
      type: result.valid ? 'sanitization' : 'validation_failed',
      timestamp: Date.now(),
      inputValue: this.sanitizeForLogging(inputValue),
      inputType: context?.inputType || 'text',
      riskLevel: result.riskLevel,
      error: result.errors.join(', ') || undefined,
      userContext: {
        userAgent: typeof navigator !== 'undefined' ? navigator.userAgent : undefined,
        // Note: IP address would need to be provided by backend
      }
    };

    this.securityEvents.push(event);

    // Keep only last 1000 events
    if (this.securityEvents.length > 1000) {
      this.securityEvents.splice(0, this.securityEvents.length - 1000);
    }
  }

  private sanitizeForLogging(value: any): string {
    if (!value) return '';

    const str = value.toString();
    // Truncate and remove sensitive patterns for logging
    const truncated = str.length > 100 ? str.substring(0, 100) + '...' : str;

    // Remove potential sensitive data patterns
    return truncated
      .replace(/['"]/g, '')
      .replace(/<[^>]*>/g, '')
      .replace(/javascript:/gi, '')
      .replace(/data:/gi, '');
  }

  private escalateRisk(current: SecurityRiskLevel, newRisk: SecurityRiskLevel): SecurityRiskLevel {
    const levels: SecurityRiskLevel[] = ['none', 'low', 'medium', 'high', 'critical'];
    const currentIndex = levels.indexOf(current);
    const newIndex = levels.indexOf(newRisk);
    return levels[Math.max(currentIndex, newIndex)];
  }

  private getErrorMessage(key: string, value: any, rule: ValidationRule): string {
    const defaultMessages: Record<string, string> = {
      required: 'This field is required',
      email: 'Please enter a valid email address',
      minlength: `Minimum length is ${value.requiredLength}`,
      maxlength: `Maximum length is ${value.requiredLength}`,
      pattern: 'Invalid format',
      xssScript: 'Script tags are not allowed',
      sqlInjection: 'Potential SQL injection detected',
      passwordTooShort: `Password must be at least ${value.minLength} characters`,
      urlInvalid: 'Please enter a valid URL'
    };

    return defaultMessages[key] || `Validation failed: ${key}`;
  }

  private createLengthValidator(min?: number, max?: number): CustomValidatorFn {
    return (control: AbstractControl) => {
      if (!control.value) return null;

      const length = control.value.toString().length;
      const errors: any = {};

      if (min !== undefined && length < min) {
        errors.minlength = { requiredLength: min, actualLength: length };
      }

      if (max !== undefined && length > max) {
        errors.maxlength = { requiredLength: max, actualLength: length };
      }

      return Object.keys(errors).length > 0 ? errors : null;
    };
  }

  private createWhitelistValidator(allowedValues: string[]): CustomValidatorFn {
    return (control: AbstractControl) => {
      if (!control.value) return null;

      const value = control.value.toString();
      if (!allowedValues.includes(value)) {
        return { whitelist: { value, allowedValues } };
      }

      return null;
    };
  }

  private createBlacklistValidator(forbiddenValues: string[]): CustomValidatorFn {
    return (control: AbstractControl) => {
      if (!control.value) return null;

      const value = control.value.toString();
      if (forbiddenValues.includes(value)) {
        return { blacklist: { value } };
      }

      return null;
    };
  }
}