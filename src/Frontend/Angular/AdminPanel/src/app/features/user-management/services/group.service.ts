import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  Group,
  CreateGroupRequest,
  UpdateGroupRequest,
  PagedResult,
  ListQuery
} from '../models/simple.models';

@Injectable({
  providedIn: 'root'
})
export class GroupService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiGateway}/api/groups`;

  // Get groups with pagination and search
  getGroups(query: ListQuery = {}): Observable<PagedResult<Group>> {
    const params = this.buildQueryParams(query);
    return this.http.get<PagedResult<Group>>(this.baseUrl, { params });
  }

  // Get single group by ID
  getGroup(id: string): Observable<Group> {
    return this.http.get<Group>(`${this.baseUrl}/${id}`);
  }

  // Create new group
  createGroup(request: CreateGroupRequest): Observable<Group> {
    return this.http.post<Group>(this.baseUrl, request);
  }

  // Update existing group
  updateGroup(request: UpdateGroupRequest): Observable<Group> {
    return this.http.put<Group>(`${this.baseUrl}/${request.id}`, request);
  }

  // Delete group
  deleteGroup(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  // Activate/Deactivate group
  toggleGroupStatus(id: string, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/status`, { isActive });
  }

  // Add users to group
  addUsersToGroup(groupId: string, userIds: string[]): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${groupId}/users`, { userIds });
  }

  // Remove users from group
  removeUsersFromGroup(groupId: string, userIds: string[]): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${groupId}/users`, {
      body: { userIds }
    });
  }

  // Get available colors for groups
  getAvailableColors(): string[] {
    return [
      '#007bff', // Blue
      '#28a745', // Green
      '#dc3545', // Red
      '#ffc107', // Yellow
      '#6f42c1', // Purple
      '#fd7e14', // Orange
      '#20c997', // Teal
      '#e83e8c', // Pink
      '#6c757d', // Gray
      '#17a2b8'  // Cyan
    ];
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