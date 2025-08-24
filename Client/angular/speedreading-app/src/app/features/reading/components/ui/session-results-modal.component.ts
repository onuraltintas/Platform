import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SessionResults, ReadingMode } from '../../../../shared/models/reading.models';

@Component({
  selector: 'app-session-results-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './session-results-modal.component.html',
  styleUrls: ['./session-results-modal.component.scss']
})
export class SessionResultsModalComponent {
  @Input() results: SessionResults | null = null;

  @Output() close = new EventEmitter<void>();
  @Output() retryReading = new EventEmitter<void>();
  @Output() finish = new EventEmitter<void>();

  // Additional properties for template
  activeStatsTab: string = 'timing';
  showComparison: boolean = false;
  previousSessions: any[] = [];
  recentSessions: any[] = [];
  maxWPM: number = 500;

  // Math reference for template
  Math = Math;

  // Event handlers
  onClose(): void {
    this.close.emit();
  }

  onRetryReading(): void {
    this.retryReading.emit();
  }

  onFinish(): void {
    this.finish.emit();
  }

  // Stats tab management
  setStatsTab(tab: string): void {
    this.activeStatsTab = tab;
  }

  // Mode helpers
  getModeIcon(): string {
    if (!this.results?.session) return 'bi-book';
    
    switch (this.results.session.readingMode) {
      case ReadingMode.CLASSIC: return 'bi-book';
      case ReadingMode.RSVP: return 'bi-eye';
      case ReadingMode.CHUNK: return 'bi-layers';
      case ReadingMode.GUIDED: return 'bi-arrow-right';
      default: return 'bi-book';
    }
  }

  getModeDisplayName(): string {
    if (!this.results?.session) return 'Okuma';
    
    switch (this.results.session.readingMode) {
      case ReadingMode.CLASSIC: return 'Klasik Okuma';
      case ReadingMode.RSVP: return 'RSVP Okuma';
      case ReadingMode.CHUNK: return 'Grup Okuma';
      case ReadingMode.GUIDED: return 'Rehberli Okuma';
      default: return 'Okuma';
    }
  }

  // Progress calculation
  getReadingProgress(): number {
    if (!this.results?.session) return 0;
    return (this.results.session.wordCount / this.results.session.wordCount) * 100;
  }

  // Statistics calculations
  getPauseFrequency(): string {
    if (!this.results?.session) return '0';
    
    const totalMinutes = this.results.session.totalDuration / (1000 * 60);
    if (totalMinutes === 0 || this.results.session.pauseCount === 0) return '0';
    
    const frequency = totalMinutes / this.results.session.pauseCount;
    return frequency.toFixed(1);
  }

  getReadingEfficiency(): number {
    if (!this.results?.session) return 0;
    
    const readingTime = this.results.session.readingDuration;
    const totalTime = this.results.session.totalDuration;
    
    if (totalTime === 0) return 0;
    return Math.round((readingTime / totalTime) * 100);
  }

  // Mode checking
  isChunkMode(): boolean {
    return this.results?.session?.readingMode === ReadingMode.CHUNK;
  }

  // Recommendation actions
  applyRecommendedSpeed(): void {
    if (this.results?.recommendedSpeed) {
      console.log('Applying recommended speed:', this.results.recommendedSpeed);
      // Emit event to parent component to apply speed
    }
  }

  shareResults(): void {
    if (!this.results) return;
    
    const shareText = `Okuma Sonuçlarım:
📚 Mod: ${this.getModeDisplayName()}
⚡ Hız: ${this.results.session.wordsPerMinute} WPM
📊 Verimlilik: ${this.results.efficiency}%
⏱️ Süre: ${this.formatTime(this.results.session.totalDuration)}
📖 Kelime: ${this.results.session.wordCount}`;

    if (navigator.share) {
      navigator.share({
        title: 'Okuma Sonuçlarım',
        text: shareText
      });
    } else if (navigator.clipboard) {
      navigator.clipboard.writeText(shareText);
      console.log('Sonuçlar panoya kopyalandı!');
    }
  }

  saveToHistory(): void {
    if (!this.results) return;
    
    try {
      const history = JSON.parse(localStorage.getItem('reading_history') || '[]');
      history.push({
        ...this.results,
        savedAt: new Date().toISOString()
      });
      
      // Keep only last 50 results
      if (history.length > 50) {
        history.splice(0, history.length - 50);
      }
      
      localStorage.setItem('reading_history', JSON.stringify(history));
      console.log('Sonuçlar geçmişe kaydedildi!');
    } catch (error) {
      console.error('Geçmişe kaydetme hatası:', error);
    }
  }

  // Time formatting
  formatTime(milliseconds: number): string {
    const totalSeconds = Math.floor(milliseconds / 1000);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    if (hours > 0) {
      return `${hours}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    }
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  }

  // Initialize component
  ngOnInit(): void {
    this.loadPreviousSessions();
    this.setupComparison();
  }

  private loadPreviousSessions(): void {
    try {
      const history = JSON.parse(localStorage.getItem('reading_history') || '[]');
      this.previousSessions = history.slice(-10); // Last 10 sessions
      this.recentSessions = history.slice(-5);   // Last 5 sessions for chart
      this.showComparison = this.previousSessions.length > 0;
    } catch (error) {
      console.error('Geçmiş oturumları yükleme hatası:', error);
      this.previousSessions = [];
      this.recentSessions = [];
      this.showComparison = false;
    }
  }

  private setupComparison(): void {
    if (this.recentSessions.length > 0) {
      const allWPMs = this.recentSessions.map(s => s.session?.wordsPerMinute || 0);
      this.maxWPM = Math.max(...allWPMs, 500);
    }
  }
}