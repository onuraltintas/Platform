import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges, SimpleChanges, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReadingSession, ReadingMode } from '../../../../shared/models/reading.models';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-reading-controls',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reading-controls.component.html',
  styleUrls: ['./reading-controls.component.scss']
})
export class ReadingControlsComponent implements OnInit, OnDestroy, OnChanges {
  @ViewChild('progressTrack', { static: false }) progressTrack!: ElementRef;

  // Inputs
  @Input() session: ReadingSession | null = null;
  @Input() isPlaying: boolean = false;
  @Input() isPaused: boolean = false;
  @Input() progress: number = 0;
  @Input() currentWPM: number = 250;

  // Outputs
  @Output() play = new EventEmitter<void>();
  @Output() pause = new EventEmitter<void>();
  @Output() stop = new EventEmitter<void>();
  

  // Additional properties for advanced template
  currentTime: number = 0;
  estimatedTotalTime: number = 0;
  targetSpeed: number = 250;
  allowSpeedAdjustment: boolean = false;
  showSecondaryControls: boolean = true;
  wordsRead: number = 0;
  totalWords: number = 0;
  sessionDuration: number = 0;
  
  isFullscreen: boolean = false;
  statusMessage: string = '';
  statusMessageType: string = '';
  showShortcuts: boolean = false;
  showMiniControls: boolean = false;
  
  // Enhanced status bar properties
  @Input() readingProgress: number = 0;

  // Subscriptions
  private subscriptions: Subscription[] = [];
  private startTime: number = 0;

  // Math reference for template
  Math = Math;

  ngOnInit(): void {
    try {
      console.log('Reading Controls: Initializing component');
      this.initializeControls();
      this.startTimerUpdate();
    } catch (error) {
      console.error('Reading Controls: Error during initialization', error);
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    // React to input changes in real-time
    if (changes['session'] && this.session) {
      console.log('Reading Controls: Session input changed, reinitializing...');
      this.initializeControls();
    }
    
    if (changes['progress']) {
      this.updateSessionData();
    }
    
    if (changes['currentWPM'] && changes['currentWPM'].currentValue) {
      // Update displays when WPM changes from parent
      console.log('Reading Controls: WPM updated from parent:', changes['currentWPM'].currentValue);
    }
    
    if (changes['isPlaying']) {
      console.log('Reading Controls: Playing state changed:', changes['isPlaying'].currentValue);
    }
  }

  ngOnDestroy(): void {
    try {
      console.log('Reading Controls: Component destroying, cleaning up resources');
      
      // Unsubscribe from all subscriptions
      this.subscriptions.forEach(sub => {
        try {
          sub.unsubscribe();
        } catch (error) {
          console.warn('Reading Controls: Error unsubscribing', error);
        }
      });
      
      // Clear arrays
      this.subscriptions = [];
      
      // Reset state
      this.isPlaying = false;
      this.isFullscreen = false;
      this.showMiniControls = false;
      
    } catch (error) {
      console.error('Reading Controls: Error during component cleanup', error);
    }
  }

  private initializeControls(): void {
    try {
      if (this.session) {
        // Initialize speed settings
        this.targetSpeed = this.session.settings?.wordsPerMinute || 250;
        this.currentWPM = this.targetSpeed; // Start with target speed
        
        // Initialize word counts
        this.totalWords = this.session.wordCount || 0;
        this.wordsRead = 0;
        
        // Initialize timing
        this.startTime = this.session.startTime?.getTime() || Date.now();
        
        // Initialize progress
        this.readingProgress = 0;
        this.progress = 0;
        
        // Update initial session data
        this.updateSessionData();
        
        console.log('Reading Controls: Initialized with session data', {
          targetSpeed: this.targetSpeed,
          totalWords: this.totalWords,
          readingMode: this.session.readingMode
        });
      } else {
        console.warn('Reading Controls: No session provided, using defaults');
        this.targetSpeed = 250;
        this.currentWPM = 250;
        this.totalWords = 0;
        this.startTime = Date.now();
      }
      
    } catch (error) {
      console.error('Reading Controls: Error initializing controls', error);
      // Set safe defaults
      this.targetSpeed = 250;
      this.currentWPM = 250;
      this.totalWords = 0;
      this.startTime = Date.now();
    }
  }

  private startTimerUpdate(): void {
    const timer = setInterval(() => {
      this.updateTimingInfo();
    }, 1000);

    // Store subscription for cleanup
    this.subscriptions.push({
      unsubscribe: () => clearInterval(timer)
    } as Subscription);
  }

  private updateTimingInfo(): void {
    if (this.session) {
      // Always update session duration if session exists
      if (this.session.startTime) {
        this.currentTime = Date.now() - this.session.startTime.getTime();
        this.sessionDuration = this.currentTime;
      }
      
      // Update session data in real-time
      this.updateSessionData();
      
      // Emit progress updates if playing
      if (this.isPlaying && this.currentWPM > 0) {
        // Calculate words read based on time and speed
        const elapsedMinutes = this.sessionDuration / (1000 * 60);
        const theoreticalWordsRead = Math.floor(elapsedMinutes * this.currentWPM);
        
        // Don't exceed total words
        this.wordsRead = Math.min(theoreticalWordsRead, this.totalWords);
        
        // Update progress percentage
        if (this.totalWords > 0) {
          this.progress = (this.wordsRead / this.totalWords) * 100;
          this.readingProgress = this.progress;
        }
      }
    }
  }


  // Event handlers
  onPlay(): void {
    this.play.emit();
    this.showStatusMessage('Okuma başlatıldı', 'success');
  }

  onPause(): void {
    this.pause.emit();
    this.showStatusMessage('Okuma duraklatıldı', 'info');
  }

  onStop(): void {
    this.stop.emit();
    this.showStatusMessage('Okuma durduruldu', 'warning');
  }

  

  onProgressClick(event: MouseEvent): void {
    if (!this.progressTrack) return;
    
    const rect = this.progressTrack.nativeElement.getBoundingClientRect();
    const percentage = ((event.clientX - rect.left) / rect.width) * 100;
    
    // Emit progress change event (would need to add this output)
    console.log('Progress clicked:', percentage);
  }

  

  

  

  toggleFullscreen(): void {
    this.isFullscreen = !this.isFullscreen;
    this.showMiniControls = this.isFullscreen;
    
    if (this.isFullscreen) {
      document.documentElement.requestFullscreen?.();
    } else {
      document.exitFullscreen?.();
    }
  }

  

  // Mode helpers
  getModeClass(): string {
    if (!this.session) return 'classic';
    return this.session.readingMode;
  }

  getModeIcon(): string {
    const mode = this.getModeClass();
    switch (mode) {
      case ReadingMode.CLASSIC: return 'bi-book';
      case ReadingMode.RSVP: return 'bi-eye';
      case ReadingMode.CHUNK: return 'bi-layers';
      case ReadingMode.GUIDED: return 'bi-arrow-right';
      default: return 'bi-book';
    }
  }

  getModeDisplayName(): string {
    const mode = this.getModeClass();
    switch (mode) {
      case ReadingMode.CLASSIC: return 'Klasik Okuma';
      case ReadingMode.RSVP: return 'RSVP Okuma';
      case ReadingMode.CHUNK: return 'Grup Okuma';
      case ReadingMode.GUIDED: return 'Rehberli Okuma';
      default: return 'Okuma';
    }
  }

  // Status message handling
  private showStatusMessage(message: string, type: string): void {
    this.statusMessage = message;
    this.statusMessageType = type;
    
    setTimeout(() => {
      this.clearStatusMessage();
    }, 3000);
  }

  clearStatusMessage(): void {
    this.statusMessage = '';
    this.statusMessageType = '';
  }

  getStatusIcon(): string {
    switch (this.statusMessageType) {
      case 'success': return 'bi-check-circle';
      case 'info': return 'bi-info-circle';
      case 'warning': return 'bi-exclamation-circle';
      case 'error': return 'bi-x-circle';
      default: return 'bi-info-circle';
    }
  }

  // Shortcuts
  hideShortcuts(): void {
    this.showShortcuts = false;
  }

  // Style methods
  getProgressFillStyles(): any {
    return {
      'background-color': '#3b82f6',
      'transition': 'width 0.3s ease'
    };
  }

  getProgressThumbStyles(): any {
    return {
      'background-color': '#1d4ed8',
      'width': '12px',
      'height': '12px',
      'border-radius': '50%',
      'transform': 'translateY(-50%)'
    };
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

  // Update session data - Enhanced with real-time calculations
  updateSessionData(): void {
    if (this.session) {
      // Update basic session info
      this.wordsRead = Math.floor((this.progress / 100) * this.session.wordCount);
      this.totalWords = this.session.wordCount;
      
      // Calculate reading progress from session
      this.readingProgress = this.progress;
      
      // Update session duration in real-time
      if (this.isPlaying && this.session.startTime) {
        const now = Date.now();
        const sessionStart = this.session.startTime.getTime();
        this.sessionDuration = now - sessionStart;
      }
      
      // Sync target speed with current settings
      if (this.session.settings?.wordsPerMinute) {
        this.targetSpeed = this.session.settings.wordsPerMinute;
      }
      
      // Calculate estimated total time
      this.calculateEstimatedTime();
      
      console.log('Session data updated:', {
        wordsRead: this.wordsRead,
        totalWords: this.totalWords,
        progress: this.readingProgress,
        currentWPM: this.currentWPM,
        targetSpeed: this.targetSpeed
      });
    }
  }
  
  private calculateEstimatedTime(): void {
    if (this.currentWPM > 0 && this.totalWords > 0) {
      const remainingWords = this.totalWords - this.wordsRead;
      const remainingMinutes = remainingWords / this.currentWPM;
      this.estimatedTotalTime = this.sessionDuration + (remainingMinutes * 60000);
    } else {
      this.estimatedTotalTime = 0;
    }
  }

}