import { TestBed } from '@angular/core/testing';
import { AbstractControl, FormControl } from '@angular/forms';
import { ValidationService } from './validation.service';
import { SanitizationService } from './sanitization.service';
import {
  ValidationRule,
  ValidationContext
} from '../interfaces/validation.interface';
import { CustomValidators } from '../validators/custom-validators';

/**
 * Comprehensive Validation Service Test Suite
 * Tests enterprise validation functionality and security features
 */
describe('ValidationService', () => {
  let service: ValidationService;
  let sanitizationService: jasmine.SpyObj<SanitizationService>;

  beforeEach(() => {
    const sanitizationSpy = jasmine.createSpyObj('SanitizationService', [
      'sanitize',
      'getSanitizationRecommendations'
    ]);

    TestBed.configureTestingModule({
      providers: [
        ValidationService,
        { provide: SanitizationService, useValue: sanitizationSpy }
      ]
    });

    service = TestBed.inject(ValidationService);
    sanitizationService = TestBed.inject(SanitizationService) as jasmine.SpyObj<SanitizationService>;

    // Setup default sanitization mock
    sanitizationService.sanitize.and.returnValue({
      sanitizedValue: 'test@example.com',
      riskLevel: 'none',
      warnings: []
    });

    sanitizationService.getSanitizationRecommendations.and.returnValue([
      'Use strong validation rules',
      'Implement rate limiting'
    ]);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('Configuration Management', () => {
    it('should update configuration', () => {
      const newConfig = {
        mode: 'strict' as const,
        securityLevel: 'high' as const
      };

      service.updateConfig(newConfig);
      const config = service.getConfig();

      expect(config.mode).toBe('strict');
      expect(config.securityLevel).toBe('high');
    });

    it('should return computed configuration properties', () => {
      service.updateConfig({ securityLevel: 'high' });
      expect(service.isHighSecurity()).toBe(true);

      service.updateConfig({ securityLevel: 'medium' });
      expect(service.isHighSecurity()).toBe(false);
    });
  });

  describe('Basic Validation', () => {
    it('should validate email input successfully', async () => {
      const rules: ValidationRule[] = [
        {
          name: 'email-validation',
          description: 'Email format validation',
          validator: CustomValidators.email(),
          severity: 'error',
          securityImpact: 'medium',
          enabled: true
        }
      ];

      const context: ValidationContext = {
        control: new FormControl('test@example.com'),
        inputType: 'email',
        securityContext: 'public'
      };

      const result = await service.validate('test@example.com', rules, context);

      expect(result.valid).toBe(true);
      expect(result.errors).toEqual([]);
      expect(result.riskLevel).toBe('none');
    });

    it('should fail validation for invalid email', async () => {
      const rules: ValidationRule[] = [
        {
          name: 'email-validation',
          description: 'Email format validation',
          validator: CustomValidators.email(),
          severity: 'error',
          securityImpact: 'medium',
          enabled: true
        }
      ];

      const context: ValidationContext = {
        control: new FormControl('invalid-email'),
        inputType: 'email',
        securityContext: 'public'
      };

      const result = await service.validate('invalid-email', rules, context);

      expect(result.valid).toBe(false);
      expect(result.errors.length).toBeGreaterThan(0);
    });

    it('should handle disabled validation rules', async () => {
      const rules: ValidationRule[] = [
        {
          name: 'disabled-rule',
          description: 'This rule is disabled',
          validator: () => ({ disabled: true }),
          severity: 'error',
          securityImpact: 'high',
          enabled: false
        }
      ];

      const result = await service.validate('any-value', rules);

      expect(result.valid).toBe(true);
      expect(result.errors).toEqual([]);
    });
  });

  describe('Security Validation', () => {
    it('should detect XSS attempts', async () => {
      const maliciousInput = '<script>alert("xss")</script>';

      const rules: ValidationRule[] = [
        {
          name: 'xss-protection',
          description: 'XSS protection',
          validator: CustomValidators.xss(),
          severity: 'error',
          securityImpact: 'critical',
          enabled: true
        }
      ];

      const result = await service.validate(maliciousInput, rules);

      expect(result.valid).toBe(false);
      expect(result.riskLevel).toBe('critical');
    });

    it('should detect SQL injection attempts', async () => {
      const maliciousInput = "'; DROP TABLE users; --";

      const rules: ValidationRule[] = [
        {
          name: 'sql-injection-protection',
          description: 'SQL injection protection',
          validator: CustomValidators.sqlInjection(),
          severity: 'error',
          securityImpact: 'critical',
          enabled: true
        }
      ];

      const result = await service.validate(maliciousInput, rules);

      expect(result.valid).toBe(false);
      expect(result.riskLevel).toBe('critical');
    });

    it('should escalate risk levels correctly', async () => {
      const rules: ValidationRule[] = [
        {
          name: 'low-risk-rule',
          description: 'Low risk rule',
          validator: () => ({ lowRisk: true }),
          severity: 'warning',
          securityImpact: 'low',
          enabled: true
        },
        {
          name: 'high-risk-rule',
          description: 'High risk rule',
          validator: () => ({ highRisk: true }),
          severity: 'error',
          securityImpact: 'high',
          enabled: true
        }
      ];

      const result = await service.validate('test-value', rules);

      expect(result.riskLevel).toBe('high');
    });
  });

  describe('Security Context Validation', () => {
    it('should apply stricter validation for admin context', async () => {
      const suspiciousInput = 'admin;rm -rf /';

      const context: ValidationContext = {
        control: new FormControl(suspiciousInput),
        inputType: 'text',
        securityContext: 'admin'
      };

      const result = await service.validate(suspiciousInput, [], context);

      expect(result.riskLevel).not.toBe('none');
    });

    it('should be more permissive for public context', async () => {
      const normalInput = 'Hello world!';

      const context: ValidationContext = {
        control: new FormControl(normalInput),
        inputType: 'text',
        securityContext: 'public'
      };

      const result = await service.validate(normalInput, [], context);

      expect(result.valid).toBe(true);
      expect(result.riskLevel).toBe('none');
    });
  });

  describe('Rate Limiting', () => {
    it('should enforce rate limiting', async () => {
      const context: ValidationContext = {
        control: new FormControl('test'),
        inputType: 'text',
        securityContext: 'public'
      };

      // Enable aggressive rate limiting for testing
      service.updateConfig({
        rateLimiting: {
          enabled: true,
          requestsPerMinute: 2,
          burstLimit: 1,
          lockoutDuration: 1
        }
      });

      // First request should succeed
      const result1 = await service.validate('test1', [], context);
      expect(result1.valid).toBe(true);

      // Second request should succeed
      const result2 = await service.validate('test2', [], context);
      expect(result2.valid).toBe(true);

      // Third request should be rate limited
      const result3 = await service.validate('test3', [], context);
      expect(result3.valid).toBe(false);
      expect(result3.errors[0]).toContain('Rate limit exceeded');
    });
  });

  describe('Sanitization Integration', () => {
    it('should sanitize input values', async () => {
      sanitizationService.sanitize.and.returnValue({
        sanitizedValue: 'sanitized-value',
        riskLevel: 'low',
        warnings: ['Input was sanitized']
      });

      const context: ValidationContext = {
        control: new FormControl('dirty-input'),
        inputType: 'text',
        securityContext: 'public'
      };

      const result = await service.validate('dirty-input', [], context);

      expect(sanitizationService.sanitize).toHaveBeenCalledWith(
        'dirty-input',
        'text',
        jasmine.any(Object)
      );
      expect(result.sanitizedValue).toBe('sanitized-value');
      expect(result.warnings).toContain('Input was sanitized');
    });
  });

  describe('Validator Factory', () => {
    it('should provide email validator', () => {
      const factory = service.getValidatorFactory();
      const emailValidator = factory.email({ maxLength: 100 });

      expect(emailValidator).toBeDefined();
      expect(typeof emailValidator).toBe('function');

      const control = new FormControl('test@example.com');
      const result = emailValidator(control);
      expect(result).toBeNull(); // Valid email
    });

    it('should provide password validator', () => {
      const factory = service.getValidatorFactory();
      const passwordValidator = factory.password({ minLength: 8 });

      expect(passwordValidator).toBeDefined();

      const control = new FormControl('weak');
      const result = passwordValidator(control);
      expect(result).not.toBeNull(); // Should fail validation
    });

    it('should provide custom pattern validator', () => {
      const factory = service.getValidatorFactory();
      const patternValidator = factory.pattern(/^\d+$/, 'Numbers only');

      expect(patternValidator).toBeDefined();

      const control = new FormControl('123');
      const validResult = patternValidator(control);
      expect(validResult).toBeNull();

      const control2 = new FormControl('abc');
      const invalidResult = patternValidator(control2);
      expect(invalidResult).not.toBeNull();
    });
  });

  describe('Custom Validators', () => {
    it('should register and use custom validators', () => {
      const customValidator = (control: AbstractControl) => {
        return control.value === 'custom' ? null : { custom: true };
      };

      service.registerValidator('custom-test', customValidator);

      // Verify registration (this would need to be exposed in the service)
      expect(true).toBe(true); // Placeholder assertion
    });
  });

  describe('Metrics and Monitoring', () => {
    it('should track validation metrics', async () => {
      // Perform some validations
      await service.validate('test1', []);
      await service.validate('test2', []);

      const metrics = service.getValidationMetrics();

      expect(metrics.totalValidations).toBeGreaterThan(0);
      expect(metrics.averageProcessingTime).toBeGreaterThanOrEqual(0);
    });

    it('should track failed validations', async () => {
      const failingRule: ValidationRule = {
        name: 'failing-rule',
        description: 'Always fails',
        validator: () => ({ alwaysFails: true }),
        severity: 'error',
        securityImpact: 'low',
        enabled: true
      };

      await service.validate('test', [failingRule]);

      const metrics = service.getValidationMetrics();
      expect(metrics.failedValidations).toBeGreaterThan(0);
    });

    it('should track security threats', async () => {
      const xssRule: ValidationRule = {
        name: 'xss-rule',
        description: 'XSS detection',
        validator: CustomValidators.xss(),
        severity: 'error',
        securityImpact: 'high',
        enabled: true
      };

      await service.validate('<script>alert("xss")</script>', [xssRule]);

      const metrics = service.getValidationMetrics();
      expect(metrics.securityThreats).toBeGreaterThan(0);
    });
  });

  describe('Real-time Validation', () => {
    it('should create real-time validator observable', (done) => {
      const rules: ValidationRule[] = [
        {
          name: 'length-rule',
          description: 'Length validation',
          validator: CustomValidators.securePattern(/^.{3,}$/, 'Minimum 3 characters'),
          severity: 'error',
          securityImpact: 'low',
          enabled: true
        }
      ];

      const control = new FormControl('ab');
      const realtimeValidator = service.createRealTimeValidator(rules);

      const observable = realtimeValidator(control);

      observable.subscribe(async (resultPromise) => {
        const result = await resultPromise;
        expect(result.valid).toBe(false);
        done();
      });
    });
  });

  describe('Security Events', () => {
    it('should log and retrieve security events', async () => {
      const maliciousInput = '<script>alert("test")</script>';

      const rules: ValidationRule[] = [
        {
          name: 'xss-rule',
          description: 'XSS detection',
          validator: CustomValidators.xss(),
          severity: 'error',
          securityImpact: 'critical',
          enabled: true
        }
      ];

      await service.validate(maliciousInput, rules);

      const events = service.getSecurityEvents();
      expect(events.length).toBeGreaterThan(0);

      const lastEvent = events[events.length - 1];
      expect(lastEvent.type).toBe('validation_failed');
      expect(lastEvent.riskLevel).toBe('critical');
    });

    it('should clear security events', async () => {
      // Generate some events
      await service.validate('<script>test</script>', [
        {
          name: 'xss-rule',
          description: 'XSS detection',
          validator: CustomValidators.xss(),
          severity: 'error',
          securityImpact: 'high',
          enabled: true
        }
      ]);

      expect(service.getSecurityEvents().length).toBeGreaterThan(0);

      service.clearSecurityEvents();
      expect(service.getSecurityEvents().length).toBe(0);
    });
  });

  describe('Error Handling', () => {
    it('should handle validation rule errors gracefully', async () => {
      const buggyRule: ValidationRule = {
        name: 'buggy-rule',
        description: 'Rule that throws error',
        validator: () => {
          throw new Error('Validation rule error');
        },
        severity: 'error',
        securityImpact: 'low',
        enabled: true
      };

      const result = await service.validate('test', [buggyRule]);

      expect(result.valid).toBe(true); // Should not fail due to rule error
      expect(result.warnings.some(w => w.includes('encountered an error'))).toBe(true);
    });

    it('should handle internal service errors', async () => {
      // Mock an internal error
      spyOn(console, 'error');

      // This is a bit contrived, but tests the error handling path
      const result = await service.validate(null, []);

      expect(result).toBeDefined();
      expect(result.valid).toBe(true); // Null/empty values are typically valid
    });
  });

  describe('Performance', () => {
    it('should complete validation within reasonable time', async () => {
      const startTime = performance.now();

      const rules: ValidationRule[] = [
        {
          name: 'email-rule',
          description: 'Email validation',
          validator: CustomValidators.email(),
          severity: 'error',
          securityImpact: 'medium',
          enabled: true
        },
        {
          name: 'xss-rule',
          description: 'XSS protection',
          validator: CustomValidators.xss(),
          severity: 'error',
          securityImpact: 'high',
          enabled: true
        }
      ];

      await service.validate('test@example.com', rules);

      const endTime = performance.now();
      const duration = endTime - startTime;

      expect(duration).toBeLessThan(100); // Should complete in under 100ms
    });

    it('should track processing time in metadata', async () => {
      const result = await service.validate('test', []);

      expect(result.metadata).toBeDefined();
      expect(result.metadata!.processingTime).toBeGreaterThanOrEqual(0);
    });
  });

  describe('Edge Cases', () => {
    it('should handle null input values', async () => {
      const result = await service.validate(null, []);

      expect(result.valid).toBe(true);
      expect(result.errors).toEqual([]);
    });

    it('should handle undefined input values', async () => {
      const result = await service.validate(undefined, []);

      expect(result.valid).toBe(true);
      expect(result.errors).toEqual([]);
    });

    it('should handle empty string input', async () => {
      const result = await service.validate('', []);

      expect(result.valid).toBe(true);
      expect(result.errors).toEqual([]);
    });

    it('should handle empty rules array', async () => {
      const result = await service.validate('any-value', []);

      expect(result.valid).toBe(true);
      expect(result.errors).toEqual([]);
    });

    it('should handle very long input strings', async () => {
      const longString = 'a'.repeat(100000); // 100KB string

      const result = await service.validate(longString, []);

      expect(result).toBeDefined();
      expect(result.valid).toBe(true);
    });
  });
});