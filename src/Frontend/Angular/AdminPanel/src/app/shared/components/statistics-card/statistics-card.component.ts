import { Component, Input, Output, EventEmitter, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LucideAngularModule, TrendingUp, TrendingDown, Minus } from 'lucide-angular';

export interface StatisticConfig {
  title: string;
  value: number | string;
  subtitle?: string;
  icon?: string;
  color?: 'primary' | 'secondary' | 'success' | 'danger' | 'warning' | 'info' | 'light' | 'dark';
  trend?: {
    value: number;
    direction: 'up' | 'down' | 'neutral';
    label?: string;
    period?: string;
  };
  prefix?: string;
  suffix?: string;
  format?: 'number' | 'currency' | 'percentage' | 'custom';
  loading?: boolean;
  clickable?: boolean;
  href?: string;
  routerLink?: string | any[];
  size?: 'sm' | 'md' | 'lg';
  chart?: {
    type: 'line' | 'bar' | 'area';
    data: number[];
    color?: string;
    height?: number;
  };
}

@Component({
  selector: 'app-statistics-card',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    LucideAngularModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="card"
         [class]="getCardClasses()"
         [style.cursor]="config.clickable ? 'pointer' : 'default'"
         (click)="onCardClick()">

      <div class="card-body" [class]="getBodyClasses()">
        <!-- Loading State -->
        @if (config.loading) {
          <div class="d-flex align-items-center justify-content-center" style="min-height: 80px;">
            <div class="spinner-border spinner-border-sm text-primary" role="status">
              <span class="visually-hidden">Yükleniyor...</span>
            </div>
          </div>
        } @else {
          <!-- Content -->
          <div class="row align-items-center">
            <!-- Main Content -->
            <div [class]="getMainContentClasses()">
              <!-- Title -->
              <div class="text-muted mb-1" [class]="getTitleClasses()">
                {{ config.title }}
              </div>

              <!-- Value -->
              <div class="h2 mb-0" [class]="getValueClasses()">
                @if (config.prefix) {
                  <span class="text-muted me-1">{{ config.prefix }}</span>
                }

                {{ getFormattedValue() }}

                @if (config.suffix) {
                  <span class="text-muted ms-1">{{ config.suffix }}</span>
                }
              </div>

              <!-- Subtitle -->
              @if (config.subtitle) {
                <div class="text-muted small mt-1">
                  {{ config.subtitle }}
                </div>
              }

              <!-- Trend -->
              @if (config.trend) {
                <div class="d-flex align-items-center mt-2">
                  <span [class]="getTrendClasses()">
                    <lucide-icon [name]="getTrendIcon()" [size]="14" class="me-1"/>
                    {{ getTrendText() }}
                  </span>

                  @if (config.trend && config.trend.period) {
                    <span class="text-muted ms-2 small">
                      {{ config.trend.period }}
                    </span>
                  }
                </div>
              }
            </div>

            <!-- Icon -->
            @if (config.icon && !config.chart) {
              <div class="col-auto">
                <div [class]="getIconContainerClasses()">
                  <lucide-icon [name]="config.icon" [size]="getIconSize()"/>
                </div>
              </div>
            }

            <!-- Chart -->
            @if (config.chart) {
              <div class="col-auto">
                <div [class]="getChartContainerClasses()">
                  <svg [attr.width]="getChartWidth()" [attr.height]="getChartHeight()" class="chart">
                    @switch (config.chart && config.chart.type) {
                      @case ('line') {
                        <polyline
                          [attr.points]="getLinePoints()"
                          [attr.stroke]="getChartColor()"
                          stroke-width="2"
                          fill="none"
                          stroke-linecap="round"
                          stroke-linejoin="round"/>
                      }

                      @case ('area') {
                        <polygon
                          [attr.points]="getAreaPoints()"
                          [attr.fill]="getChartColor()"
                          [attr.fill-opacity]="0.2"/>
                        <polyline
                          [attr.points]="getLinePoints()"
                          [attr.stroke]="getChartColor()"
                          stroke-width="2"
                          fill="none"
                          stroke-linecap="round"
                          stroke-linejoin="round"/>
                      }

                      @case ('bar') {
                        @for (point of getBarData(); track $index) {
                          <rect
                            [attr.x]="point.x"
                            [attr.y]="point.y"
                            [attr.width]="point.width"
                            [attr.height]="point.height"
                            [attr.fill]="getChartColor()"
                            [attr.fill-opacity]="0.7"/>
                        }
                      }
                    }
                  </svg>
                </div>
              </div>
            }
          </div>
        }
      </div>

      <!-- Action Indicator -->
      @if (config.clickable) {
        <div class="card-footer bg-transparent border-0 pt-0">
          <div class="text-muted small d-flex align-items-center justify-content-center">
            Detayları görüntüle
            <lucide-icon name="chevron-right" [size]="14" class="ms-1"/>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .card {
      transition: all 0.2s ease-in-out;
      border: 1px solid var(--bs-border-color);
    }

    .card.clickable {
      border-color: transparent;
    }

    .card.clickable:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
      border-color: var(--bs-primary);
    }

    .icon-container {
      width: 48px;
      height: 48px;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 8px;
      background: var(--bs-light);
    }

    .icon-container.sm {
      width: 36px;
      height: 36px;
    }

    .icon-container.lg {
      width: 56px;
      height: 56px;
    }

    .chart {
      display: block;
    }

    .trend-up {
      color: var(--bs-success);
    }

    .trend-down {
      color: var(--bs-danger);
    }

    .trend-neutral {
      color: var(--bs-secondary);
    }

    .card-footer {
      padding: 0.5rem 1rem;
    }

    .h2 {
      font-weight: 600;
    }

    .text-muted.small {
      font-size: 0.875rem;
    }
  `]
})
export class StatisticsCardComponent {
  @Input() config: StatisticConfig = {
    title: 'İstatistik',
    value: 0
  };

  @Output() cardClick = new EventEmitter<StatisticConfig>();

  // Icons
  readonly trendingUpIcon = TrendingUp;
  readonly trendingDownIcon = TrendingDown;
  readonly minusIcon = Minus;

  // Computed values
  formattedValue = computed(() => this.getFormattedValue());

  onCardClick(): void {
    if (!this.config.clickable || this.config.loading) {
      return;
    }

    this.cardClick.emit(this.config);
  }

  getCardClasses(): string {
    let classes = 'h-100';

    if (this.config.clickable) {
      classes += ' clickable';
    }

    return classes;
  }

  getBodyClasses(): string {
    const size = this.config.size || 'md';

    switch (size) {
      case 'sm': return 'p-3';
      case 'lg': return 'p-4';
      default: return '';
    }
  }

  getMainContentClasses(): string {
    const hasIcon = this.config.icon && !this.config.chart;
    const hasChart = this.config.chart;

    if (hasIcon || hasChart) {
      return 'col';
    }

    return 'col-12';
  }

  getTitleClasses(): string {
    const size = this.config.size || 'md';

    switch (size) {
      case 'sm': return 'small';
      case 'lg': return '';
      default: return 'small';
    }
  }

  getValueClasses(): string {
    const size = this.config.size || 'md';
    const color = this.config.color;

    let classes = '';

    switch (size) {
      case 'sm': classes = 'h4'; break;
      case 'lg': classes = 'h1'; break;
      default: classes = 'h2'; break;
    }

    if (color) {
      classes += ` text-${color}`;
    }

    return classes;
  }

  getIconContainerClasses(): string {
    const size = this.config.size || 'md';
    const color = this.config.color || 'primary';

    let classes = `icon-container bg-${color}-subtle text-${color}`;

    if (size !== 'md') {
      classes += ` ${size}`;
    }

    return classes;
  }

  getChartContainerClasses(): string {
    return 'chart-container';
  }

  getIconSize(): number {
    const size = this.config.size || 'md';

    switch (size) {
      case 'sm': return 18;
      case 'lg': return 28;
      default: return 24;
    }
  }

  getTrendClasses(): string {
    const trend = this.config.trend;
    if (!trend) {
      return '';
    }

    const direction = trend.direction;

    switch (direction) {
      case 'up': return 'trend-up small fw-medium';
      case 'down': return 'trend-down small fw-medium';
      default: return 'trend-neutral small fw-medium';
    }
  }

  getTrendIcon(): string {
    const trend = this.config.trend;
    if (!trend) {
      return 'minus';
    }

    const direction = trend.direction;

    switch (direction) {
      case 'up': return 'trending-up';
      case 'down': return 'trending-down';
      default: return 'minus';
    }
  }

  getTrendText(): string {
    const trend = this.config.trend;
    if (!trend) {
      return '';
    }

    const prefix = trend.direction === 'up' ? '+' : trend.direction === 'down' ? '-' : '';
    const value = Math.abs(trend.value);

    return `${prefix}${value}%${trend.label ? ' ' + trend.label : ''}`;
  }

  getFormattedValue(): string {
    const value = this.config.value;
    const format = this.config.format || 'number';

    if (typeof value === 'string') {
      return value;
    }

    switch (format) {
      case 'currency':
        return new Intl.NumberFormat('tr-TR', {
          style: 'currency',
          currency: 'TRY',
          minimumFractionDigits: 0
        }).format(value as number);

      case 'percentage':
        return `${value}%`;

      case 'number':
        return new Intl.NumberFormat('tr-TR').format(value as number);

      default:
        return value.toString();
    }
  }

  // Chart methods
  getChartWidth(): number {
    const size = this.config.size || 'md';

    switch (size) {
      case 'sm': return 60;
      case 'lg': return 100;
      default: return 80;
    }
  }

  getChartHeight(): number {
    return this.config.chart?.height || 32;
  }

  getChartColor(): string {
    const chart = this.config.chart;
    if (chart?.color) {
      return chart.color;
    }

    const color = this.config.color || 'primary';
    return `var(--bs-${color})`;
  }

  getLinePoints(): string {
    const chart = this.config.chart;
    if (!chart?.data) {
      return '';
    }

    const data = chart.data;
    const width = this.getChartWidth();
    const height = this.getChartHeight();
    const maxValue = Math.max(...data);
    const minValue = Math.min(...data);
    const range = maxValue - minValue || 1;

    return data.map((value, index) => {
      const x = (index / (data.length - 1)) * width;
      const y = height - ((value - minValue) / range) * height;
      return `${x},${y}`;
    }).join(' ');
  }

  getAreaPoints(): string {
    const linePoints = this.getLinePoints();
    const width = this.getChartWidth();
    const height = this.getChartHeight();

    return `0,${height} ${linePoints} ${width},${height}`;
  }

  getBarData(): Array<{ x: number; y: number; width: number; height: number }> {
    const chart = this.config.chart;
    if (!chart?.data) {
      return [];
    }

    const data = chart.data;
    const width = this.getChartWidth();
    const height = this.getChartHeight();
    const maxValue = Math.max(...data);
    const barWidth = width / data.length * 0.8;
    const barSpacing = width / data.length * 0.2;

    return data.map((value, index) => {
      const barHeight = (value / maxValue) * height;
      const x = index * (barWidth + barSpacing) + barSpacing / 2;
      const y = height - barHeight;

      return {
        x,
        y,
        width: barWidth,
        height: barHeight
      };
    });
  }
}