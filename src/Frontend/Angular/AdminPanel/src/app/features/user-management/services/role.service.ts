import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseUserManagementService } from './base-user-management.service';
import { environment } from '../../../../environments/environment';
import {
  RoleDto,
  CreateRoleRequest,
  UpdateRoleRequest,
  CloneRoleRequest,
  RolePermissionDiff,
  RoleStatistics,
  GetRolesRequest,
  RoleComparisonResult,
  PagedResponse,
  PermissionDto
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class RoleService extends BaseUserManagementService {
  private readonly apiPath = `${environment.endpoints.roles}`;

  /**
   * Get paginated roles list
   */
  getRoles(request: GetRolesRequest = {}): Observable<PagedResponse<RoleDto>> {
    const params = this.buildRoleParams(request);
    return this.get<PagedResponse<RoleDto>>(this.apiPath, params);
  }

  /**
   * Get all roles (no pagination) - for dropdowns
   */
  getAllRoles(): Observable<RoleDto[]> {
    return this.get<RoleDto[]>(`${this.apiPath}/all`);
  }

  /**
   * Get role by ID
   */
  getRole(id: string): Observable<RoleDto> {
    return this.get<RoleDto>(`${this.apiPath}/${id}`);
  }

  /**
   * Create new role
   */
  createRole(request: CreateRoleRequest): Observable<RoleDto> {
    return this.post<RoleDto>(
      this.apiPath,
      request,
      'Rol başarıyla oluşturuldu'
    );
  }

  /**
   * Update role
   */
  updateRole(id: string, request: UpdateRoleRequest): Observable<RoleDto> {
    return this.put<RoleDto>(
      `${this.apiPath}/${id}`,
      request,
      'Rol başarıyla güncellendi'
    );
  }

  /**
   * Delete role
   */
  deleteRole(id: string): Observable<void> {
    return this.delete<void>(
      `${this.apiPath}/${id}`,
      'Rol başarıyla silindi'
    );
  }

  /**
   * Clone role with new permissions
   */
  cloneRole(request: CloneRoleRequest): Observable<RoleDto> {
    return this.post<RoleDto>(
      `${this.apiPath}/${request.sourceRoleId}/clone`,
      {
        newRoleName: request.newRoleName,
        newDisplayName: request.newDisplayName,
        description: request.description,
        permissionIds: request.permissionIds
      },
      'Rol başarıyla kopyalandı'
    );
  }

  /**
   * Compare two roles
   */
  compareRoles(roleId1: string, roleId2: string): Observable<RoleComparisonResult> {
    return this.get<RoleComparisonResult>(`${this.apiPath}/compare/${roleId1}/${roleId2}`);
  }

  /**
   * Get role permissions
   */
  getRolePermissions(roleId: string): Observable<PermissionDto[]> {
    return this.get<PermissionDto[]>(`${this.apiPath}/${roleId}/permissions`);
  }

  /**
   * Assign permissions to role
   */
  assignPermissions(roleId: string, permissionIds: string[]): Observable<void> {
    return this.post<void>(
      `${this.apiPath}/${roleId}/permissions`,
      { permissionIds },
      'Yetkiler başarıyla atandı'
    );
  }

  /**
   * Remove permissions from role
   */
  removePermissions(roleId: string, permissionIds: string[]): Observable<void> {
    return this.delete<void>(
      `${this.apiPath}/${roleId}/permissions?${permissionIds.map(id => `permissionIds=${id}`).join('&')}`
    );
  }

  /**
   * Get permission diff for role clone preview
   */
  getPermissionDiff(sourceRoleId: string, targetPermissionIds: string[]): Observable<RolePermissionDiff> {
    return this.post<RolePermissionDiff>(
      `${this.apiPath}/${sourceRoleId}/permission-diff`,
      { permissionIds: targetPermissionIds }
    );
  }

  /**
   * Get role statistics
   */
  getRoleStatistics(): Observable<RoleStatistics> {
    return this.get<RoleStatistics>(`${this.apiPath}/statistics`);
  }

  /**
   * Search roles (lightweight for autocomplete)
   */
  searchRoles(query: string, limit: number = 10): Observable<RoleDto[]> {
    return this.get<RoleDto[]>(`${this.apiPath}/search`, {
      q: query,
      limit
    });
  }

  /**
   * Get system roles
   */
  getSystemRoles(): Observable<RoleDto[]> {
    return this.get<RoleDto[]>(`${this.apiPath}/system`);
  }

  /**
   * Get custom roles (non-system)
   */
  getCustomRoles(): Observable<RoleDto[]> {
    return this.get<RoleDto[]>(`${this.apiPath}/custom`);
  }

  /**
   * Get roles with user count
   */
  getRolesWithUserCount(): Observable<Array<RoleDto & { userCount: number }>> {
    return this.get<Array<RoleDto & { userCount: number }>>(`${this.apiPath}/with-user-count`);
  }

  /**
   * Export roles to Excel
   */
  exportRoles(filter?: GetRolesRequest): Observable<Blob> {
    const params = filter ? this.buildRoleParams(filter) : {};
    return this.downloadFile(`${this.apiPath}/export`, 'roles.xlsx', params);
  }

  /**
   * Bulk assign permissions to multiple roles
   */
  bulkAssignPermissions(roleIds: string[], permissionIds: string[]): Observable<void> {
    return this.post<void>(
      `${this.apiPath}/bulk/assign-permissions`,
      { roleIds, permissionIds },
      'Yetkiler toplu olarak atandı'
    );
  }

  /**
   * Bulk remove permissions from multiple roles
   */
  bulkRemovePermissions(roleIds: string[], permissionIds: string[]): Observable<void> {
    return this.post<void>(
      `${this.apiPath}/bulk/remove-permissions`,
      { roleIds, permissionIds },
      'Yetkiler toplu olarak kaldırıldı'
    );
  }

  /**
   * Get role usage analytics
   */
  getRoleUsageAnalytics(): Observable<Array<{ roleId: string; roleName: string; userCount: number; lastUsed: Date }>> {
    return this.get<Array<{ roleId: string; roleName: string; userCount: number; lastUsed: Date }>>(`${this.apiPath}/usage-analytics`);
  }

  /**
   * Validate role name uniqueness
   */
  validateRoleName(name: string, excludeId?: string): Observable<{ isUnique: boolean }> {
    const params: Record<string, any> = { name };
    if (excludeId) params['excludeId'] = excludeId;

    return this.get<{ isUnique: boolean }>(`${this.apiPath}/validate-name`, params);
  }

  /**
   * Build query parameters for role requests
   */
  private buildRoleParams(request: GetRolesRequest): Record<string, any> {
    const params: Record<string, any> = {};

    if (request['page'] !== undefined) params['page'] = request['page'];
    if (request['pageSize'] !== undefined) params['pageSize'] = request['pageSize'];
    if (request['search']) params['search'] = request['search'];
    if (request['includeSystemRoles'] !== undefined) params['includeSystemRoles'] = request['includeSystemRoles'];
    if (request['hasUsers'] !== undefined) params['hasUsers'] = request['hasUsers'];
    if (request['sortBy']) params['sortBy'] = request['sortBy'];
    if (request['sortDirection']) params['sortDirection'] = request['sortDirection'];

    return params;
  }

  /**
   * Update role permissions
   */
  updateRolePermissions(roleId: string, updates: {
    addPermissions?: string[];
    removePermissions?: string[];
  }): Observable<void> {
    return this.put<void>(
      `${this.apiPath}/${roleId}/permissions`,
      updates,
      'Rol yetkileri başarıyla güncellendi'
    );
  }
}