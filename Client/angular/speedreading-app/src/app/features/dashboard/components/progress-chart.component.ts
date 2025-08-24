import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';

interface UserProgress {
  currentLevel: number;
  readingSpeed: number;
  comprehensionRate: number;
  totalTextsRead: number;
  totalExercisesCompleted: number;
  streakDays: number;
  weeklyProgress: number[];
  monthlyProgress: { month: string; speed: number; comprehension: number }[];
}

@Component({
  selector: 'app-progress-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="progress-chart-container">
      
      <!-- Chart Toggle -->
      <div class="chart-toggle">
        <button 
          class="toggle-btn" 
          [class.active]="chartType === 'speed'"
          (click)="setChartType('speed')">
          Okuma Hızı
        </button>
        <button 
          class="toggle-btn" 
          [class.active]="chartType === 'comprehension'"
          (click)="setChartType('comprehension')">
          Anlama Oranı
        </button>
      </div>

      <!-- Chart Area -->
      <div class="chart-area">
        <div class="chart-grid">
          <!-- Y-axis labels -->
          <div class="y-axis">
            <span *ngFor="let label of yAxisLabels" class="y-label">{{ label }}</span>
          </div>
          
          <!-- Chart content -->
          <div class="chart-content">
            <!-- Grid lines -->
            <div class="grid-lines">
              <div class="grid-line" *ngFor="let line of gridLines"></div>
            </div>
            
            <!-- Chart bars/line -->
            <div class="chart-bars" *ngIf="filter === 'week'">
              <div 
                class="chart-bar" 
                *ngFor="let value of chartData; let i = index"
                [style.height.%]="getBarHeight(value)"
                [style.background]="getBarColor(i)">
                <div class="bar-value">{{ value }}</div>
              </div>
            </div>
            
            <!-- Line chart for monthly data -->
            <div class="chart-line" *ngIf="filter !== 'week'">
              <svg class="line-svg" viewBox="0 0 300 150">
                <!-- Speed line -->
                <polyline 
                  *ngIf="chartType === 'speed'"
                  [attr.points]="speedLinePoints" 
                  class="speed-line">
                </polyline>
                <!-- Comprehension line -->
                <polyline 
                  *ngIf="chartType === 'comprehension'"
                  [attr.points]="comprehensionLinePoints" 
                  class="comprehension-line">
                </polyline>
                <!-- Data points -->
                <circle 
                  *ngFor="let point of dataPoints; let i = index"
                  [attr.cx]="point.x" 
                  [attr.cy]="point.y" 
                  r="4" 
                  [class]="'data-point ' + chartType + '-point'">
                </circle>
              </svg>
            </div>
          </div>
        </div>
        
        <!-- X-axis labels -->
        <div class="x-axis">
          <span *ngFor="let label of xAxisLabels" class="x-label">{{ label }}</span>
        </div>
      </div>

      <!-- Chart Stats -->
      <div class="chart-stats">
        <div class="stat-item">
          <span class="stat-label">Bu Hafta</span>
          <span class="stat-value">{{ getWeeklyChange() }}%</span>
          <i class="bi" [class]="getWeeklyChangeIcon()"></i>
        </div>
        <div class="stat-item">
          <span class="stat-label">En Yüksek</span>
          <span class="stat-value">{{ getMaxValue() }}</span>
        </div>
        <div class="stat-item">
          <span class="stat-label">Ortalama</span>
          <span class="stat-value">{{ getAverageValue() }}</span>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./progress-chart.component.scss']
})
export class ProgressChartComponent implements OnChanges {
  @Input() userProgress!: UserProgress;
  @Input() filter: 'week' | 'month' | 'year' = 'week';
  
  chartType: 'speed' | 'comprehension' = 'speed';
  chartData: number[] = [];
  xAxisLabels: string[] = [];
  yAxisLabels: string[] = [];
  gridLines: number[] = [0, 1, 2, 3, 4];
  speedLinePoints: string = '';
  comprehensionLinePoints: string = '';
  dataPoints: {x: number, y: number}[] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['userProgress'] || changes['filter']) {
      this.updateChartData();
    }
  }

  setChartType(type: 'speed' | 'comprehension'): void {
    this.chartType = type;
    this.updateChartData();
  }

  private updateChartData(): void {
    if (!this.userProgress) return;

    switch (this.filter) {
      case 'week':
        this.updateWeeklyChart();
        break;
      case 'month':
        this.updateMonthlyChart();
        break;
      case 'year':
        this.updateYearlyChart();
        break;
    }
    
    this.updateAxisLabels();
    this.updateLineChart();
  }

  private updateWeeklyChart(): void {
    if (this.chartType === 'speed') {
      this.chartData = this.userProgress.weeklyProgress;
    } else {
      // Simulated comprehension data for weekly view
      this.chartData = [72, 74, 76, 75, 77, 79, 78];
    }
  }

  private updateMonthlyChart(): void {
    if (this.chartType === 'speed') {
      this.chartData = this.userProgress.monthlyProgress.map(d => d.speed);
    } else {
      this.chartData = this.userProgress.monthlyProgress.map(d => d.comprehension);
    }
  }

  private updateYearlyChart(): void {
    // Simulated yearly data
    if (this.chartType === 'speed') {
      this.chartData = [150, 170, 185, 200, 220, 235, 250, 265, 275, 280, 285, 290];
    } else {
      this.chartData = [60, 62, 65, 67, 70, 72, 74, 75, 76, 77, 78, 79];
    }
  }

  private updateAxisLabels(): void {
    switch (this.filter) {
      case 'week':
        this.xAxisLabels = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];
        break;
      case 'month':
        this.xAxisLabels = this.userProgress.monthlyProgress.map(d => d.month);
        break;
      case 'year':
        this.xAxisLabels = ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara'];
        break;
    }

    // Y-axis labels based on chart type and data range
    const maxValue = Math.max(...this.chartData);
    const minValue = Math.min(...this.chartData);
    const range = maxValue - minValue;
    const step = Math.ceil(range / 4);
    
    this.yAxisLabels = [];
    for (let i = 4; i >= 0; i--) {
      const value = minValue + (step * i);
      this.yAxisLabels.push(this.chartType === 'comprehension' ? `${value}%` : `${value}`);
    }
  }

  private updateLineChart(): void {
    if (this.filter === 'week') return;
    
    const maxValue = Math.max(...this.chartData);
    const minValue = Math.min(...this.chartData);
    const range = maxValue - minValue || 1;
    
    let points: string[] = [];
    this.dataPoints = [];
    
    this.chartData.forEach((value, index) => {
      const x = (index / (this.chartData.length - 1)) * 280 + 10;
      const y = 140 - ((value - minValue) / range) * 120;
      points.push(`${x},${y}`);
      this.dataPoints.push({x, y});
    });
    
    if (this.chartType === 'speed') {
      this.speedLinePoints = points.join(' ');
    } else {
      this.comprehensionLinePoints = points.join(' ');
    }
  }

  getBarHeight(value: number): number {
    const maxValue = Math.max(...this.chartData);
    const minValue = Math.min(...this.chartData);
    const range = maxValue - minValue || 1;
    return ((value - minValue) / range) * 80 + 10;
  }

  getBarColor(index: number): string {
    const colors = this.chartType === 'speed' 
      ? ['#3b82f6', '#60a5fa', '#93c5fd', '#bfdbfe', '#dbeafe', '#eff6ff', '#f0f9ff']
      : ['#10b981', '#34d399', '#6ee7b7', '#9deccc', '#c6f6d5', '#d1fae5', '#ecfdf5'];
    return colors[index % colors.length];
  }

  getWeeklyChange(): number {
    if (this.chartData.length < 2) return 0;
    const last = this.chartData[this.chartData.length - 1];
    const previous = this.chartData[this.chartData.length - 2];
    return Math.round(((last - previous) / previous) * 100);
  }

  getWeeklyChangeIcon(): string {
    const change = this.getWeeklyChange();
    if (change > 0) return 'bi-arrow-up text-success';
    if (change < 0) return 'bi-arrow-down text-danger';
    return 'bi-dash text-muted';
  }

  getMaxValue(): string | number {
    const max = Math.max(...this.chartData);
    return this.chartType === 'comprehension' ? `${max}%` : max;
  }

  getAverageValue(): string | number {
    const avg = Math.round(this.chartData.reduce((a, b) => a + b, 0) / this.chartData.length);
    return this.chartType === 'comprehension' ? `${avg}%` : avg;
  }
}