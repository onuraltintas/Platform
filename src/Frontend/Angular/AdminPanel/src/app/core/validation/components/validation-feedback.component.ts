import { Component, input, computed, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { AbstractControl } from '@angular/forms';
import { Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  ValidationResult,
  ValidationRule,
  ValidationContext,
  InputType,
  SecurityContext
} from '../interfaces/validation.interface';
import { ValidationService } from '../services/validation.service';

/**
 * Real-time Validation Feedback Component
 * Provides immediate visual feedback for form validation
 */
@Component({
  selector: 'app-validation-feedback',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatTooltipModule,
    MatProgressBarModule,
    MatChipsModule
  ],
  template: `
    <div class="validation-feedback" [class]="feedbackClass()">
      <!-- Real-time validation status -->
      <div class="validation-status" *ngIf="showStatus()">
        <mat-icon [class]="statusIconClass()">{{ statusIcon() }}</mat-icon>
        <span class="status-text">{{ statusText() }}</span>

        <!-- Progress bar for real-time validation -->
        <mat-progress-bar
          *ngIf="isValidating()"
          mode="indeterminate"
          class="validation-progress">
        </mat-progress-bar>
      </div>

      <!-- Security risk indicator -->
      <div class="security-indicator" *ngIf="showSecurityRisk()">
        <mat-icon
          [class]="securityIconClass()"
          [matTooltip]="securityTooltip()">
          {{ securityIcon() }}
        </mat-icon>
        <span class="security-text">{{ securityText() }}</span>
      </div>

      <!-- Validation errors -->
      <div class="validation-errors" *ngIf="hasErrors()">
        <div class="error-item" *ngFor="let error of validationResult()?.errors">
          <mat-icon class="error-icon">error</mat-icon>
          <span class="error-text">{{ error }}</span>
        </div>
      </div>

      <!-- Validation warnings -->
      <div class="validation-warnings" *ngIf="hasWarnings()">
        <div class="warning-item" *ngFor="let warning of validationResult()?.warnings">
          <mat-icon class="warning-icon">warning</mat-icon>
          <span class="warning-text">{{ warning }}</span>
        </div>
      </div>

      <!-- Security recommendations -->
      <div class="security-recommendations" *ngIf="showRecommendations()">
        <div class="recommendations-title">
          <mat-icon>security</mat-icon>
          <span>Security Recommendations</span>
        </div>
        <mat-chip-set class="recommendations-chips">
          <mat-chip *ngFor="let recommendation of recommendations()">
            {{ recommendation }}
          </mat-chip>
        </mat-chip-set>
      </div>

      <!-- Password strength indicator -->
      <div class="password-strength" *ngIf="inputType() === 'password' && showPasswordStrength()">
        <div class="strength-title">Password Strength</div>
        <div class="strength-bar">
          <div
            class="strength-fill"
            [style.width.%]="passwordStrength()"
            [class]="passwordStrengthClass()">
          </div>
        </div>
        <div class="strength-text">{{ passwordStrengthText() }}</div>
      </div>

      <!-- Sanitization info -->
      <div class="sanitization-info" *ngIf="showSanitizationInfo()">
        <mat-icon class="sanitization-icon" matTooltip="Input was sanitized for security">
          cleaning_services
        </mat-icon>
        <span class="sanitization-text">Input sanitized</span>
        <span class="sanitization-details" *ngIf="sanitizedValue()">
          ({{ sanitizedValue().length }} characters)
        </span>
      </div>

      <!-- Validation performance metrics -->
      <div class="performance-metrics" *ngIf="showPerformanceMetrics() && validationResult()?.metadata">
        <small class="metrics-text">
          Validated in {{ validationResult()?.metadata?.processingTime | number:'1.0-2' }}ms
        </small>
      </div>
    </div>
  `,
  styleUrls: ['./validation-feedback.component.scss']
})
export class ValidationFeedbackComponent implements OnInit, OnDestroy {
  // Input properties
  readonly control = input.required<AbstractControl>();
  readonly inputType = input<InputType>('text');
  readonly securityContext = input<SecurityContext>('public');
  readonly validationRules = input<ValidationRule[]>([]);
  readonly showRealtimeValidation = input(true);
  readonly showSecurityIndicator = input(true);
  readonly showPerformanceMetrics = input(false);
  readonly debounceTime = input(300);

  // Internal signals
  private readonly validationResultSignal = signal<ValidationResult | null>(null);
  private readonly isValidatingSignal = signal(false);
  private readonly recommendationsSignal = signal<string[]>([]);
  private readonly destroy$ = new Subject<void>();

  // Computed properties
  readonly validationResult = computed(() => this.validationResultSignal());
  readonly isValidating = computed(() => this.isValidatingSignal());
  readonly recommendations = computed(() => this.recommendationsSignal());

  readonly hasErrors = computed(() =>
    this.validationResult()?.errors && this.validationResult()!.errors.length > 0
  );

  readonly hasWarnings = computed(() =>
    this.validationResult()?.warnings && this.validationResult()!.warnings.length > 0
  );

  readonly showStatus = computed(() =>
    this.showRealtimeValidation() && (this.isValidating() || this.validationResult())
  );

  readonly showSecurityRisk = computed(() =>
    this.showSecurityIndicator() &&
    this.validationResult()?.riskLevel &&
    this.validationResult()!.riskLevel !== 'none'
  );

  readonly showRecommendations = computed(() =>
    this.recommendations().length > 0 && this.validationResult()?.riskLevel !== 'none'
  );

  readonly showPasswordStrength = computed(() =>
    this.inputType() === 'password' && this.control().value
  );

  readonly showSanitizationInfo = computed(() =>
    this.validationResult()?.sanitizedValue !== undefined
  );

  readonly sanitizedValue = computed(() => this.validationResult()?.sanitizedValue);

  readonly feedbackClass = computed(() => {
    const result = this.validationResult();
    if (!result) return 'validation-feedback';

    const classes = ['validation-feedback'];

    if (!result.valid) {
      classes.push('has-errors');
    } else if (result.warnings && result.warnings.length > 0) {
      classes.push('has-warnings');
    } else {
      classes.push('valid');
    }

    if (result.riskLevel !== 'none') {
      classes.push(`risk-${result.riskLevel}`);
    }

    return classes.join(' ');
  });

  readonly statusIcon = computed(() => {
    if (this.isValidating()) return 'hourglass_empty';

    const result = this.validationResult();
    if (!result) return 'help_outline';

    if (!result.valid) return 'error';
    if (result.warnings && result.warnings.length > 0) return 'warning';
    return 'check_circle';
  });

  readonly statusIconClass = computed(() => {
    if (this.isValidating()) return 'status-icon validating';

    const result = this.validationResult();
    if (!result) return 'status-icon neutral';

    if (!result.valid) return 'status-icon error';
    if (result.warnings && result.warnings.length > 0) return 'status-icon warning';
    return 'status-icon success';
  });

  readonly statusText = computed(() => {
    if (this.isValidating()) return 'Validating...';

    const result = this.validationResult();
    if (!result) return 'Enter value to validate';

    if (!result.valid) return 'Validation failed';
    if (result.warnings && result.warnings.length > 0) return 'Validation passed with warnings';
    return 'Valid';
  });

  readonly securityIcon = computed(() => {
    const risk = this.validationResult()?.riskLevel;
    switch (risk) {
      case 'critical': return 'dangerous';
      case 'high': return 'warning';
      case 'medium': return 'info';
      case 'low': return 'shield';
      default: return 'security';
    }
  });

  readonly securityIconClass = computed(() => {
    const risk = this.validationResult()?.riskLevel;
    return `security-icon risk-${risk}`;
  });

  readonly securityText = computed(() => {
    const risk = this.validationResult()?.riskLevel;
    switch (risk) {
      case 'critical': return 'Critical Security Risk';
      case 'high': return 'High Security Risk';
      case 'medium': return 'Medium Security Risk';
      case 'low': return 'Low Security Risk';
      default: return 'Secure';
    }
  });

  readonly securityTooltip = computed(() => {
    const risk = this.validationResult()?.riskLevel;
    const context = this.securityContext();

    switch (risk) {
      case 'critical':
        return `Critical security threat detected. Input blocked in ${context} context.`;
      case 'high':
        return `High security risk detected. Review required for ${context} context.`;
      case 'medium':
        return `Medium security risk. Consider security implications.`;
      case 'low':
        return 'Low security risk detected. Input processed with caution.';
      default:
        return 'Input appears secure.';
    }
  });

  readonly passwordStrength = computed(() => {
    const value = this.control().value;
    if (!value || this.inputType() !== 'password') return 0;

    let score = 0;
    const password = value.toString();

    // Length scoring
    if (password.length >= 8) score += 20;
    if (password.length >= 12) score += 10;
    if (password.length >= 16) score += 10;

    // Character variety scoring
    if (/[a-z]/.test(password)) score += 10;
    if (/[A-Z]/.test(password)) score += 10;
    if (/\d/.test(password)) score += 10;
    if (/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?~`]/.test(password)) score += 15;

    // Pattern scoring
    if (!/(.)\1{2,}/.test(password)) score += 10; // No repeating chars
    if (!/123|abc|qwe/i.test(password)) score += 15; // No sequences

    return Math.min(score, 100);
  });

  readonly passwordStrengthClass = computed(() => {
    const strength = this.passwordStrength();
    if (strength < 30) return 'strength-weak';
    if (strength < 60) return 'strength-fair';
    if (strength < 80) return 'strength-good';
    return 'strength-strong';
  });

  readonly passwordStrengthText = computed(() => {
    const strength = this.passwordStrength();
    if (strength < 30) return 'Weak';
    if (strength < 60) return 'Fair';
    if (strength < 80) return 'Good';
    return 'Strong';
  });

  constructor(private validationService: ValidationService) {}

  ngOnInit(): void {
    this.setupRealtimeValidation();
    this.loadSecurityRecommendations();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private setupRealtimeValidation(): void {
    if (!this.showRealtimeValidation()) return;

    // Listen to control value changes
    this.control().valueChanges.pipe(
      debounceTime(this.debounceTime()),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(async (value) => {
      if (value === null || value === undefined || value === '') {
        this.validationResultSignal.set(null);
        return;
      }

      this.isValidatingSignal.set(true);

      try {
        const context: ValidationContext = {
          control: this.control(),
          inputType: this.inputType(),
          securityContext: this.securityContext()
        };

        const result = await this.validationService.validate(
          value,
          this.validationRules(),
          context
        );

        this.validationResultSignal.set(result);
      } catch (error) {
        console.error('Real-time validation error:', error);
        this.validationResultSignal.set({
          valid: false,
          errors: ['Validation error occurred'],
          warnings: [],
          riskLevel: 'medium'
        });
      } finally {
        this.isValidatingSignal.set(false);
      }
    });
  }

  private loadSecurityRecommendations(): void {
    // Load recommendations based on input type
    const recommendations = this.validationService.sanitizationService
      .getSanitizationRecommendations(this.inputType());

    this.recommendationsSignal.set(recommendations);
  }

  /**
   * Force validation update
   */
  validateNow(): void {
    const value = this.control().value;
    if (value !== null && value !== undefined && value !== '') {
      this.isValidatingSignal.set(true);

      const context: ValidationContext = {
        control: this.control(),
        inputType: this.inputType(),
        securityContext: this.securityContext()
      };

      this.validationService.validate(value, this.validationRules(), context)
        .then(result => {
          this.validationResultSignal.set(result);
        })
        .catch(error => {
          console.error('Manual validation error:', error);
          this.validationResultSignal.set({
            valid: false,
            errors: ['Validation error occurred'],
            warnings: [],
            riskLevel: 'medium'
          });
        })
        .finally(() => {
          this.isValidatingSignal.set(false);
        });
    }
  }

  /**
   * Get detailed validation information
   */
  getValidationDetails(): any {
    return {
      result: this.validationResult(),
      control: {
        value: this.control().value,
        valid: this.control().valid,
        errors: this.control().errors
      },
      metadata: {
        inputType: this.inputType(),
        securityContext: this.securityContext(),
        rulesCount: this.validationRules().length
      }
    };
  }
}