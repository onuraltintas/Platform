import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';

import { TextContent, ReadingMode, ReadingSettings, ReadingSession, SessionResults } from '../../../shared/models/reading.models';
import { TextProcessingService } from '../services/text-processing.service';
import { ReadingSessionService } from '../services/reading-session.service';
import { AuthService } from '../../../core/services/auth.service';

// Reading mode components
import { ClassicReaderComponent } from './modes/classic-reader.component';
import { RsvpReaderComponent } from './modes/rsvp-reader.component';
import { ChunkReaderComponent } from './modes/chunk-reader.component';
import { GuidedReaderComponent } from './modes/guided-reader.component';

// UI components
import { ReadingControlsComponent } from './ui/reading-controls.component';
import { SessionResultsModalComponent } from './ui/session-results-modal.component';

@Component({
  selector: 'app-reading-page',
  standalone: true,
  imports: [
    CommonModule,
    ClassicReaderComponent,
    RsvpReaderComponent,
    ChunkReaderComponent,
    GuidedReaderComponent,
    ReadingControlsComponent,
    SessionResultsModalComponent
  ],
  template: `
    <div class="reading-page" [class.dark-theme]="isDarkTheme()">
      <!-- Header -->
      <div class="reading-header">
        <div class="header-left">
          <button class="back-btn" (click)="goBack()">
            <i class="bi bi-arrow-left"></i>
            Geri
          </button>
          <div class="text-info" *ngIf="currentText">
            <h2 class="text-title">{{ currentText.title }}</h2>
            <div class="text-meta">
              <span class="word-count">{{ currentText.wordCount }} kelime</span>
              <span class="mode-info">{{ getModeDisplayName() }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Reading Controls -->
      <app-reading-controls
        *ngIf="currentSession"
        [session]="currentSession"
        [isPlaying]="isPlaying"
        [isPaused]="isPaused"
        [progress]="readingProgress"
        [currentWPM]="currentWPM"
        (play)="playReading()"
        (pause)="pauseReading()"
        (stop)="stopReading()">
      </app-reading-controls>

      <!-- Text Display -->
      <div class="text-display" *ngIf="currentText && readingSettings">
        
        <!-- Classic Reading Mode -->
        <app-classic-reader
          *ngIf="selectedMode === 'classic'"
          [textContent]="currentText"
          [settings]="readingSettings"
          [isActive]="isPlaying"
          (wordsRead)="onWordsRead($event)"
          (scroll)="onScroll($event)"
          (completed)="onReadingCompleted()">
        </app-classic-reader>

        <!-- RSVP Reading Mode -->
        <app-rsvp-reader
          *ngIf="selectedMode === 'rsvp'"
          [textContent]="currentText"
          [settings]="readingSettings"
          [isActive]="isPlaying"
          (wordsRead)="onWordsRead($event)"
          (completed)="onReadingCompleted()">
        </app-rsvp-reader>

        <!-- Chunk Reading Mode -->
        <app-chunk-reader
          *ngIf="selectedMode === 'chunk'"
          [textContent]="currentText"
          [settings]="readingSettings"
          [isActive]="isPlaying"
          (wordsRead)="onWordsRead($event)"
          (completed)="onReadingCompleted()">
        </app-chunk-reader>

        <!-- Guided Reading Mode -->
        <app-guided-reader
          *ngIf="selectedMode === 'guided'"
          [textContent]="currentText"
          [settings]="readingSettings"
          [isActive]="isPlaying"
          (wordsRead)="onWordsRead($event)"
          (completed)="onReadingCompleted()">
        </app-guided-reader>
      </div>

      <!-- Session Results Modal -->
      <app-session-results-modal
        *ngIf="showResults && sessionResults"
        [results]="sessionResults"
        (close)="closeResults()"
        (retryReading)="retryReading()"
        (finish)="finishReading()">
      </app-session-results-modal>
    </div>
  `,
  styleUrls: ['./reading-page.component.scss']
})
export class ReadingPageComponent implements OnInit, OnDestroy {
  currentText: TextContent | null = null;
  selectedMode: ReadingMode = ReadingMode.CLASSIC;
  readingSettings: ReadingSettings = this.getDefaultSettings();
  
  sessionActive = false;
  isPlaying = false;
  isPaused = false;
  showResults = false;
  isLoading = false;
  
  currentSession: ReadingSession | null = null;
  sessionResults: SessionResults | null = null;
  readingProgress = 0;
  currentWPM = 0;
  
  private sessionSubscription: Subscription | null = null;

  constructor(
    private textProcessingService: TextProcessingService,
    private sessionService: ReadingSessionService,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    console.log('📖 ReadingPageComponent ngOnInit called');
    
    this.loadSettingsAndText();
    this.setupSubscriptions();
  }

  ngOnDestroy(): void {
    this.cleanup();
  }

  private loadSettingsAndText(): void {
    const qp = this.route.snapshot.queryParamMap;
    const textId = qp.get('textId');
    const mode = qp.get('mode');

    if (mode) {
      this.selectedMode = mode as ReadingMode;
    }

    // Load saved settings
    this.restoreSettings();

    // Load text
    if (textId) {
      this.loadText(textId);
    } else {
      this.router.navigate(['/reading']);
    }
  }

  private loadText(textId: string): void {
    this.isLoading = true;
    
    this.textProcessingService.getTextById(textId).subscribe({
      next: (text) => {
        if (text) {
          this.currentText = text;
          this.isLoading = false;
          this.startSession();
        } else {
          // Text not found
          console.error('Text not found:', textId);
          this.isLoading = false;
          // Could redirect to text selection page or show error
        }
      },
      error: (error) => {
        console.error('Error loading text:', error);
        this.isLoading = false;
        // Could show error message to user
      }
    });
  }

  private startSession(): void {
    if (!this.currentText) return;

    const user = this.authService.getCurrentUserValue();
    if (!user) {
      this.router.navigate(['/auth/login']);
      return;
    }

    this.currentSession = this.sessionService.startSession(
      this.currentText,
      user.id,
      this.selectedMode,
      this.readingSettings
    );

    this.sessionActive = true;
    this.isPlaying = true;
  }

  playReading(): void {
    this.isPlaying = true;
    this.isPaused = false;
    this.sessionService.resumeSession();
  }

  pauseReading(): void {
    this.isPlaying = false;
    this.isPaused = true;
    this.sessionService.pauseSession();
  }

  stopReading(): void {
    this.completeReading();
  }

  onReadingCompleted(): void {
    this.completeReading();
  }

  private completeReading(): void {
    const completedSession = this.sessionService.endSession();
    if (completedSession) {
      this.sessionService.saveSession(completedSession);
      this.showSessionResults(completedSession);
    }
    
    this.resetSession();
    this.showResults = true;
  }

  onWordsRead(wordCount: number): void {
    this.sessionService.updateWordsRead(wordCount);
    const session = this.sessionService.getCurrentSession();
    if (session) {
      this.readingProgress = Math.min(100, (wordCount / (session.wordCount || 1)) * 100);
      this.currentWPM = session.wordsPerMinute;
    }
  }

  onScroll(event: Event): void {
    this.sessionService.trackScrollEvent();
  }

  

  private showSessionResults(session: ReadingSession): void {
    this.sessionResults = {
      session: session,
      efficiency: this.calculateEfficiency(session),
      consistency: this.calculateConsistency(session),
      improvement: this.calculateImprovement(session),
      recommendedSpeed: this.calculateRecommendedSpeed(session),
      suggestedExercises: this.getSuggestedExercises(session),
      nextSteps: this.getNextSteps(session)
    };

    this.showResults = true;
  }

  closeResults(): void {
    this.showResults = false;
    this.sessionResults = null;
  }

  retryReading(): void {
    this.closeResults();
    this.startSession();
  }

  finishReading(): void {
    this.closeResults();
    this.router.navigate(['/reading']);
  }

  goBack(): void {
    if (this.sessionActive) {
      if (confirm('Okuma oturumu devam ediyor. Çıkmak istediğinizden emin misiniz?')) {
        this.sessionService.endSession();
        this.router.navigate(['/reading']);
      }
    } else {
      this.router.navigate(['/reading']);
    }
  }

  getModeDisplayName(): string {
    switch (this.selectedMode) {
      case ReadingMode.CLASSIC: return 'Klasik Okuma';
      case ReadingMode.RSVP: return 'RSVP Okuma';
      case ReadingMode.CHUNK: return 'Grup Okuma';
      case ReadingMode.GUIDED: return 'Rehberli Okuma';
      default: return 'Okuma';
    }
  }

  isDarkTheme(): boolean {
    return this.readingSettings.backgroundColor === '#1f2937';
  }

  private resetSession(): void {
    this.sessionActive = false;
    this.isPlaying = false;
    this.isPaused = false;
    this.currentSession = null;
    this.readingProgress = 0;
    this.currentWPM = 0;
  }

  private setupSubscriptions(): void {
    this.sessionSubscription = this.sessionService.session$.subscribe(session => {
      this.currentSession = session;
    });
  }

  private cleanup(): void {
    if (this.sessionSubscription) {
      this.sessionSubscription.unsubscribe();
    }
    if (this.sessionActive) {
      this.sessionService.endSession();
    }
  }

  private getDefaultSettings(): ReadingSettings {
    return {
      wordsPerMinute: 250,
      chunkSize: 3,
      fontSize: 16,
      fontFamily: 'Inter, sans-serif',
      lineHeight: 1.6,
      backgroundColor: '#ffffff',
      textColor: '#333333',
      highlightColor: '#3b82f6',
      autoStart: false,
      autoPause: false,
      showProgress: true,
      enableSounds: false,
      rsvpFocusPoint: true,
      rsvpWordDuration: 240,
      chunkHighlightDuration: 800,
      chunkPauseDuration: 200,
      showContext: true,
      showFocusPoint: true,
      highlighterSpeed: 250,
      highlighterHeight: 2,
      showReadingGuide: true,
      showFocusWindow: false,
      showGuideLines: false,
      enableSpeedMode: true,
      enableHighlighting: true,
      highlightRange: 3,
      bionicEnabled: false
    };
  }

  private restoreSettings(): void {
    try {
      const saved = localStorage.getItem('reading_settings');
      if (saved) {
        const parsed = JSON.parse(saved);
        this.readingSettings = { ...this.getDefaultSettings(), ...parsed };
      }
    } catch (error) {
      console.warn('Failed to restore settings:', error);
    }
  }


  private calculateEfficiency(session: ReadingSession): number {
    const speedScore = Math.min(100, (session.wordsPerMinute / 300) * 100);
    const consistencyScore = 100 - (session.regressionCount * 5);
    return Math.round((speedScore + consistencyScore) / 2);
  }

  private calculateConsistency(session: ReadingSession): number {
    const pausePenalty = Math.min(50, session.pauseCount * 5);
    const regressionPenalty = Math.min(30, session.regressionCount * 3);
    return Math.max(0, 100 - pausePenalty - regressionPenalty);
  }

  private calculateImprovement(session: ReadingSession): number {
    return 15; // Mock improvement
  }

  private calculateRecommendedSpeed(session: ReadingSession): number {
    if (session.regressionCount > 3) {
      return Math.max(150, session.wordsPerMinute - 25);
    }
    if (session.pauseCount < 2) {
      return Math.min(500, session.wordsPerMinute + 25);
    }
    return session.wordsPerMinute;
  }

  private getSuggestedExercises(session: ReadingSession): string[] {
    const exercises = [];
    
    if (session.wordsPerMinute < 200) {
      exercises.push('Göz Koordinasyon Egzersizi');
    }
    if (session.regressionCount > 3) {
      exercises.push('Odaklanma Egzersizi');
    }
    if (session.pauseCount > 5) {
      exercises.push('Ritim Geliştirme Egzersizi');
    }
    
    return exercises.length > 0 ? exercises : ['Hız Artırma Egzersizi'];
  }

  private getNextSteps(session: ReadingSession): string[] {
    const steps = [];
    
    if (session.wordsPerMinute < 250) {
      steps.push('Günlük 15 dakika RSVP pratiği yapın');
    }
    if (session.regressionCount > 2) {
      steps.push('Chunk reading tekniği ile pratik yapın');
    }
    
    steps.push('Daha zor metinlerle deneyim kazanın');
    
    return steps;
  }
}