import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import {
  EmailValidatorOptions,
  PasswordValidatorOptions,
  UrlValidatorOptions,
  PhoneValidatorOptions,
  CreditCardValidatorOptions,
  FilenameValidatorOptions,
  JsonValidatorOptions,
  SqlValidatorOptions,
  XssValidatorOptions,
  CreditCardType
} from '../interfaces/validation.interface';

/**
 * Enterprise Custom Validators Library
 * Implements comprehensive validation rules with security focus
 */
export class CustomValidators {

  /**
   * Enhanced email validator with security features
   */
  static email(options: EmailValidatorOptions = {}): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) return null;

      const email = control.value.toString().toLowerCase().trim();
      const {
        allowInternational = true,
        allowSubdomains = true,
        blockedDomains = [],
        requiredDomains = [],
        maxLength = 320
      } = options;

      // Length check
      if (email.length > maxLength) {
        return { emailTooLong: { maxLength, actualLength: email.length } };
      }

      // Basic email format
      const emailRegex = allowInternational
        ? /^[^\s@]+@[^\s@]+\.[^\s@]+$/
        : /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;

      if (!emailRegex.test(email)) {
        return { emailInvalid: { value: email } };
      }

      const [localPart, domain] = email.split('@');

      // Local part validation
      if (localPart.length > 64) {
        return { emailLocalTooLong: { maxLength: 64, actualLength: localPart.length } };
      }

      // Check for dangerous patterns
      const dangerousPatterns = [
        /[<>]/,           // HTML tags
        /javascript:/i,   // JavaScript protocol
        /data:/i,         // Data protocol
        /vbscript:/i,     // VBScript protocol
        /['"]/,           // Quotes that might break HTML
        /[;|&]/           // Command injection chars
      ];

      for (const pattern of dangerousPatterns) {
        if (pattern.test(email)) {
          return { emailSecurity: { reason: 'Dangerous characters detected' } };
        }
      }

      // Domain validation
      if (!allowSubdomains && domain.split('.').length > 2) {
        return { emailSubdomain: { domain } };
      }

      // Blocked domains check
      if (blockedDomains.some(blocked => domain.endsWith(blocked))) {
        return { emailBlockedDomain: { domain } };
      }

      // Required domains check
      if (requiredDomains.length > 0 && !requiredDomains.some(required => domain.endsWith(required))) {
        return { emailRequiredDomain: { domain, requiredDomains } };
      }

      // Check for disposable email domains
      const disposableDomains = [
        '10minutemail.com', 'tempmail.org', 'guerrillamail.com',
        'mailinator.com', 'yopmail.com', 'throwaway.email'
      ];

      if (disposableDomains.some(disposable => domain.endsWith(disposable))) {
        return { emailDisposable: { domain } };
      }

      return null;
    };
  }

  /**
   * Advanced password validator with security requirements
   */
  static password(options: PasswordValidatorOptions = {}): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) return null;

      const password = control.value.toString();
      const {
        minLength = 8,
        maxLength = 128,
        requireUppercase = true,
        requireLowercase = true,
        requireNumbers = true,
        requireSpecialChars = true,
        forbiddenPatterns = [],
        checkCommonPasswords = true,
        checkSequentialChars = true,
        maxRepeatingChars = 3
      } = options;

      const errors: ValidationErrors = {};

      // Length validation
      if (password.length < minLength) {
        errors['passwordTooShort'] = { minLength, actualLength: password.length };
      }

      if (password.length > maxLength) {
        errors['passwordTooLong'] = { maxLength, actualLength: password.length };
      }

      // Character requirements
      if (requireUppercase && !/[A-Z]/.test(password)) {
        errors['passwordNoUppercase'] = true;
      }

      if (requireLowercase && !/[a-z]/.test(password)) {
        errors['passwordNoLowercase'] = true;
      }

      if (requireNumbers && !/\d/.test(password)) {
        errors['passwordNoNumbers'] = true;
      }

      if (requireSpecialChars && !/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?~`]/.test(password)) {
        errors['passwordNoSpecialChars'] = true;
      }

      // Sequential characters check
      if (checkSequentialChars) {
        const sequences = ['123', 'abc', 'qwe', '456', 'def', 'ert'];
        if (sequences.some(seq => password.toLowerCase().includes(seq))) {
          errors['passwordSequential'] = true;
        }
      }

      // Repeating characters check
      if (maxRepeatingChars > 0) {
        const regex = new RegExp(`(.)\\1{${maxRepeatingChars},}`, 'i');
        if (regex.test(password)) {
          errors['passwordRepeating'] = { maxRepeating: maxRepeatingChars };
        }
      }

      // Common passwords check
      if (checkCommonPasswords) {
        const commonPasswords = [
          'password', '123456', 'password123', 'admin', 'qwerty',
          'letmein', 'welcome', 'monkey', 'dragon', 'master'
        ];

        if (commonPasswords.some(common => password.toLowerCase().includes(common))) {
          errors['passwordCommon'] = true;
        }
      }

      // Forbidden patterns
      for (const pattern of forbiddenPatterns) {
        const regex = new RegExp(pattern, 'i');
        if (regex.test(password)) {
          errors['passwordForbidden'] = { pattern };
          break;
        }
      }

      // Personal information patterns (basic check)
      const personalPatterns = [
        /\b(admin|administrator|root|user)\b/i,
        /\b(password|pass|pwd)\b/i,
        /\b\d{4}-\d{2}-\d{2}\b/, // Date patterns
        /\b\d{10,}\b/ // Long numbers (might be phone/ssn)
      ];

      for (const pattern of personalPatterns) {
        if (pattern.test(password)) {
          errors['passwordPersonalInfo'] = true;
          break;
        }
      }

      return Object.keys(errors).length > 0 ? errors : null;
    };
  }

  /**
   * Secure URL validator
   */
  static url(options: UrlValidatorOptions = {}): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) return null;

      const url = control.value.toString().trim();
      const {
        allowedProtocols = ['http', 'https'],
        allowedDomains = [],
        blockedDomains = [],
        allowLocalhost = false,
        allowIP = false,
        maxLength = 2048
      } = options;

      // Length check
      if (url.length > maxLength) {
        return { urlTooLong: { maxLength, actualLength: url.length } };
      }

      let parsedUrl: URL;
      try {
        parsedUrl = new URL(url);
      } catch {
        return { urlInvalid: { value: url } };
      }

      // Protocol validation
      const protocol = parsedUrl.protocol.slice(0, -1); // Remove ':'
      if (!allowedProtocols.includes(protocol)) {
        return { urlProtocol: { protocol, allowedProtocols } };
      }

      // Dangerous protocols check
      const dangerousProtocols = ['javascript', 'data', 'vbscript', 'file', 'ftp'];
      if (dangerousProtocols.includes(protocol)) {
        return { urlDangerous: { protocol } };
      }

      const hostname = parsedUrl.hostname.toLowerCase();

      // Localhost check
      if (!allowLocalhost && (hostname === 'localhost' || hostname === '127.0.0.1' || hostname.startsWith('192.168.'))) {
        return { urlLocalhost: { hostname } };
      }

      // IP address check
      if (!allowIP && /^\d+\.\d+\.\d+\.\d+$/.test(hostname)) {
        return { urlIP: { hostname } };
      }

      // Blocked domains
      if (blockedDomains.some(blocked => hostname.endsWith(blocked))) {
        return { urlBlocked: { hostname } };
      }

      // Allowed domains (if specified)
      if (allowedDomains.length > 0 && !allowedDomains.some(allowed => hostname.endsWith(allowed))) {
        return { urlNotAllowed: { hostname, allowedDomains } };
      }

      // Check for suspicious patterns
      if (hostname.includes('..') || hostname.includes('--')) {
        return { urlSuspicious: { hostname } };
      }

      return null;
    };
  }

  /**
   * XSS (Cross-Site Scripting) validator
   */
  static xss(options: XssValidatorOptions = {}): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) return null;

      const value = control.value.toString();
      const {
        allowedTags = [],
        stripTags = false,
        encodeEntities = true
      } = options;

      // Script tag detection
      if (/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi.test(value)) {
        return { xssScript: true };
      }

      // Event handler detection
      const eventHandlers = /on\w+\s*=/i;
      if (eventHandlers.test(value)) {
        return { xssEventHandler: true };
      }

      // JavaScript protocol detection
      if (/javascript\s*:/i.test(value)) {
        return { xssJavascript: true };
      }

      // Data URI with script
      if (/data\s*:\s*[^;]*;\s*base64/i.test(value) && /script/i.test(value)) {
        return { xssDataUri: true };
      }

      // HTML tag detection (if not allowed)
      const htmlTags = /<[^>]+>/g;
      const foundTags = value.match(htmlTags);

      if (foundTags) {
        const forbiddenTags = foundTags.filter(tag => {
          const tagName = tag.match(/<\/?([a-zA-Z][a-zA-Z0-9]*)/)?.[1]?.toLowerCase();
          return tagName && !allowedTags.includes(tagName);
        });

        if (forbiddenTags.length > 0) {
          return { xssHtmlTags: { tags: forbiddenTags } };
        }
      }

      // SQL injection patterns in HTML context
      const sqlPatterns = [
        /'\s*(or|and)\s*'?\d/i,
        /union\s+select/i,
        /insert\s+into/i,
        /delete\s+from/i,
        /drop\s+table/i
      ];

      for (const pattern of sqlPatterns) {
        if (pattern.test(value)) {
          return { xssSqlInjection: true };
        }
      }

      return null;
    };
  }

  /**
   * SQL injection validator
   */
  static sqlInjection(options: SqlValidatorOptions = {}): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) return null;

      const value = control.value.toString().toLowerCase();
      const {
        blockKeywords = [
          'select', 'insert', 'update', 'delete', 'drop', 'create',
          'alter', 'execute', 'union', 'declare', 'exec'
        ],
        checkFunctions = true,
        checkComments = true
      } = options;

      // SQL keywords
      for (const keyword of blockKeywords) {
        const regex = new RegExp(`\\b${keyword}\\b`, 'i');
        if (regex.test(value)) {
          return { sqlKeyword: { keyword } };
        }
      }

      // SQL injection patterns
      const injectionPatterns = [
        /'\s*(or|and)\s*'?\d/,      // Classic SQL injection
        /'\s*(or|and)\s*'\w/,       // String-based injection
        /'\s*;\s*\w/,               // Command chaining
        /'\s*--/,                   // Comment injection
        /'\s*\/\*/,                 // Comment block
        /'\s*\|\|/,                 // Concatenation
        /0x[0-9a-f]+/,              // Hex values
        /char\s*\(/,                // CHAR function
        /ascii\s*\(/,               // ASCII function
        /benchmark\s*\(/,           // BENCHMARK function
        /sleep\s*\(/,               // SLEEP function
        /waitfor\s+delay/           // WAITFOR DELAY
      ];

      for (const pattern of injectionPatterns) {
        if (pattern.test(value)) {
          return { sqlInjection: { pattern: pattern.source } };
        }
      }

      // SQL functions (if checking is enabled)
      if (checkFunctions) {
        const functions = [
          'concat', 'substring', 'length', 'user', 'database',
          'version', 'count', 'group_concat', 'load_file'
        ];

        for (const func of functions) {
          const regex = new RegExp(`${func}\\s*\\(`, 'i');
          if (regex.test(value)) {
            return { sqlFunction: { function: func } };
          }
        }
      }

      // SQL comments (if checking is enabled)
      if (checkComments) {
        if (/--|\|\*|\*\/|#/.test(value)) {
          return { sqlComment: true };
        }
      }

      return null;
    };
  }

  /**
   * Credit card validator with Luhn algorithm
   */
  static creditCard(options: CreditCardValidatorOptions = {}): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) return null;

      const cardNumber = control.value.toString().replace(/\D/g, '');
      const { allowedTypes = [], luhnCheck = true } = options;

      // Basic length and digit check
      if (!/^\d{13,19}$/.test(cardNumber)) {
        return { creditCardInvalid: true };
      }

      // Card type detection
      const cardType = this.detectCardType(cardNumber);
      if (allowedTypes.length > 0 && !allowedTypes.includes(cardType)) {
        return { creditCardType: { detected: cardType, allowed: allowedTypes } };
      }

      // Luhn algorithm validation
      if (luhnCheck && !this.luhnValidation(cardNumber)) {
        return { creditCardLuhn: true };
      }

      return null;
    };
  }

  /**
   * Filename validator with security checks
   */
  static filename(options: FilenameValidatorOptions = {}): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) return null;

      const filename = control.value.toString();
      const {
        allowedExtensions = [],
        blockedExtensions = ['exe', 'bat', 'cmd', 'scr', 'pif', 'com'],
        maxLength = 255,
        allowSpaces = true,
        allowUnicode = false
      } = options;

      // Length check
      if (filename.length > maxLength) {
        return { filenameTooLong: { maxLength, actualLength: filename.length } };
      }

      // Dangerous characters
      const dangerousChars = /[<>:"|?*\x00-\x1f]/;
      if (dangerousChars.test(filename)) {
        return { filenameDangerous: true };
      }

      // Path traversal
      if (filename.includes('..') || filename.includes('./') || filename.includes('.\\')) {
        return { filenameTraversal: true };
      }

      // Spaces check
      if (!allowSpaces && /\s/.test(filename)) {
        return { filenameSpaces: true };
      }

      // Unicode check
      if (!allowUnicode && /[^\x00-\x7F]/.test(filename)) {
        return { filenameUnicode: true };
      }

      // Extension validation
      const extension = filename.split('.').pop()?.toLowerCase();
      if (extension) {
        if (blockedExtensions.includes(extension)) {
          return { filenameBlocked: { extension } };
        }

        if (allowedExtensions.length > 0 && !allowedExtensions.includes(extension)) {
          return { filenameNotAllowed: { extension, allowed: allowedExtensions } };
        }
      }

      // Reserved names (Windows)
      const reservedNames = [
        'con', 'prn', 'aux', 'nul', 'com1', 'com2', 'com3', 'com4',
        'com5', 'com6', 'com7', 'com8', 'com9', 'lpt1', 'lpt2',
        'lpt3', 'lpt4', 'lpt5', 'lpt6', 'lpt7', 'lpt8', 'lpt9'
      ];

      const nameWithoutExt = filename.split('.')[0].toLowerCase();
      if (reservedNames.includes(nameWithoutExt)) {
        return { filenameReserved: { name: nameWithoutExt } };
      }

      return null;
    };
  }

  /**
   * JSON validator with security checks
   */
  static json(options: JsonValidatorOptions = {}): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) return null;

      const jsonString = control.value.toString();
      const {
        maxDepth = 10,
        maxSize = 1024 * 1024, // 1MB
        allowedKeys = [],
        forbiddenKeys = ['__proto__', 'constructor', 'prototype']
      } = options;

      // Size check
      if (jsonString.length > maxSize) {
        return { jsonTooLarge: { maxSize, actualSize: jsonString.length } };
      }

      let parsed: any;
      try {
        parsed = JSON.parse(jsonString);
      } catch (error) {
        return { jsonInvalid: { error: error.message } };
      }

      // Depth check
      const depth = this.getObjectDepth(parsed);
      if (depth > maxDepth) {
        return { jsonTooDeep: { maxDepth, actualDepth: depth } };
      }

      // Key validation
      const allKeys = this.getAllObjectKeys(parsed);

      // Forbidden keys check
      const foundForbiddenKeys = allKeys.filter(key => forbiddenKeys.includes(key));
      if (foundForbiddenKeys.length > 0) {
        return { jsonForbiddenKeys: { keys: foundForbiddenKeys } };
      }

      // Allowed keys check (if specified)
      if (allowedKeys.length > 0) {
        const invalidKeys = allKeys.filter(key => !allowedKeys.includes(key));
        if (invalidKeys.length > 0) {
          return { jsonInvalidKeys: { keys: invalidKeys, allowed: allowedKeys } };
        }
      }

      return null;
    };
  }

  // Helper methods

  private static detectCardType(cardNumber: string): CreditCardType {
    if (/^4/.test(cardNumber)) return 'visa';
    if (/^5[1-5]/.test(cardNumber)) return 'mastercard';
    if (/^3[47]/.test(cardNumber)) return 'amex';
    if (/^6(?:011|5)/.test(cardNumber)) return 'discover';
    if (/^3[0689]/.test(cardNumber)) return 'diners';
    if (/^35/.test(cardNumber)) return 'jcb';
    return 'visa'; // Default
  }

  private static luhnValidation(cardNumber: string): boolean {
    let sum = 0;
    let alternate = false;

    for (let i = cardNumber.length - 1; i >= 0; i--) {
      let digit = parseInt(cardNumber.charAt(i), 10);

      if (alternate) {
        digit *= 2;
        if (digit > 9) {
          digit = (digit % 10) + 1;
        }
      }

      sum += digit;
      alternate = !alternate;
    }

    return sum % 10 === 0;
  }

  private static getObjectDepth(obj: any): number {
    if (obj === null || typeof obj !== 'object') {
      return 0;
    }

    let maxDepth = 0;
    for (const key in obj) {
      if (obj.hasOwnProperty(key)) {
        const depth = this.getObjectDepth(obj[key]);
        maxDepth = Math.max(maxDepth, depth);
      }
    }

    return maxDepth + 1;
  }

  private static getAllObjectKeys(obj: any): string[] {
    const keys: string[] = [];

    const traverse = (current: any) => {
      if (current && typeof current === 'object') {
        for (const key in current) {
          if (current.hasOwnProperty(key)) {
            keys.push(key);
            traverse(current[key]);
          }
        }
      }
    };

    traverse(obj);
    return keys;
  }

  /**
   * Phone number validator
   */
  static phone(options: PhoneValidatorOptions = {}): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) return null;

      const phone = control.value.toString().replace(/\D/g, '');
      const { countries = ['US', 'TR'], format = 'international' } = options;

      // Basic validation - at least 10 digits
      if (phone.length < 10) {
        return { phoneTooShort: { minLength: 10, actualLength: phone.length } };
      }

      // Turkey phone validation
      if (countries.includes('TR')) {
        if (phone.startsWith('90') && phone.length === 13) return null;
        if (phone.startsWith('5') && phone.length === 10) return null;
      }

      // US phone validation
      if (countries.includes('US')) {
        if (phone.startsWith('1') && phone.length === 11) return null;
        if (phone.length === 10) return null;
      }

      return { phoneInvalid: { value: control.value } };
    };
  }

  /**
   * Custom pattern validator with security enhancements
   */
  static securePattern(pattern: RegExp, message?: string): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) return null;

      const value = control.value.toString();

      // Check for ReDoS vulnerable patterns
      if (this.isReDoSVulnerable(pattern)) {
        console.warn('Potentially vulnerable regex pattern detected:', pattern);
      }

      if (!pattern.test(value)) {
        return { pattern: { pattern: pattern.source, value, message } };
      }

      return null;
    };
  }

  private static isReDoSVulnerable(pattern: RegExp): boolean {
    const vulnerablePatterns = [
      /\(\.\*\)\+/,           // (.*)+
      /\(\.\+\)\*/,           // (.+)*
      /\(\w\*\)\+/,           // (\w*)+
      /\(\w\+\)\*/,           // (\w+)*
    ];

    return vulnerablePatterns.some(vuln => vuln.test(pattern.source));
  }
}