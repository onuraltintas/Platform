import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { HttpClientService, ApiResponse } from '../../../core/services/http-client.service';
import { environment } from '../../../../environments/environment';
import { ReadingSession, ReadingMode, SessionResults } from '../../../shared/models/reading.models';

// Backend DTOs (matching SpeedReading.ProgressService)
export interface BackendSessionDto {
  sessionId: string;
  userId: string;
  textId?: string;
  sessionStartDate: string;
  sessionEndDate?: string;
  durationSeconds?: number;
  wpm?: number;
  comprehensionScore?: number;
  eyeTrackingMetricsJson?: string;
  createdAt: string;
}

export interface SessionCreateRequest {
  userId: string;
  textId?: string;
  sessionStartDate: string;
  readingMode: string;
  settingsJson: string;
}

export interface SessionUpdateRequest {
  sessionEndDate: string;
  durationSeconds: number;
  wpm: number;
  comprehensionScore?: number;
  metricsJson: string;
}

export interface ProgressSummary {
  totalSessions: number;
  averageWPM: number;
  totalReadingTime: number;
  improvementRate: number;
  bestWPM: number;
  recentSessions: BackendSessionDto[];
}

@Injectable({
  providedIn: 'root'
})
export class ReadingProgressApiService {
  // Gateway üzerinden: '/api' prefix'i HttpClientService.baseUrl'den geliyor
  private readonly endpoint = `/sr-progress`;
  private readonly sessionsEndpoint = `${this.endpoint}/sessions`;
  private readonly usersEndpoint = `${this.endpoint}/api/v1/users`;
  private readonly summaryEndpoint = `${this.usersEndpoint}`;

  constructor(private httpClient: HttpClientService) {}

  /**
   * Create a new reading session in backend
   */
  createSession(session: ReadingSession): Observable<string | null> {
    const request: SessionCreateRequest = {
      userId: session.userId,
      textId: session.textId,
      sessionStartDate: session.startTime.toISOString(),
      readingMode: session.readingMode,
      settingsJson: JSON.stringify(session.settings)
    };

    return this.httpClient.post<{sessionId: string}>(this.sessionsEndpoint, request, {
      offline: true, // Queue for offline sync
      fallback: { sessionId: session.sessionId }
    }).pipe(
      map(response => {
        if (response.success && response.data) {
          return response.data.sessionId;
        }
        return session.sessionId; // Use original ID as fallback
      }),
      catchError(error => {
        console.warn('Failed to create session in backend:', error);
        return of(session.sessionId);
      })
    );
  }

  /**
   * Update session with final results
   */
  updateSession(sessionId: string, session: ReadingSession): Observable<boolean> {
    const request: SessionUpdateRequest = {
      sessionEndDate: session.endTime?.toISOString() || new Date().toISOString(),
      durationSeconds: Math.round(session.totalDuration / 1000),
      wpm: session.wordsPerMinute,
      comprehensionScore: undefined, // Will be added when comprehension tests are implemented
      metricsJson: JSON.stringify({
        readingDuration: session.readingDuration,
        pauseDuration: session.pauseDuration,
        pauseCount: session.pauseCount,
        scrollEvents: session.scrollEvents,
        regressionCount: session.regressionCount,
        mode: session.readingMode,
        wordCount: session.wordCount,
        charactersPerMinute: session.charactersPerMinute
      })
    };

    return this.httpClient.put<any>(`${this.sessionsEndpoint}/${sessionId}`, request, {
      offline: true, // Queue for offline sync
      fallback: true
    }).pipe(
      map(response => response.success),
      catchError(error => {
        console.warn('Failed to update session in backend:', error);
        return of(true); // Assume success for offline mode
      })
    );
  }

  /**
   * Get user's reading progress summary
   */
  getUserProgress(userId: string): Observable<ProgressSummary> {
    const fallbackProgress: ProgressSummary = {
      totalSessions: 0,
      averageWPM: 0,
      totalReadingTime: 0,
      improvementRate: 0,
      bestWPM: 0,
      recentSessions: []
    };

    return this.httpClient.get<ProgressSummary>(`${this.usersEndpoint}/${userId}/summary`, undefined, {
      fallback: fallbackProgress
    }).pipe(
      map(response => {
        if (response.success && response.data) {
          return response.data;
        }
        return fallbackProgress;
      }),
      catchError(error => {
        console.warn('Failed to fetch user progress from backend:', error);
        return of(fallbackProgress);
      })
    );
  }

  /**
   * Get user's recent sessions
   */
  getUserSessions(userId: string, options?: {
    page?: number;
    pageSize?: number;
    fromDate?: Date;
    toDate?: Date;
  }): Observable<ReadingSession[]> {
    const params = {
      page: options?.page || 1,
      pageSize: options?.pageSize || 20,
      ...(options?.fromDate && { fromDate: options.fromDate.toISOString() }),
      ...(options?.toDate && { toDate: options.toDate.toISOString() })
    };

    return this.httpClient.get<BackendSessionDto[]>(`${this.usersEndpoint}/${userId}/sessions`, params, {
      fallback: []
    }).pipe(
      map(response => {
        if (response.success && response.data) {
          return response.data.map(this.mapFromBackendDto);
        }
        return [];
      }),
      catchError(error => {
        console.warn('Failed to fetch user sessions from backend:', error);
        return of([]);
      })
    );
  }

  /**
   * Submit session results with analytics
   */
  submitSessionResults(sessionId: string, results: SessionResults): Observable<boolean> {
    const request = {
      sessionId: sessionId,
      efficiency: results.efficiency,
      consistency: results.consistency,
      improvement: results.improvement,
      recommendedSpeed: results.recommendedSpeed,
      suggestedExercises: results.suggestedExercises,
      nextSteps: results.nextSteps,
      submittedAt: new Date().toISOString()
    };

    return this.httpClient.post<any>(`${this.endpoint}/api/v1/session-results`, request, {
      offline: true,
      fallback: true
    }).pipe(
      map(response => response.success),
      catchError(error => {
        console.warn('Failed to submit session results to backend:', error);
        return of(true);
      })
    );
  }

  /**
   * Get reading statistics for dashboard
   */
  getReadingStats(userId: string, period: 'day' | 'week' | 'month' | 'year' = 'week'): Observable<any> {
    const fallbackStats = {
      totalSessions: 0,
      totalWords: 0,
      averageWPM: 0,
      improvementRate: 0,
      sessionsByDate: [],
      wpmTrend: [],
      comprehensionTrend: []
    };

    return this.httpClient.get<any>(`${this.usersEndpoint}/${userId}/stats`, { period }, {
      fallback: fallbackStats
    }).pipe(
      map(response => {
        if (response.success && response.data) {
          return response.data;
        }
        return fallbackStats;
      }),
      catchError(error => {
        console.warn('Failed to fetch reading stats from backend:', error);
        return of(fallbackStats);
      })
    );
  }

  /**
   * Unified dashboard summary
   */
  getDashboardSummary(userId: string, period: 'day' | 'week' | 'month' | 'year' = 'week') {
    const fallback = {
      summary: {
        totalSessions: 0, averageWPM: 0, totalReadingTime: 0,
        improvementRate: 0, bestWPM: 0, recentSessions: []
      },
      stats: {
        totalSessions: 0, totalWords: 0, averageWPM: 0, improvementRate: 0,
        sessionsByDate: [], wpmTrend: [], comprehensionTrend: []
      },
      recentSessions: [],
      readingExercise: { exercise: 'reading', totalCount: 0, averageWPM: 0, averageScore: 0, totalDurationSeconds: 0, trendByDate: [] },
      muscleExercise: { exercise: 'muscle', totalCount: 0, averageWPM: 0, averageScore: 0, totalDurationSeconds: 0, trendByDate: [] }
    } as any;
    return this.httpClient.get<any>(`${this.summaryEndpoint}/${userId}/dashboard-summary`, { period }, { fallback }).pipe(
      map(r => r.success && r.data ? r.data : fallback),
      catchError(() => of(fallback))
    );
  }

  /**
   * Get per-exercise stats
   */
  getExerciseStats(userId: string, exercise: 'reading' | 'muscle', period: 'day' | 'week' | 'month' | 'year' = 'week'): Observable<any> {
    const fallback = {
      exercise,
      totalCount: 0,
      averageWPM: 0,
      averageScore: 0,
      totalDurationSeconds: 0,
      trendByDate: []
    };

    return this.httpClient.get<any>(`${this.usersEndpoint}/${userId}/exercise-stats`, { exercise, period }, {
      fallback
    }).pipe(
      map(response => {
        if (response.success && response.data) {
          return response.data;
        }
        return fallback;
      }),
      catchError(() => of(fallback))
    );
  }

  /**
   * Sync offline sessions to backend
   */
  syncOfflineSessions(): Observable<any> {
    return this.httpClient.syncOfflineRequests().pipe(
      map(response => {
        console.log('Offline sessions sync result:', response);
        return response;
      })
    );
  }

  /**
   * Check if backend is available for progress tracking
   */
  isProgressTrackingAvailable(): Observable<boolean> {
    return this.httpClient.isBackendAvailable();
  }

  // Private mapping methods
  private mapFromBackendDto(dto: BackendSessionDto): ReadingSession {
    let settingsJson: any = {};
    let metricsJson: any = {};

    // Try to parse stored JSON data
    try {
      metricsJson = JSON.parse(dto.eyeTrackingMetricsJson || '{}');
    } catch (e) {
      console.warn('Failed to parse session metrics JSON');
    }

    return {
      sessionId: dto.sessionId,
      textId: dto.textId || '',
      userId: dto.userId,
      readingMode: metricsJson.mode || ReadingMode.CLASSIC,
      startTime: new Date(dto.sessionStartDate),
      endTime: dto.sessionEndDate ? new Date(dto.sessionEndDate) : undefined,
      totalDuration: (dto.durationSeconds || 0) * 1000,
      readingDuration: metricsJson.readingDuration || 0,
      pauseDuration: metricsJson.pauseDuration || 0,
      wordCount: metricsJson.wordCount || 0,
      wordsPerMinute: dto.wpm || 0,
      charactersPerMinute: metricsJson.charactersPerMinute || 0,
      pauseCount: metricsJson.pauseCount || 0,
      scrollEvents: metricsJson.scrollEvents || 0,
      regressionCount: metricsJson.regressionCount || 0,
      settings: settingsJson
    };
  }

  private mapToBackendDto(session: ReadingSession): BackendSessionDto {
    return {
      sessionId: session.sessionId,
      userId: session.userId,
      textId: session.textId,
      sessionStartDate: session.startTime.toISOString(),
      sessionEndDate: session.endTime?.toISOString(),
      durationSeconds: Math.round(session.totalDuration / 1000),
      wpm: session.wordsPerMinute,
      comprehensionScore: undefined,
      eyeTrackingMetricsJson: JSON.stringify({
        readingDuration: session.readingDuration,
        pauseDuration: session.pauseDuration,
        pauseCount: session.pauseCount,
        scrollEvents: session.scrollEvents,
        regressionCount: session.regressionCount,
        mode: session.readingMode,
        wordCount: session.wordCount,
        charactersPerMinute: session.charactersPerMinute
      }),
      createdAt: new Date().toISOString()
    };
  }
}