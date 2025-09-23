import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export type IdentitySummary = { roles: number; permissions: number; groups: number; updatedAt?: string };
export type UserSummary = { users: number; activeUsers: number; pendingVerifications: number };
export type SpeedSummary = { texts: number; exercises: number; sessionsToday: number };

@Injectable({ providedIn: 'root' })
export class SummaryClients {
  private readonly http = inject(HttpClient);
  private base(path: string): string { return `${environment.apiGateway}${path}`; }

  getIdentitySummary() {
    return this.http.get<IdentitySummary>(this.base('/identity/admin/summary'));
  }
  getUserSummary() {
    return this.http.get<UserSummary>(this.base('/user/admin/summary'));
  }
  getSpeedSummary() {
    return this.http.get<SpeedSummary>(this.base('/speed-reading/admin/summary'));
  }
}

