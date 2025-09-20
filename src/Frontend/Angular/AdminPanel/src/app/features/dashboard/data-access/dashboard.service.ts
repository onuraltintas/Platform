import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export type DashboardRange = 'today' | '7d' | '30d' | 'custom';

export interface DashboardSummary {
  totalUsers: number;
  activeUsers: number;
  totalGroups: number;
  totalRoles: number;
  dailyStats?: {
    newUsers: number;
    activeToday: number;
    logins: number;
    actions: number;
  };
}

export interface TrendPoint { label: string; value: number; }

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiGateway}/speedreading/analytics`;

  getSummary(range: DashboardRange, segment: string, start?: string, end?: string): Observable<DashboardSummary> {
    let params = new HttpParams().set('range', range).set('segment', segment);
    if (range === 'custom' && start && end) {
      params = params.set('start', start).set('end', end);
    }
    return this.http.get<DashboardSummary>(`${this.base}/summary`, { params });
  }

  getTrend(range: DashboardRange, segment: string, start?: string, end?: string): Observable<TrendPoint[]> {
    let params = new HttpParams().set('range', range).set('segment', segment);
    if (range === 'custom' && start && end) {
      params = params.set('start', start).set('end', end);
    }
    return this.http.get<TrendPoint[]>(`${this.base}/trend`, { params });
  }

  getActions(range: DashboardRange, segment: string, start?: string, end?: string): Observable<TrendPoint[]> {
    let params = new HttpParams().set('range', range).set('segment', segment);
    if (range === 'custom' && start && end) {
      params = params.set('start', start).set('end', end);
    }
    return this.http.get<TrendPoint[]>(`${this.base}/actions`, { params });
  }
}

