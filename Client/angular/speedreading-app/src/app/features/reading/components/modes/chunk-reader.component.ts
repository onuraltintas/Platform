import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TextContent, ReadingSettings, TextChunk } from '../../../../shared/models/reading.models';

@Component({
  selector: 'app-chunk-reader',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './chunk-reader.component.html',
  styleUrls: ['./chunk-reader.component.scss']
})
export class ChunkReaderComponent implements OnInit, OnDestroy, OnChanges {
  @Input() textContent!: TextContent;
  @Input() settings!: ReadingSettings;
  @Input() isActive: boolean = false;
  @Input() chunkSize: number = 3;

  @Output() wordsRead = new EventEmitter<number>();
  @Output() completed = new EventEmitter<void>();

  // Chunk tracking
  chunks: TextChunk[] = [];
  currentChunkIndex: number = 0;
  totalChunks: number = 0;
  totalWords: number = 0;
  
  // Display chunks
  currentChunk: TextChunk | null = null;
  previousDisplayChunk: TextChunk | null = null;
  nextDisplayChunk: TextChunk | null = null;
  
  // Focus system
  focusWordIndex: number = 0;
  
  // Timing
  currentSpeed: number = 250;
  chunkDuration: number = 800; // milliseconds per chunk
  private interval: any = null;
  private wordInterval: any = null;
  private startTime: number = 0;
  elapsedTime: number = 0;
  estimatedTimeLeft: number = 0;
  
  // Progress
  progressPercentage: number = 0;
  wordsReadCount: number = 0;
  
  // State
  isCompleted: boolean = false;
  showStats: boolean = true;
  finalSpeed: number = 0;
  efficiency: number = 0;
  
  // Template properties (eksik olan property'ler)
  currentWordIndex: number = 0;
  readingProgress: number = 0;
  currentChunkSize: number = 3;
  displayDuration: number = 800;
  pauseDuration: number = 200;
  wordSpacing: number = 4;
  rhythmMode: boolean = false;
  currentWPM: number = 0;
  finalWPM: number = 0;
  averageChunkSize: number = 3;
  consistency: number = 85;
  showRhythmPulse: boolean = false;
  
  // Chunk references for template
  previousChunk: TextChunk | null = null;
  nextChunk: TextChunk | null = null;
  
  // Math reference for template
  Math = Math;

  ngOnInit(): void {
    this.initializeReader();
  }

  ngOnDestroy(): void {
    this.stopReading();
  }

  ngOnChanges(): void {
    if (this.isActive && !this.interval) {
      this.startReading();
    } else if (!this.isActive && this.interval) {
      this.pauseReading();
    }

    if (this.chunkSize !== this.chunks[0]?.words.length && this.textContent) {
      this.regenerateChunks();
    }
  }

  private initializeReader(): void {
    if (!this.textContent) return;

    this.regenerateChunks();
    this.currentSpeed = this.settings.wordsPerMinute || 250;
    this.updateChunkDuration();
    this.updateDisplay();
    this.calculateEstimatedTime();
  }

  private regenerateChunks(): void {
    if (!this.textContent) return;

    // Extract words from text content
    const words = this.textContent.content
      .split(/\s+/)
      .filter(word => word.length > 0)
      .map(word => word.trim());
    
    this.totalWords = words.length;
    this.chunks = [];
    let chunkId = 0;

    // Create chunks with current chunk size
    for (let i = 0; i < words.length; i += this.chunkSize) {
      const chunkWords = words.slice(i, i + this.chunkSize);
      this.chunks.push({
        id: chunkId++,
        words: chunkWords,
        startIndex: i,
        endIndex: Math.min(i + this.chunkSize - 1, words.length - 1),
        duration: this.calculateDynamicChunkDuration(chunkWords),
        difficulty: this.calculateChunkDifficulty(chunkWords)
      });
    }
    
    this.totalChunks = this.chunks.length;
    this.currentChunkIndex = 0; // her seferinde baştan başla
    this.updateDisplay();
  }

  private startReading(): void {
    if (this.interval) return;
    
    this.startTime = Date.now() - this.elapsedTime;
    this.startChunkDisplay();
  }

  private startChunkDisplay(): void {
    if (this.currentChunkIndex >= this.totalChunks) {
      this.completeReading();
      return;
    }

    this.updateDisplay();
    this.focusWordIndex = 0;
    
    // Animate focus through words in chunk
    this.animateWordsInChunk(() => {
      // Move to next chunk after current chunk completes
      setTimeout(() => {
        this.advanceToNextChunk();
      }, this.settings.chunkPauseDuration || 200);
    });
  }

  private animateWordsInChunk(onComplete: () => void): void {
    const chunk = this.currentChunk;
    if (!chunk || chunk.words.length === 0) {
      onComplete();
      return;
    }

    const wordDuration = this.chunkDuration / chunk.words.length;
    let wordIndex = 0;

    this.wordInterval = setInterval(() => {
      this.focusWordIndex = wordIndex;
      wordIndex++;

      if (wordIndex >= chunk.words.length) {
        clearInterval(this.wordInterval);
        this.wordInterval = null;
        onComplete();
      }
    }, wordDuration);
  }

  private pauseReading(): void {
    if (this.interval) {
      clearInterval(this.interval);
      this.interval = null;
    }
    if (this.wordInterval) {
      clearInterval(this.wordInterval);
      this.wordInterval = null;
    }
  }

  private stopReading(): void {
    this.pauseReading();
    this.elapsedTime = 0;
    this.currentChunkIndex = 0;
    this.focusWordIndex = 0;
    this.updateDisplay();
  }

  private advanceToNextChunk(): void {
    if (this.currentChunkIndex >= this.totalChunks - 1) {
      this.completeReading();
      return;
    }

    this.currentChunkIndex++;
    this.updateProgress();
    this.updateStats();
    this.updateWordsRead();
    
    // Continue with next chunk
    this.startChunkDisplay();
  }

  private goToPreviousChunk(): void {
    if (this.currentChunkIndex > 0) {
      this.currentChunkIndex--;
      this.updateDisplay();
      this.updateProgress();
      this.updateWordsRead();
    }
  }

  private updateDisplay(): void {
    // Set current, previous, and next chunks
    this.currentChunk = this.chunks[this.currentChunkIndex] || null;
    this.previousDisplayChunk = this.currentChunkIndex > 0 ? this.chunks[this.currentChunkIndex - 1] : null;
    this.nextDisplayChunk = this.currentChunkIndex < this.totalChunks - 1 ? this.chunks[this.currentChunkIndex + 1] : null;
    
    this.focusWordIndex = 0;
    
    // Template properties'i güncelle
    this.updateDisplayProperties();
  }

  private updateProgress(): void {
    this.progressPercentage = Math.round((this.currentChunkIndex / this.totalChunks) * 100);
  }

  private updateWordsRead(): void {
    this.wordsReadCount = this.currentChunkIndex * this.chunkSize;
    if (this.currentChunk) {
      this.wordsReadCount += this.focusWordIndex + 1;
    }
    this.wordsRead.emit(this.wordsReadCount);
  }

  private updateStats(): void {
    this.elapsedTime = Date.now() - this.startTime;
    
    // Calculate estimated time left
    const chunksLeft = this.totalChunks - this.currentChunkIndex;
    this.estimatedTimeLeft = chunksLeft * this.chunkDuration;
  }

  private updateChunkDuration(): void {
    // Base duration adjusted by speed
    const baseDuration = 800; // 800ms base per chunk
    const speedFactor = 250 / this.currentSpeed; // Adjust based on speed
    this.chunkDuration = Math.round(baseDuration * speedFactor);
  }

  private calculateDynamicChunkDuration(words: string[]): number {
    const baseTime = this.chunkDuration;
    const avgWordLength = words.reduce((sum, word) => sum + word.length, 0) / words.length;
    const complexityMultiplier = 1 + (avgWordLength - 4) * 0.1;
    
    return Math.max(400, Math.min(2000, baseTime * complexityMultiplier));
  }

  private calculateChunkDifficulty(words: string[]): number {
    const avgLength = words.reduce((sum, word) => sum + word.length, 0) / words.length;
    
    if (avgLength < 4) return 1;
    if (avgLength < 6) return 2;
    if (avgLength < 8) return 3;
    return 4;
  }

  private calculateEstimatedTime(): void {
    this.estimatedTimeLeft = this.totalChunks * this.chunkDuration;
  }

  private completeReading(): void {
    this.pauseReading();
    this.isCompleted = true;
    this.finalSpeed = this.calculateFinalSpeed();
    this.efficiency = this.calculateEfficiency();
    try { localStorage.removeItem(`reading_resume_${this.textContent.id}`); } catch {}
    this.completed.emit();
  }

  private calculateFinalSpeed(): number {
    if (this.elapsedTime <= 0) return 0;
    const minutes = this.elapsedTime / (1000 * 60);
    return Math.round(this.totalWords / minutes);
  }

  private calculateEfficiency(): number {
    const speedEfficiency = Math.min(100, (this.finalSpeed / 300) * 100);
    const completionBonus = this.currentChunkIndex === this.totalChunks ? 20 : 0;
    return Math.min(100, speedEfficiency + completionBonus);
  }

  // Public methods for controls
  increaseSpeed(): void {
    this.currentSpeed = Math.min(800, this.currentSpeed + 25);
    this.updateChunkDuration();
  }

  decreaseSpeed(): void {
    this.currentSpeed = Math.max(100, this.currentSpeed - 25);
    this.updateChunkDuration();
  }

  increaseChunkSize(): void {
    this.chunkSize = Math.min(5, this.chunkSize + 1);
    if (!this.isActive) {
      this.regenerateChunks();
    }
  }

  decreaseChunkSize(): void {
    this.chunkSize = Math.max(1, this.chunkSize - 1);
    if (!this.isActive) {
      this.regenerateChunks();
    }
  }

  nextChunkAction(): void {
    if (!this.isActive) {
      this.advanceToNextChunk();
    }
  }

  previousChunkAction(): void {
    if (!this.isActive) {
      this.goToPreviousChunk();
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

  getChunkDisplayStyles(): string {
    return `
      font-size: ${this.settings.fontSize * 1.5}px;
      line-height: ${this.settings.lineHeight};
    `;
  }

  getCurrentChunkStyles(): string {
    return `
      color: ${this.settings.highlightColor};
      font-weight: 600;
    `;
  }

  getWordStyle(index: number): string {
    const isFocus = index === this.focusWordIndex;
    return `
      color: ${isFocus ? this.settings.highlightColor : 'inherit'};
      font-weight: ${isFocus ? '800' : '600'};
      transform: ${isFocus ? 'scale(1.1)' : 'scale(1)'};
      transition: all 0.2s ease;
    `;
  }

  // Eksik style method'ları
  getDisplayStyles(): string {
    return `
      min-height: 400px;
      display: flex;
      flex-direction: column;
      justify-content: center;
      align-items: center;
      position: relative;
      padding: 40px 20px;
    `;
  }

  getChunkContainerStyles(): string {
    return `
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 120px;
      position: relative;
    `;
  }

  getChunkWordsStyles(): string {
    return `
      display: flex;
      gap: ${this.wordSpacing}px;
      align-items: center;
      justify-content: center;
      flex-wrap: wrap;
    `;
  }

  getChunkWordStyles(index: number): string {
    const isFocus = index === this.focusWordIndex;
    return `
      font-size: ${this.settings.fontSize}px;
      font-weight: ${isFocus ? '700' : '500'};
      color: ${isFocus ? this.settings.highlightColor : this.settings.textColor};
      transition: all 0.3s ease;
      transform: ${isFocus ? 'scale(1.2)' : 'scale(1)'};
      padding: 4px 8px;
      border-radius: 4px;
      background: ${isFocus ? 'rgba(59, 130, 246, 0.1)' : 'transparent'};
    `;
  }

  getFocusIndicatorStyles(): string {
    return `
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      width: 6px;
      height: 6px;
      background-color: ${this.settings.highlightColor};
      border-radius: 50%;
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

  getPrevChunkStyles(): string {
    return `
      font-size: ${this.settings.fontSize * 0.8}px;
      color: ${this.settings.textColor};
      opacity: 0.6;
      text-align: right;
    `;
  }

  getNextChunkStyles(): string {
    return `
      font-size: ${this.settings.fontSize * 0.8}px;
      color: ${this.settings.textColor};
      opacity: 0.6;
      text-align: left;
    `;
  }

  getProgressBarStyles(): string {
    return `
      background-color: ${this.settings.highlightColor};
      transition: width 0.3s ease;
      height: 100%;
      border-radius: inherit;
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
      opacity: 0.8;
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 100;
    `;
  }

  getRhythmPulseStyles(): string {
    return `
      width: 100%;
      height: 100%;
      border-radius: 50%;
      background-color: inherit;
      animation: ${this.showRhythmPulse ? 'pulse 0.6s ease-in-out infinite alternate' : 'none'};
    `;
  }

  // Eksik event handler'lar
  changeChunkSize(delta: number): void {
    const newSize = Math.max(1, Math.min(7, this.currentChunkSize + delta));
    if (newSize !== this.currentChunkSize) {
      this.currentChunkSize = newSize;
      this.chunkSize = newSize;
      if (!this.isActive) {
        this.regenerateChunks();
      }
    }
  }

  onDisplayDurationChange(event: any): void {
    const newDuration = parseInt(event.target.value);
    if (!isNaN(newDuration) && newDuration >= 200 && newDuration <= 3000) {
      this.displayDuration = newDuration;
      this.chunkDuration = newDuration;
    }
  }

  onPauseDurationChange(event: any): void {
    const newDuration = parseInt(event.target.value);
    if (!isNaN(newDuration) && newDuration >= 50 && newDuration <= 1000) {
      this.pauseDuration = newDuration;
      // Note: pauseDuration is not in ReadingSettings interface, storing locally
    }
  }

  toggleFocusPoint(event: any): void {
    if (this.settings) {
      this.settings.rsvpFocusPoint = event?.target?.checked || false;
    }
  }

  toggleShowContext(event: any): void {
    if (this.settings) {
      this.settings.showContext = event?.target?.checked || false;
    }
  }

  toggleRhythmMode(event: any): void {
    this.rhythmMode = event?.target?.checked || false;
  }

  onFontSizeChange(event: any): void {
    const newSize = parseInt(event.target.value);
    if (!isNaN(newSize) && newSize >= 12 && newSize <= 48 && this.settings) {
      this.settings.fontSize = newSize;
    }
  }

  onWordSpacingChange(event: any): void {
    const newSpacing = parseInt(event.target.value);
    if (!isNaN(newSpacing) && newSpacing >= 0 && newSpacing <= 20) {
      this.wordSpacing = newSpacing;
    }
  }

  // Display ve progress güncellemeleri
  private updateDisplayProperties(): void {
    // Template properties'i güncelle
    this.currentWordIndex = this.currentChunkIndex * this.chunkSize + this.focusWordIndex;
    this.readingProgress = this.progressPercentage;
    this.currentChunkSize = this.chunkSize;
    this.currentWPM = this.currentSpeed;
    this.finalWPM = this.finalSpeed;
    this.averageChunkSize = this.chunkSize;
    
    // Chunk references
    this.previousChunk = this.previousDisplayChunk;
    this.nextChunk = this.nextDisplayChunk;
  }
}