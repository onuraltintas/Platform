import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges, ElementRef, ViewChild, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TextContent, ReadingSettings } from '../../../../shared/models/reading.models';

@Component({
  selector: 'app-classic-reader',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './classic-reader.component.html',
  styleUrls: ['./classic-reader.component.scss']
})
export class ClassicReaderComponent implements OnInit, OnDestroy, OnChanges {
  @ViewChild('textContainer', { static: false }) textContainer!: ElementRef;
  @ViewChild('readingArea', { static: false }) readingArea!: ElementRef;

  @Input() textContent!: TextContent;
  @Input() settings!: ReadingSettings;
  @Input() isActive: boolean = false;

  @Output() wordsRead = new EventEmitter<number>();
  @Output() completed = new EventEmitter<void>();
  @Output() scroll = new EventEmitter<Event>();

  // Text structure
  paragraphs: any[] = [];
  totalWords: number = 0;
  
  // Reading tracking
  currentWordIndex: number = 0;
  currentParagraphIndex: number = 0;
  currentWordInParagraph: number = 0;
  
  // Page system
  currentPage: number = 1;
  totalPages: number = 1;
  wordsPerPage: number = 200;
  
  // Progress tracking
  readingProgress: number = 0;
  scrollProgress: number = 0;
  
  // Speed reading
  speedMode: string = 'guided';
  targetWPM: number = 250;
  currentWPM: number = 0;
  
  // Timing
  private startTime: number = 0;
  elapsedTime: number = 0;
  private interval: any = null;
  private guidedInterval: any = null;
  
  // Statistics
  scrollEvents: number = 0;
  finalWPM: number = 0;
  
  // State
  isCompleted: boolean = false;
  showStats: boolean = true;
  showProgressIndicator: boolean = true;
  
  // Math reference for template
  Math = Math;

  ngOnInit(): void {
    this.initializeReader();
  }

  ngOnDestroy(): void {
    this.cleanup();
  }

  ngOnChanges(changes: SimpleChanges): void {
    // Play/Pause state changes
    if (this.isActive && !this.interval) {
      this.startReading();
    } else if (!this.isActive && this.interval) {
      this.pauseReading();
    }

    // React to external settings updates (e.g., speed slider in parent)
    if (changes['settings'] && this.settings) {
      const newWpm = this.settings.wordsPerMinute || 250;
      if (this.targetWPM !== newWpm) {
        this.targetWPM = newWpm;
        if (this.guidedInterval) {
          this.stopGuidedReading();
          if (this.speedMode === 'guided' || this.speedMode === 'auto') {
            this.startGuidedReading();
          }
        }
      }
    }
  }

  private initializeReader(): void {
    if (!this.textContent) return;

    this.processTextContent();
    this.calculatePages();
    this.targetWPM = this.settings.wordsPerMinute || 250;
    this.setupInitialPosition();
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

  private calculatePages(): void {
    this.totalPages = Math.ceil(this.totalWords / this.wordsPerPage);
  }

  private setupInitialPosition(): void {
    this.currentWordIndex = 0; // her seferinde baştan başla
    this.currentParagraphIndex = 0;
    this.currentWordInParagraph = 0;
    this.updateCurrentWordPosition();
  }

  private startReading(): void {
    this.startTime = Date.now() - this.elapsedTime;
    this.startStatsTracking();
    
    if (this.speedMode === 'guided' || this.speedMode === 'auto') {
      this.startGuidedReading();
    }
  }

  private pauseReading(): void {
    this.stopStatsTracking();
    this.stopGuidedReading();
  }

  private startStatsTracking(): void {
    this.interval = setInterval(() => {
      this.updateStats();
    }, 1000);
  }

  private stopStatsTracking(): void {
    if (this.interval) {
      clearInterval(this.interval);
      this.interval = null;
    }
  }

  private startGuidedReading(): void {
    if (this.guidedInterval) return;
    
    const wordsPerSecond = this.targetWPM / 60;
    const intervalMs = 1000 / wordsPerSecond;
    
    this.guidedInterval = setInterval(() => {
      this.advanceToNextWord();
    }, intervalMs);
  }

  private stopGuidedReading(): void {
    if (this.guidedInterval) {
      clearInterval(this.guidedInterval);
      this.guidedInterval = null;
    }
  }

  private advanceToNextWord(): void {
    if (this.currentWordIndex >= this.totalWords - 1) {
      this.completeReading();
      return;
    }

    this.currentWordIndex++;
    this.updateCurrentWordPosition();
    this.updateProgress();
    this.autoScrollToCurrentWord();
    
    this.wordsRead.emit(this.currentWordIndex + 1);
  }

  private updateCurrentWordPosition(): void {
    let wordCount = 0;
    
    for (let pIndex = 0; pIndex < this.paragraphs.length; pIndex++) {
      const paragraph = this.paragraphs[pIndex];
      
      if (wordCount + paragraph.words.length > this.currentWordIndex) {
        this.currentParagraphIndex = pIndex;
        this.currentWordInParagraph = this.currentWordIndex - wordCount;
        
        // Update word states
        this.updateWordStates();
        break;
      }
      
      wordCount += paragraph.words.length;
    }
  }

  private updateWordStates(): void {
    // Reset all word states
    this.paragraphs.forEach(p => {
      p.words.forEach((w: any) => {
        w.isCurrent = false;
        w.isRead = false;
      });
    });

    // Set current and read words
    let globalIndex = 0;
    for (let pIndex = 0; pIndex < this.paragraphs.length; pIndex++) {
      const paragraph = this.paragraphs[pIndex];
      
      for (let wIndex = 0; wIndex < paragraph.words.length; wIndex++) {
        const word = paragraph.words[wIndex];
        
        if (globalIndex === this.currentWordIndex) {
          word.isCurrent = true;
        } else if (globalIndex < this.currentWordIndex) {
          word.isRead = true;
        }
        
        globalIndex++;
      }
    }
  }

  private autoScrollToCurrentWord(): void {
    if (!this.textContainer) return;
    
    const currentWordElement = document.getElementById(
      `word-${this.currentParagraphIndex}-${this.currentWordInParagraph}`
    );
    
    if (currentWordElement) {
      currentWordElement.scrollIntoView({
        behavior: 'smooth',
        block: 'center',
        inline: 'nearest'
      });
    }
  }

  private updateProgress(): void {
    this.readingProgress = (this.currentWordIndex / this.totalWords) * 100;
    this.currentPage = Math.ceil((this.currentWordIndex + 1) / this.wordsPerPage);
  }

  private updateStats(): void {
    this.elapsedTime = Date.now() - this.startTime;
    
    if (this.elapsedTime > 0) {
      const minutes = this.elapsedTime / (1000 * 60);
      this.currentWPM = Math.round(this.currentWordIndex / minutes);
    }
  }

  private completeReading(): void {
    this.stopGuidedReading();
    this.stopStatsTracking();
    this.isCompleted = true;
    this.finalWPM = this.currentWPM;
    // Kaydırma çubuğu ve ilerleme barı güncel olsun
    this.updateProgress();
    // Clear resume point at completion
    try { localStorage.removeItem(`reading_resume_${this.textContent.id}`); } catch {}
    this.completed.emit();
  }

  private cleanup(): void {
    this.stopStatsTracking();
    this.stopGuidedReading();
  }

  // Event handlers
  onScroll(event: Event): void {
    this.scrollEvents++;
    this.scroll.emit(event);
    this.updateScrollProgress();
  }

  private updateScrollProgress(): void {
    if (!this.textContainer) return;
    
    const element = this.textContainer.nativeElement;
    const scrollTop = element.scrollTop;
    const scrollHeight = element.scrollHeight - element.clientHeight;
    
    this.scrollProgress = scrollHeight > 0 ? (scrollTop / scrollHeight) * 100 : 0;
  }

  onSpeedModeChange(): void {
    if (this.isActive) {
      this.stopGuidedReading();
      if (this.speedMode === 'guided' || this.speedMode === 'auto') {
        this.startGuidedReading();
      }
    }
  }

  onWPMChange(event: any): void {
    this.targetWPM = parseInt(event.target.value);
    if (this.guidedInterval) {
      this.stopGuidedReading();
      this.startGuidedReading();
    }
  }

  // Navigation methods
  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.goToPageStart();
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.goToPageStart();
    }
  }

  goToPage(event: any): void {
    const page = parseInt(event.target.value);
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.goToPageStart();
    }
  }

  private goToPageStart(): void {
    const wordIndex = (this.currentPage - 1) * this.wordsPerPage;
    this.currentWordIndex = Math.min(wordIndex, this.totalWords - 1);
    this.updateCurrentWordPosition();
    this.updateProgress();
    this.autoScrollToCurrentWord();
  }

  // Helper methods
  isCurrentWord(pIndex: number, wIndex: number): boolean {
    return pIndex === this.currentParagraphIndex && wIndex === this.currentWordInParagraph;
  }

  isReadWord(pIndex: number, wIndex: number): boolean {
    const globalIndex = this.getGlobalWordIndex(pIndex, wIndex);
    return globalIndex < this.currentWordIndex;
  }

  isHighlightedWord(pIndex: number, wIndex: number): boolean {
    if (!this.settings.enableHighlighting) return false;
    
    const globalIndex = this.getGlobalWordIndex(pIndex, wIndex);
    const highlightRange = this.settings.highlightRange || 3;
    
    return Math.abs(globalIndex - this.currentWordIndex) <= highlightRange;
  }

  private getGlobalWordIndex(pIndex: number, wIndex: number): number {
    let globalIndex = 0;
    
    for (let i = 0; i < pIndex; i++) {
      globalIndex += this.paragraphs[i].words.length;
    }
    
    return globalIndex + wIndex;
  }

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

  getReadingAreaStyles(): string {
    return `
      max-height: calc(100vh - 220px);
      overflow-y: auto;
    `;
  }

  getTextStyles(): string {
    return `
      font-size: ${this.settings.fontSize}px;
      line-height: ${this.settings.lineHeight};
      letter-spacing: 0.5px;
    `;
  }

  getWordStyle(pIndex: number, wIndex: number): string {
    let styles = '';
    
    if (this.isCurrentWord(pIndex, wIndex)) {
      styles += `
        background-color: ${this.settings.highlightColor};
        color: white;
        font-weight: normal;
        padding: 2px 4px;
        margin: 0 1px;
        box-sizing: border-box;
      `;
    } else if (this.isReadWord(pIndex, wIndex)) {
      styles += `
        opacity: 0.6;
        color: #6b7280;
        padding: 2px 4px;
        margin: 0 1px;
        font-weight: normal;
        box-sizing: border-box;
      `;
    }
    
    return styles;
  }

  getGuideStyles(): string {
    return `
      position: absolute;
      top: 50%;
      left: 0;
      right: 0;
      height: 2px;
      background: ${this.settings.highlightColor};
      opacity: 0.3;
      pointer-events: none;
    `;
  }
}