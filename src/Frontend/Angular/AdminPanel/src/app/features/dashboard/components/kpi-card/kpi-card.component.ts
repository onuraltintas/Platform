import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from '@coreui/angular';

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  imports: [CommonModule, CardModule],
  template: `
    <c-card class="h-100 stat-card" [ngClass]="cardClass">
      <c-card-body class="d-flex justify-content-between align-items-center">
        <div>
          <h6 class="text-white-50 mb-1 fw-normal">{{ title }}</h6>
          <h2 class="mb-0 fw-bold">{{ value }}</h2>
          <small [ngClass]="trendColorClass">
            <i [class]="trendIcon + ' me-1'"></i>
            {{ trendText }}
          </small>
        </div>
        <div class="stat-icon">
          <i [class]="icon + ' fs-1 opacity-75'"></i>
        </div>
      </c-card-body>
    </c-card>
  `,
  styles: [`
    .stat-card {
      transform: translateY(0);
      transition: all 0.3s ease;
      overflow: hidden;
      position: relative;
    }

    .stat-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15) !important;
    }

    .stat-card::before {
      content: '';
      position: absolute;
      top: -50%;
      right: -20%;
      width: 120px;
      height: 120px;
      background: rgba(255, 255, 255, 0.1);
      border-radius: 50%;
      z-index: 0;
    }

    .text-white-50 { opacity: 0.85; }
    .text-white-75 { opacity: 0.9; }

    .bg-gradient-primary { background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); }
    .bg-gradient-success { background: linear-gradient(135deg, #10b981 0%, #059669 100%); }
    .bg-gradient-info    { background: linear-gradient(135deg, #06b6d4 0%, #0891b2 100%); }
    .bg-gradient-warning { background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); }
    .bg-light-card { background: #ffffff; border: 1px solid var(--bs-border-color); }
    .text-dark { color: var(--bs-body-color) !important; }
  `]
})
export class KpiCardComponent {
  @Input() title: string = '';
  @Input() value: string | number = 0;
  @Input() trendText: string = '';
  @Input() trendIcon: string = 'bi bi-arrow-up';
  @Input() icon: string = 'bi bi-info-circle';
  @Input() gradient: 'primary' | 'success' | 'info' | 'warning' | 'light' = 'primary';
  @Input() trend: 'up' | 'down' | 'neutral' = 'up';

  get gradientClass(): string {
    return `text-white bg-gradient-${this.gradient}`;
  }

  get cardClass(): string {
    return this.gradient === 'light' ? 'bg-light-card border-0' : this.gradientClass + ' text-white border-0';
  }

  get trendColorClass(): string {
    if (this.gradient === 'light') {
      return this.trend === 'up' ? 'text-success' : this.trend === 'down' ? 'text-danger' : 'text-muted';
    }
    return 'text-white-75';
  }
}

