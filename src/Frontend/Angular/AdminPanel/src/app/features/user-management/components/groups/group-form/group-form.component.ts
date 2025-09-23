import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-group-form',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card">
      <div class="card-header">
        <h3 class="card-title">Grup Formu</h3>
      </div>
      <div class="card-body">
        <p>Grup form komponenti henüz geliştirilmektedir.</p>
      </div>
    </div>
  `
})
export class GroupFormComponent {
}