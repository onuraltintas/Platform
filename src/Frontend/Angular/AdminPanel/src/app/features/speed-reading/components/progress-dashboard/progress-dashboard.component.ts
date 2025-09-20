import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-progress-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container">
      <h2>İlerleme Takibi</h2>
      <p>Progress dashboard component - placeholder</p>
    </div>
  `
})
export class ProgressDashboardComponent {
  constructor() {}
}