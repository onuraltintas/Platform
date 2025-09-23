import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-permission-form',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card">
      <div class="card-header">
        <h3 class="card-title">Yetki Formu</h3>
      </div>
      <div class="card-body">
        <p>Yetki form komponenti henüz geliştirilmektedir.</p>
      </div>
    </div>
  `
})
export class PermissionFormComponent {
}
