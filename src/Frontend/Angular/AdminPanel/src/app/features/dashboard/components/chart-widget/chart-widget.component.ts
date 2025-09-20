import { AfterViewInit, Component, ElementRef, Input, OnChanges, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Chart, ChartConfiguration } from 'chart.js/auto';

@Component({
  selector: 'app-chart-widget',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="chart-card">
      <div class="chart-header d-flex justify-content-between align-items-center mb-2">
        <h6 class="m-0">{{ title }}</h6>
        <small class="text-muted">{{ subtitle }}</small>
      </div>
      <canvas #canvas></canvas>
    </div>
  `,
  styles: [`
    .chart-card {
      background: var(--bs-card-bg);
      border: 1px solid var(--bs-border-color);
      border-radius: 0.75rem;
      padding: 1rem;
      position: relative;
      height: 260px;
    }

    .chart-card canvas {
      display: block;
      width: 100% !important;
      height: 100% !important;
    }
  `]
})
export class ChartWidgetComponent implements AfterViewInit, OnChanges {
  @Input() title: string = '';
  @Input() subtitle: string = '';
  @Input() config!: ChartConfiguration;

  @ViewChild('canvas', { static: false }) canvas!: ElementRef<HTMLCanvasElement>;
  private chart?: Chart;

  ngAfterViewInit(): void {
    this.render();
  }

  ngOnChanges(): void {
    if (this.chart) {
      this.chart.destroy();
      this.chart = undefined;
    }
    if (this.canvas) {
      this.render();
    }
  }

  private render(): void {
    if (!this.canvas || !this.config) return;
    const ctx = this.canvas.nativeElement.getContext('2d');
    if (!ctx) return;
    // Global theme tweaks
    Chart.defaults.color = getComputedStyle(document.documentElement).getPropertyValue('--bs-body-color') || '#1e293b';
    Chart.defaults.borderColor = 'rgba(148,163,184,0.3)';
    Chart.defaults.plugins.tooltip.backgroundColor = 'rgba(30,41,59,0.9)';
    Chart.defaults.plugins.tooltip.titleColor = '#fff';
    Chart.defaults.plugins.tooltip.bodyColor = '#e2e8f0';
    Chart.defaults.plugins.tooltip.cornerRadius = 8;
    Chart.defaults.scales = Chart.defaults.scales || {};

    this.chart = new Chart(ctx, this.config);
  }
}

