import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card">
      <div class="card-header">
        <h3 class="card-title">Kullanıcı Detayı</h3>
      </div>
      <div class="card-body">
        <p>Kullanıcı detay komponenti henüz geliştirilmektedir.</p>
      </div>
    </div>
  `
})
export class UserDetailComponent {
}