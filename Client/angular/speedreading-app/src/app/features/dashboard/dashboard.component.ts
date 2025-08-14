import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="dashboard-container">
      <h1>Dashboard (Geçici)</h1>
      <p>Hızlı okuma dashboard'u burada geliştirilecek...</p>
    </div>
  `,
  styles: [`
    .dashboard-container {
      padding: 40px;
      text-align: center;
    }
  `]
})
export class DashboardComponent {}