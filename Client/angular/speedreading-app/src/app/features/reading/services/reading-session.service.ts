import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, interval, Subscription } from 'rxjs';
import { ReadingSession, ReadingMode, ReadingSettings, ReadingPerformance, TextContent } from '../../../shared/models/reading.models';
import { ReadingProgressApiService } from './reading-progress-api.service';

@Injectable({
  providedIn: 'root'
})
export class ReadingSessionService {
  private currentSession: ReadingSession | null = null;
  private sessionSubject = new BehaviorSubject<ReadingSession | null>(null);
  private performanceSubject = new BehaviorSubject<ReadingPerformance | null>(null);
  private timerSubscription: Subscription | null = null;
  
  private startTime: number = 0;
  private pausedTime: number = 0;
  private totalPausedDuration: number = 0;
  private isPaused: boolean = false;
  private wordsRead: number = 0;
  private regressionCount: number = 0;
  private pauseCount: number = 0;
  private scrollEventCount: number = 0;

  public session$ = this.sessionSubject.asObservable();
  public performance$ = this.performanceSubject.asObservable();

  constructor(private progressApi: ReadingProgressApiService) {}

  /**
   * Start a new reading session
   */
  startSession(textContent: TextContent, userId: string, mode: ReadingMode, settings: ReadingSettings): ReadingSession {
    this.endCurrentSession();

    const sessionId = this.generateSessionId();
    const now = new Date();

    this.currentSession = {
      sessionId: sessionId,
      textId: textContent.id,
      userId: userId,
      readingMode: mode,
      startTime: now,
      totalDuration: 0,
      readingDuration: 0,
      pauseDuration: 0,
      wordCount: textContent.wordCount,
      wordsPerMinute: 0,
      charactersPerMinute: 0,
      pauseCount: 0,
      scrollEvents: 0,
      regressionCount: 0,
      settings: settings
    };

    this.resetCounters();
    this.startTime = Date.now();
    this.startPerformanceTracking();
    
    this.sessionSubject.next(this.currentSession);
    console.log('Reading session started:', this.currentSession);
    
    // Try to create session in backend (async, non-blocking)
    this.progressApi.createSession(this.currentSession).subscribe({
      next: (backendSessionId) => {
        if (backendSessionId && this.currentSession) {
          console.log('Session created in backend with ID:', backendSessionId);
        }
      },
      error: (error) => {
        console.warn('Failed to create session in backend:', error);
      }
    });
    
    return this.currentSession;
  }

  /**
   * End current reading session
   */
  endSession(): ReadingSession | null {
    if (!this.currentSession) {
      return null;
    }

    const endTime = new Date();
    const totalDuration = Date.now() - this.startTime;
    const readingDuration = totalDuration - this.totalPausedDuration;

    this.currentSession.endTime = endTime;
    this.currentSession.totalDuration = totalDuration;
    this.currentSession.readingDuration = readingDuration;
    this.currentSession.pauseDuration = this.totalPausedDuration;
    this.currentSession.wordsPerMinute = this.calculateWPM(this.wordsRead, readingDuration);
    this.currentSession.charactersPerMinute = this.calculateCPM(this.wordsRead, readingDuration);
    this.currentSession.pauseCount = this.pauseCount;
    this.currentSession.scrollEvents = this.scrollEventCount;
    this.currentSession.regressionCount = this.regressionCount;

    this.stopPerformanceTracking();
    
    const completedSession = { ...this.currentSession };
    
    // Try to update session in backend (async, non-blocking)
    this.progressApi.updateSession(completedSession.sessionId, completedSession).subscribe({
      next: (success) => {
        if (success) {
          console.log('Session updated in backend successfully');
        }
      },
      error: (error) => {
        console.warn('Failed to update session in backend:', error);
      }
    });
    
    this.currentSession = null;
    this.sessionSubject.next(null);
    
    console.log('Reading session ended:', completedSession);
    return completedSession;
  }

  /**
   * Pause current session
   */
  pauseSession(): void {
    if (!this.currentSession || this.isPaused) {
      return;
    }

    this.isPaused = true;
    this.pausedTime = Date.now();
    this.pauseCount++;
    
    console.log('Session paused');
  }

  /**
   * Resume current session
   */
  resumeSession(): void {
    if (!this.currentSession || !this.isPaused) {
      return;
    }

    this.isPaused = false;
    this.totalPausedDuration += Date.now() - this.pausedTime;
    
    console.log('Session resumed');
  }

  /**
   * Update words read count
   */
  updateWordsRead(wordCount: number): void {
    if (!this.currentSession) return;
    
    if (wordCount < this.wordsRead) {
      this.regressionCount++;
    }
    
    this.wordsRead = wordCount;
    this.updatePerformance();
  }

  /**
   * Track scroll event
   */
  trackScrollEvent(): void {
    this.scrollEventCount++;
  }

  /**
   * Get current session
   */
  getCurrentSession(): ReadingSession | null {
    return this.currentSession;
  }

  /**
   * Check if session is active
   */
  isSessionActive(): boolean {
    return this.currentSession !== null;
  }

  /**
   * Check if session is paused
   */
  isSessionPaused(): boolean {
    return this.isPaused;
  }

  /**
   * Get session duration in milliseconds
   */
  getSessionDuration(): number {
    if (!this.currentSession) return 0;
    
    const currentTime = Date.now();
    const elapsed = currentTime - this.startTime;
    const activeDuration = elapsed - this.totalPausedDuration;
    
    if (this.isPaused) {
      return activeDuration - (currentTime - this.pausedTime);
    }
    
    return activeDuration;
  }

  /**
   * Get current WPM
   */
  getCurrentWPM(): number {
    const duration = this.getSessionDuration();
    return this.calculateWPM(this.wordsRead, duration);
  }

  /**
   * Get reading progress percentage
   */
  getProgress(): number {
    if (!this.currentSession) return 0;
    return Math.round((this.wordsRead / this.currentSession.wordCount) * 100);
  }

  /**
   * Save session to storage
   */
  saveSession(session: ReadingSession): void {
    const sessions = this.getSavedSessions();
    sessions.push(session);
    
    // Keep only last 50 sessions
    if (sessions.length > 50) {
      sessions.splice(0, sessions.length - 50);
    }
    
    localStorage.setItem('reading_sessions', JSON.stringify(sessions));
    console.log('Session saved to storage');
  }

  /**
   * Get saved sessions from storage
   */
  getSavedSessions(): ReadingSession[] {
    const sessionsJson = localStorage.getItem('reading_sessions');
    return sessionsJson ? JSON.parse(sessionsJson) : [];
  }

  /**
   * Get user's reading statistics
   */
  getUserStats(userId: string): any {
    const sessions = this.getSavedSessions().filter(s => s.userId === userId);
    
    if (sessions.length === 0) {
      return {
        totalSessions: 0,
        averageWPM: 0,
        totalWordsRead: 0,
        totalReadingTime: 0,
        improvementRate: 0
      };
    }

    const totalWordsRead = sessions.reduce((sum, s) => sum + s.wordCount, 0);
    const totalReadingTime = sessions.reduce((sum, s) => sum + s.readingDuration, 0);
    const averageWPM = sessions.reduce((sum, s) => sum + s.wordsPerMinute, 0) / sessions.length;
    
    // Calculate improvement rate (last 5 vs first 5 sessions)
    let improvementRate = 0;
    if (sessions.length >= 10) {
      const firstFive = sessions.slice(0, 5);
      const lastFive = sessions.slice(-5);
      const firstAvg = firstFive.reduce((sum, s) => sum + s.wordsPerMinute, 0) / 5;
      const lastAvg = lastFive.reduce((sum, s) => sum + s.wordsPerMinute, 0) / 5;
      improvementRate = Math.round(((lastAvg - firstAvg) / firstAvg) * 100);
    }

    return {
      totalSessions: sessions.length,
      averageWPM: Math.round(averageWPM),
      totalWordsRead: totalWordsRead,
      totalReadingTime: Math.round(totalReadingTime / 1000 / 60), // in minutes
      improvementRate: improvementRate
    };
  }

  private endCurrentSession(): void {
    if (this.currentSession) {
      this.endSession();
    }
  }

  private resetCounters(): void {
    this.startTime = 0;
    this.pausedTime = 0;
    this.totalPausedDuration = 0;
    this.isPaused = false;
    this.wordsRead = 0;
    this.regressionCount = 0;
    this.pauseCount = 0;
    this.scrollEventCount = 0;
  }

  private startPerformanceTracking(): void {
    this.timerSubscription = interval(1000).subscribe(() => {
      this.updatePerformance();
    });
  }

  private stopPerformanceTracking(): void {
    if (this.timerSubscription) {
      this.timerSubscription.unsubscribe();
      this.timerSubscription = null;
    }
  }

  private updatePerformance(): void {
    if (!this.currentSession) return;

    const duration = this.getSessionDuration();
    const currentWPM = this.calculateWPM(this.wordsRead, duration);
    const averageWPM = this.getCurrentWPM();
    const progress = this.getProgress();

    const performance: ReadingPerformance = {
      sessionId: this.currentSession.sessionId,
      timestamp: Date.now(),
      currentWPM: currentWPM,
      averageWPM: averageWPM,
      wordsRead: this.wordsRead,
      totalWords: this.currentSession.wordCount,
      progressPercentage: progress,
      regressions: this.regressionCount,
      pauses: this.pauseCount
    };

    this.performanceSubject.next(performance);
  }

  private calculateWPM(wordsRead: number, durationMs: number): number {
    if (durationMs <= 0) return 0;
    const minutes = durationMs / (1000 * 60);
    return Math.round(wordsRead / minutes);
  }

  private calculateCPM(wordsRead: number, durationMs: number): number {
    if (durationMs <= 0) return 0;
    const minutes = durationMs / (1000 * 60);
    const avgCharsPerWord = 5; // Türkçe için ortalama
    return Math.round((wordsRead * avgCharsPerWord) / minutes);
  }

  private generateSessionId(): string {
    return 'session_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
  }
}