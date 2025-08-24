import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ReadingMode, ReadingSettings, TextContent } from '../../../shared/models/reading.models';
import { TextProcessingService } from '../services/text-processing.service';
import { ReadingContentApiService } from '../services/reading-content-api.service';


@Component({
  selector: 'app-reading-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="reading-settings-page">
      <div class="settings-header">
        <button class="back-btn" (click)="goBack()">
          <i class="bi bi-arrow-left"></i>
          Geri
        </button>
        <div class="mode-info">
          <div class="mode-icon">
            <i class="bi" [class]="selectedModeInfo?.icon"></i>
          </div>
          <div>
            <h2>{{ selectedModeInfo?.name }}</h2>
            <p>{{ selectedModeInfo?.description }}</p>
          </div>
        </div>
      </div>

      <div class="settings-content">
        <div class="settings-sections">
          
          <!-- Text Selection -->
          <div class="settings-section">
            <h3>Metin Seçimi</h3>

            <!-- Difficulty Filter -->
            <div class="difficulty-filter">
              <label>Zorluk:</label>
              <select [(ngModel)]="selectedDifficulty" (change)="onDifficultyChange()">
                <option value="">Tümü</option>
                <option value="Temel">Temel</option>
                <option value="Orta">Orta</option>
                <option value="İleri">İleri</option>
                <option value="Uzman">Uzman</option>
              </select>
            </div>
            
            <!-- Loading State -->
            <div class="text-loading" *ngIf="isLoadingTexts">
              <i class="bi bi-arrow-clockwise spin"></i>
              <span>Metinler yükleniyor...</span>
            </div>
            
            <!-- No Texts Available -->
            <div class="no-texts" *ngIf="!isLoadingTexts && availableTexts.length === 0">
              <i class="bi bi-exclamation-triangle"></i>
              <span>Henüz metin bulunmuyor. Lütfen daha sonra tekrar deneyin.</span>
            </div>
            
            <!-- Text Options -->
            <div class="text-options" *ngIf="!isLoadingTexts && availableTexts.length > 0">
              <div 
                class="text-option"
                *ngFor="let text of availableTexts"
                [class.selected]="selectedText?.id === text.id"
                (click)="selectText(text)">
                <div class="text-meta">
                  <h4>{{ text.title }}</h4>
                  <div class="text-stats">
                    <span><i class="bi bi-file-text"></i> {{ text.wordCount }} kelime</span>
                    <span><i class="bi bi-clock"></i> ~{{ text.estimatedReadingTime }} dk</span>
                    <span><i class="bi bi-bar-chart"></i> {{ getDifficultyLabel(text.difficultyLevel) }}</span>
                  </div>
                </div>
                <div class="select-indicator">
                  <i class="bi bi-check-circle-fill" *ngIf="selectedText?.id === text.id"></i>
                  <i class="bi bi-circle" *ngIf="selectedText?.id !== text.id"></i>
                </div>
              </div>
            </div>
          </div>

          <!-- Speed Settings -->
          <div class="settings-section">
            <h3>Hız Ayarları</h3>
            <div class="speed-controls">
              <label>Okuma Hızı: {{ settings.wordsPerMinute }} WPM</label>
              <input 
                type="range" 
                [(ngModel)]="settings.wordsPerMinute"
                min="60" 
                max="800" 
                step="5"
                class="speed-slider">
              <div class="speed-labels">
                <span>Yavaş (100)</span>
                <span>Orta (250)</span>
                <span>Hızlı (500)</span>
                <span>Çok Hızlı (800)</span>
              </div>
            </div>
          </div>

          <!-- Mode Specific Settings -->
          <div class="settings-section" *ngIf="selectedMode === 'chunk'">
            <h3>Grup Okuma Ayarları</h3>
            <div class="chunk-settings">
              <label>Grup Boyutu: {{ settings.chunkSize }} kelime</label>
              <input 
                type="range" 
                [(ngModel)]="settings.chunkSize"
                min="2" 
                max="6" 
                step="1"
                class="chunk-slider">
            </div>
          </div>

          <div class="settings-section" *ngIf="selectedMode === 'guided'">
            <h3>Rehberli Okuma Ayarları</h3>
            <div class="guided-settings">
              <label>Rehber Hızı: {{ settings.highlighterSpeed }} WPM</label>
              <input 
                type="range" 
                [(ngModel)]="settings.highlighterSpeed"
                min="150" 
                max="600" 
                step="25"
                class="highlighter-slider">
              
              <div class="checkbox-group">
                <label class="checkbox-item">
                  <input type="checkbox" [(ngModel)]="settings.showFocusWindow">
                  <span class="checkmark"></span>
                  Odak Penceresi Göster
                </label>
                <label class="checkbox-item">
                  <input type="checkbox" [(ngModel)]="settings.showReadingGuide">
                  <span class="checkmark"></span>
                  Okuma Rehberi Göster
                </label>
              </div>
            </div>
          </div>

          <!-- Display Settings -->
          <div class="settings-section">
            <h3>Görünüm Ayarları</h3>
            <div class="display-settings">
              <div class="font-settings">
                <label>Yazı Boyutu: {{ settings.fontSize }}px</label>
                <input 
                  type="range" 
                  [(ngModel)]="settings.fontSize"
                  min="12" 
                  max="24" 
                  step="1"
                  class="font-slider">
              </div>

              <div class="theme-settings">
                <label>Tema</label>
                <div class="theme-options">
                  <button 
                    class="theme-btn light"
                    [class.active]="settings.backgroundColor === '#ffffff'"
                    (click)="setTheme('light')">
                    <i class="bi bi-sun"></i>
                    Açık
                  </button>
                  <button 
                    class="theme-btn dark"
                    [class.active]="settings.backgroundColor === '#1f2937'"
                    (click)="setTheme('dark')">
                    <i class="bi bi-moon"></i>
                    Koyu
                  </button>
                </div>
              </div>
            </div>
          </div>

          
        </div>

        <!-- Start Button -->
        <div class="start-section">
          <button 
            class="btn-start" 
            [disabled]="!selectedText"
            (click)="startReading()">
            <i class="bi bi-play-fill"></i>
            Okumaya Başla
          </button>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./reading-settings.component.scss']
})
export class ReadingSettingsComponent implements OnInit {
  selectedMode: ReadingMode = ReadingMode.CLASSIC;
  settings: ReadingSettings = this.getDefaultSettings();
  selectedText: TextContent | null = null;
  availableTexts: TextContent[] = [];
  isLoadingTexts: boolean = false;
  selectedDifficulty: string = '';

  selectedModeInfo: any = null;

  modeInfoMap = {
    [ReadingMode.CLASSIC]: {
      name: 'Klasik Okuma',
      description: 'Geleneksel okuma deneyimi ile hızınızı doğal olarak artırın',
      icon: 'bi-book-open'
    },
    [ReadingMode.RSVP]: {
      name: 'RSVP Okuma',
      description: 'Kelimeleri tek tek merkezi noktada görerek okuma hızınızı maksimize edin',
      icon: 'bi-eye'
    },
    [ReadingMode.CHUNK]: {
      name: 'Grup Okuma',
      description: 'Kelimeleri gruplar halinde görerek çevresel görüş kapasitesini geliştirin',
      icon: 'bi-layers'
    },
    [ReadingMode.GUIDED]: {
      name: 'Rehberli Okuma',
      description: 'Hareket eden vurgu çubuğu ile rehberli okuma yapın',
      icon: 'bi-arrow-right-square'
    }
  };

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private textProcessingService: TextProcessingService,
    private readingContentApiService: ReadingContentApiService
  ) {}

  ngOnInit(): void {
    console.log('⚙️ ReadingSettingsComponent ngOnInit called');
    
    // Get selected mode from query params
    const mode = this.route.snapshot.queryParamMap.get('mode') as ReadingMode;
    console.log('⚙️ Mode from query params:', mode);
    
    if (mode) {
      this.selectedMode = mode;
      this.selectedModeInfo = this.modeInfoMap[mode];
    }

    this.restoreSettings();
    this.restoreDifficulty();
    this.loadAvailableTexts();
  }

  goBack(): void {
    this.router.navigate(['/reading']);
  }

  selectText(text: TextContent): void {
    this.selectedText = text;
  }

  setTheme(theme: 'light' | 'dark'): void {
    if (theme === 'light') {
      this.settings.backgroundColor = '#ffffff';
      this.settings.textColor = '#374151';
      this.settings.highlightColor = '#3b82f6';
    } else {
      this.settings.backgroundColor = '#1f2937';
      this.settings.textColor = '#f9fafb';
      this.settings.highlightColor = '#60a5fa';
    }
  }

  startReading(): void {
    if (!this.selectedText) return;

    // Save settings
    this.persistSettings();

    // Navigate to reading page
    this.router.navigate(['/reading/start'], {
      queryParams: {
        textId: this.selectedText.id,
        mode: this.selectedMode
      }
    });
  }

  getDifficultyLabel(level: number): string {
    if (level <= 3) return 'Kolay';
    if (level <= 6) return 'Orta';
    if (level <= 8) return 'Zor';
    return 'Çok Zor';
  }

  private loadAvailableTexts(): void {
    // Load texts from database
    this.isLoadingTexts = true;
    
    this.readingContentApiService.getTexts({ difficultyLevel: this.selectedDifficulty || undefined }).subscribe({
      next: (texts) => {
        this.availableTexts = texts;
        this.isLoadingTexts = false;
        
        // Select first text by default
        if (this.availableTexts.length > 0) {
          this.selectedText = this.availableTexts[0];
        }
      },
      error: (error) => {
        console.error('Failed to load texts:', error);
        this.isLoadingTexts = false;
        // Fallback to empty array - user will see no texts available
        this.availableTexts = [];
      }
    });
  }

  onDifficultyChange(): void {
    try { localStorage.setItem('reading_selected_difficulty', this.selectedDifficulty || ''); } catch {}
    this.loadAvailableTexts();
  }

  private restoreDifficulty(): void {
    try {
      const saved = localStorage.getItem('reading_selected_difficulty');
      if (saved !== null) {
        this.selectedDifficulty = saved;
      }
    } catch {}
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

  persistSettings(): void {
    try {
      localStorage.setItem('reading_settings', JSON.stringify(this.settings));
    } catch (error) {
      console.warn('Failed to save settings:', error);
    }
  }

  private restoreSettings(): void {
    try {
      const saved = localStorage.getItem('reading_settings');
      if (saved) {
        const parsed = JSON.parse(saved);
        this.settings = { ...this.getDefaultSettings(), ...parsed };
      }
    } catch (error) {
      console.warn('Failed to restore settings:', error);
    }
  }
}