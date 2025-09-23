import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-group-activity',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card">
      <div class="card-header">
        <h3 class="card-title">Grup Aktivitesi</h3>
      </div>
      <div class="card-body">
        <p>Grup aktivite komponenti henüz geliştirilmektedir.</p>
      </div>
    </div>
  `
})
export class GroupActivityComponent {
}