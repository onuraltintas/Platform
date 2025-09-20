import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-exercise-form',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container">
      <h2>Egzersiz Form</h2>
      <p>Exercise form component - placeholder</p>
    </div>
  `
})
export class ExerciseFormComponent {
  constructor() {}
}