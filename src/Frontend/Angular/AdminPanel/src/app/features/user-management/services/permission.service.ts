import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseUserManagementService } from './base-user-management.service';
import { environment } from '../../../../environments/environment';
import {
  PermissionDto,
  PermissionMatrixItem,
  PermissionsByService,
  GetPermissionsRequest,
  PermissionStatistics,
  CreatePermissionRequest,
  UpdatePermissionRequest,
  BulkPermissionAssignment,
  PagedResponse
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class PermissionService extends BaseUserManagementService {
  private readonly apiPath = `${environment.endpoints.permissions}`;

  /**
   * Get paginated permissions list
   */
  getPermissions(request: GetPermissionsRequest = {}): Observable<PagedResponse<PermissionDto>> {
    const params = this.buildPermissionParams(request);
    return this.get<PagedResponse<PermissionDto>>(this.apiPath, params);
  }

  /**
   * Get all permissions (no pagination) - for dropdowns
   */
  getAllPermissions(): Observable<PermissionDto[]> {
    return this.get<PermissionDto[]>(`${this.apiPath}/all`);
  }

  /**
   * Get permission by ID
   */
  getPermission(id: string): Observable<PermissionDto> {
    return this.get<PermissionDto>(`${this.apiPath}/${id}`);
  }

  /**
   * Create new permission
   */
  createPermission(request: CreatePermissionRequest): Observable<PermissionDto> {
    return this.post<PermissionDto>(
      this.apiPath,
      request,
      'Yetki başarıyla oluşturuldu'
    );
  }

  /**
   * Update permission
   */
  updatePermission(id: string, request: UpdatePermissionRequest): Observable<PermissionDto> {
    return this.put<PermissionDto>(
      `${this.apiPath}/${id}`,
      request,
      'Yetki başarıyla güncellendi'
    );
  }

  /**
   * Delete permission
   */
  deletePermission(id: string): Observable<void> {
    return this.delete<void>(
      `${this.apiPath}/${id}`,
      'Yetki başarıyla silindi'
    );
  }

  /**
   * Get permission matrix (permissions vs roles)
   */
  getPermissionMatrix(): Observable<PermissionMatrixItem[]> {
    return this.get<PermissionMatrixItem[]>(`${this.apiPath}/matrix`);
  }

  /**
   * Get permissions grouped by service
   */
  getPermissionsByService(): Observable<PermissionsByService[]> {
    return this.get<PermissionsByService[]>(`${this.apiPath}/by-service`);
  }

  /**
   * Get permissions grouped by resource
   */
  getPermissionsByResource(): Observable<PermissionsByService[]> {
    return this.get<PermissionsByService[]>(`${this.apiPath}/by-resource`);
  }

  /**
   * Get permissions by category
   */
  getPermissionsByCategory(): Observable<Record<string, PermissionDto[]>> {
    return this.get<Record<string, PermissionDto[]>>(`${this.apiPath}/by-category`);
  }

  /**
   * Get all available services
   */
  getServices(): Observable<string[]> {
    return this.get<string[]>(`${this.apiPath}/services`);
  }

  /**
   * Get resources for a specific service
   */
  getResourcesByService(service: string): Observable<string[]> {
    return this.get<string[]>(`${this.apiPath}/services/${service}/resources`);
  }

  /**
   * Get actions for a specific resource
   */
  getActionsByResource(service: string, resource: string): Observable<string[]> {
    return this.get<string[]>(`${this.apiPath}/services/${service}/resources/${resource}/actions`);
  }

  /**
   * Get all available categories
   */
  getCategories(): Observable<string[]> {
    return this.get<string[]>(`${this.apiPath}/categories`);
  }

  /**
   * Search permissions (lightweight for autocomplete)
   */
  searchPermissions(query: string, limit: number = 10): Observable<PermissionDto[]> {
    return this.get<PermissionDto[]>(`${this.apiPath}/search`, {
      q: query,
      limit
    });
  }

  /**
   * Get system permissions
   */
  getSystemPermissions(): Observable<PermissionDto[]> {
    return this.get<PermissionDto[]>(`${this.apiPath}/system`);
  }

  /**
   * Get custom permissions (non-system)
   */
  getCustomPermissions(): Observable<PermissionDto[]> {
    return this.get<PermissionDto[]>(`${this.apiPath}/custom`);
  }

  /**
   * Get unassigned permissions (not assigned to any role)
   */
  getUnassignedPermissions(): Observable<PermissionDto[]> {
    return this.get<PermissionDto[]>(`${this.apiPath}/unassigned`);
  }

  /**
   * Get permission statistics
   */
  getPermissionStatistics(): Observable<PermissionStatistics> {
    return this.get<PermissionStatistics>(`${this.apiPath}/statistics`);
  }

  /**
   * Bulk assign permissions to roles
   */
  bulkAssignToRoles(assignment: BulkPermissionAssignment): Observable<void> {
    const message = assignment.operation === 'assign'
      ? 'Yetkiler toplu olarak atandı'
      : 'Yetkiler toplu olarak kaldırıldı';

    return this.post<void>(
      `${this.apiPath}/bulk/assign-to-roles`,
      assignment,
      message
    );
  }

  /**
   * Export permissions to Excel
   */
  exportPermissions(filter?: GetPermissionsRequest): Observable<Blob> {
    const params = filter ? this.buildPermissionParams(filter) : {};
    return this.downloadFile(`${this.apiPath}/export`, 'permissions.xlsx', params);
  }

  /**
   * Import permissions from Excel
   */
  importPermissions(file: File): Observable<{ imported: number; skipped: number; errors: string[] }> {
    const formData = new FormData();
    formData.append('file', file);

    return this.post<{ imported: number; skipped: number; errors: string[] }>(
      `${this.apiPath}/import`,
      formData,
      'Yetkiler başarıyla içe aktarıldı'
    );
  }

  /**
   * Get permission dependencies (permissions that depend on this one)
   */
  getPermissionDependencies(permissionId: string): Observable<PermissionDto[]> {
    return this.get<PermissionDto[]>(`${this.apiPath}/${permissionId}/dependencies`);
  }

  /**
   * Get permission usage analytics
   */
  getPermissionUsageAnalytics(): Observable<Array<{
    permissionId: string;
    permissionName: string;
    roleCount: number;
    userCount: number;
    lastUsed: Date;
  }>> {
    return this.get<Array<{
      permissionId: string;
      permissionName: string;
      roleCount: number;
      userCount: number;
      lastUsed: Date;
    }>>(`${this.apiPath}/usage-analytics`);
  }

  /**
   * Validate permission name uniqueness
   */
  validatePermissionName(name: string, excludeId?: string): Observable<{ isUnique: boolean }> {
    const params: Record<string, any> = { name };
    if (excludeId) params['excludeId'] = excludeId;

    return this.get<{ isUnique: boolean }>(`${this.apiPath}/validate-name`, params);
  }

  /**
   * Discover permissions from services
   */
  discoverPermissions(): Observable<{ discovered: number; existing: number }> {
    return this.post<{ discovered: number; existing: number }>(
      '/permission-discovery/discover',
      {},
      'Yetki keşfi tamamlandı'
    );
  }

  /**
   * Get discovered permissions preview
   */
  getDiscoveredPermissionsPreview(): Observable<PermissionDto[]> {
    return this.get<PermissionDto[]>('/permission-discovery/preview');
  }

  /**
   * Sync permissions with services
   */
  syncPermissions(): Observable<{ synced: number; removed: number }> {
    return this.post<{ synced: number; removed: number }>(
      '/permission-discovery/sync',
      {},
      'Yetki senkronizasyonu tamamlandı'
    );
  }

  /**
   * Build query parameters for permission requests
   */
  private buildPermissionParams(request: GetPermissionsRequest): Record<string, any> {
    const params: Record<string, any> = {};

    if (request['page'] !== undefined) params['page'] = request['page'];
    if (request['pageSize'] !== undefined) params['pageSize'] = request['pageSize'];
    if (request['search']) params['search'] = request['search'];
    if (request['service']) params['service'] = request['service'];
    if (request['resource']) params['resource'] = request['resource'];
    if (request['action']) params['action'] = request['action'];
    if (request['category']) params['category'] = request['category'];
    if (request['includeSystemPermissions'] !== undefined) params['includeSystemPermissions'] = request['includeSystemPermissions'];
    if (request['sortBy']) params['sortBy'] = request['sortBy'];
    if (request['sortDirection']) params['sortDirection'] = request['sortDirection'];

    return params;
  }

  /**
   * Get permissions with usage statistics
   */
  getPermissionsWithUsage(): Observable<Array<PermissionDto & {
    roleCount: number;
    userCount: number;
    riskLevel: 'low' | 'medium' | 'high' | 'critical';
    lastUsed?: Date;
    usageFrequency: number;
  }>> {
    return this.get<Array<PermissionDto & {
      roleCount: number;
      userCount: number;
      riskLevel: 'low' | 'medium' | 'high' | 'critical';
      lastUsed?: Date;
      usageFrequency: number;
    }>>(`${this.apiPath}/with-usage`);
  }

  /**
   * Export service report
   */
  exportServiceReport(filters: any): Observable<Blob> {
    return this.downloadFile(`${this.apiPath}/service-report`, 'service-report.xlsx', filters);
  }

  /**
   * Export service permissions
   */
  exportServicePermissions(serviceName: string): Observable<Blob> {
    return this.downloadFile(`${this.apiPath}/service/${serviceName}/export`, `${serviceName}-permissions.xlsx`);
  }

  /**
   * Generate risk analysis
   */
  generateRiskAnalysis(): Observable<{
    totalPermissions: number;
    riskBreakdown: Record<string, number>;
    recommendations: string[];
  }> {
    return this.get<{
      totalPermissions: number;
      riskBreakdown: Record<string, number>;
      recommendations: string[];
    }>(`${this.apiPath}/risk-analysis`);
  }

  /**
   * Generate usage report
   */
  generateUsageReport(): Observable<{
    totalUsage: number;
    usageBreakdown: Array<{
      permission: string;
      usageCount: number;
      frequency: number;
    }>;
  }> {
    return this.get<{
      totalUsage: number;
      usageBreakdown: Array<{
        permission: string;
        usageCount: number;
        frequency: number;
      }>;
    }>(`${this.apiPath}/usage-report`);
  }

  /**
   * Export all services
   */
  exportAllServices(): Observable<Blob> {
    return this.downloadFile(`${this.apiPath}/export-all-services`, 'all-services.xlsx');
  }

  /**
   * Export permission matrix
   */
  exportMatrix(data: any): Observable<Blob> {
    return this.downloadFile(`${this.apiPath}/export-matrix`, 'permission-matrix.xlsx', data);
  }
}