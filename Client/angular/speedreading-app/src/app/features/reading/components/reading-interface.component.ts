import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';

import { TextContent, ReadingMode, ReadingSettings, ReadingSession, SessionResults } from '../../../shared/models/reading.models';
import { TextProcessingService } from '../services/text-processing.service';
import { ReadingSessionService } from '../services/reading-session.service';
import { AuthService } from '../../../core/services/auth.service';
import { BackendSyncService } from '../services/backend-sync.service';
import { ReadingProgressApiService } from '../services/reading-progress-api.service';

// UI components
import { ClassicReaderComponent } from './modes/classic-reader.component';
import { RsvpReaderComponent } from './modes/rsvp-reader.component';
import { GuidedReaderComponent } from './modes/guided-reader.component';
import { ChunkReaderComponent } from './modes/chunk-reader.component';
import { SessionResultsModalComponent } from './ui/session-results-modal.component';
import { ComprehensionTestComponent } from './ui/comprehension-test.component';

@Component({
  selector: 'app-reading-interface',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ClassicReaderComponent,
    RsvpReaderComponent,
    GuidedReaderComponent,
    ChunkReaderComponent,
    SessionResultsModalComponent,
    ComprehensionTestComponent
  ],
  templateUrl: './reading-interface.component.html',
  styleUrls: ['./reading-interface.component.scss']
})
export class ReadingInterfaceComponent implements OnInit, OnDestroy {
  currentText: TextContent | null = null;
  selectedMode: ReadingMode = ReadingMode.CLASSIC;
  readingSettings: ReadingSettings = this.getDefaultSettings();
  
  sessionActive = false;
  isPlaying = false;
  isPaused = false;
  showResults = false;
  isLoading = false;
  showOnboarding = false;
  showComprehensionTest = false;
  
  // Backend status
  backendAvailable = false;
  syncInProgress = false;
  
  currentSession: ReadingSession | null = null;
  sessionResults: SessionResults | null = null;
  comprehensionQuestions: any[] = [];
  readingProgress = 0;
  currentWPM = 0;
  
  private sessionSubscription: Subscription | null = null;
  private performanceSubscription: Subscription | null = null;

  availableModes = [
    {
      id: ReadingMode.CLASSIC,
      name: 'Klasik Okuma',
      description: 'Geleneksel okuma deneyimi ile hızınızı doğal olarak artırın',
      icon: 'bi-book',
      features: ['Doğal okuma', 'İlerleme takibi', 'Vurgu sistemi']
    },
    {
      id: ReadingMode.RSVP,
      name: 'RSVP Okuma',
      description: 'Kelimeler tek tek merkezi noktada gösterilir',
      icon: 'bi-eye',
      features: ['Hız kontrolü', 'Odak noktası', 'Göz hareketi eliminasyonu']
    },
    {
      id: ReadingMode.CHUNK,
      name: 'Grup Okuma',
      description: 'Kelimeler 2-5\'li gruplar halinde gösterilir',
      icon: 'bi-layers',
      features: ['Grup boyutu', 'Ritim kontrolü', 'Çevresel görüş']
    },
    {
      id: ReadingMode.GUIDED,
      name: 'Rehberli Okuma',
      description: 'Hareket eden vurgu çubuğu ile rehberli okuma',
      icon: 'bi-arrow-right',
      features: ['Hız rehberi', 'Fokus penceresi', 'Yumuşak takip']
    }
  ];

  constructor(
    private textProcessingService: TextProcessingService,
    private sessionService: ReadingSessionService,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private backendSync: BackendSyncService,
    private progressApi: ReadingProgressApiService
  ) {}

  ngOnInit(): void {
    this.restoreSettings();
    this.tryLoadTextFromRoute();
    this.setupSubscriptions();
    this.checkBackendStatus();
    this.setupKeyboardShortcuts();
    this.initOnboarding();
  }

  ngOnDestroy(): void {
    this.cleanup();
  }

  selectMode(mode: ReadingMode): void {
    this.selectedMode = mode;
  }

  // header now only shows back button; removed mode icon/name helpers

  goToSettings(): void {
    if (!this.selectedMode) return;
    
    console.log('🔧 goToSettings() called with mode:', this.selectedMode);
    console.log('🔧 Navigating to /reading/settings');
    
    // Navigate to settings page with selected mode
    this.router.navigate(['/reading/settings'], {
      queryParams: { mode: this.selectedMode }
    });
  }

  startReading(): void {
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

    // Son okunan metni hatırla
    // Kaldığı yerden devam özelliği kaldırıldı: id ve index saklanmıyor
    try {
      localStorage.removeItem('reading_last_text_id');
      localStorage.removeItem(`reading_resume_${this.currentText.id}`);
    } catch {}
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
    // Kontrolleri başlangıç durumuna getir
    this.isPlaying = false;
    this.isPaused = false;
  }

  private completeReading(): void {
    const completedSession = this.sessionService.endSession();
    if (completedSession) {
      this.sessionService.saveSession(completedSession);

      // Oturum sonuçlarını backend'e gönder
      this.progressApi.updateSession(completedSession.sessionId, completedSession).subscribe({
        next: (success) => {
          if (success) {
            console.log('✅ Session results successfully synced with backend.');
          } else {
            console.warn('⚠️ Session results saved locally, will sync later.');
          }
        },
        error: (err) => {
          console.error('❌ Error syncing session results:', err);
        }
      });

      // Okuma tamamlandıktan veya durdurulduktan sonra: önce sonuç ekranı
      // this.showSessionResults(completedSession);
      // İsterseniz sonuçtan sonra anlama testi gösterilecek
      this.prepareComprehensionTest(completedSession.textId);
    } else {
      this.resetSession();
    }
    
    // Sonuç modalını açma işlemini `prepareComprehensionTest` veya 
    // `showSessionResults` sonrasına bırakıyoruz.
    // this.showResults = true; 
  }

  onWordsRead(wordCount: number): void {
    this.sessionService.updateWordsRead(wordCount);
    // UI göstergeleri için güncel WPM/ilerlemeyi koru
    const session = this.sessionService.getCurrentSession();
    if (session) {
      this.readingProgress = Math.min(100, (wordCount / (session.wordCount || 1)) * 100);
      this.currentWPM = session.wordsPerMinute;
    }
  }

  onScroll(event: Event): void {
    this.sessionService.trackScrollEvent();
  }

  

  onSettingsChange(newSettings: ReadingSettings): void {
    this.readingSettings = { ...newSettings };
    this.persistSettings();
  }

  onModeChange(newMode: ReadingMode): void {
    if (this.sessionActive) {
      // If session is active, confirm mode change
      if (confirm('Okuma modunu değiştirmek mevcut oturumu sonlandıracak. Devam etmek istiyor musunuz?')) {
        this.stopReading();
        this.selectedMode = newMode;
      }
    } else {
      this.selectedMode = newMode;
    }
  }

  

  goBack(): void {
    if (this.sessionActive) {
      if (confirm('Okuma oturumu devam ediyor. Çıkmak istediğinizden emin misiniz?')) {
        this.stopReading();
        this.router.navigate(['/dashboard']);
      }
    } else {
      this.router.navigate(['/dashboard']);
    }
  }

  finishReading(): void {
    this.showResults = false;
    this.sessionResults = null;
    // Bitir butonuna basıldığında reading ana sayfasına git (dashboard'a değil)
    this.router.navigate(['/reading']);
  }

  retryReading(): void {
    // Yeniden başlat: istatistikler sıfırlanır ve okuma sayfası gelir
    this.showResults = false;
    this.sessionResults = null;
    
    // Session'ı tamamen temizle
    this.sessionService.endSession();
    this.resetSession();
    
    // Okuma modunu ve ayarları koru, ama yeniden başlat
    this.sessionActive = false;
    this.isPlaying = false;
    this.isPaused = false;
    
    // Aynı metin ve ayarlarla yeni oturum başlat
    if (this.currentText) {
      this.startReading();
    }
  }

  getDifficultyLabel(level: number): string {
    if (level <= 3) return 'Kolay';
    if (level <= 6) return 'Orta';
    if (level <= 8) return 'Zor';
    return 'Çok Zor';
  }

  private loadSampleText(): void {
    this.isLoading = true;
    // Önce son okunan metni dene, yoksa zorluğa göre rastgele/ilk, o da yoksa genel ilk metni seç
    let desiredDifficulty: string | null = null;
    try { desiredDifficulty = localStorage.getItem('reading_selected_difficulty'); } catch {}

    const pickByDifficulty = () => this.textProcessingService.getTexts({ page: 1, pageSize: 1, difficultyLevel: desiredDifficulty || undefined }).subscribe({
      next: (list) => {
        const picked = (list && list.length) ? list[0] : null;
        if (picked) {
          this.currentText = picked;
          this.isLoading = false;
        } else {
          pickFirst();
        }
      },
      error: () => pickFirst()
    });

    const pickFirst = () => this.textProcessingService.getTexts({ page: 1, pageSize: 1 }).subscribe({
      next: (list) => {
        const picked = (list && list.length) ? list[0] : null;
        if (picked) {
          this.currentText = picked;
          this.isLoading = false;
        } else {
          this.fallbackToSample();
        }
      },
      error: (error) => {
        console.warn('Failed to fetch texts list, using sample:', error);
        this.fallbackToSample();
      }
    });

    if (desiredDifficulty) {
      pickByDifficulty();
    } else {
      pickFirst();
    }
  }

  private fallbackToSample(): void {
    this.currentText = this.textProcessingService.processText(
      this.getSampleTextContent(),
      'sample_text_1',
      'Hızlı Okuma Teknikleri'
    );
    this.isLoading = false;
  }

  private tryLoadTextFromRoute(): void {
    this.isLoading = true;
    const qp = this.route.snapshot.queryParamMap;
    const paramId = qp.get('textId') || this.route.snapshot.paramMap.get('textId');
    const mode = qp.get('mode');
    const sample = qp.get('sample');
    const autoStart = qp.get('autoStart');

    // Set mode from query params if provided
    if (mode) {
      this.selectedMode = mode as ReadingMode;
    }
    
    if (paramId) {
      this.textProcessingService.getTextById(paramId).subscribe({
        next: (text) => {
          if (text) {
            this.currentText = text;
            this.isLoading = false;
            // Auto-start reading if requested from settings
            if (autoStart === 'true') {
              setTimeout(() => this.startReading(), 500);
            }
          } else {
            this.loadSampleText();
          }
        },
        error: () => this.loadSampleText()
      });
    } else if (sample === '1') {
      this.loadSampleText();
    } else {
      // Load default text for interface
      this.loadSampleText();
    }
  }

  private getSampleTextContent(): string {
    return `Hızlı okuma, modern dünyanın en değerli becerilerinden biridir. Günümüzde bilgi çağında yaşıyoruz ve her gün tonlarca bilgiyle karşılaşıyoruz. Bu bilgileri hızlı bir şekilde işleyebilmek, hem akademik hem de profesyonel hayatta büyük avantaj sağlar.

    Hızlı okuma teknikleri, beynimizin doğal kapasitesini kullanarak okuma hızımızı artırmamıza yardımcı olur. Bu teknikler arasında RSVP (Rapid Serial Visual Presentation), chunk reading (grup okuma) ve guided reading (rehberli okuma) gibi yöntemler bulunur.

    RSVP tekniği, kelimeleri tek tek merkezi bir noktada göstererek göz hareketlerini minimize eder. Bu sayede okuma hızı önemli ölçüde artar. Chunk reading tekniği ise kelimeleri gruplar halinde sunarak çevresel görüş kapasitesini geliştirir.

    Rehberli okuma tekniği, hareket eden bir vurgu çubuğu ile okuma ritmi sağlar. Bu teknik, özellikle dikkat dağınıklığı yaşayan okuyucular için faydalıdır. Düzenli pratik ile bu tekniklerin tümü öğrenilebilir ve okuma hızı önemli ölçüde artırılabilir.

    Hızlı okuma sadece hız değil, aynı zamanda anlama kapasitesini de geliştirir. Doğru tekniklerle hem hızlı hem de etkili okuma mümkündür. Bu beceri, hayat boyu öğrenmenin vazgeçilmez bir parçasıdır.`;
  }

  private checkBackendStatus(): void {
    this.backendSync.checkAndSync().subscribe({
      next: (status) => {
        this.backendAvailable = status.available;
        this.syncInProgress = this.backendSync.getSyncStatus().inProgress;
        
        if (status.available) {
          console.log('✅ Backend connected and synced');
        } else {
          console.log('📱 Working in offline mode');
        }
      },
      error: (error) => {
        console.warn('Backend status check failed:', error);
        this.backendAvailable = false;
      }
    });
  }

  private setupSubscriptions(): void {
    this.sessionSubscription = this.sessionService.session$.subscribe(session => {
      this.currentSession = session;
    });

    this.performanceSubscription = this.sessionService.performance$.subscribe(performance => {
      if (performance) {
        this.readingProgress = performance.progressPercentage;
        this.currentWPM = performance.currentWPM;
      }
    });
  }

  private showSessionResults(session: ReadingSession): void {
    // Create session results
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

  private resetSession(): void {
    this.sessionActive = false;
    this.isPlaying = false;
    this.isPaused = false;
    this.currentSession = null;
    this.readingProgress = 0;
    this.currentWPM = 0;
    this.showComprehensionTest = false;
  }

  private cleanup(): void {
    if (this.sessionSubscription) {
      this.sessionSubscription.unsubscribe();
    }
    if (this.performanceSubscription) {
      this.performanceSubscription.unsubscribe();
    }
    if (this.sessionActive) {
      this.sessionService.endSession();
    }
    window.removeEventListener('keydown', this.keydownHandler as any);
  }

  private getDefaultSettings(): ReadingSettings {
    return {
      wordsPerMinute: 60,
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

  private prepareComprehensionTest(textId: string): void {
    this.showComprehensionTest = true;
    this.textProcessingService.getComprehensionQuestions(textId).subscribe({
      next: (list) => {
        this.comprehensionQuestions = list || [];
        if (!this.comprehensionQuestions.length && this.currentText) {
          this.comprehensionQuestions = this.textProcessingService.generateSimpleQuestions(this.currentText);
        }
      },
      error: () => {
        if (this.currentText) this.comprehensionQuestions = this.textProcessingService.generateSimpleQuestions(this.currentText);
      }
    });
  }

  onComprehensionCompleted(test: any): void {
    const lastSession = this.sessionService.getSavedSessions().slice(-1)[0];
    if (lastSession) {
      this.sessionResults = {
        session: lastSession,
        comprehensionTest: test,
        efficiency: this.calculateEfficiency(lastSession),
        consistency: this.calculateConsistency(lastSession),
        improvement: this.calculateImprovement(lastSession),
        recommendedSpeed: this.calculateRecommendedSpeed(lastSession),
        suggestedExercises: this.getSuggestedExercises(lastSession),
        nextSteps: this.getNextSteps(lastSession)
      };
      // Sonuçları backend'e gönder (offline kuyruğa alınabilir)
      this.progressApi.submitSessionResults(lastSession.sessionId, this.sessionResults).subscribe({
        next: (ok) => {
          if (ok) {
            try { (window as any).ngx_toastr?.success?.('Sonuçlar kaydedildi'); } catch {}
          } else {
            try { (window as any).ngx_toastr?.warning?.('Sonuçlar çevrimdışı kuyruğa alındı'); } catch {}
          }
        },
        error: () => {
          try { (window as any).ngx_toastr?.error?.('Sonuçlar gönderilemedi, çevrimdışı kuyruğa alındı'); } catch {}
        }
      });
    }
    this.showComprehensionTest = false;
    this.showResults = true;
    this.resetSession(); // Oturumu burada sıfırla
  }

  onComprehensionSkipped(): void {
    const lastSession = this.sessionService.getSavedSessions().slice(-1)[0];
    if (lastSession) {
      this.showSessionResults(lastSession);
    }
    this.showComprehensionTest = false;
    this.resetSession(); // Oturumu burada da sıfırla
  }

  private keydownHandler = (e: KeyboardEvent) => {
    if (e.code === 'Space') {
      e.preventDefault();
      if (this.isPlaying) this.pauseReading(); else this.playReading();
    } else if (e.code === 'ArrowRight') {
      this.onReadingCompleted();
    }
  };

  private setupKeyboardShortcuts(): void {
    window.addEventListener('keydown', this.keydownHandler as any);
  }

  private initOnboarding(): void {
    try {
      const seen = localStorage.getItem('reading_onboarding_seen');
      this.showOnboarding = seen !== '1';
    } catch {
      this.showOnboarding = true;
    }
  }

  closeOnboarding(): void {
    this.showOnboarding = false;
    try { localStorage.setItem('reading_onboarding_seen', '1'); } catch {}
  }

  // theme toggle removed

  private persistSettings(): void {
    try { localStorage.setItem('reading_settings', JSON.stringify(this.readingSettings)); } catch {}
  }

  private restoreSettings(): void {
    try {
      const s = localStorage.getItem('reading_settings');
      if (s) {
        const parsed = JSON.parse(s);
        this.readingSettings = { ...this.getDefaultSettings(), ...parsed };
      }
    } catch {}
  }

  private calculateEfficiency(session: ReadingSession): number {
    // Simple efficiency calculation (can be enhanced with comprehension data)
    const speedScore = Math.min(100, (session.wordsPerMinute / 300) * 100);
    const consistencyScore = 100 - (session.regressionCount * 5);
    return Math.round((speedScore + consistencyScore) / 2);
  }

  private calculateConsistency(session: ReadingSession): number {
    // Consistency based on pauses and regressions
    const pausePenalty = Math.min(50, session.pauseCount * 5);
    const regressionPenalty = Math.min(30, session.regressionCount * 3);
    return Math.max(0, 100 - pausePenalty - regressionPenalty);
  }

  private calculateImprovement(session: ReadingSession): number {
    const userStats = this.sessionService.getUserStats(session.userId);
    if (userStats.totalSessions < 2) return 0;
    
    const improvementRate = ((session.wordsPerMinute - userStats.averageWPM) / userStats.averageWPM) * 100;
    return Math.round(improvementRate);
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