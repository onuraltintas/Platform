import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-role-detail',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card">
      <div class="card-header">
        <h3 class="card-title">Rol Detayı</h3>
      </div>
      <div class="card-body">
        <p>Rol detay komponenti henüz geliştirilmektedir.</p>
      </div>
    </div>
  `
})
export class RoleDetailComponent {
}