import { Component, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ReadingProgressApiService } from './features/reading/services/reading-progress-api.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  protected readonly title = signal('speedreading-app');

  constructor(private progressApi: ReadingProgressApiService) {}

  ngOnInit(): void {
    // Trigger offline sync on app start and when connection is restored
    try {
      this.progressApi.syncOfflineSessions().subscribe({ next: () => {} });
      window.addEventListener('online', () => {
        this.progressApi.syncOfflineSessions().subscribe({ next: () => {} });
      });
    } catch {}
  }
}
