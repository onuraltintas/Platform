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
  ContentChildren,
  QueryList,
  AfterContentInit
} from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import {
  SecurityRiskLevel,
  SecurityContext,
  FormSecurityEvent
} from '../interfaces/validation.interface';
import { ValidationService } from '../services/validation.service';
import { SecureInputDirective } from './secure-input.directive';

/**
 * Secure Form Directive
 * Provides comprehensive form-level security monitoring and protection
 */
@Directive({
  selector: '[appSecureForm]',
  standalone: true,
  exportAs: 'secureForm'
})
export class SecureFormDirective implements OnInit, OnDestroy, AfterContentInit {

  private readonly el = inject(ElementRef);
  private readonly renderer = inject(Renderer2);
  private readonly validationService = inject(ValidationService);
  private readonly destroy$ = new Subject<void>();

  @ContentChildren(SecureInputDirective, { descendants: true })
  secureInputs!: QueryList<SecureInputDirective>;

  // Input properties
  @Input() securityContext: SecurityContext = 'public';
  @Input() enableSubmissionProtection = true;
  @Input() enableRealTimeMonitoring = true;
  @Input() enableSecurityLogging = true;
  @Input() maxSubmissionAttempts = 5;
  @Input() submissionCooldownMs = 60000; // 1 minute
  @Input() blockOnHighRisk = true;
  @Input() enableCSRFProtection = true;
  @Input() enableHoneypot = true;

  // Signals for reactive state management
  private readonly formSecurityState = signal({
    overallRiskLevel: 'none' as SecurityRiskLevel,
    threatCount: 0,
    submissionAttempts: 0,
    lastSubmissionTime: 0,
    isBlocked: false,
    hasActiveThreats: false
  });

  private readonly securityEvents = signal<FormSecurityEvent[]>([]);

  // Computed properties
  private readonly canSubmit = computed(() => {
    const state = this.formSecurityState();
    const now = Date.now();
    const cooldownExpired = now - state.lastSubmissionTime > this.submissionCooldownMs;

    return !state.isBlocked &&
           state.submissionAttempts < this.maxSubmissionAttempts &&
           (state.submissionAttempts === 0 || cooldownExpired) &&
           (!this.blockOnHighRisk || state.overallRiskLevel !== 'critical');
  });

  private readonly securityStatus = computed(() => {
    const state = this.formSecurityState();
    if (state.isBlocked) return 'blocked';
    if (state.hasActiveThreats) return 'threat-detected';
    if (state.overallRiskLevel === 'critical') return 'critical';
    if (state.overallRiskLevel === 'high') return 'high-risk';
    return 'secure';
  });

  private honeypotElement?: HTMLInputElement;
  private csrfToken?: string;

  constructor() {
    // React to security state changes
    effect(() => {
      const status = this.securityStatus();
      this.updateFormVisualState(status);
    });

    // React to submission capability changes
    effect(() => {
      const canSubmit = this.canSubmit();
      this.updateSubmitButtonState(canSubmit);
    });
  }

  ngOnInit(): void {
    this.initializeSecureForm();
    this.setupSecurityMonitoring();

    if (this.enableHoneypot) {
      this.createHoneypot();
    }

    if (this.enableCSRFProtection) {
      this.setupCSRFProtection();
    }
  }

  ngAfterContentInit(): void {
    this.setupInputMonitoring();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.cleanupHoneypot();
  }

  /**
   * Initialize secure form with security attributes
   */
  private initializeSecureForm(): void {
    const form = this.el.nativeElement;

    // Set security attributes
    this.renderer.setAttribute(form, 'data-security-context', this.securityContext);
    this.renderer.setAttribute(form, 'data-security-enabled', 'true');
    this.renderer.setAttribute(form, 'autocomplete', 'off');

    // Add security CSS classes
    this.renderer.addClass(form, 'secure-form');
    this.renderer.addClass(form, `security-context-${this.securityContext}`);

    // Disable browser autofill for sensitive contexts
    if (this.securityContext === 'admin' || this.securityContext === 'system') {
      this.renderer.setAttribute(form, 'data-lpignore', 'true'); // LastPass ignore
      this.renderer.setAttribute(form, 'data-form-type', 'other'); // Chrome ignore
    }

    // Add CSRF protection meta tag
    if (this.enableCSRFProtection) {
      this.addCSRFTokenToForm();
    }
  }

  /**
   * Setup security monitoring for the form
   */
  private setupSecurityMonitoring(): void {
    if (!this.enableRealTimeMonitoring) return;

    const form = this.el.nativeElement;

    // Monitor form focus events
    this.renderer.listen(form, 'focusin', (event) => {
      this.logSecurityEvent({
        type: 'validation_failed',
        timestamp: Date.now(),
        inputValue: '[FORM_FOCUS]',
        inputType: 'custom',
        riskLevel: 'none',
        userContext: {
          elementTag: event.target.tagName,
          elementType: event.target.type,
          elementId: event.target.id
        }
      });
    });

    // Monitor form change events
    this.renderer.listen(form, 'change', () => {
      this.assessOverallFormSecurity();
    });

    // Monitor form reset events
    this.renderer.listen(form, 'reset', () => {
      this.resetSecurityState();
    });
  }

  /**
   * Setup monitoring for secure input directives
   */
  private setupInputMonitoring(): void {
    if (!this.secureInputs) return;

    // Monitor changes to secure inputs
    this.secureInputs.changes.pipe(
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.setupIndividualInputMonitoring();
    });

    this.setupIndividualInputMonitoring();
  }

  /**
   * Setup monitoring for individual secure inputs
   */
  private setupIndividualInputMonitoring(): void {
    this.secureInputs.forEach(secureInput => {
      // Monitor validation results from each secure input
      // This would require exposing validation result as observable in SecureInputDirective
      // For now, we'll assess security periodically
    });

    // Assess form security every 5 seconds
    if (this.enableRealTimeMonitoring) {
      setInterval(() => {
        this.assessOverallFormSecurity();
      }, 5000);
    }
  }

  /**
   * Assess overall form security based on all inputs
   */
  private assessOverallFormSecurity(): void {
    if (!this.secureInputs) return;

    let maxRiskLevel: SecurityRiskLevel = 'none';
    let threatCount = 0;
    let hasActiveThreats = false;

    this.secureInputs.forEach(secureInput => {
      const riskLevel = secureInput.getSecurityRiskLevel();
      const validationResult = secureInput.getValidationResult();

      if (riskLevel !== 'none') {
        threatCount++;
        hasActiveThreats = true;

        // Escalate risk level
        if (this.getRiskLevelNumber(riskLevel) > this.getRiskLevelNumber(maxRiskLevel)) {
          maxRiskLevel = riskLevel;
        }
      }

      // Log validation failures
      if (validationResult && !validationResult.valid) {
        this.logSecurityEvent({
          type: 'validation_failed',
          timestamp: Date.now(),
          inputValue: '[FORM_VALIDATION_FAILED]',
          inputType: secureInput.inputType,
          riskLevel: riskLevel,
          error: validationResult.errors?.join(', ')
        });
      }
    });

    // Update form security state
    this.formSecurityState.update(state => ({
      ...state,
      overallRiskLevel: maxRiskLevel,
      threatCount,
      hasActiveThreats
    }));
  }

  /**
   * Create honeypot field for bot detection
   */
  private createHoneypot(): void {
    const form = this.el.nativeElement;
    const honeypot = this.renderer.createElement('input');

    this.renderer.setAttribute(honeypot, 'type', 'text');
    this.renderer.setAttribute(honeypot, 'name', 'website_url'); // Common bot field
    this.renderer.setAttribute(honeypot, 'tabindex', '-1');
    this.renderer.setAttribute(honeypot, 'autocomplete', 'off');

    // Hide the honeypot field
    this.renderer.setStyle(honeypot, 'position', 'absolute');
    this.renderer.setStyle(honeypot, 'left', '-9999px');
    this.renderer.setStyle(honeypot, 'opacity', '0');
    this.renderer.setStyle(honeypot, 'pointer-events', 'none');

    // Listen for honeypot changes (bot detection)
    this.renderer.listen(honeypot, 'input', () => {
      this.handleBotDetection();
    });

    this.renderer.appendChild(form, honeypot);
    this.honeypotElement = honeypot;
  }

  /**
   * Setup CSRF protection
   */
  private setupCSRFProtection(): void {
    this.csrfToken = this.generateCSRFToken();
    this.addCSRFTokenToForm();
  }

  /**
   * Add CSRF token to form
   */
  private addCSRFTokenToForm(): void {
    if (!this.csrfToken) return;

    const form = this.el.nativeElement;
    let csrfInput = form.querySelector('input[name="_csrf_token"]');

    if (!csrfInput) {
      csrfInput = this.renderer.createElement('input');
      this.renderer.setAttribute(csrfInput, 'type', 'hidden');
      this.renderer.setAttribute(csrfInput, 'name', '_csrf_token');
      this.renderer.appendChild(form, csrfInput);
    }

    this.renderer.setProperty(csrfInput, 'value', this.csrfToken);
  }

  /**
   * Generate CSRF token
   */
  private generateCSRFToken(): string {
    const array = new Uint8Array(32);
    crypto.getRandomValues(array);
    return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
  }

  /**
   * Handle bot detection from honeypot
   */
  private handleBotDetection(): void {
    this.logSecurityEvent({
      type: 'security_threat',
      timestamp: Date.now(),
      inputValue: '[BOT_DETECTED]',
      inputType: 'custom',
      riskLevel: 'high',
      error: 'Bot activity detected via honeypot'
    });

    this.formSecurityState.update(state => ({
      ...state,
      isBlocked: true,
      overallRiskLevel: 'high'
    }));

    // Block form submission
    this.blockFormSubmission('Bot activity detected');
  }

  /**
   * Block form submission with reason
   */
  private blockFormSubmission(reason: string): void {
    const form = this.el.nativeElement;
    const submitButtons = form.querySelectorAll('button[type="submit"], input[type="submit"]');

    submitButtons.forEach((button: HTMLElement) => {
      this.renderer.setAttribute(button, 'disabled', 'true');
      this.renderer.addClass(button, 'security-blocked');
    });

    this.showSecurityMessage(reason, 'error');
  }

  /**
   * Update form visual state based on security status
   */
  private updateFormVisualState(status: string): void {
    const form = this.el.nativeElement;

    // Remove existing status classes
    const statusClasses = ['secure', 'high-risk', 'critical', 'threat-detected', 'blocked'];
    statusClasses.forEach(cls => {
      this.renderer.removeClass(form, `form-${cls}`);
    });

    // Add current status class
    this.renderer.addClass(form, `form-${status}`);

    // Update visual indicators
    switch (status) {
      case 'blocked':
        this.renderer.setStyle(form, 'border', '2px solid #d32f2f');
        this.renderer.setStyle(form, 'box-shadow', '0 0 10px rgba(211, 47, 47, 0.3)');
        break;
      case 'critical':
        this.renderer.setStyle(form, 'border', '2px solid #ff5722');
        this.renderer.setStyle(form, 'box-shadow', '0 0 8px rgba(255, 87, 34, 0.3)');
        break;
      case 'high-risk':
        this.renderer.setStyle(form, 'border', '1px solid #ff9800');
        this.renderer.setStyle(form, 'box-shadow', '0 0 5px rgba(255, 152, 0, 0.3)');
        break;
      case 'threat-detected':
        this.renderer.setStyle(form, 'border', '1px solid #ffc107');
        break;
      case 'secure':
        this.renderer.removeStyle(form, 'border');
        this.renderer.removeStyle(form, 'box-shadow');
        break;
    }
  }

  /**
   * Update submit button state
   */
  private updateSubmitButtonState(canSubmit: boolean): void {
    const form = this.el.nativeElement;
    const submitButtons = form.querySelectorAll('button[type="submit"], input[type="submit"]');

    submitButtons.forEach((button: HTMLElement) => {
      if (canSubmit) {
        this.renderer.removeAttribute(button, 'disabled');
        this.renderer.removeClass(button, 'security-blocked');
      } else {
        this.renderer.setAttribute(button, 'disabled', 'true');
        this.renderer.addClass(button, 'security-blocked');
      }
    });
  }

  /**
   * Reset security state
   */
  private resetSecurityState(): void {
    this.formSecurityState.set({
      overallRiskLevel: 'none',
      threatCount: 0,
      submissionAttempts: 0,
      lastSubmissionTime: 0,
      isBlocked: false,
      hasActiveThreats: false
    });

    this.securityEvents.set([]);
  }

  /**
   * Log security events
   */
  private logSecurityEvent(event: FormSecurityEvent): void {
    if (!this.enableSecurityLogging) return;

    this.securityEvents.update(events => [...events, event]);

    // Keep only last 100 events
    if (this.securityEvents().length > 100) {
      this.securityEvents.update(events => events.slice(-100));
    }

    // Log critical events immediately
    if (event.riskLevel === 'critical' || event.riskLevel === 'high') {
      console.warn('Form Security Event:', event);
    }

    // Send to monitoring service in production
    if (this.isProductionEnvironment()) {
      // this.monitoringService.logFormSecurityEvent(event);
    }
  }

  /**
   * Show security message to user
   */
  private showSecurityMessage(message: string, type: 'warning' | 'error' | 'info' = 'warning'): void {
    // Create or update security message element
    const form = this.el.nativeElement;
    let messageElement = form.querySelector('.security-message');

    if (!messageElement) {
      messageElement = this.renderer.createElement('div');
      this.renderer.addClass(messageElement, 'security-message');
      this.renderer.insertBefore(form, messageElement, form.firstChild);
    }

    this.renderer.addClass(messageElement, `security-message-${type}`);
    this.renderer.setProperty(messageElement, 'textContent', message);

    // Auto-hide after 10 seconds for warnings
    if (type === 'warning') {
      setTimeout(() => {
        if (messageElement && messageElement.parentNode) {
          this.renderer.removeChild(form, messageElement);
        }
      }, 10000);
    }
  }

  /**
   * Get numeric representation of risk level for comparison
   */
  private getRiskLevelNumber(riskLevel: SecurityRiskLevel): number {
    const levels = { none: 0, low: 1, medium: 2, high: 3, critical: 4 };
    return levels[riskLevel] || 0;
  }

  /**
   * Cleanup honeypot element
   */
  private cleanupHoneypot(): void {
    if (this.honeypotElement && this.honeypotElement.parentNode) {
      this.renderer.removeChild(this.honeypotElement.parentNode, this.honeypotElement);
    }
  }

  /**
   * Check if running in production environment
   */
  private isProductionEnvironment(): boolean {
    return typeof window !== 'undefined' &&
           window.location.hostname !== 'localhost' &&
           !window.location.hostname.startsWith('127.0.0.1');
  }

  // Host listeners

  @HostListener('submit', ['$event'])
  onSubmit(event: Event): void {
    if (!this.validateSubmission()) {
      event.preventDefault();
      event.stopPropagation();
      return;
    }

    this.handleFormSubmission();
  }

  @HostListener('keydown.enter', ['$event'])
  onEnterSubmit(event: KeyboardEvent): void {
    // Prevent accidental submissions with Enter key in high-security contexts
    if (this.securityContext === 'system' && event.target !== event.currentTarget) {
      const target = event.target as HTMLElement;
      if (target.tagName !== 'TEXTAREA' && target.tagName !== 'BUTTON') {
        event.preventDefault();
      }
    }
  }

  /**
   * Validate form submission
   */
  private validateSubmission(): boolean {
    // Check if submission is allowed
    if (!this.canSubmit()) {
      this.showSecurityMessage('Form submission blocked due to security concerns', 'error');
      return false;
    }

    // Check honeypot
    if (this.honeypotElement && this.honeypotElement.value) {
      this.handleBotDetection();
      return false;
    }

    // Validate CSRF token
    if (this.enableCSRFProtection && !this.validateCSRFToken()) {
      this.showSecurityMessage('CSRF token validation failed', 'error');
      return false;
    }

    // Final security assessment
    this.assessOverallFormSecurity();
    const state = this.formSecurityState();

    if (state.overallRiskLevel === 'critical' && this.blockOnHighRisk) {
      this.showSecurityMessage('Form contains critical security threats', 'error');
      return false;
    }

    return true;
  }

  /**
   * Handle form submission
   */
  private handleFormSubmission(): void {
    this.formSecurityState.update(state => ({
      ...state,
      submissionAttempts: state.submissionAttempts + 1,
      lastSubmissionTime: Date.now()
    }));

    this.logSecurityEvent({
      type: 'validation_failed',
      timestamp: Date.now(),
      inputValue: '[FORM_SUBMITTED]',
      inputType: 'custom',
      riskLevel: this.formSecurityState().overallRiskLevel,
      userContext: {
        submissionAttempt: this.formSecurityState().submissionAttempts
      }
    });
  }

  /**
   * Validate CSRF token
   */
  private validateCSRFToken(): boolean {
    if (!this.enableCSRFProtection) return true;

    const form = this.el.nativeElement;
    const csrfInput = form.querySelector('input[name="_csrf_token"]') as HTMLInputElement;

    return csrfInput && csrfInput.value === this.csrfToken;
  }

  // Public API methods

  /**
   * Get current form security state
   */
  getSecurityState(): any {
    return this.formSecurityState();
  }

  /**
   * Get security events
   */
  getSecurityEvents(): FormSecurityEvent[] {
    return this.securityEvents();
  }

  /**
   * Check if form can be submitted
   */
  canSubmitForm(): boolean {
    return this.canSubmit();
  }

  /**
   * Get overall security status
   */
  getSecurityStatus(): string {
    return this.securityStatus();
  }

  /**
   * Force security assessment
   */
  assessSecurity(): void {
    this.assessOverallFormSecurity();
  }

  /**
   * Reset form security state
   */
  resetSecurity(): void {
    this.resetSecurityState();
  }

  /**
   * Manually block form
   */
  blockForm(reason: string): void {
    this.blockFormSubmission(reason);
  }

  /**
   * Unblock form (admin function)
   */
  unblockForm(): void {
    this.formSecurityState.update(state => ({
      ...state,
      isBlocked: false,
      submissionAttempts: 0
    }));

    this.updateSubmitButtonState(true);
    this.updateFormVisualState('secure');
  }
}