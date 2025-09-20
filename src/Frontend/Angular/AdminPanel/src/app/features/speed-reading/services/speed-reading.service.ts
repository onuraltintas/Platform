import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  SpeedReadingText,
  SpeedReadingExercise,
  UserProgress,
  ProgressAnalytics,
  CreateSpeedReadingTextDto,
  CreateExerciseDto,
  SpeedReadingFilter,
  SpeedReadingStatistics
} from '../models/speed-reading.models';
import { PagedResult, PageRequest } from '../../../core/api/models/api.models';

@Injectable({
  providedIn: 'root'
})
export class SpeedReadingService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiGateway}/speed-reading`;

  // Text Management
  getTexts(params?: PageRequest & SpeedReadingFilter): Observable<PagedResult<SpeedReadingText>> {
    let httpParams = new HttpParams();

    if (params) {
      Object.keys(params).forEach(key => {
        const value = (params as any)[key];
        if (value !== undefined && value !== null) {
          if (Array.isArray(value)) {
            value.forEach(v => httpParams = httpParams.append(key, v));
          } else {
            httpParams = httpParams.set(key, value.toString());
          }
        }
      });
    }

    return this.http.get<PagedResult<SpeedReadingText>>(`${this.apiUrl}/texts`, { params: httpParams });
  }

  getText(id: string): Observable<SpeedReadingText> {
    return this.http.get<SpeedReadingText>(`${this.apiUrl}/texts/${id}`);
  }

  createText(dto: CreateSpeedReadingTextDto): Observable<SpeedReadingText> {
    const wordCount = dto.content.split(/\s+/).length;
    const estimatedReadingTime = Math.ceil(wordCount / 200); // Average 200 WPM

    return this.http.post<SpeedReadingText>(`${this.apiUrl}/texts`, {
      ...dto,
      wordCount,
      estimatedReadingTime
    });
  }

  updateText(id: string, dto: Partial<CreateSpeedReadingTextDto>): Observable<SpeedReadingText> {
    let updateData: any = { ...dto };

    if (dto.content) {
      updateData.wordCount = dto.content.split(/\s+/).length;
      updateData.estimatedReadingTime = Math.ceil(updateData.wordCount / 200);
    }

    return this.http.put<SpeedReadingText>(`${this.apiUrl}/texts/${id}`, updateData);
  }

  deleteText(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/texts/${id}`);
  }

  // Exercise Management
  getExercises(params?: PageRequest & SpeedReadingFilter): Observable<PagedResult<SpeedReadingExercise>> {
    let httpParams = new HttpParams();

    if (params) {
      Object.keys(params).forEach(key => {
        const value = (params as any)[key];
        if (value !== undefined && value !== null) {
          if (Array.isArray(value)) {
            value.forEach(v => httpParams = httpParams.append(key, v));
          } else {
            httpParams = httpParams.set(key, value.toString());
          }
        }
      });
    }

    return this.http.get<PagedResult<SpeedReadingExercise>>(`${this.apiUrl}/exercises`, { params: httpParams });
  }

  getExercise(id: string): Observable<SpeedReadingExercise> {
    return this.http.get<SpeedReadingExercise>(`${this.apiUrl}/exercises/${id}`);
  }

  createExercise(dto: CreateExerciseDto): Observable<SpeedReadingExercise> {
    return this.http.post<SpeedReadingExercise>(`${this.apiUrl}/exercises`, dto);
  }

  updateExercise(id: string, dto: Partial<CreateExerciseDto>): Observable<SpeedReadingExercise> {
    return this.http.put<SpeedReadingExercise>(`${this.apiUrl}/exercises/${id}`, dto);
  }

  deleteExercise(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/exercises/${id}`);
  }

  duplicateExercise(id: string): Observable<SpeedReadingExercise> {
    return this.http.post<SpeedReadingExercise>(`${this.apiUrl}/exercises/${id}/duplicate`, {});
  }

  // Progress Tracking
  getUserProgress(userId: string, params?: PageRequest): Observable<PagedResult<UserProgress>> {
    let httpParams = new HttpParams();

    if (params) {
      Object.keys(params).forEach(key => {
        const value = (params as any)[key];
        if (value !== undefined && value !== null) {
          httpParams = httpParams.set(key, value.toString());
        }
      });
    }

    return this.http.get<PagedResult<UserProgress>>(`${this.apiUrl}/progress/${userId}`, { params: httpParams });
  }

  getProgressDetails(progressId: string): Observable<UserProgress> {
    return this.http.get<UserProgress>(`${this.apiUrl}/progress/details/${progressId}`);
  }

  startExercise(exerciseId: string, userId: string): Observable<UserProgress> {
    return this.http.post<UserProgress>(`${this.apiUrl}/progress/start`, {
      exerciseId,
      userId,
      startedAt: new Date()
    });
  }

  updateProgress(progressId: string, data: Partial<UserProgress>): Observable<UserProgress> {
    return this.http.patch<UserProgress>(`${this.apiUrl}/progress/${progressId}`, data);
  }

  completeExercise(progressId: string, results: any): Observable<UserProgress> {
    return this.http.post<UserProgress>(`${this.apiUrl}/progress/${progressId}/complete`, results);
  }

  // Analytics
  getUserAnalytics(userId: string, dateRange?: { startDate: Date; endDate: Date }): Observable<ProgressAnalytics> {
    let httpParams = new HttpParams();

    if (dateRange) {
      httpParams = httpParams
        .set('startDate', dateRange.startDate.toISOString())
        .set('endDate', dateRange.endDate.toISOString());
    }

    return this.http.get<ProgressAnalytics>(`${this.apiUrl}/analytics/user/${userId}`, { params: httpParams });
  }

  getOverallStatistics(): Observable<SpeedReadingStatistics> {
    return this.http.get<SpeedReadingStatistics>(`${this.apiUrl}/analytics/statistics`);
  }

  getLeaderboard(period: 'daily' | 'weekly' | 'monthly' | 'all-time' = 'weekly'): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/analytics/leaderboard`, {
      params: new HttpParams().set('period', period)
    });
  }

  // Categories & Tags
  getCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/metadata/categories`);
  }

  getTags(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/metadata/tags`);
  }

  // Bulk Operations
  bulkDeleteTexts(ids: string[]): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/texts/bulk-delete`, { ids });
  }

  bulkDeleteExercises(ids: string[]): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/exercises/bulk-delete`, { ids });
  }

  bulkUpdateTexts(ids: string[], updates: Partial<SpeedReadingText>): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/texts/bulk-update`, { ids, updates });
  }

  // Import/Export
  importTexts(file: File): Observable<{ imported: number; failed: number; errors: string[] }> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<{ imported: number; failed: number; errors: string[] }>(
      `${this.apiUrl}/import/texts`,
      formData
    );
  }

  exportTexts(filter?: SpeedReadingFilter): Observable<Blob> {
    let httpParams = new HttpParams();

    if (filter) {
      Object.keys(filter).forEach(key => {
        const value = (filter as any)[key];
        if (value !== undefined && value !== null) {
          httpParams = httpParams.set(key, value.toString());
        }
      });
    }

    return this.http.get(`${this.apiUrl}/export/texts`, {
      params: httpParams,
      responseType: 'blob'
    });
  }

  // Text Analysis
  analyzeText(content: string): Observable<{
    wordCount: number;
    sentenceCount: number;
    paragraphCount: number;
    averageWordLength: number;
    readabilityScore: number;
    difficulty: 'beginner' | 'intermediate' | 'advanced' | 'expert';
    estimatedReadingTime: number;
  }> {
    return this.http.post<any>(`${this.apiUrl}/analyze`, { content });
  }

  // Exercise Preview
  previewExercise(exerciseId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/exercises/${exerciseId}/preview`);
  }
}