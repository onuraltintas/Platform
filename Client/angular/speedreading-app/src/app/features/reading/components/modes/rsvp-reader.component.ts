import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TextContent, ReadingSettings } from '../../../../shared/models/reading.models';

@Component({
  selector: 'app-rsvp-reader',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './rsvp-reader.component.html',
  styleUrls: ['./rsvp-reader.component.scss']
})
export class RsvpReaderComponent implements OnInit, OnDestroy, OnChanges {
  @Input() textContent!: TextContent;
  @Input() settings!: ReadingSettings;
  @Input() isActive: boolean = false;
  @Input() speed: number = 250; // WPM

  @Output() wordsRead = new EventEmitter<number>();
  @Output() completed = new EventEmitter<void>();

  // Word tracking
  words: string[] = [];
  currentWordIndex: number = 0;
  totalWords: number = 0;
  
  // Display words
  currentWord: string = '';
  previousDisplayWord: string = '';
  nextDisplayWord: string = '';
  
  // Focus letter (for optimal reading point)
  currentWordPrefix: string = '';
  focusLetter: string = '';
  currentWordSuffix: string = '';
  
  // Timing
  currentSpeed: number = 250;
  wordDuration: number = 240; // milliseconds per word
  private interval: any = null; // stats timer
  private wordTimer: any = null; // per-word dynamic timer
  private startTime: number = 0;
  elapsedTime: number = 0;
  estimatedTimeLeft: number = 0;
  
  // Progress
  progressPercentage: number = 0;
  averageSpeed: number = 0;
  
  // State
  isCompleted: boolean = false;
  isPaused: boolean = false;
  showStats: boolean = true;
  finalSpeed: number = 0;
  efficiency: number = 0;
  consistency: number = 85;
  regressionCount: number = 0;
  
  // Additional properties for template
  focusDistance: number = 5;
  previousWord: any = null;
  nextWord: any = null;
  readingProgress: number = 0;
  currentWPM: number = 0;
  finalWPM: number = 0;
  
  // Math reference for template
  Math = Math;

  ngOnInit(): void {
    this.initializeReader();
  }

  ngOnDestroy(): void {
    try {
      console.log('RSVP Reader: Component destroying, cleaning up resources');
      
      // Stop reading and clear interval
      this.stopReading();
      
      // Clear any remaining references
      this.words = [];
      this.currentWord = '';
      this.previousDisplayWord = '';
      this.nextDisplayWord = '';
      this.previousWord = null;
      this.nextWord = null;
      
      // Reset state
      this.isActive = false;
      this.isCompleted = false;
      this.isPaused = false;
      
    } catch (error) {
      console.error('RSVP Reader: Error during component cleanup', error);
    }
  }

  ngOnChanges(): void {
    if (this.isActive && !this.interval) {
      this.startReading();
    } else if (!this.isActive && this.interval) {
      this.pauseReading();
    }

    if (this.speed !== this.currentSpeed) {
      this.currentSpeed = this.speed;
      this.updateWordDuration();
    }
  }

  private initializeReader(): void {
    try {
      if (!this.textContent?.content) {
        console.warn('RSVP Reader: No text content provided');
        return;
      }

      // Extract words from text content with error handling
      this.words = this.textContent.content
        .split(/\s+/)
        .filter(word => word && word.length > 0)
        .map(word => word.trim());
      
      if (this.words.length === 0) {
        console.warn('RSVP Reader: No valid words found in content');
        return;
      }
      
      this.totalWords = this.words.length;
      // Resume support
      // Başlangıç her zaman 0: resume kaldırıldı
      this.currentSpeed = this.settings?.wordsPerMinute || 250;
      this.updateWordDuration();
      this.updateDisplay();
      this.calculateEstimatedTime();
      
      console.log(`RSVP Reader: Initialized with ${this.totalWords} words`);
    } catch (error) {
      console.error('RSVP Reader: Error initializing reader', error);
      // Reset to safe defaults
      this.words = [];
      this.totalWords = 0;
      this.currentWordIndex = 0;
    }
  }

  private startReading(): void {
    try {
      if (this.wordTimer) {
        console.warn('RSVP Reader: Reading already in progress');
        return;
      }
      
      if (this.words.length === 0) {
        console.warn('RSVP Reader: Cannot start reading - no words available');
        return;
      }
      
      this.startTime = Date.now() - this.elapsedTime;
      // stats timer (per second)
      this.interval = setInterval(() => {
        try { this.updateStats(); } catch {}
      }, 1000);
      // kick off dynamic per-word scheduler
      this.scheduleNextWord();
      
      console.log('RSVP Reader: Reading started');
    } catch (error) {
      console.error('RSVP Reader: Error starting reading', error);
      this.pauseReading();
    }
  }

  private pauseReading(): void {
    try {
      if (this.interval) { clearInterval(this.interval); this.interval = null; }
      if (this.wordTimer) { clearTimeout(this.wordTimer); this.wordTimer = null; }
      console.log('RSVP Reader: Reading paused');
    } catch (error) {
      console.error('RSVP Reader: Error pausing reading', error);
    }
  }

  private stopReading(): void {
    try {
      this.pauseReading();
      this.elapsedTime = 0;
      this.currentWordIndex = 0;
      this.updateDisplay();
      console.log('RSVP Reader: Reading stopped');
    } catch (error) {
      console.error('RSVP Reader: Error stopping reading', error);
    }
  }

  private scheduleNextWord(): void {
    if (this.isPaused) return;
    const delay = this.computeCurrentWordDuration();
    this.wordTimer = setTimeout(() => {
      try {
        this.advanceToNextWord();
      } finally {
        if (!this.isCompleted && !this.isPaused) {
          this.scheduleNextWord();
        }
      }
    }, delay);
  }

  private advanceToNextWord(): void {
    if (this.currentWordIndex >= this.totalWords - 1) {
      this.completeReading();
      return;
    }

    this.currentWordIndex++;
    this.updateDisplay();
    this.updateProgress();
    this.updateStats();
    
    // Emit words read
    this.wordsRead.emit(this.currentWordIndex + 1);
  }

  private goToPreviousWord(): void {
    if (this.currentWordIndex > 0) {
      this.currentWordIndex--;
      this.updateDisplay();
      this.updateProgress();
    }
  }

  private updateDisplay(): void {
    try {
      // Validate current word index
      if (this.currentWordIndex < 0 || this.currentWordIndex >= this.words.length) {
        console.warn('RSVP Reader: Invalid word index', this.currentWordIndex);
        this.currentWordIndex = Math.max(0, Math.min(this.currentWordIndex, this.words.length - 1));
      }

      // Set current, previous, and next words with error handling
      this.currentWord = this.words[this.currentWordIndex] || '';
      this.previousDisplayWord = this.currentWordIndex > 0 ? this.words[this.currentWordIndex - 1] : '';
      this.nextDisplayWord = this.currentWordIndex < this.totalWords - 1 ? this.words[this.currentWordIndex + 1] : '';
      
      // Update legacy properties for template compatibility
      this.previousWord = { text: this.previousDisplayWord };
      this.nextWord = { text: this.nextDisplayWord };
      
      // Update progress safely
      this.readingProgress = this.progressPercentage;
      this.currentWPM = this.averageSpeed;
      
      // Calculate optimal reading point (ORP) with error handling
      this.calculateOptimalReadingPoint();
    } catch (error) {
      console.error('RSVP Reader: Error updating display', error);
      // Reset to safe defaults
      this.currentWord = '';
      this.previousDisplayWord = '';
      this.nextDisplayWord = '';
    }
  }

  private calculateOptimalReadingPoint(): void {
    if (!this.currentWord) return;
    
    const wordLength = this.currentWord.length;
    let orpIndex: number;
    
    // ORP calculation based on word length
    if (wordLength === 1) {
      orpIndex = 0;
    } else if (wordLength <= 5) {
      orpIndex = 1;
    } else if (wordLength <= 9) {
      orpIndex = 2;
    } else {
      orpIndex = 3;
    }
    
    // Ensure ORP is within word bounds
    orpIndex = Math.min(orpIndex, wordLength - 1);
    
    this.currentWordPrefix = this.currentWord.substring(0, orpIndex);
    this.focusLetter = this.currentWord.charAt(orpIndex);
    this.currentWordSuffix = this.currentWord.substring(orpIndex + 1);
  }

  private updateProgress(): void {
    this.progressPercentage = Math.round((this.currentWordIndex / this.totalWords) * 100);
  }

  private updateStats(): void {
    this.elapsedTime = Date.now() - this.startTime;
    
    // Calculate average speed
    if (this.elapsedTime > 0) {
      const minutes = this.elapsedTime / (1000 * 60);
      this.averageSpeed = Math.round(this.currentWordIndex / minutes);
    }
    
    // Calculate estimated time left
    const wordsLeft = this.totalWords - this.currentWordIndex;
    this.estimatedTimeLeft = (wordsLeft * this.wordDuration);
  }

  private updateWordDuration(): void {
    // Base word duration from speed (used as default)
    this.wordDuration = Math.max(120, Math.round(60000 / this.currentSpeed));
  }

  private computeCurrentWordDuration(): number {
    let base = Math.round(60000 / this.currentSpeed);
    const w = this.currentWord || '';
    if (/[\.;!?]$/.test(w)) base = Math.round(base * 1.6);
    else if (/[,;:]$/.test(w)) base = Math.round(base * 1.3);
    if (w.length >= 10) base = Math.round(base * 1.15);
    return Math.max(120, base);
  }

  private calculateEstimatedTime(): void {
    this.estimatedTimeLeft = this.totalWords * this.wordDuration;
  }

  private completeReading(): void {
    this.pauseReading();
    this.isCompleted = true;
    this.finalSpeed = this.averageSpeed;
    this.finalWPM = this.averageSpeed;
    this.efficiency = this.calculateEfficiency();
    try { localStorage.removeItem(`reading_resume_${this.textContent.id}`); } catch {}
    this.completed.emit();
  }

  private calculateEfficiency(): number {
    const timeEfficiency = Math.min(100, (this.currentSpeed / 300) * 100);
    const completionBonus = this.currentWordIndex === this.totalWords ? 20 : 0;
    return Math.min(100, timeEfficiency + completionBonus);
  }

  // Public methods for controls
  increaseSpeed(): void {
    this.currentSpeed = Math.min(1000, this.currentSpeed + 25);
    this.updateWordDuration();
  }

  decreaseSpeed(): void {
    this.currentSpeed = Math.max(100, this.currentSpeed - 25);
    this.updateWordDuration();
  }

  nextWordAction(): void {
    if (!this.isActive) {
      this.advanceToNextWord();
    }
  }

  previousWordAction(): void {
    if (!this.isActive) {
      this.goToPreviousWord();
    }
  }

  // Utility methods
  formatTime(milliseconds: number): string {
    const totalSeconds = Math.floor(milliseconds / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  }

  // Style methods
  getContainerStyles(): string {
    return `
      background-color: ${this.settings.backgroundColor};
      color: ${this.settings.textColor};
      font-family: ${this.settings.fontFamily};
    `;
  }

  getWordDisplayStyles(): string {
    return `
      font-size: ${this.settings.fontSize * 1.5}px;
      line-height: ${this.settings.lineHeight};
    `;
  }

  getCurrentWordStyles(): string {
    return `
      color: ${this.settings.highlightColor};
      font-weight: 600;
    `;
  }

  getFocusLetterStyle(): string {
    return `
      color: ${this.settings.highlightColor};
      font-weight: 800;
      text-decoration: underline;
    `;
  }

  // Event handlers with error handling
  onSpeedChange(event: any): void {
    try {
      if (!event?.target?.value) return;
      
      const newSpeed = parseInt(event.target.value);
      if (isNaN(newSpeed)) {
        console.warn('RSVP Reader: Invalid speed value');
        return;
      }
      
      if (newSpeed >= 100 && newSpeed <= 1000) {
        this.currentSpeed = newSpeed;
        this.updateWordDuration();
      } else {
        console.warn('RSVP Reader: Speed value out of range (100-1000)');
      }
    } catch (error) {
      console.error('RSVP Reader: Error changing speed', error);
    }
  }

  onSpeedNumberChange(event: any): void {
    try {
      if (!event?.target?.value) return;
      
      const newSpeed = parseInt(event.target.value);
      if (isNaN(newSpeed)) {
        console.warn('RSVP Reader: Invalid speed number value');
        return;
      }
      
      if (newSpeed >= 100 && newSpeed <= 1000) {
        this.currentSpeed = newSpeed;
        this.updateWordDuration();
      } else {
        console.warn('RSVP Reader: Speed number value out of range (100-1000)');
      }
    } catch (error) {
      console.error('RSVP Reader: Error changing speed number', error);
    }
  }

  toggleFocusPoint(event: any): void {
    try {
      if (!this.settings) return;
      this.settings.rsvpFocusPoint = event?.target?.checked || false;
    } catch (error) {
      console.error('RSVP Reader: Error toggling focus point', error);
    }
  }

  toggleShowContext(event: any): void {
    try {
      if (!this.settings) return;
      this.settings.showContext = event?.target?.checked || false;
    } catch (error) {
      console.error('RSVP Reader: Error toggling show context', error);
    }
  }

  toggleSounds(event: any): void {
    try {
      if (!this.settings) return;
      this.settings.enableSounds = event?.target?.checked || false;
    } catch (error) {
      console.error('RSVP Reader: Error toggling sounds', error);
    }
  }

  onFontSizeChange(event: any): void {
    try {
      if (!event?.target?.value || !this.settings) return;
      
      const newSize = parseInt(event.target.value);
      if (isNaN(newSize)) {
        console.warn('RSVP Reader: Invalid font size value');
        return;
      }
      
      if (newSize >= 12 && newSize <= 48) {
        this.settings.fontSize = newSize;
      } else {
        console.warn('RSVP Reader: Font size out of range (12-48)');
      }
    } catch (error) {
      console.error('RSVP Reader: Error changing font size', error);
    }
  }

  onFocusDistanceChange(event: any): void {
    try {
      if (!event?.target?.value) return;
      
      const newDistance = parseInt(event.target.value);
      if (isNaN(newDistance)) {
        console.warn('RSVP Reader: Invalid focus distance value');
        return;
      }
      
      if (newDistance >= 1 && newDistance <= 10) {
        this.focusDistance = newDistance;
      } else {
        console.warn('RSVP Reader: Focus distance out of range (1-10)');
      }
    } catch (error) {
      console.error('RSVP Reader: Error changing focus distance', error);
    }
  }

  // Additional style methods for template
  getDisplayStyles(): string {
    return `
      min-height: 400px;
      display: flex;
      flex-direction: column;
      justify-content: center;
      align-items: center;
      position: relative;
    `;
  }

  getFocusPointStyles(): string {
    return `
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      pointer-events: none;
      z-index: 10;
    `;
  }

  getContextStyles(): string {
    return `
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      width: 100%;
      display: flex;
      justify-content: space-between;
      align-items: center;
      opacity: 0.4;
      pointer-events: none;
    `;
  }

  getPrevWordStyles(): string {
    return `
      font-size: ${this.settings.fontSize}px;
      color: ${this.settings.textColor};
      opacity: 0.5;
    `;
  }

  getNextWordStyles(): string {
    return `
      font-size: ${this.settings.fontSize}px;
      color: ${this.settings.textColor};
      opacity: 0.5;
    `;
  }

  getProgressBarStyles(): string {
    return `
      background-color: ${this.settings.highlightColor};
      transition: width 0.1s ease;
    `;
  }

  getFocusIndicatorStyles(): string {
    return `
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      width: 4px;
      height: 4px;
      background-color: ${this.settings.highlightColor};
      border-radius: 50%;
      pointer-events: none;
    `;
  }

  getRhythmIndicatorStyles(): string {
    return `
      position: fixed;
      bottom: 20px;
      right: 20px;
      width: 60px;
      height: 60px;
      border-radius: 50%;
      background-color: ${this.settings.highlightColor};
      opacity: 0.7;
      display: flex;
      align-items: center;
      justify-content: center;
    `;
  }

  getRhythmPulseStyles(): string {
    return `
      width: 100%;
      height: 100%;
      border-radius: 50%;
      background-color: inherit;
      animation: pulse 0.6s ease-in-out infinite alternate;
    `;
  }

  getChunkContainerStyles(): string {
    return `
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100px;
    `;
  }

  getChunkWordsStyles(): string {
    return `
      display: flex;
      gap: 8px;
      align-items: center;
    `;
  }

  getChunkWordStyles(index: number): string {
    const isFocus = index === Math.floor(this.focusDistance / 2);
    return `
      font-size: ${this.settings.fontSize}px;
      font-weight: ${isFocus ? '600' : '400'};
      color: ${isFocus ? this.settings.highlightColor : this.settings.textColor};
    `;
  }

}