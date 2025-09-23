import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface ValidationRule {
  name: string;
  type: 'token' | 'request' | 'data' | 'permission';
  validator: (value: any, context?: any) => Promise<ValidationResult>;
  required: boolean;
  errorMessage: string;
}

export interface ValidationResult {
  isValid: boolean;
  errorMessage?: string;
  details?: any;
  timestamp: number;
}

export interface ValidationContext {
  userId?: string;
  sessionId?: string;
  endpoint?: string;
  action?: string;
  timestamp: number;
}

/**
 * Advanced Validation Service for Security Checks
 * Lightweight implementation for lazy loading
 */
@Injectable({
  providedIn: 'root'
})
export class AdvancedValidationService {
  private validationResults$ = new BehaviorSubject<ValidationResult[]>([]);

  private readonly validationRules: ValidationRule[] = [
    {
      name: 'token-expiry',
      type: 'token',
      validator: this.validateTokenExpiry.bind(this),
      required: true,
      errorMessage: 'Token has expired'
    },
    {
      name: 'request-rate-limit',
      type: 'request',
      validator: this.validateRateLimit.bind(this),
      required: false,
      errorMessage: 'Rate limit exceeded'
    },
    {
      name: 'data-integrity',
      type: 'data',
      validator: this.validateDataIntegrity.bind(this),
      required: true,
      errorMessage: 'Data integrity check failed'
    }
  ];

  constructor() {
    console.log('🛡️ Advanced Validation Service initialized');
  }

  /**
   * Validate token
   */
  async validateToken(token: string, context?: ValidationContext): Promise<ValidationResult> {
    const result = await this.validateTokenExpiry(token, context);
    this.recordValidationResult(result);
    return result;
  }

  /**
   * Validate request
   */
  async validateRequest(request: any, context?: ValidationContext): Promise<ValidationResult> {
    const result = await this.validateRateLimit(request, context);
    this.recordValidationResult(result);
    return result;
  }

  /**
   * Get validation history
   */
  getValidationHistory(): Observable<ValidationResult[]> {
    return this.validationResults$.asObservable();
  }

  // Private validation methods

  private async validateTokenExpiry(token: string, context?: ValidationContext): Promise<ValidationResult> {
    try {
      // Simple JWT payload extraction (without verification for demo)
      const payload = JSON.parse(atob(token.split('.')[1]));
      const now = Math.floor(Date.now() / 1000);
      const isValid = payload.exp > now;

      return {
        isValid,
        errorMessage: isValid ? undefined : 'Token has expired',
        details: { exp: payload.exp, now },
        timestamp: Date.now()
      };
    } catch (error) {
      return {
        isValid: false,
        errorMessage: 'Invalid token format',
        timestamp: Date.now()
      };
    }
  }

  private async validateRateLimit(request: any, context?: ValidationContext): Promise<ValidationResult> {
    // Simple rate limiting simulation
    const isValid = Math.random() > 0.1; // 90% pass rate

    return {
      isValid,
      errorMessage: isValid ? undefined : 'Rate limit exceeded',
      timestamp: Date.now()
    };
  }

  private async validateDataIntegrity(data: any, context?: ValidationContext): Promise<ValidationResult> {
    // Simple data validation
    const isValid = data && typeof data === 'object';

    return {
      isValid,
      errorMessage: isValid ? undefined : 'Invalid data format',
      timestamp: Date.now()
    };
  }

  private recordValidationResult(result: ValidationResult): void {
    const currentResults = this.validationResults$.value;
    const newResults = [result, ...currentResults].slice(0, 100);
    this.validationResults$.next(newResults);
  }
}