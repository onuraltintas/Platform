import { TestBed } from '@angular/core/testing';
import { SanitizationService } from './sanitization.service';
import { SanitizationConfig } from '../interfaces/validation.interface';

/**
 * Comprehensive Sanitization Service Test Suite
 * Tests input sanitization with security focus
 */
describe('SanitizationService', () => {
  let service: SanitizationService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SanitizationService]
    });
    service = TestBed.inject(SanitizationService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('Email Sanitization', () => {
    it('should sanitize email addresses correctly', () => {
      const result = service.sanitize('  TEST@EXAMPLE.COM  ', 'email');

      expect(result.sanitizedValue).toBe('test@example.com');
      expect(result.riskLevel).toBe('none');
    });

    it('should remove dangerous characters from emails', () => {
      const maliciousEmail = 'test<script>@example.com';
      const result = service.sanitize(maliciousEmail, 'email');

      expect(result.sanitizedValue).toBe('testscript@example.com');
      expect(result.riskLevel).toBe('medium');
      expect(result.warnings).toContain('Dangerous characters removed from email');
    });

    it('should handle multiple @ symbols', () => {
      const result = service.sanitize('test@@extra@example.com', 'email');

      expect(result.sanitizedValue).toBe('test@extraexample.com');
      expect(result.warnings).toContain('Multiple @ symbols detected and corrected');
    });

    it('should remove spaces from emails', () => {
      const result = service.sanitize('test @exam ple.com', 'email');

      expect(result.sanitizedValue).toBe('test@example.com');
      expect(result.warnings).toContain('Spaces removed from email');
    });
  });

  describe('Password Sanitization', () => {
    it('should minimally sanitize passwords', () => {
      const password = '  MySecureP@ssw0rd!  ';
      const result = service.sanitize(password, 'password');

      expect(result.sanitizedValue).toBe('MySecureP@ssw0rd!');
      expect(result.warnings).toContain('Leading/trailing whitespace removed');
    });

    it('should remove control characters from passwords', () => {
      const passwordWithControlChars = 'Pass\x00word\x01123!';
      const result = service.sanitize(passwordWithControlChars, 'password');

      expect(result.sanitizedValue).toBe('Password123!');
      expect(result.riskLevel).toBe('high');
      expect(result.warnings).toContain('Control characters removed');
    });

    it('should preserve special characters in passwords', () => {
      const complexPassword = 'Comp1ex!@#$%^&*()_+{}[]|\\:";\'<>?,./`~';
      const result = service.sanitize(complexPassword, 'password');

      expect(result.sanitizedValue).toBe(complexPassword);
      expect(result.riskLevel).toBe('none');
    });
  });

  describe('URL Sanitization', () => {
    it('should sanitize safe URLs', () => {
      const result = service.sanitize('  example.com  ', 'url');

      expect(result.sanitizedValue).toBe('https://example.com');
      expect(result.warnings).toContain('HTTPS protocol added for security');
    });

    it('should remove dangerous protocols', () => {
      const dangerousUrls = [
        'javascript:alert(1)',
        'data:text/html,<script>alert(1)</script>',
        'vbscript:msgbox(1)',
        'file:///etc/passwd'
      ];

      dangerousUrls.forEach(url => {
        const result = service.sanitize(url, 'url');
        expect(result.riskLevel).toBe('critical');
        expect(result.warnings.some(w => w.includes('Dangerous protocol'))).toBe(true);
      });
    });

    it('should decode suspicious URL encoding', () => {
      const encodedUrl = 'https://example.com%2e%2e/path';
      const result = service.sanitize(encodedUrl, 'url');

      expect(result.riskLevel).toBe('medium');
      expect(result.warnings).toContain('Suspicious URL encoding detected and decoded');
    });

    it('should preserve valid URLs', () => {
      const validUrl = 'https://subdomain.example.com/path?query=value#fragment';
      const result = service.sanitize(validUrl, 'url');

      expect(result.sanitizedValue).toBe(validUrl);
      expect(result.riskLevel).toBe('none');
    });
  });

  describe('HTML Sanitization', () => {
    it('should remove script tags', () => {
      const maliciousHtml = '<p>Hello</p><script>alert("xss")</script><p>World</p>';
      const result = service.sanitize(maliciousHtml, 'html');

      expect(result.sanitizedValue).not.toContain('<script>');
      expect(result.riskLevel).toBe('critical');
      expect(result.warnings).toContain('Script tags removed');
    });

    it('should remove event handlers', () => {
      const maliciousHtml = '<div onclick="alert(1)">Click me</div>';
      const result = service.sanitize(maliciousHtml, 'html');

      expect(result.sanitizedValue).not.toContain('onclick');
      expect(result.riskLevel).toBe('high');
      expect(result.warnings).toContain('Event handlers removed');
    });

    it('should remove dangerous tags', () => {
      const dangerousTags = [
        '<iframe src="http://evil.com"></iframe>',
        '<object data="malware.swf"></object>',
        '<embed src="plugin.swf">',
        '<form><input type="password"></form>'
      ];

      dangerousTags.forEach(tag => {
        const result = service.sanitize(tag, 'html');
        expect(result.riskLevel).toBe('high');
        expect(result.warnings.some(w => w.includes('tags removed'))).toBe(true);
      });
    });

    it('should remove dangerous protocols from attributes', () => {
      const maliciousHtml = '<a href="javascript:alert(1)">Link</a>';
      const result = service.sanitize(maliciousHtml, 'html');

      expect(result.sanitizedValue).not.toContain('javascript:');
      expect(result.riskLevel).toBe('high');
    });
  });

  describe('Filename Sanitization', () => {
    it('should sanitize safe filenames', () => {
      const result = service.sanitize('  my-document.pdf  ', 'filename');

      expect(result.sanitizedValue).toBe('my-document.pdf');
      expect(result.riskLevel).toBe('none');
    });

    it('should remove path traversal patterns', () => {
      const maliciousFilename = '../../../etc/passwd';
      const result = service.sanitize(maliciousFilename, 'filename');

      expect(result.sanitizedValue).not.toContain('..');
      expect(result.riskLevel).toBe('high');
      expect(result.warnings).toContain('Path traversal patterns removed');
    });

    it('should replace dangerous characters', () => {
      const dangerousFilename = 'file<>:"|?*.txt';
      const result = service.sanitize(dangerousFilename, 'filename');

      expect(result.sanitizedValue).toBe('file_______.txt');
      expect(result.riskLevel).toBe('medium');
      expect(result.warnings).toContain('Dangerous characters replaced with underscores');
    });

    it('should handle reserved Windows names', () => {
      const reservedName = 'con.txt';
      const result = service.sanitize(reservedName, 'filename');

      expect(result.sanitizedValue).toBe('_con.txt');
      expect(result.riskLevel).toBe('medium');
      expect(result.warnings).toContain('Reserved filename prefix added');
    });

    it('should truncate long filenames', () => {
      const longFilename = 'a'.repeat(300) + '.txt';
      const result = service.sanitize(longFilename, 'filename');

      expect(result.sanitizedValue.length).toBe(255);
      expect(result.warnings).toContain('Filename truncated to 255 characters');
    });
  });

  describe('JSON Sanitization', () => {
    it('should validate and sanitize JSON', () => {
      const validJson = '{"name": "test", "value": 123}';
      const result = service.sanitize(validJson, 'json');

      expect(result.sanitizedValue).toBe(validJson);
      expect(result.riskLevel).toBe('none');
    });

    it('should remove dangerous object keys', () => {
      const maliciousJson = '{"__proto__": {"isAdmin": true}, "name": "test"}';
      const result = service.sanitize(maliciousJson, 'json');

      const parsed = JSON.parse(result.sanitizedValue);
      expect(parsed.__proto__).toBeUndefined();
      expect(parsed.name).toBe('test');
      expect(result.riskLevel).toBe('high');
      expect(result.warnings).toContain('Dangerous object keys removed');
    });

    it('should handle invalid JSON', () => {
      const invalidJson = '{"name": "test", "value":}';
      const result = service.sanitize(invalidJson, 'json');

      expect(result.sanitizedValue).toBe(JSON.stringify(invalidJson));
      expect(result.riskLevel).toBe('medium');
      expect(result.warnings).toContain('Invalid JSON converted to string literal');
    });
  });

  describe('General Text Sanitization', () => {
    it('should sanitize general text input', () => {
      const result = service.sanitize('  Normal text with  extra   spaces  ', 'text');

      expect(result.sanitizedValue).toBe('Normal text with extra spaces');
      expect(result.riskLevel).toBe('none');
    });

    it('should apply SQL injection protection', () => {
      const maliciousText = "'; DROP TABLE users; --";
      const config: Partial<SanitizationConfig> = { sql: true };
      const result = service.sanitize(maliciousText, 'text', config);

      expect(result.sanitizedValue).not.toContain('DROP TABLE');
      expect(result.riskLevel).toBe('critical');
    });

    it('should apply XSS protection', () => {
      const maliciousText = '<script>alert("xss")</script>Normal text';
      const config: Partial<SanitizationConfig> = { html: true, scripts: true };
      const result = service.sanitize(maliciousText, 'text', config);

      expect(result.sanitizedValue).not.toContain('<script>');
      expect(result.riskLevel).toBe('high');
    });
  });

  describe('Custom Sanitization Rules', () => {
    it('should apply custom sanitization rules', () => {
      const config: Partial<SanitizationConfig> = {
        custom: [
          {
            name: 'remove-phone',
            pattern: /\d{3}-\d{3}-\d{4}/g,
            replacement: '[PHONE REDACTED]',
            global: true
          }
        ]
      };

      const text = 'Call me at 555-123-4567 or 555-987-6543';
      const result = service.sanitize(text, 'text', config);

      expect(result.sanitizedValue).toBe('Call me at [PHONE REDACTED] or [PHONE REDACTED]');
      expect(result.warnings).toContain('Custom rule applied: remove-phone');
    });
  });

  describe('Length Limitations', () => {
    it('should truncate input that exceeds maximum length', () => {
      const longText = 'a'.repeat(20000);
      const config: Partial<SanitizationConfig> = { maxLength: 1000 };
      const result = service.sanitize(longText, 'text', config);

      expect(result.sanitizedValue.length).toBe(1000);
      expect(result.warnings).toContain('Input truncated to 1000 characters');
    });
  });

  describe('Risk Level Escalation', () => {
    it('should correctly escalate risk levels', () => {
      // Test with multiple threats of different levels
      const multiThreatText = '<script>alert(1)</script>'; // High risk
      const config: Partial<SanitizationConfig> = {
        html: true,
        scripts: true,
        maxLength: 10 // This will also trigger a low risk
      };

      const result = service.sanitize(multiThreatText, 'html', config);

      expect(result.riskLevel).toBe('critical'); // Should be highest risk
    });
  });

  describe('Whitespace Preservation', () => {
    it('should preserve whitespace when configured', () => {
      const config: Partial<SanitizationConfig> = { preserveWhitespace: true };
      const text = '  Text  with   preserved   spaces  ';
      const result = service.sanitize(text, 'text', config);

      expect(result.sanitizedValue).toBe(text);
    });

    it('should normalize whitespace by default', () => {
      const text = '  Text  with   normalized   spaces  ';
      const result = service.sanitize(text, 'text');

      expect(result.sanitizedValue).toBe('Text with normalized spaces');
    });
  });

  describe('Security Recommendations', () => {
    it('should provide recommendations for email input', () => {
      const recommendations = service.getSanitizationRecommendations('email');

      expect(recommendations).toContain('Enable email domain validation');
      expect(recommendations).toContain('Block disposable email domains');
      expect(recommendations).toContain('Implement email verification');
    });

    it('should provide recommendations for password input', () => {
      const recommendations = service.getSanitizationRecommendations('password');

      expect(recommendations).toContain('Implement strong password policies');
      expect(recommendations).toContain('Use secure password hashing (bcrypt/scrypt)');
      expect(recommendations).toContain('Enable password breach checking');
    });

    it('should provide recommendations for HTML input', () => {
      const recommendations = service.getSanitizationRecommendations('html');

      expect(recommendations).toContain('Use Content Security Policy (CSP)');
      expect(recommendations).toContain('Implement HTML tag allowlisting');
      expect(recommendations).toContain('Enable DOM purification');
    });

    it('should provide recommendations for URL input', () => {
      const recommendations = service.getSanitizationRecommendations('url');

      expect(recommendations).toContain('Validate URL protocols (allow only HTTP/HTTPS)');
      expect(recommendations).toContain('Implement domain allowlisting');
      expect(recommendations).toContain('Check for URL redirects');
    });

    it('should provide general recommendations for other input types', () => {
      const recommendations = service.getSanitizationRecommendations('text');

      expect(recommendations).toContain('Implement input length limits');
      expect(recommendations).toContain('Use parameterized queries for database operations');
      expect(recommendations).toContain('Enable real-time security monitoring');
    });
  });

  describe('Edge Cases', () => {
    it('should handle null and undefined values', () => {
      const nullResult = service.sanitize(null, 'text');
      const undefinedResult = service.sanitize(undefined, 'text');

      expect(nullResult.sanitizedValue).toBe('');
      expect(nullResult.riskLevel).toBe('none');
      expect(undefinedResult.sanitizedValue).toBe('');
      expect(undefinedResult.riskLevel).toBe('none');
    });

    it('should handle empty strings', () => {
      const result = service.sanitize('', 'text');

      expect(result.sanitizedValue).toBe('');
      expect(result.riskLevel).toBe('none');
      expect(result.warnings).toEqual([]);
    });

    it('should handle non-string input types', () => {
      const numberResult = service.sanitize(12345, 'text');
      const objectResult = service.sanitize({ test: 'value' }, 'text');

      expect(numberResult.sanitizedValue).toBe('12345');
      expect(objectResult.sanitizedValue).toContain('test');
    });

    it('should handle very large inputs', () => {
      const hugeInput = 'x'.repeat(1000000); // 1MB of data
      const result = service.sanitize(hugeInput, 'text');

      expect(result.sanitizedValue.length).toBeLessThanOrEqual(10000); // Default max length
      expect(result.warnings).toContain('Input truncated to 10000 characters');
    });

    it('should handle special Unicode characters', () => {
      const unicodeText = '🔒 Secure 测试 ñoño 🔑';
      const result = service.sanitize(unicodeText, 'text');

      expect(result.sanitizedValue).toContain('🔒');
      expect(result.sanitizedValue).toContain('测试');
      expect(result.sanitizedValue).toContain('ñoño');
      expect(result.riskLevel).toBe('none');
    });
  });

  describe('Performance', () => {
    it('should complete sanitization within reasonable time', () => {
      const startTime = performance.now();
      const largeText = 'Lorem ipsum '.repeat(1000);

      service.sanitize(largeText, 'text');

      const endTime = performance.now();
      const duration = endTime - startTime;

      expect(duration).toBeLessThan(100); // Should complete in under 100ms
    });

    it('should handle multiple rapid sanitizations', () => {
      const inputs = Array.from({ length: 100 }, (_, i) => `test input ${i}`);
      const startTime = performance.now();

      inputs.forEach(input => {
        service.sanitize(input, 'text');
      });

      const endTime = performance.now();
      const duration = endTime - startTime;

      expect(duration).toBeLessThan(1000); // Should complete 100 sanitizations in under 1 second
    });
  });

  describe('Configuration Handling', () => {
    it('should merge custom config with defaults', () => {
      const customConfig: Partial<SanitizationConfig> = {
        html: false,
        maxLength: 500
      };

      const result = service.sanitize('<script>alert(1)</script>', 'html', customConfig);

      // HTML sanitization should be disabled
      expect(result.sanitizedValue).toContain('<script>');
      expect(result.riskLevel).toBe('none');
    });

    it('should handle missing config gracefully', () => {
      const result = service.sanitize('test input', 'text', undefined);

      expect(result.sanitizedValue).toBe('test input');
      expect(result.riskLevel).toBe('none');
    });
  });
});