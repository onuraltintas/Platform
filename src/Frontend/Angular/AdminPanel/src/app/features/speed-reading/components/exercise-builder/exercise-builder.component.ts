import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-exercise-builder',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container">
      <h2>Egzersiz Yönetimi</h2>
      <p>Exercise builder component - placeholder</p>
    </div>
  `
})
export class ExerciseBuilderComponent {
  constructor() {}
}