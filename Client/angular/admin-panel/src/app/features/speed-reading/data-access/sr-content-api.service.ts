import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

export interface PagedRequest {
  page?: number;
  pageSize?: number;
  search?: string;
  level?: string;
  difficultyLevel?: string;
  category?: string;
}

export interface PagedResponse<T> {
  items: T[];
  total: number;
}

export interface TextDto {
  textId: string;
  title: string;
  difficultyLevel: string;
  levelId?: string;
  levelName?: string;
  wordCount?: number;
  updatedAt?: string;
}

export interface UpsertTextRequest {
  title: string;
  content: string;
  difficultyLevel: string;
  levelId?: string;
  tagsJson?: string;
}

export interface ExerciseDto {
  exerciseId: string;
  exerciseTypeId: string;
  title: string;
  description?: string;
  difficultyLevel: string;
  levelId?: string;
  contentJson?: string;
  durationMinutes?: number;
}

export interface UpsertExerciseRequest {
  exerciseTypeId: string;
  title: string;
  description?: string;
  difficultyLevel: string;
  levelId?: string;
  contentJson?: string;
  durationMinutes?: number;
}

export interface QuestionDto {
  questionId: string;
  textId: string;
  questionText: string;
  questionType?: string;
  correctAnswer?: string;
  optionsJson?: string;
  levelId?: string;
}

export interface UpsertQuestionRequest {
  textId: string;
  questionText: string;
  questionType?: string;
  correctAnswer?: string;
  optionsJson?: string;
  levelId?: string;
}

export interface LevelDto {
  levelId: string;
  levelName: string;
  minAge?: number;
  maxAge?: number;
  minWPM?: number;
  maxWPM?: number;
  targetComprehension?: number;
}

export interface ExerciseTypeDto { exerciseTypeId: string; typeName: string; description?: string }

@Injectable({ providedIn: 'root' })
export class SrContentApiService {
  private base = `${environment.apiUrl}/sr-content/api/v1`;

  constructor(private http: HttpClient) {}

  listTexts(req: PagedRequest) {
    let params = new HttpParams();
    Object.entries(req).forEach(([k, v]) => {
      if (v !== undefined && v !== null && v !== '') params = params.set(k, String(v));
    });
    return this.http.get<PagedResponse<TextDto>>(`${this.base}/admin/texts`, { params });
  }

  getText(id: string) {
    return this.http.get<TextDto>(`${this.base}/admin/texts/${id}`);
  }

  createText(body: UpsertTextRequest) {
    return this.http.post<TextDto>(`${this.base}/admin/texts`, body);
  }

  updateText(id: string, body: UpsertTextRequest) {
    return this.http.put<TextDto>(`${this.base}/admin/texts/${id}`, body);
  }

  deleteText(id: string) {
    return this.http.delete<void>(`${this.base}/admin/texts/${id}`);
  }

  // Exercises
  listExercises(req: PagedRequest) {
    let params = new HttpParams();
    Object.entries(req).forEach(([k, v]) => { if (v !== undefined && v !== null && v !== '') params = params.set(k, String(v)); });
    return this.http.get<PagedResponse<ExerciseDto>>(`${this.base}/admin/exercises`, { params });
  }
  getExercise(id: string) { return this.http.get<ExerciseDto>(`${this.base}/admin/exercises/${id}`); }
  createExercise(body: UpsertExerciseRequest) { return this.http.post<ExerciseDto>(`${this.base}/admin/exercises`, body); }
  updateExercise(id: string, body: UpsertExerciseRequest) { return this.http.put<ExerciseDto>(`${this.base}/admin/exercises/${id}`, body); }
  deleteExercise(id: string) { return this.http.delete<void>(`${this.base}/admin/exercises/${id}`); }

  // Questions
  listQuestions(params: { textId?: string; page?: number; pageSize?: number; level?: string; questionType?: string }) {
    let p = new HttpParams();
    Object.entries(params).forEach(([k, v]) => { if (v != null && v !== '') p = p.set(k, String(v)); });
    return this.http.get<PagedResponse<QuestionDto>>(`${this.base}/admin/questions`, { params: p });
  }
  getQuestion(id: string) { return this.http.get<QuestionDto>(`${this.base}/admin/questions/${id}`); }
  createQuestion(body: UpsertQuestionRequest) { return this.http.post<QuestionDto>(`${this.base}/admin/questions`, body); }
  updateQuestion(id: string, body: UpsertQuestionRequest) { return this.http.put<QuestionDto>(`${this.base}/admin/questions/${id}`, body); }
  deleteQuestion(id: string) { return this.http.delete<void>(`${this.base}/admin/questions/${id}`); }

  // Levels
  listLevels() { return this.http.get<LevelDto[]>(`${this.base}/admin/levels`); }
  getLevel(id: string) { return this.http.get<LevelDto>(`${this.base}/admin/levels/${id}`); }
  createLevel(body: Omit<LevelDto, 'levelId'>) { return this.http.post<LevelDto>(`${this.base}/admin/levels`, body); }
  updateLevel(id: string, body: Omit<LevelDto, 'levelId'>) { return this.http.put<LevelDto>(`${this.base}/admin/levels/${id}`, body); }
  deleteLevel(id: string) { return this.http.delete<void>(`${this.base}/admin/levels/${id}`); }

  // Exercise types
  listExerciseTypes() { return this.http.get<ExerciseTypeDto[]>(`${this.base}/admin/exercise-types`); }
  getExerciseType(id: string) { return this.http.get<ExerciseTypeDto>(`${this.base}/admin/exercise-types/${id}`); }
  createExerciseType(body: Omit<ExerciseTypeDto, 'exerciseTypeId'>) { return this.http.post<ExerciseTypeDto>(`${this.base}/admin/exercise-types`, body); }
  updateExerciseType(id: string, body: Omit<ExerciseTypeDto, 'exerciseTypeId'>) { return this.http.put<ExerciseTypeDto>(`${this.base}/admin/exercise-types/${id}`, body); }
  deleteExerciseType(id: string) { return this.http.delete<void>(`${this.base}/admin/exercise-types/${id}`); }
}

