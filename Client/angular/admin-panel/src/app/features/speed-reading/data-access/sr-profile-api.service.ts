import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

export interface ProfileDto { userId: string; currentReadingLevelId?: string | null; goals?: string | null; learningStyle?: string | null; accessibilityNeeds?: string | null; preferencesJson?: string | null; }

@Injectable({ providedIn: 'root' })
export class SrProfileApiService {
  private base = `${environment.apiUrl}/sr-profile/api/v1`;
  constructor(private http: HttpClient) {}

  getMyProfile() { return this.http.get<ProfileDto>(`${this.base}/profile/me`); }
  updateMyProfile(body: Partial<ProfileDto>) { return this.http.put<ProfileDto>(`${this.base}/profile/me`, body); }
}

