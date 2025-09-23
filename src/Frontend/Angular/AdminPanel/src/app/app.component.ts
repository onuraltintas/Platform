import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './core/services/theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  private readonly themeService = inject(ThemeService);

  // Legacy test expectation
  title = 'platformv1-admin';

  ngOnInit(): void {
    // Force light theme on startup
    this.themeService.forceLightTheme();
  }
}
