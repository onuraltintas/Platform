import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatExpansionModule } from '@angular/material/expansion';
import { SecurityHeadersService } from '../services/security-headers.service';
import { CSPViolationReport } from '../interfaces/security-headers.interface';

interface ViolationSummary {
  totalViolations: number;
  uniqueDirectives: Set<string>;
  blockedUris: Set<string>;
  lastViolation?: Date;
  violationsByDirective: Map<string, number>;
}

/**
 * CSP Monitoring Component
 * Real-time display of CSP violations and security metrics
 */
@Component({
  selector: 'app-csp-monitor',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatBadgeModule,
    MatTooltipModule,
    MatExpansionModule
  ],
  template: `
    <mat-card class="csp-monitor-card">
      <mat-card-header>
        <mat-card-title class="monitor-title">
          <mat-icon class="security-icon">security</mat-icon>
          CSP Güvenlik Monitörü
        </mat-card-title>
        <mat-card-subtitle>
          Content Security Policy ihlalleri ve güvenlik metrikleri
        </mat-card-subtitle>
      </mat-card-header>

      <mat-card-content>
        <!-- Security Metrics Summary -->
        <div class="metrics-grid">
          <div class="metric-item" [class.warning]="violationSummary().totalViolations > 0">
            <mat-icon>report_problem</mat-icon>
            <div class="metric-value">{{ violationSummary().totalViolations }}</div>
            <div class="metric-label">CSP İhlalleri</div>
          </div>

          <div class="metric-item">
            <mat-icon>policy</mat-icon>
            <div class="metric-value">{{ violationSummary().uniqueDirectives.size }}</div>
            <div class="metric-label">Etkilenen Direktifler</div>
          </div>

          <div class="metric-item">
            <mat-icon>block</mat-icon>
            <div class="metric-value">{{ violationSummary().blockedUris.size }}</div>
            <div class="metric-label">Engellenmiş URI'ler</div>
          </div>

          <div class="metric-item" [class.active]="securityMetrics().cspEnabled">
            <mat-icon>{{ securityMetrics().cspEnabled ? 'check_circle' : 'error' }}</mat-icon>
            <div class="metric-value">{{ securityMetrics().cspEnabled ? 'Aktif' : 'Pasif' }}</div>
            <div class="metric-label">CSP Durumu</div>
          </div>
        </div>

        <!-- Recent Violations -->
        <mat-expansion-panel class="violations-panel" [expanded]="showViolations()">
          <mat-expansion-panel-header>
            <mat-panel-title>
              <mat-icon>warning</mat-icon>
              Son CSP İhlalleri
              <mat-badge
                [content]="recentViolations().length"
                [hidden]="recentViolations().length === 0"
                color="warn">
              </mat-badge>
            </mat-panel-title>
          </mat-expansion-panel-header>

          <div class="violations-list">
            <div
              *ngFor="let violation of recentViolations(); trackBy: trackViolation"
              class="violation-item">
              <div class="violation-header">
                <mat-icon class="violation-icon">error_outline</mat-icon>
                <div class="violation-directive">{{ violation.violatedDirective }}</div>
                <div class="violation-time">{{ formatTime(violation.timestamp) }}</div>
              </div>

              <div class="violation-details">
                <div class="detail-row">
                  <span class="detail-label">Engellenmiş URI:</span>
                  <span class="detail-value">{{ violation.blockedUri }}</span>
                </div>
                <div class="detail-row">
                  <span class="detail-label">Kaynak:</span>
                  <span class="detail-value">{{ violation.sourceFile || 'Bilinmiyor' }}</span>
                </div>
                <div class="detail-row" *ngIf="violation.lineNumber">
                  <span class="detail-label">Satır:</span>
                  <span class="detail-value">{{ violation.lineNumber }}:{{ violation.columnNumber }}</span>
                </div>
                <div class="detail-row" *ngIf="violation.scriptSample">
                  <span class="detail-label">Kod Örneği:</span>
                  <code class="code-sample">{{ violation.scriptSample }}</code>
                </div>
              </div>
            </div>

            <div *ngIf="recentViolations().length === 0" class="no-violations">
              <mat-icon>check_circle_outline</mat-icon>
              <p>Hiç CSP ihlali tespit edilmedi</p>
            </div>
          </div>
        </mat-expansion-panel>

        <!-- Violation Statistics -->
        <mat-expansion-panel class="statistics-panel">
          <mat-expansion-panel-header>
            <mat-panel-title>
              <mat-icon>analytics</mat-icon>
              İhlal İstatistikleri
            </mat-panel-title>
          </mat-expansion-panel-header>

          <div class="statistics-content">
            <div *ngFor="let stat of getViolationStats()" class="stat-item">
              <div class="stat-directive">{{ stat.directive }}</div>
              <div class="stat-bar">
                <div
                  class="stat-fill"
                  [style.width.%]="(stat.count / violationSummary().totalViolations) * 100">
                </div>
              </div>
              <div class="stat-count">{{ stat.count }}</div>
            </div>
          </div>
        </mat-expansion-panel>

        <!-- Security Recommendations -->
        <div class="recommendations-section" *ngIf="recommendations().length > 0">
          <h3>
            <mat-icon>lightbulb</mat-icon>
            Güvenlik Önerileri
          </h3>
          <ul class="recommendations-list">
            <li *ngFor="let recommendation of recommendations()">
              {{ recommendation }}
            </li>
          </ul>
        </div>
      </mat-card-content>

      <mat-card-actions>
        <button mat-button (click)="clearViolations()">
          <mat-icon>clear_all</mat-icon>
          İhlalleri Temizle
        </button>
        <button mat-button (click)="exportViolations()">
          <mat-icon>download</mat-icon>
          Dışa Aktar
        </button>
        <button mat-button (click)="refreshMetrics()">
          <mat-icon>refresh</mat-icon>
          Yenile
        </button>
      </mat-card-actions>
    </mat-card>
  `,
  styleUrl: './csp-monitor.component.scss'
})
export class CspMonitorComponent implements OnInit, OnDestroy {
  private readonly securityService = inject(SecurityHeadersService);

  // Signals
  public readonly recentViolations = signal<(CSPViolationReport & { timestamp: Date })[]>([]);
  public readonly securityMetrics = signal(this.securityService.getSecurityMetrics());
  public readonly showViolations = signal(false);
  public readonly recommendations = signal<string[]>([]);

  // Computed properties
  public readonly violationSummary = signal<ViolationSummary>({
    totalViolations: 0,
    uniqueDirectives: new Set(),
    blockedUris: new Set(),
    violationsByDirective: new Map()
  });

  private violationListener?: (event: Event) => void;

  ngOnInit(): void {
    this.initializeViolationMonitoring();
    this.loadSecurityMetrics();
    this.loadRecommendations();
  }

  ngOnDestroy(): void {
    if (this.violationListener && typeof document !== 'undefined') {
      document.removeEventListener('securitypolicyviolation', this.violationListener);
    }
  }

  private initializeViolationMonitoring(): void {
    if (typeof document === 'undefined') return;

    this.violationListener = (event: Event) => {
      const violationEvent = event as SecurityPolicyViolationEvent;
      this.handleViolation(violationEvent);
    };

    document.addEventListener('securitypolicyviolation', this.violationListener);
  }

  private handleViolation(event: SecurityPolicyViolationEvent): void {
    const violation: CSPViolationReport & { timestamp: Date } = {
      'document-uri': event.documentURI,
      'referrer': event.referrer,
      'blocked-uri': event.blockedURI,
      'violated-directive': event.violatedDirective,
      'effective-directive': event.effectiveDirective,
      'original-policy': event.originalPolicy,
      'disposition': event.disposition as 'enforce' | 'report',
      'status-code': event.statusCode,
      'script-sample': event.sample,
      'line-number': event.lineNumber,
      'column-number': event.columnNumber,
      'source-file': event.sourceFile,
      timestamp: new Date()
    };

    // Add to recent violations (keep last 20)
    const current = this.recentViolations();
    this.recentViolations.set([violation, ...current].slice(0, 20));

    // Update summary
    this.updateViolationSummary();

    // Report to service
    this.securityService.reportViolation(violation);

    // Show violations panel
    this.showViolations.set(true);
  }

  private updateViolationSummary(): void {
    const violations = this.recentViolations();
    const summary: ViolationSummary = {
      totalViolations: violations.length,
      uniqueDirectives: new Set(violations.map(v => v.violatedDirective)),
      blockedUris: new Set(violations.map(v => v.blockedUri)),
      lastViolation: violations[0]?.timestamp,
      violationsByDirective: new Map()
    };

    // Count violations by directive
    violations.forEach(violation => {
      const directive = violation.violatedDirective;
      const count = summary.violationsByDirective.get(directive) || 0;
      summary.violationsByDirective.set(directive, count + 1);
    });

    this.violationSummary.set(summary);
  }

  private loadSecurityMetrics(): void {
    const metrics = this.securityService.getSecurityMetrics();
    this.securityMetrics.set(metrics);
  }

  private loadRecommendations(): void {
    this.securityService.getCurrentConfig().subscribe(config => {
      const recommendations = this.securityService.getSecurityRecommendations(config);
      this.recommendations.set(recommendations);
    });
  }

  getViolationStats(): { directive: string; count: number }[] {
    const summary = this.violationSummary();
    return Array.from(summary.violationsByDirective.entries())
      .map(([directive, count]) => ({ directive, count }))
      .sort((a, b) => b.count - a.count);
  }

  clearViolations(): void {
    this.recentViolations.set([]);
    this.updateViolationSummary();
    this.showViolations.set(false);
  }

  exportViolations(): void {
    const violations = this.recentViolations();
    const data = {
      exportDate: new Date().toISOString(),
      totalViolations: violations.length,
      violations: violations
    };

    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `csp-violations-${new Date().toISOString().split('T')[0]}.json`;
    link.click();
    URL.revokeObjectURL(url);
  }

  refreshMetrics(): void {
    this.loadSecurityMetrics();
    this.loadRecommendations();
  }

  trackViolation(index: number, violation: CSPViolationReport & { timestamp: Date }): string {
    return `${violation.violatedDirective}-${violation.blockedUri}-${violation.timestamp.getTime()}`;
  }

  formatTime(date: Date): string {
    return date.toLocaleTimeString('tr-TR', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  }
}