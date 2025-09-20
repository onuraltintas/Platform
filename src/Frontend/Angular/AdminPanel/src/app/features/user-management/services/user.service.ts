import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  User,
  CreateUserRequest,
  UpdateUserRequest,
  PagedResult,
  ListQuery
} from '../models/simple.models';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiGateway}/api/users`;

  // Get users with pagination and search
  getUsers(query: ListQuery = {}): Observable<PagedResult<User>> {
    const params = this.buildQueryParams(query);
    return this.http.get<PagedResult<User>>(this.baseUrl, { params });
  }

  // Get single user by ID
  getUser(id: string): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/${id}`);
  }

  // Create new user
  createUser(request: CreateUserRequest): Observable<User> {
    return this.http.post<User>(this.baseUrl, request);
  }

  // Update existing user
  updateUser(request: UpdateUserRequest): Observable<User> {
    return this.http.put<User>(`${this.baseUrl}/${request.id}`, request);
  }

  // Delete user
  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  // Activate/Deactivate user
  toggleUserStatus(id: string, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/status`, { isActive });
  }

  // Helper method to build query parameters
  private buildQueryParams(query: ListQuery): any {
    const params: any = {};

    if (query.page) params.page = query.page.toString();
    if (query.pageSize) params.pageSize = query.pageSize.toString();
    if (query.search) params.search = query.search;
    if (query.sortBy) params.sortBy = query.sortBy;
    if (query.sortDirection) params.sortDirection = query.sortDirection;

    return params;
  }
}