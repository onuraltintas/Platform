import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  User,
  Role,
  Permission,
  Group,
  UserQuery,
  RoleQuery,
  PermissionQuery,
  GroupQuery,
  CreateUserRequest,
  UpdateUserRequest,
  CreateRoleRequest,
  UpdateRoleRequest,
  CreateGroupRequest,
  UpdateGroupRequest,
  ChangePasswordRequest,
  ResetPasswordRequest,
  BulkUserOperationRequest,
  PagedResult,
  UserStatistics,
  RoleStatistics,
  PermissionStatistics,
  GroupStatistics,
  GroupAuditLog,
  PermissionCategory,
  PermissionAuditLog,
  RoleAuditLog,
  UserAuditLog
} from '../models/user-management.models';

@Injectable({
  providedIn: 'root'
})
export class UserManagementService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiGateway}${environment.endpoints.users}`;
  private readonly rolesUrl = `${environment.apiGateway}${environment.endpoints.roles}`;
  private readonly permissionsUrl = `${environment.apiGateway}${environment.endpoints.permissions}`;
  private readonly groupsUrl = `${environment.apiGateway}${environment.endpoints.groups}`;

  // User Operations
  getUsers(query?: UserQuery): Observable<PagedResult<User>> {
    let params = new HttpParams();

    if (query) {
      if (query.page) params = params.set('page', query.page.toString());
      if (query.pageSize) params = params.set('pageSize', query.pageSize.toString());
      if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);
      if (query.isActive !== undefined) params = params.set('isActive', query.isActive.toString());
      if (query.emailConfirmed !== undefined) params = params.set('emailConfirmed', query.emailConfirmed.toString());
      if (query.roleId) params = params.set('roleId', query.roleId);
      if (query.groupId) params = params.set('groupId', query.groupId);
      if (query.sortBy) params = params.set('sortBy', query.sortBy);
      if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    }

    return this.http.get<PagedResult<User>>(this.baseUrl, { params });
  }

  getUser(id: string): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/${id}`);
  }

  createUser(request: CreateUserRequest): Observable<User> {
    return this.http.post<User>(this.baseUrl, request);
  }

  updateUser(request: UpdateUserRequest): Observable<User> {
    return this.http.put<User>(`${this.baseUrl}/${request.id}`, request);
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${request.userId}/change-password`, request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${request.userId}/reset-password`, request);
  }

  lockUser(id: string, lockoutEnd?: Date): Observable<void> {
    const body = { lockoutEnd };
    return this.http.post<void>(`${this.baseUrl}/${id}/lock`, body);
  }

  unlockUser(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/unlock`, {});
  }

  confirmEmail(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/confirm-email`, {});
  }

  resendEmailConfirmation(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/resend-email-confirmation`, {});
  }

  getUserStatistics(): Observable<UserStatistics> {
    return this.http.get<UserStatistics>(`${this.baseUrl}/statistics`);
  }

  bulkUserOperation(request: BulkUserOperationRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/bulk-operation`, request);
  }

  // Role Operations
  getRoles(query?: RoleQuery): Observable<PagedResult<Role>> {
    let params = new HttpParams();

    if (query) {
      if (query.page) params = params.set('page', query.page.toString());
      if (query.pageSize) params = params.set('pageSize', query.pageSize.toString());
      if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);
      if (query.isActive !== undefined) params = params.set('isActive', query.isActive.toString());
      if (query.isDefault !== undefined) params = params.set('isDefault', query.isDefault.toString());
      if (query.sortBy) params = params.set('sortBy', query.sortBy);
      if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    }

    return this.http.get<PagedResult<Role>>(this.rolesUrl, { params });
  }

  getAllRoles(): Observable<Role[]> {
    return this.http.get<Role[]>(`${this.rolesUrl}/all`);
  }

  getRole(id: string): Observable<Role> {
    return this.http.get<Role>(`${this.rolesUrl}/${id}`);
  }

  createRole(request: CreateRoleRequest): Observable<Role> {
    return this.http.post<Role>(this.rolesUrl, request);
  }

  updateRole(request: UpdateRoleRequest): Observable<Role> {
    return this.http.put<Role>(`${this.rolesUrl}/${request.id}`, request);
  }

  deleteRole(id: string): Observable<void> {
    return this.http.delete<void>(`${this.rolesUrl}/${id}`);
  }

  getRoleUsers(id: string, query?: UserQuery): Observable<PagedResult<User>> {
    let params = new HttpParams();

    if (query) {
      if (query.page) params = params.set('page', query.page.toString());
      if (query.pageSize) params = params.set('pageSize', query.pageSize.toString());
      if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);
    }

    return this.http.get<PagedResult<User>>(`${this.rolesUrl}/${id}/users`, { params });
  }

  assignUsersToRole(roleId: string, userIds: string[]): Observable<void> {
    return this.http.post<void>(`${this.rolesUrl}/${roleId}/assign-users`, { userIds });
  }

  removeUsersFromRole(roleId: string, userIds: string[]): Observable<void> {
    return this.http.post<void>(`${this.rolesUrl}/${roleId}/remove-users`, { userIds });
  }

  getRoleStatistics(): Observable<RoleStatistics> {
    return this.http.get<RoleStatistics>(`${this.rolesUrl}/statistics`);
  }

  // Permission Operations
  getPermissions(query?: PermissionQuery): Observable<PagedResult<Permission>> {
    let params = new HttpParams();

    if (query) {
      if (query.page) params = params.set('page', query.page.toString());
      if (query.pageSize) params = params.set('pageSize', query.pageSize.toString());
      if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);
      if (query.category) params = params.set('category', query.category);
      if (query.isActive !== undefined) params = params.set('isActive', query.isActive.toString());
      if (query.sortBy) params = params.set('sortBy', query.sortBy);
      if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    }

    return this.http.get<PagedResult<Permission>>(this.permissionsUrl, { params });
  }

  getAllPermissions(): Observable<Permission[]> {
    return this.http.get<Permission[]>(`${this.permissionsUrl}/all`);
  }

  getPermission(id: string): Observable<Permission> {
    return this.http.get<Permission>(`${this.permissionsUrl}/${id}`);
  }

  getPermissionCategoriesBasic(): Observable<string[]> {
    return this.http.get<string[]>(`${this.permissionsUrl}/categories/basic`);
  }

  getPermissionStatisticsBasic(): Observable<PermissionStatistics> {
    return this.http.get<PermissionStatistics>(`${this.permissionsUrl}/statistics/basic`);
  }

  // Group Operations
  getGroups(query?: GroupQuery): Observable<PagedResult<Group>> {
    let params = new HttpParams();

    if (query) {
      if (query.page) params = params.set('page', query.page.toString());
      if (query.pageSize) params = params.set('pageSize', query.pageSize.toString());
      if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);
      if (query.isActive !== undefined) params = params.set('isActive', query.isActive.toString());
      if (query.sortBy) params = params.set('sortBy', query.sortBy);
      if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    }

    return this.http.get<PagedResult<Group>>(this.groupsUrl, { params });
  }

  getAllGroups(): Observable<Group[]> {
    return this.http.get<Group[]>(`${this.groupsUrl}/all`);
  }

  getGroup(id: string): Observable<Group> {
    return this.http.get<Group>(`${this.groupsUrl}/${id}`);
  }

  createGroup(request: CreateGroupRequest): Observable<Group> {
    return this.http.post<Group>(this.groupsUrl, request);
  }

  updateGroup(request: UpdateGroupRequest): Observable<Group> {
    return this.http.put<Group>(`${this.groupsUrl}/${request.id}`, request);
  }

  deleteGroup(id: string): Observable<void> {
    return this.http.delete<void>(`${this.groupsUrl}/${id}`);
  }

  getGroupUsers(id: string, query?: UserQuery): Observable<PagedResult<User>> {
    let params = new HttpParams();

    if (query) {
      if (query.page) params = params.set('page', query.page.toString());
      if (query.pageSize) params = params.set('pageSize', query.pageSize.toString());
      if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);
    }

    return this.http.get<PagedResult<User>>(`${this.groupsUrl}/${id}/users`, { params });
  }

  addUsersToGroup(groupId: string, userIds: string[]): Observable<void> {
    return this.http.post<void>(`${this.groupsUrl}/${groupId}/add-users`, { userIds });
  }

  removeUsersFromGroup(groupId: string, userIds: string[]): Observable<void> {
    return this.http.post<void>(`${this.groupsUrl}/${groupId}/remove-users`, { userIds });
  }

  addRolesToGroup(groupId: string, roleIds: string[]): Observable<void> {
    return this.http.post<void>(`${this.groupsUrl}/${groupId}/add-roles`, { roleIds });
  }

  removeRolesFromGroup(groupId: string, roleIds: string[]): Observable<void> {
    return this.http.post<void>(`${this.groupsUrl}/${groupId}/remove-roles`, { roleIds });
  }

  getGroupById(id: string): Observable<Group> {
    return this.http.get<Group>(`${this.groupsUrl}/${id}`);
  }

  getGroupStatistics(): Observable<GroupStatistics> {
    return this.http.get<GroupStatistics>(`${this.groupsUrl}/statistics`);
  }

  getGroupAuditLogs(groupId: string): Observable<GroupAuditLog[]> {
    return this.http.get<GroupAuditLog[]>(`${this.groupsUrl}/${groupId}/audit-logs`);
  }

  // Permission Category Operations
  getPermissionCategories(): Observable<PermissionCategory[]> {
    return this.http.get<PermissionCategory[]>(`${this.permissionsUrl}/categories`);
  }

  getPermissionById(id: string): Observable<Permission> {
    return this.http.get<Permission>(`${this.permissionsUrl}/${id}`);
  }

  getPermissionStatistics(): Observable<PermissionStatistics> {
    return this.http.get<PermissionStatistics>(`${this.permissionsUrl}/statistics`);
  }

  getPermissionAuditLogs(permissionId: string): Observable<PermissionAuditLog[]> {
    return this.http.get<PermissionAuditLog[]>(`${this.permissionsUrl}/${permissionId}/audit-logs`);
  }

  getChildPermissions(permissionId: string): Observable<Permission[]> {
    return this.http.get<Permission[]>(`${this.permissionsUrl}/${permissionId}/children`);
  }

  // Role Permission Operations
  getRolePermissions(roleId: string): Observable<Permission[]> {
    return this.http.get<Permission[]>(`${this.rolesUrl}/${roleId}/permissions`);
  }

  getRoleAuditLogs(roleId: string): Observable<RoleAuditLog[]> {
    return this.http.get<RoleAuditLog[]>(`${this.rolesUrl}/${roleId}/audit-logs`);
  }

  getUserAuditLogs(userId: string): Observable<UserAuditLog[]> {
    return this.http.get<UserAuditLog[]>(`${this.baseUrl}/${userId}/audit-logs`);
  }
}