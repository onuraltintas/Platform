import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TextContent, ReadingSettings } from '../../../../shared/models/reading.models';

@Component({
  selector: 'app-guided-reader',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './guided-reader.component.html',
  styleUrls: ['./guided-reader.component.scss']
})
export class GuidedReaderComponent implements OnInit, OnDestroy, OnChanges {
  @ViewChild('textDisplay', { static: false }) textDisplay!: ElementRef;

  @Input() textContent!: TextContent;
  @Input() settings!: ReadingSettings;
  @Input() isActive: boolean = false;
  @Input() speed: number = 250;

  @Output() wordsRead = new EventEmitter<number>();
  @Output() completed = new EventEmitter<void>();

  // Text structure
  paragraphs: any[] = [];
  totalWords: number = 0;
  
  // Tracking
  currentWordIndex: number = 0;
  currentParagraphIndex: number = 0;
  currentWordInParagraph: number = 0;
  
  // Highlighter
  highlighterPosition: number = 0;
  highlighterHeight: number = 2;
  highlighterProgress: number = 0;
  showHighlighter: boolean = true;
  highlighterOpacity: number = 40;
  
  // Focus window
  focusWindowHeight: number = 150;
  focusWindowPosition: number = 0;
  
  // Speed and timing
  currentSpeed: number = 250;
  private interval: any = null;
  private startTime: number = 0;
  elapsedTime: number = 0;
  estimatedTimeLeft: number = 0;
  averageSpeed: number = 0;
  currentWPM: number = 0;
  
  // Progress
  progressPercentage: number = 0;
  readingProgress: number = 0;
  
  // State
  isCompleted: boolean = false;
  showStats: boolean = true;
  finalSpeed: number = 0;
  consistency: number = 0;
  followAccuracy: number = 0;
  regressionCount: number = 0;
  
  // Template properties
  showSpeedIndicator: boolean = true;
  efficiency: number = 0;
  finalWPM: number = 0;
  maxSpeed: number = 500;
  highlighterSpeed: number = 250;
  focusWindowSize: number = 150;
  smoothScrolling: boolean = true;
  highlighterColor: string = '#3b82f6';
  showEyeStrainWarning: boolean = false;
  continuousReadingTime: number = 0;
  
  // Math reference for template
  Math = Math;

  ngOnInit(): void {
    this.initializeReader();
  }

  ngOnDestroy(): void {
    this.stopGuiding();
  }

  ngOnChanges(): void {
    if (this.isActive && !this.interval) {
      this.startGuiding();
    } else if (!this.isActive && this.interval) {
      this.pauseGuiding();
    }

    if (this.speed !== this.currentSpeed) {
      this.currentSpeed = this.speed;
      this.restartInterval();
    }
  }

  private initializeReader(): void {
    if (!this.textContent) return;

    this.processTextContent();
    this.currentSpeed = this.settings.highlighterSpeed || 250;
    this.highlighterHeight = this.settings.highlighterHeight || 2;
    this.setupInitialPosition();
    this.calculateEstimatedTime();
  }

  private processTextContent(): void {
    if (!this.textContent.paragraphs || this.textContent.paragraphs.length === 0) {
      // Fallback: create paragraphs from content
      const paragraphTexts = this.textContent.content.split(/\n\s*\n/);
      this.paragraphs = paragraphTexts.map((text, index) => ({
        id: index,
        text: text.trim(),
        words: this.extractWordsFromText(text.trim()),
        startIndex: 0,
        endIndex: text.length
      }));
    } else {
      // Use existing paragraph structure
      this.paragraphs = this.textContent.paragraphs.map(p => ({
        ...p,
        words: this.extractWordsFromText(p.sentences.map(s => s.text).join(' '))
      }));
    }

    this.totalWords = this.paragraphs.reduce((sum, p) => sum + p.words.length, 0);
  }

  private extractWordsFromText(text: string): any[] {
    return text.split(/\s+/)
      .filter(word => word.length > 0)
      .map((word, index) => ({
        text: word,
        index: index,
        isRead: false,
        isCurrent: false
      }));
  }

  private setupInitialPosition(): void {
    this.currentWordIndex = 0; // her seferinde baştan başla
    this.updateWordPosition();
    this.updateHighlighterPosition();
  }

  private startGuiding(): void {
    if (this.interval) return;
    
    this.startTime = Date.now() - this.elapsedTime;
    this.startInterval();
  }

  private pauseGuiding(): void {
    this.stopInterval();
  }

  private stopGuiding(): void {
    this.stopInterval();
    this.elapsedTime = 0;
    this.currentWordIndex = 0;
    this.setupInitialPosition();
  }

  private startInterval(): void {
    const wordsPerSecond = this.currentSpeed / 60;
    const intervalMs = 1000 / wordsPerSecond;
    
    this.interval = setInterval(() => {
      this.advanceHighlighter();
    }, intervalMs);
  }

  private stopInterval(): void {
    if (this.interval) {
      clearInterval(this.interval);
      this.interval = null;
    }
  }

  private restartInterval(): void {
    if (this.interval) {
      this.stopInterval();
      this.startInterval();
    }
  }

  private advanceHighlighter(): void {
    if (this.currentWordIndex >= this.totalWords - 1) {
      this.completeReading();
      return;
    }

    this.currentWordIndex++;
    this.updateWordPosition();
    this.updateHighlighterPosition();
    this.updateProgress();
    this.updateStats();
    this.autoScrollToPosition();
    
    this.wordsRead.emit(this.currentWordIndex + 1);
  }

  private updateWordPosition(): void {
    let wordCount = 0;
    
    for (let pIndex = 0; pIndex < this.paragraphs.length; pIndex++) {
      const paragraph = this.paragraphs[pIndex];
      
      if (wordCount + paragraph.words.length > this.currentWordIndex) {
        this.currentParagraphIndex = pIndex;
        this.currentWordInParagraph = this.currentWordIndex - wordCount;
        break;
      }
      
      wordCount += paragraph.words.length;
    }
  }

  private updateHighlighterPosition(): void {
    const currentWordElement = document.getElementById(
      `word-${this.currentParagraphIndex}-${this.currentWordInParagraph}`
    );
    
    if (currentWordElement && this.textDisplay) {
      const textDisplayElement = this.textDisplay.nativeElement;
      const wordRect = currentWordElement.getBoundingClientRect();
      const displayRect = textDisplayElement.getBoundingClientRect();
      
      this.highlighterPosition = wordRect.top - displayRect.top + textDisplayElement.scrollTop;
      this.highlighterProgress = (this.currentWordIndex / this.totalWords) * 100;
      this.readingProgress = this.highlighterProgress;
    }
  }

  private autoScrollToPosition(): void {
    if (!this.textDisplay) return;
    
    const element = this.textDisplay.nativeElement;
    const targetScrollTop = this.highlighterPosition - (element.clientHeight / 2);
    
    element.scrollTo({
      top: Math.max(0, targetScrollTop),
      behavior: 'smooth'
    });
  }

  private updateProgress(): void {
    this.progressPercentage = (this.currentWordIndex / this.totalWords) * 100;
  }

  private updateStats(): void {
    this.elapsedTime = Date.now() - this.startTime;
    
    if (this.elapsedTime > 0) {
      const minutes = this.elapsedTime / (1000 * 60);
      this.averageSpeed = Math.round(this.currentWordIndex / minutes);
      this.currentWPM = this.averageSpeed;
    }
    
    // Calculate estimated time left
    const wordsLeft = this.totalWords - this.currentWordIndex;
    const wordsPerMs = this.currentSpeed / (60 * 1000);
    this.estimatedTimeLeft = wordsLeft / wordsPerMs;

    // Derived metrics
    const diff = Math.abs(this.averageSpeed - this.currentSpeed);
    const denom = Math.max(1, this.currentSpeed);
    const acc = Math.max(0, 100 - (diff / denom) * 100);
    this.followAccuracy = Math.round(acc);
  }

  private calculateEstimatedTime(): void {
    const wordsPerMs = this.currentSpeed / (60 * 1000);
    this.estimatedTimeLeft = this.totalWords / wordsPerMs;
  }

  private completeReading(): void {
    this.stopInterval();
    this.isCompleted = true;
    this.finalSpeed = this.averageSpeed;
    this.consistency = this.calculateConsistency();
    try { localStorage.removeItem(`reading_resume_${this.textContent.id}`); } catch {}
    this.completed.emit();
  }

  private calculateConsistency(): number {
    // Simple consistency calculation based on how close average speed is to target speed
    if (this.averageSpeed === 0 || this.currentSpeed === 0) return 0;
    
    const speedDifference = Math.abs(this.averageSpeed - this.currentSpeed);
    const maxDifference = this.currentSpeed * 0.5; // 50% tolerance
    const consistency = Math.max(0, 100 - (speedDifference / maxDifference) * 100);
    
    return Math.min(100, consistency);
  }

  // Public control methods
  increaseSpeed(): void {
    this.currentSpeed = Math.min(800, this.currentSpeed + 25);
    this.restartInterval();
  }

  decreaseSpeed(): void {
    this.currentSpeed = Math.max(100, this.currentSpeed - 25);
    this.restartInterval();
  }

  onHighlighterHeightChange(event: any): void {
    this.highlighterHeight = parseInt(event.target.value);
  }

  onFocusWindowChange(event: any): void {
    this.focusWindowHeight = parseInt(event.target.value);
  }

  // Navigation methods
  goToStart(): void {
    this.currentWordIndex = 0;
    this.updateWordPosition();
    this.updateHighlighterPosition();
    this.updateProgress();
    this.autoScrollToPosition();
  }

  goToEnd(): void {
    this.currentWordIndex = this.totalWords - 1;
    this.updateWordPosition();
    this.updateHighlighterPosition();
    this.updateProgress();
    this.autoScrollToPosition();
  }

  previousSection(): void {
    const sectionSize = Math.floor(this.totalWords / 10); // 10% sections
    const newIndex = Math.max(0, this.currentWordIndex - sectionSize);
    this.jumpToWord(newIndex);
  }

  nextSection(): void {
    const sectionSize = Math.floor(this.totalWords / 10); // 10% sections
    const newIndex = Math.min(this.totalWords - 1, this.currentWordIndex + sectionSize);
    this.jumpToWord(newIndex);
  }

  private jumpToWord(wordIndex: number): void {
    this.currentWordIndex = wordIndex;
    this.updateWordPosition();
    this.updateHighlighterPosition();
    this.updateProgress();
    this.autoScrollToPosition();
  }

  // Helper methods
  isCurrentWord(pIndex: number, wIndex: number): boolean {
    return pIndex === this.currentParagraphIndex && wIndex === this.currentWordInParagraph;
  }

  isWordRead(pIndex: number, wIndex: number): boolean {
    const globalIndex = this.getGlobalWordIndex(pIndex, wIndex);
    return globalIndex < this.currentWordIndex;
  }

  isUpcomingWord(pIndex: number, wIndex: number): boolean {
    const globalIndex = this.getGlobalWordIndex(pIndex, wIndex);
    const lookAhead = 5; // Show next 5 words as upcoming
    return globalIndex > this.currentWordIndex && globalIndex <= this.currentWordIndex + lookAhead;
  }

  private getGlobalWordIndex(pIndex: number, wIndex: number): number {
    let globalIndex = 0;
    
    for (let i = 0; i < pIndex; i++) {
      globalIndex += this.paragraphs[i].words.length;
    }
    
    return globalIndex + wIndex;
  }

  // Removed duplicate formatTime (kept single implementation at bottom)

  // Style methods
  getContainerStyles(): string {
    return `
      background-color: ${this.settings.backgroundColor};
      color: ${this.settings.textColor};
      font-family: ${this.settings.fontFamily};
    `;
  }

  getTextDisplayStyles(): string {
    return `
      max-height: 60vh;
      overflow-y: auto;
    `;
  }

  getTextStyles(): string {
    return `
      font-size: ${this.settings.fontSize}px;
      line-height: ${this.settings.lineHeight};
    `;
  }

  getWordStyle(pIndex: number, wIndex: number): string {
    if (this.isCurrentWord(pIndex, wIndex)) {
      return `
        background-color: ${this.settings.highlightColor};
        color: white;
        font-weight: 600;
      `;
    } else if (this.isWordRead(pIndex, wIndex)) {
      return `
        opacity: 0.6;
        color: #6b7280;
      `;
    } else if (this.isUpcomingWord(pIndex, wIndex)) {
      return `
        background-color: rgba(59, 130, 246, 0.1);
        color: #2563eb;
      `;
    }
    return '';
  }

  getHighlighterStyles(): string {
    const lineHeight = this.settings.fontSize * this.settings.lineHeight;
    const height = lineHeight * this.highlighterHeight;
    
    return `
      position: absolute;
      left: 0;
      right: 0;
      top: ${this.highlighterPosition}px;
      height: ${height}px;
      background: linear-gradient(90deg, 
        transparent 0%, 
        ${this.settings.highlightColor}${this.toHexOpacity(this.highlighterOpacity/2)} 10%, 
        ${this.settings.highlightColor}${this.toHexOpacity(this.highlighterOpacity)} 50%, 
        ${this.settings.highlightColor}${this.toHexOpacity(this.highlighterOpacity/2)} 90%, 
        transparent 100%
      );
      border: 2px solid ${this.settings.highlightColor};
      border-radius: 8px;
      transition: top 0.3s ease;
      pointer-events: none;
      z-index: 1;
    `;
  }

  private toHexOpacity(percent: number): string {
    const p = Math.max(0, Math.min(100, Math.round(percent)));
    const v = Math.round((p / 100) * 255);
    return v.toString(16).padStart(2, '0');
  }

  getFocusWindowStyles(): string {
    return `
      position: absolute;
      left: 0;
      right: 0;
      top: 0;
      bottom: 0;
      pointer-events: none;
      z-index: 2;
    `;
  }

  // Eksik method'lar
  getReadingAreaStyles(): string {
    return `
      background-color: ${this.settings.backgroundColor};
      color: ${this.settings.textColor};
      font-family: ${this.settings.fontFamily};
      font-size: ${this.settings.fontSize}px;
      line-height: ${this.settings.lineHeight};
      padding: 20px;
      position: relative;
    `;
  }

  isReadWord(paragraphIndex: number, wordIndex: number): boolean {
    // Calculate if this word has been read
    const currentWordPosition = this.currentParagraphIndex * 10 + this.currentWordInParagraph;
    const wordPosition = paragraphIndex * 10 + wordIndex;
    return wordPosition <= currentWordPosition;
  }

  isInFocusWindow(paragraphIndex: number, wordIndex: number): boolean {
    // Check if word is in focus window
    return paragraphIndex === this.currentParagraphIndex && 
           Math.abs(wordIndex - this.currentWordInParagraph) <= 2;
  }

  getGuideLineStyles(position: string): string {
    const color = this.settings.highlightColor || '#3b82f6';
    if (position === 'top') {
      return `
        position: absolute;
        top: ${this.highlighterPosition - this.highlighterHeight}px;
        left: 0;
        right: 0;
        height: 1px;
        background: ${color};
        opacity: 0.6;
      `;
    } else {
      return `
        position: absolute;
        top: ${this.highlighterPosition + this.highlighterHeight}px;
        left: 0;
        right: 0;
        height: 1px;
        background: ${color};
        opacity: 0.6;
      `;
    }
  }

  getSpeedBarStyles(): string {
    const percentage = (this.highlighterSpeed / this.maxSpeed) * 100;
    return `
      width: ${percentage}%;
      background: linear-gradient(90deg, #10b981 0%, #059669 100%);
      height: 100%;
      border-radius: inherit;
      transition: width 0.3s ease;
    `;
  }

  getProgressBarStyles(): string {
    return `
      background: ${this.settings.highlightColor};
      transition: width 0.2s ease;
      height: 100%;
    `;
  }

  getProgressMarkerStyles(): string {
    return `
      position: absolute;
      top: -4px;
      width: 8px;
      height: 8px;
      background: ${this.settings.highlightColor};
      border-radius: 50%;
      transform: translateX(-50%);
    `;
  }

  // Event handlers
  onSpeedChange(event: any): void {
    const newSpeed = parseInt(event.target.value);
    if (!isNaN(newSpeed) && newSpeed >= 50 && newSpeed <= this.maxSpeed) {
      this.highlighterSpeed = newSpeed;
      this.currentSpeed = newSpeed;
    }
  }

  onSpeedNumberChange(event: any): void {
    const newSpeed = parseInt(event.target.value);
    if (!isNaN(newSpeed) && newSpeed >= 50 && newSpeed <= this.maxSpeed) {
      this.highlighterSpeed = newSpeed;
      this.currentSpeed = newSpeed;
    }
  }

  onHeightChange(event: any): void {
    const newHeight = parseInt(event.target.value);
    if (!isNaN(newHeight) && newHeight >= 1 && newHeight <= 10) {
      this.highlighterHeight = newHeight;
    }
  }

  onWindowSizeChange(event: any): void {
    const newSize = parseInt(event.target.value);
    if (!isNaN(newSize) && newSize >= 50 && newSize <= 300) {
      this.focusWindowSize = newSize;
      this.focusWindowHeight = newSize;
    }
  }

  toggleReadingGuide(event: any): void {
    if (this.settings) {
      this.settings.showReadingGuide = event?.target?.checked || false;
    }
  }

  toggleFocusWindow(event: any): void {
    if (this.settings) {
      this.settings.showFocusWindow = event?.target?.checked || false;
    }
  }

  toggleGuideLines(event: any): void {
    if (this.settings) {
      this.settings.showGuideLines = event?.target?.checked || false;
    }
  }

  toggleSmoothScrolling(event: any): void {
    this.smoothScrolling = event?.target?.checked || false;
  }

  onColorChange(event: any): void {
    const newColor = event.target.value;
    if (newColor && this.settings) {
      this.highlighterColor = newColor;
      this.settings.highlightColor = newColor;
    }
  }

  onOpacityChange(event: any): void {
    const v = parseInt(event.target.value);
    if (!isNaN(v) && v >= 10 && v <= 80) this.highlighterOpacity = v;
  }

  getEyeStrainWarningStyles(): string {
    return `
      position: fixed;
      top: 20px;
      right: 20px;
      background: #fef3c7;
      border: 1px solid #f59e0b;
      color: #92400e;
      padding: 16px;
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      z-index: 1000;
      max-width: 300px;
    `;
  }

  dismissEyeStrainWarning(): void {
    this.showEyeStrainWarning = false;
  }

  formatTime(milliseconds: number): string {
    const totalSeconds = Math.floor(milliseconds / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  }

  // Alias for template compatibility
  get speedConsistency(): number {
    return this.consistency;
  }
}