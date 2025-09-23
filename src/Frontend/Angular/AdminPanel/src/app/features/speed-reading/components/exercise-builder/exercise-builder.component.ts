import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-exercise-builder',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './exercise-builder.component.html',
  styleUrl: './exercise-builder.component.scss'
})
export class ExerciseBuilderComponent {
  constructor() {}
}