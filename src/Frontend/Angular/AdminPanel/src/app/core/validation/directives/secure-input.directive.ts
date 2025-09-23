import {
  Directive,
  ElementRef,
  Input,
  OnInit,
  OnDestroy,
  Renderer2,
  HostListener,
  inject,
  signal,
  computed,
  effect,
  ViewContainerRef,
  ComponentRef
} from '@angular/core';
import { NgControl } from '@angular/forms';
import { Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  ValidationRule,
  ValidationContext,
  InputType,
  SecurityContext,
  SecurityRiskLevel
} from '../interfaces/validation.interface';
import { ValidationService } from '../services/validation.service';
import { SecurityValidationRules } from '../rules/security-rules';
import { ValidationFeedbackComponent } from '../components/validation-feedback.component';

/**
 * Secure Input Directive
 * Provides real-time XSS/Injection protection for form inputs
 */
@Directive({
  selector: '[appSecureInput]',
  standalone: true,
  exportAs: 'secureInput'
})
export class SecureInputDirective implements OnInit, OnDestroy {

  private readonly el = inject(ElementRef);
  private readonly renderer = inject(Renderer2);
  private readonly ngControl = inject(NgControl, { optional: true });
  private readonly validationService = inject(ValidationService);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly destroy$ = new Subject<void>();

  // Input properties
  @Input() inputType: InputType = 'text';
  @Input() securityContext: SecurityContext = 'public';
  @Input() enableRealTimeValidation = true;
  @Input() enableSecurityHighlighting = true;
  @Input() enableSanitization = true;
  @Input() customValidationRules: ValidationRule[] = [];
  @Input() showValidationFeedback = true;
  @Input() maxLength?: number;
  @Input() debounceTime = 300;

  // Signals for reactive state management
  private readonly currentValue = signal<string>('');
  private readonly validationResult = signal<any>(null);
  private readonly isValidating = signal(false);
  private readonly securityThreatDetected = signal(false);

  // Computed properties
  private readonly validationRules = computed(() => {
    const defaultRules = SecurityValidationRules.getDefaultRules(
      this.inputType,
      this.securityContext
    );
    return [...defaultRules, ...this.customValidationRules];
  });

  private readonly currentRiskLevel = computed((): SecurityRiskLevel => {
    const result = this.validationResult();
    return result?.riskLevel || 'none';
  });

  private readonly shouldHighlight = computed(() =>
    this.enableSecurityHighlighting && this.currentRiskLevel() !== 'none'
  );

  private feedbackComponent?: ComponentRef<ValidationFeedbackComponent>;

  constructor() {
    // React to security risk changes
    effect(() => {
      if (this.shouldHighlight()) {
        this.applySecurityHighlighting();
      } else {
        this.removeSecurityHighlighting();
      }
    });

    // React to validation results
    effect(() => {
      const result = this.validationResult();
      if (result && !result.valid) {
        this.securityThreatDetected.set(true);
        this.handleSecurityThreat(result);
      } else {
        this.securityThreatDetected.set(false);
      }
    });
  }

  ngOnInit(): void {
    this.initializeSecureInput();
    this.setupValidationMonitoring();
    this.setupEventListeners();

    if (this.showValidationFeedback && this.ngControl) {
      this.createValidationFeedback();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.cleanupValidationFeedback();
  }

  /**
   * Initialize secure input with security attributes
   */
  private initializeSecureInput(): void {
    const element = this.el.nativeElement;

    // Set security attributes
    this.renderer.setAttribute(element, 'data-security-context', this.securityContext);
    this.renderer.setAttribute(element, 'data-input-type', this.inputType);
    this.renderer.setAttribute(element, 'autocomplete', 'off');
    this.renderer.setAttribute(element, 'spellcheck', 'false');

    // Set maximum length if specified
    if (this.maxLength) {
      this.renderer.setAttribute(element, 'maxlength', this.maxLength.toString());
    }

    // Add security CSS classes
    this.renderer.addClass(element, 'secure-input');
    this.renderer.addClass(element, `security-context-${this.securityContext}`);
    this.renderer.addClass(element, `input-type-${this.inputType}`);

    // Disable drag and drop for security
    this.renderer.setAttribute(element, 'ondrop', 'return false;');
    this.renderer.setAttribute(element, 'ondragover', 'return false;');

    // Add paste protection for sensitive contexts
    if (this.securityContext === 'admin' || this.securityContext === 'system') {
      this.renderer.setAttribute(element, 'onpaste', 'return false;');
    }
  }

  /**
   * Setup real-time validation monitoring
   */
  private setupValidationMonitoring(): void {
    if (!this.enableRealTimeValidation || !this.ngControl) return;

    // Monitor control value changes
    this.ngControl.valueChanges?.pipe(
      debounceTime(this.debounceTime),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(async (value) => {
      if (value !== null && value !== undefined) {
        this.currentValue.set(value.toString());
        await this.validateInput(value);
      }
    });
  }

  /**
   * Setup additional event listeners for security
   */
  private setupEventListeners(): void {
    const element = this.el.nativeElement;

    // Prevent right-click in high-security contexts
    if (this.securityContext === 'system') {
      this.renderer.listen(element, 'contextmenu', (event) => {
        event.preventDefault();
        return false;
      });
    }

    // Monitor focus events for logging
    this.renderer.listen(element, 'focus', () => {
      this.logSecurityEvent('input_focus');
    });

    // Monitor input events for immediate threat detection
    this.renderer.listen(element, 'input', (event) => {
      this.performImmediateThreatDetection(event.target.value);
    });

    // Monitor paste events
    this.renderer.listen(element, 'paste', (event) => {
      this.handlePasteEvent(event);
    });
  }

  /**
   * Validate input value with security rules
   */
  private async validateInput(value: string): Promise<void> {
    if (!value) {
      this.validationResult.set(null);
      return;
    }

    this.isValidating.set(true);

    try {
      const context: ValidationContext = {
        control: this.ngControl!.control!,
        inputType: this.inputType,
        securityContext: this.securityContext
      };

      const result = await this.validationService.validate(
        value,
        this.validationRules(),
        context
      );

      this.validationResult.set(result);

      // Apply sanitization if enabled and needed
      if (this.enableSanitization && result.sanitizedValue !== undefined) {
        this.applySanitizedValue(result.sanitizedValue);
      }

    } catch (error) {
      console.error('Validation error in secure input directive:', error);
      this.validationResult.set({
        valid: false,
        errors: ['Validation failed'],
        warnings: [],
        riskLevel: 'medium'
      });
    } finally {
      this.isValidating.set(false);
    }
  }

  /**
   * Perform immediate threat detection without debouncing
   */
  private performImmediateThreatDetection(value: string): void {
    if (!value) return;

    // Check for immediate critical threats
    const criticalPatterns = [
      /<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi,
      /javascript\s*:/gi,
      /data\s*:\s*text\/html/gi,
      /vbscript\s*:/gi
    ];

    const hasCriticalThreat = criticalPatterns.some(pattern => pattern.test(value));

    if (hasCriticalThreat) {
      this.handleImmediateThreat(value);
    }
  }

  /**
   * Handle immediate security threats
   */
  private handleImmediateThreat(value: string): void {
    const element = this.el.nativeElement;

    // Add critical threat styling
    this.renderer.addClass(element, 'critical-security-threat');

    // Block input in production for critical threats
    if (this.isProductionEnvironment() && this.securityContext !== 'public') {
      this.renderer.setProperty(element, 'value', '');
      this.ngControl?.control?.setValue('', { emitEvent: false });

      // Show immediate warning
      this.showSecurityWarning('Critical security threat detected. Input blocked.');
    }

    this.logSecurityEvent('immediate_threat_detected', { value: this.sanitizeForLogging(value) });
  }

  /**
   * Handle paste events with security checks
   */
  private handlePasteEvent(event: ClipboardEvent): void {
    const pastedData = event.clipboardData?.getData('text') || '';

    if (!pastedData) return;

    // Log paste event
    this.logSecurityEvent('paste_event', {
      length: pastedData.length,
      content: this.sanitizeForLogging(pastedData)
    });

    // Perform immediate security check on pasted content
    this.performImmediateThreatDetection(pastedData);

    // Block paste in high-security contexts if threats detected
    if (this.securityContext === 'system' && this.containsSecurityThreats(pastedData)) {
      event.preventDefault();
      this.showSecurityWarning('Paste blocked due to security policy.');
    }
  }

  /**
   * Apply security highlighting based on risk level
   */
  private applySecurityHighlighting(): void {
    const element = this.el.nativeElement;
    const riskLevel = this.currentRiskLevel();

    // Remove existing risk classes
    this.removeSecurityHighlighting();

    // Add risk-level specific classes
    this.renderer.addClass(element, `security-risk-${riskLevel}`);

    // Add visual indicators
    switch (riskLevel) {
      case 'critical':
        this.renderer.setStyle(element, 'border-color', '#d32f2f');
        this.renderer.setStyle(element, 'box-shadow', '0 0 5px rgba(211, 47, 47, 0.5)');
        break;
      case 'high':
        this.renderer.setStyle(element, 'border-color', '#f57c00');
        this.renderer.setStyle(element, 'box-shadow', '0 0 5px rgba(245, 124, 0, 0.5)');
        break;
      case 'medium':
        this.renderer.setStyle(element, 'border-color', '#fbc02d');
        this.renderer.setStyle(element, 'box-shadow', '0 0 3px rgba(251, 192, 45, 0.5)');
        break;
      case 'low':
        this.renderer.setStyle(element, 'border-color', '#388e3c');
        break;
    }
  }

  /**
   * Remove security highlighting
   */
  private removeSecurityHighlighting(): void {
    const element = this.el.nativeElement;
    const riskLevels = ['none', 'low', 'medium', 'high', 'critical'];

    riskLevels.forEach(level => {
      this.renderer.removeClass(element, `security-risk-${level}`);
    });

    this.renderer.removeClass(element, 'critical-security-threat');
    this.renderer.removeStyle(element, 'border-color');
    this.renderer.removeStyle(element, 'box-shadow');
  }

  /**
   * Handle security threat detection
   */
  private handleSecurityThreat(result: any): void {
    const element = this.el.nativeElement;

    // Add threat detected class
    this.renderer.addClass(element, 'security-threat-detected');

    // Log security event
    this.logSecurityEvent('security_threat_detected', {
      riskLevel: result.riskLevel,
      errors: result.errors,
      warnings: result.warnings
    });

    // Block input for critical threats in production
    if (result.riskLevel === 'critical' && this.isProductionEnvironment()) {
      this.blockInput();
    }
  }

  /**
   * Block input for security reasons
   */
  private blockInput(): void {
    const element = this.el.nativeElement;

    this.renderer.setAttribute(element, 'readonly', 'true');
    this.renderer.addClass(element, 'input-blocked');

    // Remove readonly after a delay to allow user correction
    setTimeout(() => {
      this.renderer.removeAttribute(element, 'readonly');
      this.renderer.removeClass(element, 'input-blocked');
    }, 3000);
  }

  /**
   * Apply sanitized value to input
   */
  private applySanitizedValue(sanitizedValue: string): void {
    if (sanitizedValue !== this.currentValue()) {
      this.ngControl?.control?.setValue(sanitizedValue, { emitEvent: false });
      this.renderer.setProperty(this.el.nativeElement, 'value', sanitizedValue);

      this.logSecurityEvent('input_sanitized', {
        original: this.currentValue(),
        sanitized: sanitizedValue
      });
    }
  }

  /**
   * Create validation feedback component
   */
  private createValidationFeedback(): void {
    if (!this.ngControl) return;

    this.feedbackComponent = this.viewContainer.createComponent(ValidationFeedbackComponent);

    // Configure the feedback component
    this.feedbackComponent.setInput('control', this.ngControl.control!);
    this.feedbackComponent.setInput('inputType', this.inputType);
    this.feedbackComponent.setInput('securityContext', this.securityContext);
    this.feedbackComponent.setInput('validationRules', this.validationRules());
    this.feedbackComponent.setInput('debounceTime', this.debounceTime);

    // Position the feedback component
    const element = this.el.nativeElement;
    const parent = element.parentElement;
    if (parent) {
      parent.appendChild(this.feedbackComponent.location.nativeElement);
    }
  }

  /**
   * Cleanup validation feedback component
   */
  private cleanupValidationFeedback(): void {
    if (this.feedbackComponent) {
      this.feedbackComponent.destroy();
      this.feedbackComponent = undefined;
    }
  }

  /**
   * Show security warning to user
   */
  private showSecurityWarning(message: string): void {
    // This could be enhanced to show a proper notification
    console.warn('Security Warning:', message);

    // You could integrate with your notification service here
    // this.notificationService.showWarning(message);
  }

  /**
   * Check if value contains security threats
   */
  private containsSecurityThreats(value: string): boolean {
    const threatPatterns = [
      /<script\b/gi,
      /javascript\s*:/gi,
      /on\w+\s*=/gi,
      /data\s*:\s*text\/html/gi,
      /'\s*(or|and)\s*'?\d/gi,
      /union\s+select/gi,
      /drop\s+table/gi
    ];

    return threatPatterns.some(pattern => pattern.test(value));
  }

  /**
   * Log security events
   */
  private logSecurityEvent(type: string, data?: any): void {
    const event = {
      type,
      timestamp: Date.now(),
      inputType: this.inputType,
      securityContext: this.securityContext,
      elementId: this.el.nativeElement.id,
      data
    };

    // Log to console in development
    if (!this.isProductionEnvironment()) {
      console.log('Security Event:', event);
    }

    // You could send this to your monitoring service
    // this.monitoringService.logSecurityEvent(event);
  }

  /**
   * Sanitize value for logging (remove sensitive info)
   */
  private sanitizeForLogging(value: string): string {
    if (!value) return '';

    return value.length > 50 ? value.substring(0, 50) + '...' : value;
  }

  /**
   * Check if running in production environment
   */
  private isProductionEnvironment(): boolean {
    return typeof window !== 'undefined' &&
           window.location.hostname !== 'localhost' &&
           !window.location.hostname.startsWith('127.0.0.1');
  }

  // Host listeners for additional security events

  @HostListener('keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    // Block dangerous key combinations in high-security contexts
    if (this.securityContext === 'system') {
      // Block Ctrl+V (paste), Ctrl+Shift+I (dev tools), F12, etc.
      if ((event.ctrlKey && event.key === 'v') ||
          (event.ctrlKey && event.shiftKey && event.key === 'I') ||
          event.key === 'F12') {
        event.preventDefault();
        this.showSecurityWarning('Key combination blocked by security policy.');
      }
    }
  }

  @HostListener('drop', ['$event'])
  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.logSecurityEvent('drop_attempt');
    this.showSecurityWarning('File drop not allowed for security reasons.');
  }

  @HostListener('dragover', ['$event'])
  onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  // Public API methods

  /**
   * Get current validation result
   */
  getValidationResult(): any {
    return this.validationResult();
  }

  /**
   * Get current security risk level
   */
  getSecurityRiskLevel(): SecurityRiskLevel {
    return this.currentRiskLevel();
  }

  /**
   * Check if input is currently being validated
   */
  isValidating(): boolean {
    return this.isValidating();
  }

  /**
   * Force immediate validation
   */
  async validateNow(): Promise<void> {
    const currentValue = this.ngControl?.control?.value;
    if (currentValue) {
      await this.validateInput(currentValue);
    }
  }

  /**
   * Clear validation state
   */
  clearValidation(): void {
    this.validationResult.set(null);
    this.securityThreatDetected.set(false);
    this.removeSecurityHighlighting();
  }
}