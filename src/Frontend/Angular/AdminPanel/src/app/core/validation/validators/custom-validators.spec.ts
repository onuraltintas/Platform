import { FormControl } from '@angular/forms';
import { CustomValidators } from './custom-validators';

/**
 * Comprehensive Custom Validators Test Suite
 * Tests all custom validators with security focus
 */
describe('CustomValidators', () => {

  describe('Email Validator', () => {
    it('should validate correct email addresses', () => {
      const validator = CustomValidators.email();
      const control = new FormControl('test@example.com');

      const result = validator(control);
      expect(result).toBeNull();
    });

    it('should reject invalid email formats', () => {
      const validator = CustomValidators.email();
      const testCases = [
        'invalid-email',
        '@example.com',
        'test@',
        'test.example.com',
        'test@.com',
        'test@com',
        ''
      ];

      testCases.forEach(email => {
        const control = new FormControl(email);
        const result = validator(control);
        expect(result).not.toBeNull();
      });
    });

    it('should enforce maximum length', () => {
      const validator = CustomValidators.email({ maxLength: 20 });
      const longEmail = 'verylongemailaddress@example.com';
      const control = new FormControl(longEmail);

      const result = validator(control);
      expect(result).toEqual(jasmine.objectContaining({
        emailTooLong: jasmine.any(Object)
      }));
    });

    it('should block dangerous characters', () => {
      const validator = CustomValidators.email();
      const dangerousEmails = [
        'test<script>@example.com',
        'test>alert@example.com',
        'test"@example.com',
        "test'@example.com",
        'test;@example.com',
        'test&@example.com'
      ];

      dangerousEmails.forEach(email => {
        const control = new FormControl(email);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          emailSecurity: jasmine.any(Object)
        }));
      });
    });

    it('should block disposable email domains', () => {
      const validator = CustomValidators.email();
      const disposableEmails = [
        'test@10minutemail.com',
        'test@tempmail.org',
        'test@guerrillamail.com',
        'test@mailinator.com'
      ];

      disposableEmails.forEach(email => {
        const control = new FormControl(email);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          emailDisposable: jasmine.any(Object)
        }));
      });
    });

    it('should respect blocked domains configuration', () => {
      const validator = CustomValidators.email({
        blockedDomains: ['blocked.com', 'spam.net']
      });

      const control = new FormControl('test@blocked.com');
      const result = validator(control);

      expect(result).toEqual(jasmine.objectContaining({
        emailBlockedDomain: jasmine.any(Object)
      }));
    });

    it('should require specific domains when configured', () => {
      const validator = CustomValidators.email({
        requiredDomains: ['company.com', 'organization.org']
      });

      const control = new FormControl('test@external.com');
      const result = validator(control);

      expect(result).toEqual(jasmine.objectContaining({
        emailRequiredDomain: jasmine.any(Object)
      }));
    });
  });

  describe('Password Validator', () => {
    it('should validate strong passwords', () => {
      const validator = CustomValidators.password();
      const control = new FormControl('StrongP@ssw0rd123!');

      const result = validator(control);
      expect(result).toBeNull();
    });

    it('should enforce minimum length', () => {
      const validator = CustomValidators.password({ minLength: 12 });
      const control = new FormControl('Short1!');

      const result = validator(control);
      expect(result).toEqual(jasmine.objectContaining({
        passwordTooShort: jasmine.any(Object)
      }));
    });

    it('should enforce maximum length', () => {
      const validator = CustomValidators.password({ maxLength: 20 });
      const longPassword = 'A'.repeat(25) + '1!';
      const control = new FormControl(longPassword);

      const result = validator(control);
      expect(result).toEqual(jasmine.objectContaining({
        passwordTooLong: jasmine.any(Object)
      }));
    });

    it('should require uppercase letters', () => {
      const validator = CustomValidators.password({ requireUppercase: true });
      const control = new FormControl('lowercase123!');

      const result = validator(control);
      expect(result).toEqual(jasmine.objectContaining({
        passwordNoUppercase: true
      }));
    });

    it('should require lowercase letters', () => {
      const validator = CustomValidators.password({ requireLowercase: true });
      const control = new FormControl('UPPERCASE123!');

      const result = validator(control);
      expect(result).toEqual(jasmine.objectContaining({
        passwordNoLowercase: true
      }));
    });

    it('should require numbers', () => {
      const validator = CustomValidators.password({ requireNumbers: true });
      const control = new FormControl('NoNumbers!');

      const result = validator(control);
      expect(result).toEqual(jasmine.objectContaining({
        passwordNoNumbers: true
      }));
    });

    it('should require special characters', () => {
      const validator = CustomValidators.password({ requireSpecialChars: true });
      const control = new FormControl('NoSpecialChars123');

      const result = validator(control);
      expect(result).toEqual(jasmine.objectContaining({
        passwordNoSpecialChars: true
      }));
    });

    it('should detect sequential characters', () => {
      const validator = CustomValidators.password({ checkSequentialChars: true });
      const passwords = ['Password123abc', 'MyPass456def', 'Test123qwe'];

      passwords.forEach(password => {
        const control = new FormControl(password);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          passwordSequential: true
        }));
      });
    });

    it('should detect repeating characters', () => {
      const validator = CustomValidators.password({ maxRepeatingChars: 2 });
      const control = new FormControl('Passsssword123!');

      const result = validator(control);
      expect(result).toEqual(jasmine.objectContaining({
        passwordRepeating: jasmine.any(Object)
      }));
    });

    it('should detect common passwords', () => {
      const validator = CustomValidators.password({ checkCommonPasswords: true });
      const commonPasswords = ['password123', 'admin123', 'qwerty123'];

      commonPasswords.forEach(password => {
        const control = new FormControl(password);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          passwordCommon: true
        }));
      });
    });

    it('should detect personal information patterns', () => {
      const validator = CustomValidators.password();
      const personalPasswords = [
        'AdminPassword123',
        'UserPassword456',
        'MyPassword2023-01-01'
      ];

      personalPasswords.forEach(password => {
        const control = new FormControl(password);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          passwordPersonalInfo: true
        }));
      });
    });
  });

  describe('URL Validator', () => {
    it('should validate correct URLs', () => {
      const validator = CustomValidators.url();
      const validUrls = [
        'https://example.com',
        'http://subdomain.example.org',
        'https://example.com/path?query=value'
      ];

      validUrls.forEach(url => {
        const control = new FormControl(url);
        const result = validator(control);
        expect(result).toBeNull();
      });
    });

    it('should reject invalid URLs', () => {
      const validator = CustomValidators.url();
      const invalidUrls = [
        'not-a-url',
        'ftp://example.com',
        'javascript:alert(1)',
        'data:text/html,<script>alert(1)</script>'
      ];

      invalidUrls.forEach(url => {
        const control = new FormControl(url);
        const result = validator(control);
        expect(result).not.toBeNull();
      });
    });

    it('should block dangerous protocols', () => {
      const validator = CustomValidators.url();
      const dangerousUrls = [
        'javascript:alert(1)',
        'data:text/html,<h1>test</h1>',
        'vbscript:msgbox(1)',
        'file:///etc/passwd'
      ];

      dangerousUrls.forEach(url => {
        const control = new FormControl(url);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          urlDangerous: jasmine.any(Object)
        }));
      });
    });

    it('should respect protocol restrictions', () => {
      const validator = CustomValidators.url({
        allowedProtocols: ['https']
      });

      const control = new FormControl('http://example.com');
      const result = validator(control);

      expect(result).toEqual(jasmine.objectContaining({
        urlProtocol: jasmine.any(Object)
      }));
    });

    it('should block localhost when configured', () => {
      const validator = CustomValidators.url({ allowLocalhost: false });
      const localhostUrls = [
        'http://localhost:3000',
        'https://127.0.0.1:8080',
        'http://192.168.1.1'
      ];

      localhostUrls.forEach(url => {
        const control = new FormControl(url);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          urlLocalhost: jasmine.any(Object)
        }));
      });
    });
  });

  describe('XSS Validator', () => {
    it('should allow safe content', () => {
      const validator = CustomValidators.xss();
      const control = new FormControl('Safe content without scripts');

      const result = validator(control);
      expect(result).toBeNull();
    });

    it('should detect script tags', () => {
      const validator = CustomValidators.xss();
      const maliciousInputs = [
        '<script>alert("xss")</script>',
        '<SCRIPT>alert("XSS")</SCRIPT>',
        '<script type="text/javascript">alert(1)</script>'
      ];

      maliciousInputs.forEach(input => {
        const control = new FormControl(input);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          xssScript: true
        }));
      });
    });

    it('should detect event handlers', () => {
      const validator = CustomValidators.xss();
      const maliciousInputs = [
        '<img onload="alert(1)">',
        '<div onclick="alert(1)">',
        '<button onmouseover="alert(1)">'
      ];

      maliciousInputs.forEach(input => {
        const control = new FormControl(input);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          xssEventHandler: true
        }));
      });
    });

    it('should detect javascript protocols', () => {
      const validator = CustomValidators.xss();
      const maliciousInputs = [
        'javascript:alert(1)',
        'JAVASCRIPT:alert("XSS")',
        'java\nscript:alert(1)'
      ];

      maliciousInputs.forEach(input => {
        const control = new FormControl(input);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          xssJavascript: true
        }));
      });
    });

    it('should allow specific HTML tags when configured', () => {
      const validator = CustomValidators.xss({
        allowedTags: ['p', 'strong', 'em']
      });

      const control = new FormControl('<p>Safe <strong>content</strong></p>');
      const result = validator(control);
      expect(result).toBeNull();
    });

    it('should block forbidden HTML tags', () => {
      const validator = CustomValidators.xss({
        allowedTags: ['p']
      });

      const control = new FormControl('<div>Not allowed</div>');
      const result = validator(control);
      expect(result).toEqual(jasmine.objectContaining({
        xssHtmlTags: jasmine.any(Object)
      }));
    });
  });

  describe('SQL Injection Validator', () => {
    it('should allow safe content', () => {
      const validator = CustomValidators.sqlInjection();
      const control = new FormControl('Safe user input');

      const result = validator(control);
      expect(result).toBeNull();
    });

    it('should detect SQL keywords', () => {
      const validator = CustomValidators.sqlInjection();
      const maliciousInputs = [
        "'; SELECT * FROM users; --",
        "'; INSERT INTO users VALUES ('hacker', 'password'); --",
        "'; DROP TABLE users; --",
        "'; UPDATE users SET password = 'hacked'; --"
      ];

      maliciousInputs.forEach(input => {
        const control = new FormControl(input);
        const result = validator(control);
        expect(result).not.toBeNull();
      });
    });

    it('should detect SQL injection patterns', () => {
      const validator = CustomValidators.sqlInjection();
      const injectionPatterns = [
        "' OR '1'='1",
        "' AND '1'='1",
        "' OR 1=1 --",
        "'; EXEC xp_cmdshell('dir'); --"
      ];

      injectionPatterns.forEach(pattern => {
        const control = new FormControl(pattern);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          sqlInjection: jasmine.any(Object)
        }));
      });
    });

    it('should detect SQL functions when enabled', () => {
      const validator = CustomValidators.sqlInjection({ checkFunctions: true });
      const functionCalls = [
        'CONCAT(username, password)',
        'SUBSTRING(password, 1, 1)',
        'USER()',
        'DATABASE()'
      ];

      functionCalls.forEach(func => {
        const control = new FormControl(func);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          sqlFunction: jasmine.any(Object)
        }));
      });
    });

    it('should detect SQL comments when enabled', () => {
      const validator = CustomValidators.sqlInjection({ checkComments: true });
      const comments = [
        'test -- comment',
        'test /* comment */',
        'test # comment'
      ];

      comments.forEach(comment => {
        const control = new FormControl(comment);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          sqlComment: true
        }));
      });
    });
  });

  describe('Credit Card Validator', () => {
    it('should validate correct credit card numbers', () => {
      const validator = CustomValidators.creditCard();
      const validCards = [
        '4111111111111111', // Visa test number
        '5555555555554444', // Mastercard test number
        '378282246310005'   // Amex test number
      ];

      validCards.forEach(card => {
        const control = new FormControl(card);
        const result = validator(control);
        expect(result).toBeNull();
      });
    });

    it('should reject invalid credit card formats', () => {
      const validator = CustomValidators.creditCard();
      const invalidCards = [
        '123',
        '12345678901234567890', // Too long
        'abcd1234567890123',     // Contains letters
        '4111-1111-1111-1111'    // Contains dashes (should be numbers only)
      ];

      invalidCards.forEach(card => {
        const control = new FormControl(card);
        const result = validator(control);
        expect(result).not.toBeNull();
      });
    });

    it('should detect card types correctly', () => {
      const validator = CustomValidators.creditCard({
        allowedTypes: ['visa']
      });

      // Mastercard number should be rejected
      const control = new FormControl('5555555555554444');
      const result = validator(control);

      expect(result).toEqual(jasmine.objectContaining({
        creditCardType: jasmine.any(Object)
      }));
    });

    it('should validate with Luhn algorithm', () => {
      const validator = CustomValidators.creditCard({ luhnCheck: true });
      const invalidLuhnCard = '4111111111111112'; // Invalid checksum

      const control = new FormControl(invalidLuhnCard);
      const result = validator(control);

      expect(result).toEqual(jasmine.objectContaining({
        creditCardLuhn: true
      }));
    });
  });

  describe('Filename Validator', () => {
    it('should validate safe filenames', () => {
      const validator = CustomValidators.filename();
      const safeFilenames = [
        'document.pdf',
        'image.jpg',
        'data-file.csv',
        'my_file_name.txt'
      ];

      safeFilenames.forEach(filename => {
        const control = new FormControl(filename);
        const result = validator(control);
        expect(result).toBeNull();
      });
    });

    it('should block dangerous file extensions', () => {
      const validator = CustomValidators.filename();
      const dangerousFiles = [
        'virus.exe',
        'script.bat',
        'malware.scr',
        'trojan.com'
      ];

      dangerousFiles.forEach(filename => {
        const control = new FormControl(filename);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          filenameBlocked: jasmine.any(Object)
        }));
      });
    });

    it('should detect path traversal attempts', () => {
      const validator = CustomValidators.filename();
      const maliciousFilenames = [
        '../../../etc/passwd',
        '..\\..\\windows\\system32\\config\\sam',
        './sensitive/file.txt'
      ];

      maliciousFilenames.forEach(filename => {
        const control = new FormControl(filename);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          filenameTraversal: true
        }));
      });
    });

    it('should detect dangerous characters', () => {
      const validator = CustomValidators.filename();
      const dangerousFilenames = [
        'file<script>.txt',
        'file>output.txt',
        'file:stream.txt',
        'file|pipe.txt',
        'file?query.txt',
        'file*wildcard.txt'
      ];

      dangerousFilenames.forEach(filename => {
        const control = new FormControl(filename);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          filenameDangerous: true
        }));
      });
    });

    it('should detect reserved Windows names', () => {
      const validator = CustomValidators.filename();
      const reservedNames = [
        'con.txt',
        'prn.pdf',
        'aux.doc',
        'nul.jpg',
        'com1.exe',
        'lpt1.bat'
      ];

      reservedNames.forEach(filename => {
        const control = new FormControl(filename);
        const result = validator(control);
        expect(result).toEqual(jasmine.objectContaining({
          filenameReserved: jasmine.any(Object)
        }));
      });
    });

    it('should respect allowed extensions', () => {
      const validator = CustomValidators.filename({
        allowedExtensions: ['jpg', 'png', 'pdf']
      });

      const control = new FormControl('document.doc');
      const result = validator(control);

      expect(result).toEqual(jasmine.objectContaining({
        filenameNotAllowed: jasmine.any(Object)
      }));
    });
  });

  describe('JSON Validator', () => {
    it('should validate correct JSON', () => {
      const validator = CustomValidators.json();
      const validJson = '{"name": "test", "value": 123}';

      const control = new FormControl(validJson);
      const result = validator(control);
      expect(result).toBeNull();
    });

    it('should reject invalid JSON', () => {
      const validator = CustomValidators.json();
      const invalidJson = '{"name": "test", "value":}';

      const control = new FormControl(invalidJson);
      const result = validator(control);
      expect(result).toEqual(jasmine.objectContaining({
        jsonInvalid: jasmine.any(Object)
      }));
    });

    it('should enforce size limits', () => {
      const validator = CustomValidators.json({ maxSize: 100 });
      const largeJson = '{"data": "' + 'x'.repeat(200) + '"}';

      const control = new FormControl(largeJson);
      const result = validator(control);
      expect(result).toEqual(jasmine.objectContaining({
        jsonTooLarge: jasmine.any(Object)
      }));
    });

    it('should enforce depth limits', () => {
      const validator = CustomValidators.json({ maxDepth: 2 });
      const deepJson = '{"a": {"b": {"c": {"d": "too deep"}}}}';

      const control = new FormControl(deepJson);
      const result = validator(control);
      expect(result).toEqual(jasmine.objectContaining({
        jsonTooDeep: jasmine.any(Object)
      }));
    });

    it('should block forbidden keys', () => {
      const validator = CustomValidators.json();
      const maliciousJson = '{"__proto__": {"isAdmin": true}}';

      const control = new FormControl(maliciousJson);
      const result = validator(control);
      expect(result).toEqual(jasmine.objectContaining({
        jsonForbiddenKeys: jasmine.any(Object)
      }));
    });
  });

  describe('Phone Validator', () => {
    it('should validate correct phone numbers', () => {
      const validator = CustomValidators.phone();
      const validPhones = [
        '1234567890',      // US format
        '905551234567',    // TR format with country code
        '5551234567'       // TR mobile format
      ];

      validPhones.forEach(phone => {
        const control = new FormControl(phone);
        const result = validator(control);
        expect(result).toBeNull();
      });
    });

    it('should reject short phone numbers', () => {
      const validator = CustomValidators.phone();
      const shortPhone = '123456';

      const control = new FormControl(shortPhone);
      const result = validator(control);
      expect(result).toEqual(jasmine.objectContaining({
        phoneTooShort: jasmine.any(Object)
      }));
    });

    it('should validate country-specific formats', () => {
      const validator = CustomValidators.phone({ countries: ['US'] });

      // Turkish phone number should fail for US-only validator
      const control = new FormControl('905551234567');
      const result = validator(control);
      expect(result).not.toBeNull();
    });
  });

  describe('Secure Pattern Validator', () => {
    it('should validate with custom patterns', () => {
      const validator = CustomValidators.securePattern(/^\d+$/, 'Numbers only');

      const validControl = new FormControl('123456');
      expect(validator(validControl)).toBeNull();

      const invalidControl = new FormControl('abc123');
      const result = validator(invalidControl);
      expect(result).toEqual(jasmine.objectContaining({
        pattern: jasmine.any(Object)
      }));
    });

    it('should detect ReDoS vulnerable patterns', () => {
      spyOn(console, 'warn');

      // This pattern is vulnerable to ReDoS
      const vulnerablePattern = /^(a+)+$/;
      CustomValidators.securePattern(vulnerablePattern);

      expect(console.warn).toHaveBeenCalledWith(
        'Potentially vulnerable regex pattern detected:',
        vulnerablePattern
      );
    });
  });

  describe('Edge Cases', () => {
    it('should handle null and undefined values', () => {
      const validators = [
        CustomValidators.email(),
        CustomValidators.password(),
        CustomValidators.url(),
        CustomValidators.xss(),
        CustomValidators.sqlInjection()
      ];

      validators.forEach(validator => {
        expect(validator(new FormControl(null))).toBeNull();
        expect(validator(new FormControl(undefined))).toBeNull();
        expect(validator(new FormControl(''))).toBeNull();
      });
    });

    it('should handle very long input strings', () => {
      const longString = 'a'.repeat(100000);
      const validator = CustomValidators.email({ maxLength: 50000 });

      const control = new FormControl(longString);
      const result = validator(control);

      expect(result).toEqual(jasmine.objectContaining({
        emailTooLong: jasmine.any(Object)
      }));
    });

    it('should handle special Unicode characters', () => {
      const validator = CustomValidators.email();
      const unicodeEmail = 'tëst@ëxämple.com';

      const control = new FormControl(unicodeEmail);
      const result = validator(control);

      // Should pass with international support enabled (default)
      expect(result).toBeNull();
    });
  });
});