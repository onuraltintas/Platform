import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-group-clone',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card">
      <div class="card-header">
        <h3 class="card-title">Grup Kopyala</h3>
      </div>
      <div class="card-body">
        <p>Grup kopyalama komponenti henüz geliştirilmektedir.</p>
      </div>
    </div>
  `
})
export class GroupCloneComponent {
}