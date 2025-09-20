import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-analytics-report',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container">
      <h2>Analitik Raporlar</h2>
      <p>Analytics report component - placeholder</p>
    </div>
  `
})
export class AnalyticsReportComponent {
  constructor() {}
}