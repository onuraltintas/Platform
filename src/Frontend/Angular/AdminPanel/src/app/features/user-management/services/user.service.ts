import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { BaseUserManagementService } from './base-user-management.service';
import {
  UserDto,
  CreateUserRequest,
  UpdateUserRequest,
  GetUsersRequest,
  BulkUserOperation,
  UserStatistics,
  PagedResponse,
  RoleDto,
  GroupDto
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class UserService extends BaseUserManagementService {
  private readonly apiPath = '/users';

  /**
   * Get paginated users list
   */
  getUsers(request: GetUsersRequest = {}): Observable<PagedResponse<UserDto>> {
    const params = this.buildUserParams(request);
    return this.get<PagedResponse<UserDto>>(this.apiPath, params);
  }

  /**
   * Get user by ID
   */
  getUser(id: string): Observable<UserDto> {
    return this.get<UserDto>(`${this.apiPath}/${id}`);
  }

  /**
   * Create new user
   */
  createUser(request: CreateUserRequest): Observable<UserDto> {
    return this.post<UserDto>(
      this.apiPath,
      request,
      'Kullanıcı başarıyla oluşturuldu'
    );
  }

  /**
   * Update user
   */
  updateUser(id: string, request: UpdateUserRequest): Observable<UserDto> {
    return this.put<UserDto>(
      `${this.apiPath}/${id}`,
      request,
      'Kullanıcı başarıyla güncellendi'
    );
  }

  /**
   * Delete user
   */
  deleteUser(id: string): Observable<void> {
    return this.delete<void>(
      `${this.apiPath}/${id}`,
      'Kullanıcı başarıyla silindi'
    );
  }

  /**
   * Get user roles
   */
  getUserRoles(userId: string): Observable<RoleDto[]> {
    return this.get<RoleDto[]>(`${this.apiPath}/${userId}/roles`);
  }

  /**
   * Assign roles to user
   */
  assignRoles(userId: string, roleIds: string[]): Observable<void> {
    return this.post<void>(
      `${this.apiPath}/${userId}/roles`,
      { roleIds },
      'Roller başarıyla atandı'
    );
  }

  /**
   * Remove roles from user
   */
  removeRoles(userId: string, roleIds: string[]): Observable<void> {
    return this.delete<void>(
      `${this.apiPath}/${userId}/roles?${roleIds.map(id => `roleIds=${id}`).join('&')}`
    );
  }

  /**
   * Get user groups
   */
  getUserGroups(userId: string): Observable<GroupDto[]> {
    return this.get<GroupDto[]>(`${this.apiPath}/${userId}/groups`);
  }

  /**
   * Assign groups to user
   */
  assignGroups(userId: string, groupIds: string[]): Observable<void> {
    return this.post<void>(
      `${this.apiPath}/${userId}/groups`,
      { groupIds },
      'Gruplar başarıyla atandı'
    );
  }

  /**
   * Remove groups from user
   */
  removeGroups(userId: string, groupIds: string[]): Observable<void> {
    return this.delete<void>(
      `${this.apiPath}/${userId}/groups?${groupIds.map(id => `groupIds=${id}`).join('&')}`
    );
  }

  /**
   * Get user permissions (resolved from roles and groups)
   */
  getUserPermissions(userId: string): Observable<string[]> {
    return this.get<{ permissions: string[] }>(`${this.apiPath}/${userId}/permissions`)
      .pipe(map(response => response.permissions));
  }

  /**
   * Bulk operations on users
   */
  bulkOperation(operation: BulkUserOperation): Observable<void> {
    return this.post<void>(
      `${this.apiPath}/bulk`,
      operation,
      'Toplu işlem başarıyla tamamlandı'
    );
  }

  /**
   * Change user password
   */
  changePassword(userId: string, newPassword: string): Observable<void> {
    return this.post<void>(
      `${this.apiPath}/${userId}/change-password`,
      { newPassword },
      'Şifre başarıyla değiştirildi'
    );
  }

  /**
   * Reset user password
   */
  resetPassword(userId: string): Observable<{ temporaryPassword: string }> {
    return this.post<{ temporaryPassword: string }>(
      `${this.apiPath}/${userId}/reset-password`,
      {},
      'Şifre sıfırlama e-postası gönderildi'
    );
  }

  /**
   * Lock/unlock user account
   */
  setLockStatus(userId: string, locked: boolean): Observable<void> {
    const action = locked ? 'lock' : 'unlock';
    const message = locked ? 'Hesap kilitlendi' : 'Hesap kilidi açıldı';

    return this.post<void>(
      `${this.apiPath}/${userId}/${action}`,
      {},
      message
    );
  }

  /**
   * Export users to Excel
   */
  exportUsers(filter?: GetUsersRequest): Observable<Blob> {
    const params = filter ? this.buildUserParams(filter) : {};
    return this.downloadFile(`${this.apiPath}/export`, 'users.xlsx', params);
  }

  /**
   * Get user statistics
   */
  getUserStatistics(): Observable<UserStatistics> {
    return this.get<UserStatistics>(`${this.apiPath}/statistics`);
  }

  /**
   * Get user statistics (alias for backward compatibility)
   */
  getStatistics(): Observable<UserStatistics> {
    return this.getUserStatistics();
  }

  /**
   * Search users (lightweight for autocomplete)
   */
  searchUsers(query: string, limit: number = 10): Observable<UserDto[]> {
    return this.get<UserDto[]>(`${this.apiPath}/search`, {
      q: query,
      limit
    });
  }

  /**
   * Get recently active users
   */
  getRecentlyActiveUsers(limit: number = 10): Observable<UserDto[]> {
    return this.get<UserDto[]>(`${this.apiPath}/recent`, { limit });
  }

  /**
   * Send email verification
   */
  sendEmailVerification(userId: string): Observable<void> {
    return this.post<void>(
      `${this.apiPath}/${userId}/send-verification-email`,
      {},
      'Doğrulama e-postası gönderildi'
    );
  }

  /**
   * Enable/disable two-factor authentication
   */
  setTwoFactorAuth(userId: string, enabled: boolean): Observable<void> {
    const message = enabled
      ? 'İki faktörlü doğrulama etkinleştirildi'
      : 'İki faktörlü doğrulama devre dışı bırakıldı';

    return this.post<void>(
      `${this.apiPath}/${userId}/two-factor`,
      { enabled },
      message
    );
  }

  /**
   * Get available users for group membership (excluding specified users)
   */
  getAvailableUsers(options: {
    search?: string;
    excludeUserIds?: string[];
    page?: number;
    pageSize?: number;
    isActive?: boolean;
  } = {}): Observable<{
    data: UserDto[];
    totalCount: number;
  }> {
    const params: Record<string, any> = {};

    if (options['search']) params['search'] = options['search'];
    if (options['excludeUserIds']?.length) params['excludeUserIds'] = options['excludeUserIds'].join(',');
    if (options['page'] !== undefined) params['page'] = options['page'];
    if (options['pageSize'] !== undefined) params['pageSize'] = options['pageSize'];
    if (options['isActive'] !== undefined) params['isActive'] = options['isActive'];

    return this.get<{
      data: UserDto[];
      totalCount: number;
    }>(`${this.apiPath}/available`, params);
  }

  /**
   * Build query parameters for user requests
   */
  private buildUserParams(request: GetUsersRequest): Record<string, any> {
    const params: Record<string, any> = {};

    if (request.page !== undefined) params['page'] = request.page;
    if (request.pageSize !== undefined) params['pageSize'] = request.pageSize;
    if (request.search) params['search'] = request.search;
    if (request.isActive !== undefined) params['isActive'] = request.isActive;
    if (request.roleIds?.length) params['roleIds'] = request.roleIds;
    if (request.groupIds?.length) params['groupIds'] = request.groupIds;
    if (request.createdFrom) params['createdFrom'] = this.formatDate(request.createdFrom);
    if (request.createdTo) params['createdTo'] = this.formatDate(request.createdTo);
    if (request.lastLoginFrom) params['lastLoginFrom'] = this.formatDate(request.lastLoginFrom);
    if (request.lastLoginTo) params['lastLoginTo'] = this.formatDate(request.lastLoginTo);
    if (request.sortBy) params['sortBy'] = request.sortBy;
    if (request.sortDirection) params['sortDirection'] = request.sortDirection;

    return params;
  }

  /**
   * Download import template
   */
  downloadImportTemplate(): Observable<Blob> {
    return this.downloadFile(`${this.apiPath}/import-template`, 'user-import-template.xlsx');
  }
}