import { Injectable } from '@angular/core';
import {
  SanitizationConfig,
  SecurityRiskLevel,
  InputType
} from '../interfaces/validation.interface';

/**
 * Enterprise Sanitization Service
 * Implements comprehensive input sanitization with security focus
 */
@Injectable({
  providedIn: 'root'
})
export class SanitizationService {

  private readonly defaultConfig: SanitizationConfig = {
    html: true,
    sql: true,
    scripts: true,
    urls: true,
    custom: [],
    preserveWhitespace: false,
    maxLength: 10000
  };

  /**
   * Sanitize input based on type and configuration
   */
  sanitize(
    value: any,
    inputType: InputType,
    config: Partial<SanitizationConfig> = {}
  ): { sanitizedValue: string; riskLevel: SecurityRiskLevel; warnings: string[] } {

    if (value === null || value === undefined) {
      return { sanitizedValue: '', riskLevel: 'none', warnings: [] };
    }

    const mergedConfig = { ...this.defaultConfig, ...config };
    let sanitized = value.toString();
    const warnings: string[] = [];
    let riskLevel: SecurityRiskLevel = 'none';

    // Length limitation
    if (mergedConfig.maxLength && sanitized.length > mergedConfig.maxLength) {
      sanitized = sanitized.substring(0, mergedConfig.maxLength);
      warnings.push(`Input truncated to ${mergedConfig.maxLength} characters`);
      riskLevel = this.escalateRisk(riskLevel, 'low');
    }

    // Input type specific sanitization
    switch (inputType) {
      case 'email':
        return this.sanitizeEmail(sanitized, mergedConfig, warnings, riskLevel);
      case 'password':
        return this.sanitizePassword(sanitized, mergedConfig, warnings, riskLevel);
      case 'url':
        return this.sanitizeUrl(sanitized, mergedConfig, warnings, riskLevel);
      case 'html':
        return this.sanitizeHtml(sanitized, mergedConfig, warnings, riskLevel);
      case 'filename':
        return this.sanitizeFilename(sanitized, mergedConfig, warnings, riskLevel);
      case 'json':
        return this.sanitizeJson(sanitized, mergedConfig, warnings, riskLevel);
      default:
        return this.sanitizeGeneral(sanitized, mergedConfig, warnings, riskLevel);
    }
  }

  /**
   * Email sanitization
   */
  private sanitizeEmail(
    value: string,
    config: SanitizationConfig,
    warnings: string[],
    riskLevel: SecurityRiskLevel
  ): { sanitizedValue: string; riskLevel: SecurityRiskLevel; warnings: string[] } {

    let sanitized = value.toLowerCase().trim();

    // Remove dangerous characters
    const dangerousChars = /[<>'";&|`$]/g;
    if (dangerousChars.test(sanitized)) {
      sanitized = sanitized.replace(dangerousChars, '');
      warnings.push('Dangerous characters removed from email');
      riskLevel = this.escalateRisk(riskLevel, 'medium');
    }

    // Remove multiple @ symbols (keep only first)
    const atCount = (sanitized.match(/@/g) || []).length;
    if (atCount > 1) {
      const parts = sanitized.split('@');
      sanitized = parts[0] + '@' + parts.slice(1).join('');
      warnings.push('Multiple @ symbols detected and corrected');
      riskLevel = this.escalateRisk(riskLevel, 'low');
    }

    // Remove spaces
    if (sanitized.includes(' ')) {
      sanitized = sanitized.replace(/\s+/g, '');
      warnings.push('Spaces removed from email');
      riskLevel = this.escalateRisk(riskLevel, 'low');
    }

    return { sanitizedValue: sanitized, riskLevel, warnings };
  }

  /**
   * Password sanitization (minimal - preserve security)
   */
  private sanitizePassword(
    value: string,
    config: SanitizationConfig,
    warnings: string[],
    riskLevel: SecurityRiskLevel
  ): { sanitizedValue: string; riskLevel: SecurityRiskLevel; warnings: string[] } {

    // For passwords, minimal sanitization to preserve intended characters
    let sanitized = value;

    // Only trim leading/trailing whitespace
    if (sanitized !== sanitized.trim()) {
      sanitized = sanitized.trim();
      warnings.push('Leading/trailing whitespace removed');
    }

    // Check for null bytes and control characters (security risk)
    if (/[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]/.test(sanitized)) {
      sanitized = sanitized.replace(/[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]/g, '');
      warnings.push('Control characters removed');
      riskLevel = this.escalateRisk(riskLevel, 'high');
    }

    return { sanitizedValue: sanitized, riskLevel, warnings };
  }

  /**
   * URL sanitization
   */
  private sanitizeUrl(
    value: string,
    config: SanitizationConfig,
    warnings: string[],
    riskLevel: SecurityRiskLevel
  ): { sanitizedValue: string; riskLevel: SecurityRiskLevel; warnings: string[] } {

    let sanitized = value.trim();

    // Remove dangerous protocols
    const dangerousProtocols = ['javascript:', 'data:', 'vbscript:', 'file:', 'ftp:'];
    for (const protocol of dangerousProtocols) {
      if (sanitized.toLowerCase().startsWith(protocol)) {
        sanitized = sanitized.substring(protocol.length);
        warnings.push(`Dangerous protocol ${protocol} removed`);
        riskLevel = this.escalateRisk(riskLevel, 'critical');
      }
    }

    // Ensure safe protocol if none provided
    if (!sanitized.match(/^https?:\/\//i) && sanitized.includes('.')) {
      sanitized = 'https://' + sanitized;
      warnings.push('HTTPS protocol added for security');
      riskLevel = this.escalateRisk(riskLevel, 'low');
    }

    // Remove HTML entities that might be used for obfuscation
    sanitized = this.decodeHtmlEntities(sanitized);

    // Remove suspicious URL encoding
    const suspiciousPatterns = [
      /%2[eE]/g,  // Encoded dots
      /%2[fF]/g,  // Encoded slashes
      /%3[cC]/g,  // Encoded <
      /%3[eE]/g,  // Encoded >
    ];

    for (const pattern of suspiciousPatterns) {
      if (pattern.test(sanitized)) {
        sanitized = decodeURIComponent(sanitized);
        warnings.push('Suspicious URL encoding detected and decoded');
        riskLevel = this.escalateRisk(riskLevel, 'medium');
        break;
      }
    }

    return { sanitizedValue: sanitized, riskLevel, warnings };
  }

  /**
   * HTML sanitization
   */
  private sanitizeHtml(
    value: string,
    config: SanitizationConfig,
    warnings: string[],
    riskLevel: SecurityRiskLevel
  ): { sanitizedValue: string; riskLevel: SecurityRiskLevel; warnings: string[] } {

    let sanitized = value;

    if (config.html) {
      // Remove script tags
      const scriptRegex = /<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi;
      if (scriptRegex.test(sanitized)) {
        sanitized = sanitized.replace(scriptRegex, '');
        warnings.push('Script tags removed');
        riskLevel = this.escalateRisk(riskLevel, 'critical');
      }

      // Remove event handlers
      const eventHandlerRegex = /\s*on\w+\s*=\s*["'][^"']*["']/gi;
      if (eventHandlerRegex.test(sanitized)) {
        sanitized = sanitized.replace(eventHandlerRegex, '');
        warnings.push('Event handlers removed');
        riskLevel = this.escalateRisk(riskLevel, 'high');
      }

      // Remove dangerous tags
      const dangerousTags = [
        'iframe', 'object', 'embed', 'applet', 'form', 'input',
        'button', 'select', 'textarea', 'link', 'meta', 'style'
      ];

      for (const tag of dangerousTags) {
        const tagRegex = new RegExp(`<${tag}\\b[^>]*>.*?<\/${tag}>`, 'gi');
        const selfClosingRegex = new RegExp(`<${tag}\\b[^>]*\/?>`, 'gi');

        if (tagRegex.test(sanitized) || selfClosingRegex.test(sanitized)) {
          sanitized = sanitized.replace(tagRegex, '').replace(selfClosingRegex, '');
          warnings.push(`Dangerous ${tag} tags removed`);
          riskLevel = this.escalateRisk(riskLevel, 'high');
        }
      }

      // Remove javascript: and data: protocols from attributes
      sanitized = sanitized.replace(/\b(href|src|action|formaction|background|cite|longdesc)\s*=\s*["']?(javascript|data|vbscript):/gi, '');
      if (sanitized !== value) {
        warnings.push('Dangerous protocols removed from attributes');
        riskLevel = this.escalateRisk(riskLevel, 'high');
      }
    }

    // HTML entity encoding for remaining content
    if (config.scripts) {
      sanitized = this.encodeHtmlEntities(sanitized);
    }

    return { sanitizedValue: sanitized, riskLevel, warnings };
  }

  /**
   * Filename sanitization
   */
  private sanitizeFilename(
    value: string,
    config: SanitizationConfig,
    warnings: string[],
    riskLevel: SecurityRiskLevel
  ): { sanitizedValue: string; riskLevel: SecurityRiskLevel; warnings: string[] } {

    let sanitized = value.trim();

    // Remove path traversal patterns
    if (sanitized.includes('..') || sanitized.includes('./') || sanitized.includes('.\\')) {
      sanitized = sanitized.replace(/\.\.[\/\\]?/g, '').replace(/\.[\/\\]/g, '');
      warnings.push('Path traversal patterns removed');
      riskLevel = this.escalateRisk(riskLevel, 'high');
    }

    // Remove dangerous characters
    const dangerousChars = /[<>:"|?*\x00-\x1f]/g;
    if (dangerousChars.test(sanitized)) {
      sanitized = sanitized.replace(dangerousChars, '_');
      warnings.push('Dangerous characters replaced with underscores');
      riskLevel = this.escalateRisk(riskLevel, 'medium');
    }

    // Handle reserved Windows names
    const reservedNames = /^(con|prn|aux|nul|com[1-9]|lpt[1-9])(\.|$)/i;
    if (reservedNames.test(sanitized)) {
      sanitized = '_' + sanitized;
      warnings.push('Reserved filename prefix added');
      riskLevel = this.escalateRisk(riskLevel, 'medium');
    }

    // Limit length and ensure extension
    if (sanitized.length > 255) {
      const parts = sanitized.split('.');
      if (parts.length > 1) {
        const ext = parts.pop();
        const name = parts.join('.');
        sanitized = name.substring(0, 255 - ext!.length - 1) + '.' + ext;
      } else {
        sanitized = sanitized.substring(0, 255);
      }
      warnings.push('Filename truncated to 255 characters');
      riskLevel = this.escalateRisk(riskLevel, 'low');
    }

    return { sanitizedValue: sanitized, riskLevel, warnings };
  }

  /**
   * JSON sanitization
   */
  private sanitizeJson(
    value: string,
    config: SanitizationConfig,
    warnings: string[],
    riskLevel: SecurityRiskLevel
  ): { sanitizedValue: string; riskLevel: SecurityRiskLevel; warnings: string[] } {

    let sanitized = value.trim();

    try {
      // Parse and re-stringify to validate JSON
      const parsed = JSON.parse(sanitized);

      // Remove dangerous keys
      const cleanedObject = this.removeObjectKeys(parsed, ['__proto__', 'constructor', 'prototype']);

      if (JSON.stringify(cleanedObject) !== JSON.stringify(parsed)) {
        warnings.push('Dangerous object keys removed');
        riskLevel = this.escalateRisk(riskLevel, 'high');
        // Null-prototype object to ensure __proto__ is truly undefined on parsed result
        const nullProto = Object.assign(Object.create(null), cleanedObject);
        sanitized = JSON.stringify(nullProto);
      } else {
        // No change -> orijinal format korunur
        sanitized = value;
      }

    } catch (error) {
      // If parsing fails, escape the string
      sanitized = JSON.stringify(sanitized);
      warnings.push('Invalid JSON converted to string literal');
      riskLevel = this.escalateRisk(riskLevel, 'medium');
    }

    return { sanitizedValue: sanitized, riskLevel, warnings };
  }

  /**
   * General sanitization for text inputs
   */
  private sanitizeGeneral(
    value: string,
    config: SanitizationConfig,
    warnings: string[],
    riskLevel: SecurityRiskLevel
  ): { sanitizedValue: string; riskLevel: SecurityRiskLevel; warnings: string[] } {

    let sanitized = value;

    if (!config.preserveWhitespace) {
      sanitized = sanitized.trim().replace(/\s+/g, ' ');
    }

    // Apply SQL injection protection
    if (config.sql) {
      const result = this.sanitizeSqlInjection(sanitized, warnings, riskLevel);
      sanitized = result.sanitizedValue;
      riskLevel = result.riskLevel;
      warnings.push(...result.warnings);
    }

    // Apply XSS protection
    if (config.html || config.scripts) {
      const result = this.sanitizeXss(sanitized, warnings, riskLevel);
      sanitized = result.sanitizedValue;
      riskLevel = result.riskLevel;
      warnings.push(...result.warnings);
    }

    // Apply custom rules
    for (const rule of config.custom) {
      if (rule.pattern.test(sanitized)) {
        sanitized = sanitized.replace(rule.global ? new RegExp(rule.pattern, 'g') : rule.pattern, rule.replacement);
        warnings.push(`Custom rule applied: ${rule.name}`);
        riskLevel = this.escalateRisk(riskLevel, 'low');
      }
    }

    return { sanitizedValue: sanitized, riskLevel, warnings };
  }

  /**
   * SQL injection sanitization
   */
  private sanitizeSqlInjection(
    value: string,
    warnings: string[],
    riskLevel: SecurityRiskLevel
  ): { sanitizedValue: string; riskLevel: SecurityRiskLevel; warnings: string[] } {

    let sanitized = value;
    const originalValue = value;

    // Remove SQL comments
    sanitized = sanitized.replace(/--.*$/gm, '').replace(/\/\*.*?\*\//g, '');

    // Escape single quotes
    sanitized = sanitized.replace(/'/g, "''");

    // Remove dangerous SQL keywords in suspicious contexts
    const dangerousPatterns = [
      /(\bselect\b.*\bfrom\b)/gi,
      /(\binsert\b.*\binto\b)/gi,
      /(\bupdate\b.*\bset\b)/gi,
      /(\bdelete\b.*\bfrom\b)/gi,
      /(\bdrop\b.*\btable\b)/gi,
      /(\bunion\b.*\bselect\b)/gi,
      /(\bexec\b|\bexecute\b)/gi
    ];

    for (const pattern of dangerousPatterns) {
      if (pattern.test(sanitized)) {
        sanitized = sanitized.replace(pattern, '');
        warnings.push('Potential SQL injection pattern removed');
        riskLevel = this.escalateRisk(riskLevel, 'critical');
      }
    }

    if (sanitized !== originalValue) {
      warnings.push('SQL injection protection applied');
    }

    return { sanitizedValue: sanitized, riskLevel, warnings };
  }

  /**
   * XSS sanitization
   */
  private sanitizeXss(
    value: string,
    warnings: string[],
    riskLevel: SecurityRiskLevel
  ): { sanitizedValue: string; riskLevel: SecurityRiskLevel; warnings: string[] } {

    let sanitized = value;
    const originalValue = value;

    // Remove script tags
    sanitized = sanitized.replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, '');

    // Remove event handlers
    sanitized = sanitized.replace(/\bon\w+\s*=\s*["'][^"']*["']/gi, '');

    // Remove javascript: protocols
    sanitized = sanitized.replace(/javascript\s*:/gi, '');

    // Remove data: protocols with base64
    sanitized = sanitized.replace(/data\s*:\s*[^;]*;\s*base64/gi, '');

    // Encode HTML entities
    sanitized = this.encodeHtmlEntities(sanitized);

    if (sanitized !== originalValue) {
      warnings.push('XSS protection applied');
      riskLevel = this.escalateRisk(riskLevel, 'high');
    }

    return { sanitizedValue: sanitized, riskLevel, warnings };
  }

  /**
   * Helper: Escalate risk level
   */
  private escalateRisk(current: SecurityRiskLevel, newRisk: SecurityRiskLevel): SecurityRiskLevel {
    const levels: SecurityRiskLevel[] = ['none', 'low', 'medium', 'high', 'critical'];
    const currentIndex = levels.indexOf(current);
    const newIndex = levels.indexOf(newRisk);
    return levels[Math.max(currentIndex, newIndex)];
  }

  /**
   * Helper: HTML entity encoding
   */
  private encodeHtmlEntities(text: string): string {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }

  /**
   * Helper: HTML entity decoding
   */
  private decodeHtmlEntities(text: string): string {
    const div = document.createElement('div');
    div.innerHTML = text;
    return div.textContent || div.innerText || '';
  }

  /**
   * Helper: Remove dangerous object keys recursively
   */
  private removeObjectKeys(obj: any, keysToRemove: string[]): any {
    if (obj === null || typeof obj !== 'object') {
      return obj;
    }

    if (Array.isArray(obj)) {
      return obj.map(item => this.removeObjectKeys(item, keysToRemove));
    }

    const cleaned: any = {};
    for (const key in obj) {
      if (Object.prototype.hasOwnProperty.call(obj, key) && !keysToRemove.includes(key)) {
        cleaned[key] = this.removeObjectKeys(obj[key], keysToRemove);
      }
    }

    return cleaned;
  }

  /**
   * Get sanitization recommendations based on input type
   */
  getSanitizationRecommendations(inputType: InputType): string[] {
    const recommendations: string[] = [];

    switch (inputType) {
      case 'email':
        recommendations.push(
          'Enable email domain validation',
          'Block disposable email domains',
          'Implement email verification',
          'Use allowlist for trusted domains'
        );
        break;

      case 'password':
        recommendations.push(
          'Implement strong password policies',
          'Use secure password hashing (bcrypt/scrypt)',
          'Enable password breach checking',
          'Implement rate limiting for login attempts'
        );
        break;

      case 'html':
        recommendations.push(
          'Use Content Security Policy (CSP)',
          'Implement HTML tag allowlisting',
          'Enable DOM purification',
          'Use secure rendering frameworks'
        );
        break;

      case 'url':
        recommendations.push(
          'Validate URL protocols (allow only HTTP/HTTPS)',
          'Implement domain allowlisting',
          'Check for URL redirects',
          'Scan URLs for malicious content'
        );
        break;

      default:
        recommendations.push(
          'Implement input length limits',
          'Use parameterized queries for database operations',
          'Enable real-time security monitoring',
          'Regular security audits of input validation'
        );
    }

    return recommendations;
  }
}