import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { UserProfileDto, CreateUserProfileRequest, UpdateUserProfileRequest, UserSettingsDto, UpdateUserSettingsRequest } from '../models/user.models';

@Injectable({ providedIn: 'root' })
export class UsersService {
  // Gateway v1 admin rotası üzerinden kullanıcı yönetimi
  private adminBaseUrl = `${environment.apiUrl}/v1/admin/users`;
  // Gateway üzerinden UserService v1 profiller/ayarlar
  private profilesBaseUrl = `${environment.apiUrl}/v1/profiles`;
  private settingsBaseUrl = `${environment.apiUrl}/v1/settings`;

  constructor(private http: HttpClient) {}

  getProfile(userId: string): Observable<UserProfileDto> {
    return this.http.get<any>(`${this.profilesBaseUrl}/${userId}`).pipe(map(r => r?.data ?? r));
  }

  createProfile(request: CreateUserProfileRequest): Observable<UserProfileDto> {
    return this.http.post<any>(`${this.profilesBaseUrl}`, request).pipe(map(r => r?.data ?? r));
  }

  updateProfile(userId: string, request: UpdateUserProfileRequest): Observable<UserProfileDto> {
    return this.http.put<any>(`${this.profilesBaseUrl}/${userId}`, request).pipe(map(r => r?.data ?? r));
  }

  getSettings(userId: string): Observable<UserSettingsDto> {
    return this.http.get<any>(`${this.settingsBaseUrl}/${userId}`).pipe(map(r => r?.data ?? r));
  }

  updateSettings(userId: string, request: UpdateUserSettingsRequest): Observable<UserSettingsDto> {
    return this.http.put<any>(`${this.settingsBaseUrl}/${userId}`, request).pipe(map(r => r?.data ?? r));
  }

  // Admin endpoints via gateway v1
  listUsers(params: { page?: number; pageSize?: number; search?: string; roleId?: string; categoryId?: string; isActive?: boolean; isEmailConfirmed?: boolean; }): Observable<any> {
    const query = new URLSearchParams();
    if (params.page) query.set('page', String(params.page));
    if (params.pageSize) query.set('pageSize', String(params.pageSize));
    if (params.search) query.set('search', params.search);
    if (params.roleId) query.set('roleId', params.roleId);
    if (params.categoryId) query.set('categoryId', params.categoryId);
    if (params.isActive !== undefined) query.set('isActive', String(params.isActive));
    if (params.isEmailConfirmed !== undefined) query.set('isEmailConfirmed', String(params.isEmailConfirmed));
    const qs = query.toString();
    return this.http.get<any>(`${this.adminBaseUrl}${qs ? `?${qs}` : ''}`);
  }

  getUser(userId: string): Observable<any> {
    return this.http.get<any>(`${this.adminBaseUrl}/${userId}`);
  }

  createUser(body: { userName: string; email: string; firstName: string; lastName: string; password: string; phoneNumber?: string; roleIds?: string[]; categoryIds?: string[]; isActive?: boolean; isEmailConfirmed?: boolean; }): Observable<any> {
    return this.http.post<any>(`${this.adminBaseUrl}`, body);
  }

  updateUser(userId: string, body: { userName: string; email: string; firstName: string; lastName: string; phoneNumber?: string; isActive: boolean; isEmailConfirmed: boolean; }): Observable<any> {
    return this.http.put<any>(`${this.adminBaseUrl}/${userId}`, body);
  }

  deactivateUser(userId: string): Observable<any> {
    return this.http.patch<any>(`${this.adminBaseUrl}/${userId}/deactivate`, {});
  }

  activateUser(userId: string): Observable<any> {
    return this.http.patch<any>(`${this.adminBaseUrl}/${userId}/activate`, {});
  }
}

