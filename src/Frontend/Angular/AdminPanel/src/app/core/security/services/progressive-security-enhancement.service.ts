import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, fromEvent, timer, combineLatest } from 'rxjs';
import { throttleTime, map, distinctUntilChanged, startWith } from 'rxjs/operators';
import { LazySecurityLoaderService, SecurityLoadResult } from './lazy-security-loader.service';

export interface SecurityLevel {
  level: 'basic' | 'standard' | 'enhanced' | 'maximum';
  score: number;
  loadedModules: string[];
  activeFeatures: string[];
  userActions: number;
  sessionTime: number;
  riskScore: number;
}

export interface UserBehaviorPattern {
  actionFrequency: number;
  sessionDuration: number;
  dataAccess: string[];
  adminActions: number;
  sensitiveOperations: number;
  lastActivity: number;
  trustScore: number;
}

export interface SecurityEnhancementConfig {
  enhancementThresholds: {
    basic: number;
    standard: number;
    enhanced: number;
    maximum: number;
  };
  userActionWeights: {
    login: number;
    dataAccess: number;
    adminAction: number;
    sensitiveOperation: number;
  };
  timeBasedFactors: {
    sessionTimeWeight: number;
    inactivityPenalty: number;
    frequencyBonus: number;
  };
  riskFactors: {
    failedAttempts: number;
    unusualActivity: number;
    offHoursAccess: number;
  };
}

/**
 * Progressive Security Enhancement Service
 * Adapts security measures based on user behavior and risk assessment
 */
@Injectable({
  providedIn: 'root'
})
export class ProgressiveSecurityEnhancementService {
  private lazyLoader = inject(LazySecurityLoaderService);

  private currentSecurityLevel$ = new BehaviorSubject<SecurityLevel>(this.getInitialSecurityLevel());
  private userBehavior$ = new BehaviorSubject<UserBehaviorPattern>(this.getInitialBehaviorPattern());
  private securityEvents$ = new BehaviorSubject<string[]>([]);

  private readonly config: SecurityEnhancementConfig = {
    enhancementThresholds: {
      basic: 0.2,
      standard: 0.5,
      enhanced: 0.75,
      maximum: 0.9
    },
    userActionWeights: {
      login: 0.1,
      dataAccess: 0.2,
      adminAction: 0.4,
      sensitiveOperation: 0.6
    },
    timeBasedFactors: {
      sessionTimeWeight: 0.1,
      inactivityPenalty: 0.2,
      frequencyBonus: 0.15
    },
    riskFactors: {
      failedAttempts: 0.3,
      unusualActivity: 0.25,
      offHoursAccess: 0.15
    }
  };

  private sessionStartTime = Date.now();
  private userActions = 0;
  private lastActivityTime = Date.now();
  private failedAttempts = 0;

  constructor() {
    this.initializeProgressiveEnhancement();
  }

  /**
   * Initialize progressive security enhancement monitoring
   */
  private initializeProgressiveEnhancement(): void {
    // Monitor user activity
    this.setupActivityMonitoring();

    // Setup automatic security level adjustments
    this.setupAutomaticEnhancements();

    // Monitor session health
    this.setupSessionMonitoring();

    console.log('🔒 Progressive Security Enhancement initialized');
  }

  /**
   * Track user action and update security level
   */
  trackUserAction(
    action: 'login' | 'dataAccess' | 'adminAction' | 'sensitiveOperation',
    context?: { endpoint?: string; dataType?: string; success?: boolean }
  ): void {
    this.userActions++;
    this.lastActivityTime = Date.now();

    // Update behavior pattern
    const currentBehavior = this.userBehavior$.value;
    const actionWeight = this.config.userActionWeights[action];

    const updatedBehavior: UserBehaviorPattern = {
      ...currentBehavior,
      actionFrequency: this.calculateActionFrequency(),
      sessionDuration: Date.now() - this.sessionStartTime,
      lastActivity: this.lastActivityTime,
      trustScore: this.calculateTrustScore(action, context?.success !== false),
      adminActions: action === 'adminAction' ? currentBehavior.adminActions + 1 : currentBehavior.adminActions,
      sensitiveOperations: action === 'sensitiveOperation' ? currentBehavior.sensitiveOperations + 1 : currentBehavior.sensitiveOperations
    };

    // Track data access
    if (action === 'dataAccess' && context?.dataType) {
      updatedBehavior.dataAccess = [...new Set([...currentBehavior.dataAccess, context.dataType])];
    }

    this.userBehavior$.next(updatedBehavior);

    // Handle failed attempts
    if (context?.success === false) {
      this.handleFailedAttempt();
    }

    // Evaluate and adjust security level
    this.evaluateSecurityLevel();

    console.log(`👤 User action tracked: ${action}`, {
      totalActions: this.userActions,
      trustScore: updatedBehavior.trustScore.toFixed(2),
      securityLevel: this.currentSecurityLevel$.value.level
    });
  }

  /**
   * Get current security level
   */
  getCurrentSecurityLevel(): Observable<SecurityLevel> {
    return this.currentSecurityLevel$.asObservable();
  }

  /**
   * Get user behavior pattern
   */
  getUserBehaviorPattern(): Observable<UserBehaviorPattern> {
    return this.userBehavior$.asObservable();
  }

  /**
   * Get security events
   */
  getSecurityEvents(): Observable<string[]> {
    return this.securityEvents$.asObservable();
  }

  /**
   * Force security level change
   */
  forceSecurityLevel(level: 'basic' | 'standard' | 'enhanced' | 'maximum'): void {
    console.log(`🔒 Forcing security level to: ${level}`);
    this.applySecurityLevel(level);
  }

  /**
   * Get security recommendations
   */
  getSecurityRecommendations(): string[] {
    const currentLevel = this.currentSecurityLevel$.value;
    const behavior = this.userBehavior$.value;
    const recommendations: string[] = [];

    // Risk-based recommendations
    if (behavior.trustScore < 0.5) {
      recommendations.push('Consider enabling additional authentication factors');
    }

    if (currentLevel.level === 'basic' && behavior.sensitiveOperations > 5) {
      recommendations.push('Upgrade to enhanced security for sensitive operations');
    }

    if (behavior.actionFrequency > 50 && currentLevel.level !== 'maximum') {
      recommendations.push('High activity detected - consider maximum security level');
    }

    if (this.isOffHoursAccess()) {
      recommendations.push('Off-hours access detected - enhanced monitoring recommended');
    }

    return recommendations;
  }

  /**
   * Reset security metrics
   */
  resetSecurityMetrics(): void {
    this.userActions = 0;
    this.sessionStartTime = Date.now();
    this.lastActivityTime = Date.now();
    this.failedAttempts = 0;

    this.userBehavior$.next(this.getInitialBehaviorPattern());
    this.currentSecurityLevel$.next(this.getInitialSecurityLevel());
    this.securityEvents$.next([]);

    console.log('🔄 Security metrics reset');
  }

  // Private methods

  private setupActivityMonitoring(): void {
    // Monitor mouse/keyboard activity
    const userActivity$ = fromEvent(document, 'click').pipe(
      throttleTime(1000),
      map(() => Date.now()),
      startWith(Date.now())
    );

    userActivity$.subscribe(() => {
      this.lastActivityTime = Date.now();
    });

    // Check for inactivity every 30 seconds
    timer(30000, 30000).subscribe(() => {
      this.checkInactivity();
    });
  }

  private setupAutomaticEnhancements(): void {
    // Monitor security level changes
    this.currentSecurityLevel$.subscribe(level => {
      this.loadRequiredSecurityModules(level);
    });

    // Evaluate security level every 60 seconds
    timer(60000, 60000).subscribe(() => {
      this.evaluateSecurityLevel();
    });
  }

  private setupSessionMonitoring(): void {
    // Monitor overall session health
    timer(0, 120000).subscribe(() => { // Every 2 minutes
      this.monitorSessionHealth();
    });
  }

  private evaluateSecurityLevel(): void {
    const behavior = this.userBehavior$.value;
    const riskScore = this.calculateRiskScore();
    const enhancementScore = this.calculateEnhancementScore(behavior);

    let newLevel: 'basic' | 'standard' | 'enhanced' | 'maximum' = 'basic';

    if (enhancementScore >= this.config.enhancementThresholds.maximum || riskScore > 0.8) {
      newLevel = 'maximum';
    } else if (enhancementScore >= this.config.enhancementThresholds.enhanced || riskScore > 0.6) {
      newLevel = 'enhanced';
    } else if (enhancementScore >= this.config.enhancementThresholds.standard || riskScore > 0.3) {
      newLevel = 'standard';
    }

    const currentLevel = this.currentSecurityLevel$.value.level;
    if (newLevel !== currentLevel) {
      this.applySecurityLevel(newLevel);
      this.logSecurityLevelChange(currentLevel, newLevel, enhancementScore, riskScore);
    }
  }

  private calculateEnhancementScore(behavior: UserBehaviorPattern): number {
    let score = 0;

    // Base score from trust score
    score += behavior.trustScore * 0.4;

    // Session duration factor
    const sessionHours = behavior.sessionDuration / (1000 * 60 * 60);
    score += Math.min(sessionHours * this.config.timeBasedFactors.sessionTimeWeight, 0.2);

    // Activity frequency factor
    score += Math.min(behavior.actionFrequency * 0.01, 0.2);

    // Admin and sensitive operations
    score += Math.min(behavior.adminActions * 0.05, 0.15);
    score += Math.min(behavior.sensitiveOperations * 0.08, 0.2);

    return Math.min(score, 1.0);
  }

  private calculateRiskScore(): number {
    let riskScore = 0;

    // Failed attempts risk
    riskScore += Math.min(this.failedAttempts * this.config.riskFactors.failedAttempts, 0.4);

    // Off-hours access risk
    if (this.isOffHoursAccess()) {
      riskScore += this.config.riskFactors.offHoursAccess;
    }

    // Inactivity risk
    const inactivityMinutes = (Date.now() - this.lastActivityTime) / (1000 * 60);
    if (inactivityMinutes > 15) {
      riskScore += Math.min(inactivityMinutes * 0.01, 0.2);
    }

    // Unusual activity patterns
    const behavior = this.userBehavior$.value;
    if (behavior.actionFrequency > 100) { // Very high frequency might be automated
      riskScore += this.config.riskFactors.unusualActivity;
    }

    return Math.min(riskScore, 1.0);
  }

  private calculateTrustScore(action: string, success: boolean): number {
    const currentBehavior = this.userBehavior$.value;
    const actionWeight = this.config.userActionWeights[action as keyof typeof this.config.userActionWeights];

    let newTrustScore = currentBehavior.trustScore;

    if (success) {
      newTrustScore += actionWeight * 0.1; // Increase trust on success
    } else {
      newTrustScore -= actionWeight * 0.2; // Decrease trust on failure
    }

    return Math.max(0, Math.min(1, newTrustScore));
  }

  private calculateActionFrequency(): number {
    const sessionMinutes = (Date.now() - this.sessionStartTime) / (1000 * 60);
    return sessionMinutes > 0 ? this.userActions / sessionMinutes : 0;
  }

  private applySecurityLevel(level: 'basic' | 'standard' | 'enhanced' | 'maximum'): void {
    const behavior = this.userBehavior$.value;
    const riskScore = this.calculateRiskScore();

    const securityLevel: SecurityLevel = {
      level,
      score: this.calculateEnhancementScore(behavior),
      loadedModules: this.getRequiredModules(level),
      activeFeatures: this.getActiveFeatures(level),
      userActions: this.userActions,
      sessionTime: Date.now() - this.sessionStartTime,
      riskScore
    };

    this.currentSecurityLevel$.next(securityLevel);
    this.addSecurityEvent(`Security level changed to: ${level}`);
  }

  private getRequiredModules(level: 'basic' | 'standard' | 'enhanced' | 'maximum'): string[] {
    const modules = ['secure-storage']; // Always required

    switch (level) {
      case 'maximum':
        modules.push('advanced-validation', 'audit-logging');
        // fallthrough
      case 'enhanced':
        modules.push('integrity-check');
        // fallthrough
      case 'standard':
        modules.push('encryption');
        // fallthrough
      case 'basic':
      default:
        break;
    }

    return modules;
  }

  private getActiveFeatures(level: 'basic' | 'standard' | 'enhanced' | 'maximum'): string[] {
    const features = ['basic-auth'];

    switch (level) {
      case 'maximum':
        features.push('audit-trail', 'advanced-validation', 'real-time-monitoring');
        // fallthrough
      case 'enhanced':
        features.push('integrity-validation', 'background-checks');
        // fallthrough
      case 'standard':
        features.push('data-encryption', 'secure-storage');
        // fallthrough
      case 'basic':
      default:
        break;
    }

    return features;
  }

  private async loadRequiredSecurityModules(securityLevel: SecurityLevel): Promise<void> {
    for (const moduleName of securityLevel.loadedModules) {
      try {
        await this.lazyLoader.loadModule(moduleName, false);
      } catch (error) {
        console.error(`Failed to load security module ${moduleName}:`, error);
        this.addSecurityEvent(`Failed to load module: ${moduleName}`);
      }
    }
  }

  private handleFailedAttempt(): void {
    this.failedAttempts++;

    if (this.failedAttempts >= 3) {
      this.addSecurityEvent(`Multiple failed attempts detected: ${this.failedAttempts}`);

      // Force higher security level on multiple failures
      if (this.failedAttempts >= 5) {
        this.forceSecurityLevel('maximum');
      }
    }
  }

  private checkInactivity(): void {
    const inactivityMinutes = (Date.now() - this.lastActivityTime) / (1000 * 60);

    if (inactivityMinutes > 30) {
      this.addSecurityEvent(`Extended inactivity detected: ${inactivityMinutes.toFixed(1)} minutes`);

      // Reduce security level on extended inactivity
      const currentLevel = this.currentSecurityLevel$.value.level;
      if (currentLevel === 'maximum' && inactivityMinutes > 60) {
        this.applySecurityLevel('enhanced');
      }
    }
  }

  private isOffHoursAccess(): boolean {
    const hour = new Date().getHours();
    return hour < 7 || hour > 22; // Before 7 AM or after 10 PM
  }

  private monitorSessionHealth(): void {
    const behavior = this.userBehavior$.value;
    const level = this.currentSecurityLevel$.value;

    console.log('🏥 Session Health Monitor:', {
      securityLevel: level.level,
      trustScore: behavior.trustScore.toFixed(2),
      sessionDuration: `${((Date.now() - this.sessionStartTime) / (1000 * 60)).toFixed(1)} min`,
      userActions: this.userActions,
      riskScore: level.riskScore.toFixed(2),
      loadedModules: level.loadedModules.length,
      recommendations: this.getSecurityRecommendations().length
    });
  }

  private addSecurityEvent(event: string): void {
    const currentEvents = this.securityEvents$.value;
    const timestamp = new Date().toISOString();
    const timestampedEvent = `[${timestamp}] ${event}`;

    // Keep only last 50 events
    const newEvents = [timestampedEvent, ...currentEvents].slice(0, 50);
    this.securityEvents$.next(newEvents);
  }

  private logSecurityLevelChange(
    oldLevel: string,
    newLevel: string,
    enhancementScore: number,
    riskScore: number
  ): void {
    console.log(`🔒 Security Level Changed: ${oldLevel} → ${newLevel}`, {
      enhancementScore: enhancementScore.toFixed(2),
      riskScore: riskScore.toFixed(2),
      userActions: this.userActions,
      sessionTime: `${((Date.now() - this.sessionStartTime) / (1000 * 60)).toFixed(1)} min`,
      failedAttempts: this.failedAttempts
    });

    this.addSecurityEvent(`Security level upgraded from ${oldLevel} to ${newLevel}`);
  }

  private getInitialSecurityLevel(): SecurityLevel {
    return {
      level: 'basic',
      score: 0.2,
      loadedModules: ['secure-storage'],
      activeFeatures: ['basic-auth'],
      userActions: 0,
      sessionTime: 0,
      riskScore: 0
    };
  }

  private getInitialBehaviorPattern(): UserBehaviorPattern {
    return {
      actionFrequency: 0,
      sessionDuration: 0,
      dataAccess: [],
      adminActions: 0,
      sensitiveOperations: 0,
      lastActivity: Date.now(),
      trustScore: 0.5 // Start with neutral trust
    };
  }
}