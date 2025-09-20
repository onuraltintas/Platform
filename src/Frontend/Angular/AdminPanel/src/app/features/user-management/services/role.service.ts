import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  Role,
  CreateRoleRequest,
  UpdateRoleRequest,
  PagedResult,
  ListQuery
} from '../models/simple.models';

@Injectable({
  providedIn: 'root'
})
export class RoleService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiGateway}/api/roles`;

  // Get all roles (simple list)
  getRoles(query: ListQuery = {}): Observable<PagedResult<Role>> {
    const params = this.buildQueryParams(query);
    return this.http.get<PagedResult<Role>>(this.baseUrl, { params });
  }

  // Get single role by ID
  getRole(id: string): Observable<Role> {
    return this.http.get<Role>(`${this.baseUrl}/${id}`);
  }

  // Create new role
  createRole(request: CreateRoleRequest): Observable<Role> {
    return this.http.post<Role>(this.baseUrl, request);
  }

  // Update existing role
  updateRole(request: UpdateRoleRequest): Observable<Role> {
    return this.http.put<Role>(`${this.baseUrl}/${request.id}`, request);
  }

  // Delete role
  deleteRole(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
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