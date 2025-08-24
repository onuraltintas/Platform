import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { TextContent, ComprehensionQuestion, QuestionType } from '../../../shared/models/reading.models';

// Backend DTOs (matching SpeedReading.ContentService)
export interface BackendTextDto {
  textId: string;
  title: string;
  content: string;
  difficultyLevel: string;
  levelId?: string;
  wordCount?: number;
  createdAt: string;
  updatedAt: string;
}

export interface BackendQuestionDto {
  questionId: string;
  textId: string;
  questionText: string;
  questionType: string;
  correctAnswer: string;
  optionsJson?: string;
  levelId?: string;
}

export interface TextListResponse {
  items: BackendTextDto[];
  total: number;
  page: number;
  pageSize: number;
}

@Injectable({
  providedIn: 'root'
})
export class ReadingContentApiService {
  // Public endpoints via API Gateway
  private readonly endpoint = `${environment.apiUrl}/sr-content/api/v1/texts`;
  private readonly questionsEndpoint = `${environment.apiUrl}/sr-content/api/v1/questions`;

  constructor(private http: HttpClient) {}

  /**
   * Get texts from backend
   */
  getTexts(options?: {
    page?: number;
    pageSize?: number;
    search?: string;
    levelId?: string;
    difficultyLevel?: string;
  }): Observable<TextContent[]> {
    
    const params = {
      page: options?.page || 1,
      pageSize: options?.pageSize || 20,
      ...(options?.search && { search: options.search }),
      ...(options?.levelId && { levelId: options.levelId }),
      ...(options?.difficultyLevel && { difficultyLevel: options.difficultyLevel })
    };

    const httpParams = new HttpParams({ fromObject: params });
    
    return this.http.get<TextListResponse>(this.endpoint, { params: httpParams }).pipe(
      map(response => {
        if (response && response.items) {
          return response.items.map(item => this.mapFromBackendDto(item));
        }
        return [];
      }),
      catchError(error => {
        console.error('Failed to fetch texts from backend:', error);
        return of([]);
      })
    );
  }

  /**
   * Get specific text by ID
   */
  getTextById(textId: string): Observable<TextContent | null> {
    return this.http.get<BackendTextDto>(`${this.endpoint}/${textId}`).pipe(
      map(response => {
        if (response) {
          return this.mapFromBackendDto(response);
        }
        return null;
      }),
      catchError(error => {
        console.warn('Failed to fetch text from backend:', error);
        return of(null);
      })
    );
  }

  /**
   * Get comprehension questions for a text
   */
  getQuestionsByTextId(textId: string): Observable<ComprehensionQuestion[]> {
    const fallbackQuestions = this.getSampleQuestions(textId);

    // Backend şu an /api/v1/admin/questions?textId={id} şeklinde list endpoint'i sağlıyor
    const httpParams = new HttpParams().set('textId', textId);

    return this.http.get<{ items: BackendQuestionDto[]; total: number }>(`${this.questionsEndpoint}`, { params: httpParams }).pipe(
      map(response => {
        if (response && response.items) {
          return (response.items as BackendQuestionDto[]).map(item => this.mapQuestionFromBackendDto(item));
        }
        return fallbackQuestions;
      }),
      catchError(error => {
        console.warn('Failed to fetch questions from backend, using sample data:', error);
        return of(fallbackQuestions);
      })
    );
  }

  /**
   * Save text to backend (for admin features)
   */
  saveText(text: Partial<TextContent>): Observable<TextContent | null> {
    const backendDto = {
      title: text.title || '',
      content: text.content || '',
      difficultyLevel: text.difficultyLevel?.toString() || '1',
      levelId: null,
      tagsJson: JSON.stringify(text.tags || [])
    };

    return this.http.post<BackendTextDto>(this.endpoint, backendDto).pipe(
      map(response => {
        if (response) {
          return this.mapFromBackendDto(response);
        }
        return null;
      }),
      catchError(error => {
        console.warn('Failed to save text to backend:', error);
        return of(null);
      })
    );
  }

  /**
   * Check if backend is available
   */
  isBackendAvailable(): Observable<boolean> {
    return this.http.get(`${environment.apiUrl}/sr-content/health`).pipe(
      map(() => true),
      catchError(() => of(false))
    );
  }

  // Private mapping methods
  private mapFromBackendDto(dto: BackendTextDto): TextContent {
    return {
      id: dto.textId,
      title: dto.title,
      content: (dto as any).content ?? dto.content,
      wordCount: dto.wordCount || this.countWords((dto as any).content ?? dto.content ?? ''),
      difficultyLevel: this.mapDifficultyToNumber(dto.difficultyLevel),
      category: 'General',
      estimatedReadingTime: Math.ceil((dto.wordCount || this.countWords(dto.content)) / 250),
      language: 'tr',
      tags: [],
      sentences: [],
      paragraphs: [],
      chunks: []
    };
  }

  private mapDifficultyToNumber(difficulty: string | undefined): number {
    const d = (difficulty || '').toLowerCase();
    if (['1','2','3','4','5','6','7','8','9','10'].includes(d)) return parseInt(d, 10);
    if (d.includes('temel')) return 2;
    if (d.includes('orta')) return 5;
    if (d.includes('ileri')) return 8;
    if (d.includes('uzman')) return 10;
    return 3;
  }

  private mapToBackendDto(text: TextContent): BackendTextDto {
    return {
      textId: text.id,
      title: text.title,
      content: text.content,
      difficultyLevel: this.mapDifficultyToBackend(text.difficultyLevel),
      wordCount: text.wordCount,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    };
  }

  private mapDifficultyToBackend(level: number): string {
    if (level >= 9) return 'Uzman';
    if (level >= 7) return 'İleri';
    if (level >= 4) return 'Orta';
    return 'Temel';
  }

  private mapQuestionFromBackendDto(dto: BackendQuestionDto): ComprehensionQuestion {
    return {
      id: dto.questionId,
      textId: dto.textId,
      type: this.mapQuestionType(dto.questionType),
      question: dto.questionText,
      options: dto.optionsJson ? JSON.parse(dto.optionsJson) : undefined,
      correctAnswer: dto.correctAnswer,
      difficulty: 2,
      points: 10
    };
  }

  private mapQuestionToBackendDto(question: ComprehensionQuestion): BackendQuestionDto {
    return {
      questionId: question.id,
      textId: question.textId,
      questionText: question.question,
      questionType: question.type,
      correctAnswer: question.correctAnswer.toString(),
      optionsJson: question.options ? JSON.stringify(question.options) : undefined
    };
  }

  private mapQuestionType(backendType: string): QuestionType {
    switch (backendType?.toLowerCase()) {
      case 'multiplechoice':
        return QuestionType.MULTIPLE_CHOICE;
      case 'truefalse':
        return QuestionType.TRUE_FALSE;
      case 'fillblank':
        return QuestionType.FILL_BLANK;
      case 'shortanswer':
        return QuestionType.SHORT_ANSWER;
      default:
        return QuestionType.MULTIPLE_CHOICE;
    }
  }

  private countWords(text: string): number {
    if (!text) return 0;
    // HTML etiketlerini temizle
    const withoutHtml = text.replace(/<[^>]*>/g, ' ');
    // Harf ve rakam dışındaki karakterleri boşluğa çevir (Unicode destekli)
    const normalized = withoutHtml.replace(/[^\p{L}\p{N}]+/gu, ' ').trim();
    if (!normalized) return 0;
    return normalized.split(/\s+/).length;
  }

  private createDummyBackendText(text: Partial<TextContent>): BackendTextDto {
    return {
      textId: text.id || 'temp_' + Date.now(),
      title: text.title || 'Untitled',
      content: text.content || '',
      difficultyLevel: text.difficultyLevel?.toString() || '1',
      wordCount: text.wordCount || 0,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    };
  }


  private getSampleQuestions(textId: string): ComprehensionQuestion[] {
    return [
      {
        id: `q_${textId}_1`,
        textId: textId,
        type: QuestionType.MULTIPLE_CHOICE,
        question: 'Hızlı okuma tekniklerinden hangisi kelimeleri tek tek gösterir?',
        options: ['RSVP', 'Chunk Reading', 'Guided Reading', 'Classic Reading'],
        correctAnswer: 0,
        difficulty: 2,
        points: 10
      },
      {
        id: `q_${textId}_2`,
        textId: textId,
        type: QuestionType.TRUE_FALSE,
        question: 'Hızlı okuma sadece okuma hızını artırır, anlama kapasitesini etkilemez.',
        correctAnswer: 'false',
        difficulty: 1,
        points: 5
      }
    ];
  }
}