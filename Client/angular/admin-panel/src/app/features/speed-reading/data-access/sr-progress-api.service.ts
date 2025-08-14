import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

export interface PagedResponse<T> { items: T[]; total: number; }

@Injectable({ providedIn: 'root' })
export class SrProgressApiService {
  private base = `${environment.apiUrl}/sr-progress/api/v1`;
  constructor(private http: HttpClient) {}

  listSessions(params: { userId?: string; textId?: string; dateFrom?: string; dateTo?: string; page?: number; pageSize?: number }) {
    let p = new HttpParams();
    Object.entries(params).forEach(([k, v]) => { if (v != null) p = p.set(k, String(v)); });
    return this.http.get<PagedResponse<any>>(`${this.base}/admin/sessions`, { params: p });
  }

  listAttempts(params: { userId?: string; exerciseId?: string; dateFrom?: string; dateTo?: string; page?: number; pageSize?: number }) {
    let p = new HttpParams();
    Object.entries(params).forEach(([k, v]) => { if (v != null) p = p.set(k, String(v)); });
    return this.http.get<PagedResponse<any>>(`${this.base}/admin/attempts`, { params: p });
  }

  listResponses(params: { attemptId?: string; textId?: string; page?: number; pageSize?: number }) {
    let p = new HttpParams();
    Object.entries(params).forEach(([k, v]) => { if (v != null) p = p.set(k, String(v)); });
    return this.http.get<PagedResponse<any>>(`${this.base}/admin/responses`, { params: p });
  }
}

