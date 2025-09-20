import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-text-form',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container">
      <h2>Metin Form</h2>
      <p>Text form component - placeholder</p>
    </div>
  `
})
export class TextFormComponent {
  constructor() {}
}