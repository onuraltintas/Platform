import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { BaseUserManagementService } from './base-user-management.service';
import { environment } from '../../../../environments/environment';
import {
  GroupDto,
  GroupMemberDto,
  CreateGroupRequest,
  UpdateGroupRequest,
  GroupMemberRequest,
  BulkGroupMemberOperation,
  GetGroupsRequest,
  GroupStatistics,
  GroupPermissionAssignment,
  PagedResponse,
  PermissionDto,
  UserDto
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class GroupService extends BaseUserManagementService {
  private readonly apiPath = `${environment.endpoints.groups}`;

  /**
   * Get paginated groups list
   */
  getGroups(request: GetGroupsRequest = {}): Observable<PagedResponse<GroupDto>> {
    // Simplified params for backend compatibility
    const params: Record<string, any> = {};
    if (request.page) params['page'] = request.page;
    if (request.pageSize) params['pageSize'] = request.pageSize;
    if (request.search) params['search'] = request.search;

    return this.get<PagedResponse<GroupDto>>(this.apiPath, params);
  }

  /**
   * Get all groups (no pagination) - for dropdowns
   */
  getAllGroups(): Observable<GroupDto[]> {
    return this.get<GroupDto[]>(`${this.apiPath}/all`);
  }

  /**
   * Get group statistics
   */
  getGroupStatistics(): Observable<GroupStatistics> {
    return this.get<GroupStatistics>(`${this.apiPath}/statistics`).pipe(
      catchError(() => {
        // Return default statistics if endpoint not available
        const defaultStats: GroupStatistics = {
          totalGroups: 0,
          systemGroups: 0,
          customGroups: 0,
          totalMembers: 0,
          averageMembersPerGroup: 0,
          largestGroup: null as any,
          emptyGroups: 0
        };
        return of(defaultStats);
      })
    );
  }

  /**
   * Get group by ID
   */
  getGroup(id: string): Observable<GroupDto> {
    return this.get<GroupDto>(`${this.apiPath}/${id}`);
  }

  /**
   * Create new group
   */
  createGroup(request: CreateGroupRequest): Observable<GroupDto> {
    return this.post<GroupDto>(
      this.apiPath,
      request,
      'Grup başarıyla oluşturuldu'
    );
  }

  /**
   * Update group
   */
  updateGroup(id: string, request: UpdateGroupRequest): Observable<GroupDto> {
    return this.put<GroupDto>(
      `${this.apiPath}/${id}`,
      request,
      'Grup başarıyla güncellendi'
    );
  }

  /**
   * Delete group
   */
  deleteGroup(id: string): Observable<void> {
    return this.delete<void>(
      `${this.apiPath}/${id}`,
      'Grup başarıyla silindi'
    );
  }

  /**
   * Get group members
   */
  getGroupMembers(groupId: string): Observable<GroupMemberDto[]> {
    return this.get<GroupMemberDto[]>(`${this.apiPath}/${groupId}/members`);
  }

  /**
   * Add member to group
   */
  addMember(groupId: string, request: GroupMemberRequest): Observable<void> {
    return this.post<void>(
      `${this.apiPath}/${groupId}/members`,
      request,
      'Üye başarıyla eklendi'
    );
  }

  /**
   * Remove member from group
   */
  removeMember(groupId: string, userId: string): Observable<void> {
    return this.delete<void>(
      `${this.apiPath}/${groupId}/members/${userId}`,
      'Üye başarıyla kaldırıldı'
    );
  }

  /**
   * Update member role in group
   */
  updateMemberRole(groupId: string, userId: string, role: string): Observable<void> {
    return this.put<void>(
      `${this.apiPath}/${groupId}/members/${userId}/role`,
      { role },
      'Üye rolü başarıyla güncellendi'
    );
  }

  /**
   * Bulk operations on group members
   */
  bulkMemberOperation(groupId: string, operation: BulkGroupMemberOperation): Observable<void> {
    return this.post<void>(
      `${this.apiPath}/${groupId}/members/bulk`,
      operation,
      'Toplu işlem başarıyla tamamlandı'
    );
  }

  /**
   * Get group permissions
   */
  getGroupPermissions(groupId: string): Observable<PermissionDto[]> {
    return this.get<PermissionDto[]>(`${this.apiPath}/${groupId}/permissions`);
  }

  /**
   * Assign permissions to group
   */
  assignPermissions(assignment: GroupPermissionAssignment): Observable<void> {
    const message = assignment.operation === 'assign'
      ? 'Yetkiler başarıyla atandı'
      : 'Yetkiler başarıyla kaldırıldı';

    return this.post<void>(
      `${this.apiPath}/${assignment.groupId}/permissions`,
      {
        permissionIds: assignment.permissionIds,
        operation: assignment.operation
      },
      message
    );
  }

  /**
   * Get users not in group (for adding members)
   */
  getAvailableUsers(groupId: string, search?: string): Observable<UserDto[]> {
    const params: Record<string, any> = {};
    if (search) params['search'] = search;

    return this.get<UserDto[]>(`${this.apiPath}/${groupId}/available-users`, params);
  }

  /**
   * Search groups (lightweight for autocomplete)
   */
  searchGroups(query: string, limit: number = 10): Observable<GroupDto[]> {
    return this.get<GroupDto[]>(`${this.apiPath}/search`, {
      q: query,
      limit
    });
  }

  /**
   * Get system groups
   */
  getSystemGroups(): Observable<GroupDto[]> {
    return this.get<GroupDto[]>(`${this.apiPath}/system`);
  }

  /**
   * Get custom groups (non-system)
   */
  getCustomGroups(): Observable<GroupDto[]> {
    return this.get<GroupDto[]>(`${this.apiPath}/custom`);
  }

  /**
   * Get groups with member count
   */
  getGroupsWithMemberCount(): Observable<Array<GroupDto & { memberCount: number }>> {
    return this.get<Array<GroupDto & { memberCount: number }>>(`${this.apiPath}/with-member-count`);
  }


  /**
   * Export groups to Excel
   */
  exportGroups(filter?: GetGroupsRequest): Observable<Blob> {
    const params = filter ? this.buildGroupParams(filter) : {};
    return this.downloadFile(`${this.apiPath}/export`, 'groups.xlsx', params);
  }

  /**
   * Export group members to Excel
   */
  exportGroupMembers(groupId: string): Observable<Blob> {
    return this.downloadFile(`${this.apiPath}/${groupId}/members/export`, `group-${groupId}-members.xlsx`);
  }

  /**
   * Get group hierarchy (if groups have parent-child relationships)
   */
  getGroupHierarchy(): Observable<Array<GroupDto & { children: GroupDto[] }>> {
    return this.get<Array<GroupDto & { children: GroupDto[] }>>(`${this.apiPath}/hierarchy`);
  }

  /**
   * Get user's groups
   */
  getUserGroups(userId: string): Observable<GroupDto[]> {
    return this.get<GroupDto[]>(`${this.apiPath}/user/${userId}`);
  }

  /**
   * Get group activity log
   */
  getGroupActivityLog(groupId: string): Observable<Array<{
    id: string;
    action: string;
    userId: string;
    userName: string;
    timestamp: Date;
    details: string;
  }>> {
    return this.get<Array<{
      id: string;
      action: string;
      userId: string;
      userName: string;
      timestamp: Date;
      details: string;
    }>>(`${this.apiPath}/${groupId}/activity-log`);
  }

  /**
   * Invite users to group via email
   */
  inviteUsers(groupId: string, emails: string[], message?: string): Observable<{ sent: number; failed: string[] }> {
    return this.post<{ sent: number; failed: string[] }>(
      `${this.apiPath}/${groupId}/invite`,
      { emails, message },
      'Davetiyeler başarıyla gönderildi'
    );
  }

  /**
   * Get pending group invitations
   */
  getPendingInvitations(groupId: string): Observable<Array<{
    id: string;
    email: string;
    invitedAt: Date;
    expiresAt: Date;
    status: string;
  }>> {
    return this.get<Array<{
      id: string;
      email: string;
      invitedAt: Date;
      expiresAt: Date;
      status: string;
    }>>(`${this.apiPath}/${groupId}/invitations`);
  }

  /**
   * Cancel group invitation
   */
  cancelInvitation(groupId: string, invitationId: string): Observable<void> {
    return this.delete<void>(
      `${this.apiPath}/${groupId}/invitations/${invitationId}`,
      'Davetiye iptal edildi'
    );
  }

  /**
   * Validate group name uniqueness
   */
  validateGroupName(name: string, excludeId?: string): Observable<{ isUnique: boolean }> {
    const params: Record<string, any> = { name };
    if (excludeId) params['excludeId'] = excludeId;

    return this.get<{ isUnique: boolean }>(`${this.apiPath}/validate-name`, params);
  }

  /**
   * Add multiple members to group
   */
  addMembersToGroup(groupId: string, requests: GroupMemberRequest[]): Observable<void> {
    return this.post<void>(
      `${this.apiPath}/${groupId}/members/bulk-add`,
      { members: requests },
      'Üyeler başarıyla eklendi'
    );
  }

  /**
   * Remove multiple members from group
   */
  removeMembersFromGroup(groupId: string, userIds: string[]): Observable<void> {
    return this.post<void>(
      `${this.apiPath}/${groupId}/members/bulk-remove`,
      { userIds },
      'Üyeler başarıyla kaldırıldı'
    );
  }

  /**
   * Change group member role
   */
  changeGroupMemberRole(groupId: string, userId: string, newRole: string): Observable<void> {
    return this.put<void>(
      `${this.apiPath}/${groupId}/members/${userId}/role`,
      { role: newRole },
      'Üye rolü başarıyla değiştirildi'
    );
  }

  /**
   * Toggle group member status (active/inactive)
   */
  toggleGroupMemberStatus(groupId: string, userId: string): Observable<void> {
    return this.post<void>(
      `${this.apiPath}/${groupId}/members/${userId}/toggle-status`,
      {},
      'Üye durumu başarıyla değiştirildi'
    );
  }

  /**
   * Bulk toggle member status
   */
  bulkToggleGroupMemberStatus(groupId: string, userIds: string[], activate: boolean): Observable<void> {
    return this.post<void>(
      `${this.apiPath}/${groupId}/members/bulk-status`,
      { userIds, activate },
      `Üyeler başarıyla ${activate ? 'aktifleştirildi' : 'pasifleştirildi'}`
    );
  }

  /**
   * Get group member statistics
   */
  getGroupMemberStatistics(groupId: string): Observable<{
    totalMembers: number;
    membersByRole: Record<string, number>;
    activeMembers: number;
    inactiveMembers: number;
    recentJoins: number;
  }> {
    return this.get<{
      totalMembers: number;
      membersByRole: Record<string, number>;
      activeMembers: number;
      inactiveMembers: number;
      recentJoins: number;
    }>(`${this.apiPath}/${groupId}/member-statistics`);
  }

  /**
   * Assign permissions to group
   */
  assignPermissionsToGroup(assignment: GroupPermissionAssignment): Observable<void> {
    const message = assignment.operation === 'assign'
      ? 'İzinler başarıyla atandı'
      : 'İzinler başarıyla kaldırıldı';

    return this.post<void>(
      `${this.apiPath}/${assignment.groupId}/permissions/assign`,
      {
        permissionIds: assignment.permissionIds,
        operation: assignment.operation
      },
      message
    );
  }

  /**
   * Get group permissions with inheritance info
   */
  getGroupPermissionsWithStatus(groupId: string): Observable<Array<PermissionDto & {
    granted: boolean;
    inherited: boolean;
    inheritedFrom?: string;
  }>> {
    return this.get<Array<PermissionDto & {
      granted: boolean;
      inherited: boolean;
      inheritedFrom?: string;
    }>>(`${this.apiPath}/${groupId}/permissions/status`);
  }

  /**
   * Clone group with options
   */
  cloneGroup(groupId: string, options: {
    name: string;
    description?: string;
    copyMembers: boolean;
    copyPermissions: boolean;
    copyRoles: boolean;
  }): Observable<GroupDto> {
    return this.post<GroupDto>(
      `${this.apiPath}/${groupId}/clone`,
      options,
      'Grup başarıyla kopyalandı'
    );
  }

  /**
   * Get available users for group membership (excluding current members)
   */
  getAvailableUsersForGroup(groupId: string, options: {
    search?: string;
    excludeUserIds?: string[];
    page?: number;
    pageSize?: number;
  } = {}): Observable<{
    users: UserDto[];
    totalCount: number;
  }> {
    const params: Record<string, any> = {};

    if (options['search']) params['search'] = options['search'];
    if (options['excludeUserIds']?.length) params['excludeUserIds'] = options['excludeUserIds'].join(',');
    if (options['page'] !== undefined) params['page'] = options['page'];
    if (options['pageSize'] !== undefined) params['pageSize'] = options['pageSize'];

    return this.get<{
      users: UserDto[];
      totalCount: number;
    }>(`${this.apiPath}/${groupId}/available-users`, params);
  }

  /**
   * Get group permission analytics
   */
  getGroupPermissionAnalytics(groupId: string): Observable<{
    totalPermissions: number;
    grantedPermissions: number;
    serviceBreakdown: Array<{
      service: string;
      total: number;
      granted: number;
      riskLevel: 'low' | 'medium' | 'high' | 'critical';
    }>;
    categoryBreakdown: Array<{
      category: string;
      total: number;
      granted: number;
    }>;
    recentChanges: Array<{
      permissionId: string;
      permissionName: string;
      action: 'granted' | 'revoked';
      changedBy: string;
      changedAt: Date;
    }>;
  }> {
    return this.get<{
      totalPermissions: number;
      grantedPermissions: number;
      serviceBreakdown: Array<{
        service: string;
        total: number;
        granted: number;
        riskLevel: 'low' | 'medium' | 'high' | 'critical';
      }>;
      categoryBreakdown: Array<{
        category: string;
        total: number;
        granted: number;
      }>;
      recentChanges: Array<{
        permissionId: string;
        permissionName: string;
        action: 'granted' | 'revoked';
        changedBy: string;
        changedAt: Date;
      }>;
    }>(`${this.apiPath}/${groupId}/permission-analytics`);
  }

  /**
   * Export group data with options
   */
  exportGroupData(groupId: string, options: {
    format: 'excel' | 'csv' | 'pdf';
    includeMembers: boolean;
    includePermissions: boolean;
    includeAnalytics: boolean;
  }): Observable<Blob> {
    const params = {
      format: options.format,
      includeMembers: options.includeMembers,
      includePermissions: options.includePermissions,
      includeAnalytics: options.includeAnalytics
    };

    const filename = `group-${groupId}-export.${options.format}`;
    return this.downloadFile(`${this.apiPath}/${groupId}/export`, filename, params);
  }

  /**
   * Bulk export multiple groups
   */
  bulkExportGroups(groupIds: string[], format: 'excel' | 'csv' | 'pdf'): Observable<Blob> {
    const params = {
      groupIds: groupIds.join(','),
      format
    };

    const filename = `groups-bulk-export.${format}`;
    return this.downloadFile(`${this.apiPath}/bulk-export`, filename, params);
  }

  /**
   * Get group access logs
   */
  getGroupAccessLogs(groupId: string, options: {
    page?: number;
    pageSize?: number;
    startDate?: Date;
    endDate?: Date;
  } = {}): Observable<{
    logs: Array<{
      id: string;
      userId: string;
      userName: string;
      action: string;
      resource: string;
      ipAddress: string;
      userAgent: string;
      timestamp: Date;
      success: boolean;
      details?: string;
    }>;
    totalCount: number;
  }> {
    const params: Record<string, any> = {};

    if (options['page'] !== undefined) params['page'] = options['page'];
    if (options['pageSize'] !== undefined) params['pageSize'] = options['pageSize'];
    if (options['startDate']) params['startDate'] = options['startDate'].toISOString();
    if (options['endDate']) params['endDate'] = options['endDate'].toISOString();

    return this.get<{
      logs: Array<{
        id: string;
        userId: string;
        userName: string;
        action: string;
        resource: string;
        ipAddress: string;
        userAgent: string;
        timestamp: Date;
        success: boolean;
        details?: string;
      }>;
      totalCount: number;
    }>(`${this.apiPath}/${groupId}/access-logs`, params);
  }

  /**
   * Get group recommendations based on user activity
   */
  getGroupRecommendations(userId: string): Observable<Array<{
    group: GroupDto;
    score: number;
    reason: string;
    matchingPermissions: string[];
  }>> {
    return this.get<Array<{
      group: GroupDto;
      score: number;
      reason: string;
      matchingPermissions: string[];
    }>>(`${this.apiPath}/recommendations/${userId}`);
  }

  /**
   * Preview group changes before applying
   */
  previewGroupChanges(groupId: string, changes: {
    memberChanges?: {
      add?: string[];
      remove?: string[];
      roleChanges?: Array<{ userId: string; newRole: string }>;
    };
    permissionChanges?: {
      grant?: string[];
      revoke?: string[];
    };
  }): Observable<{
    memberImpact: {
      affectedUsers: number;
      newPermissions: string[];
      removedPermissions: string[];
    };
    permissionImpact: {
      affectedPermissions: number;
      riskAssessment: 'low' | 'medium' | 'high' | 'critical';
      warnings: string[];
    };
  }> {
    return this.post<{
      memberImpact: {
        affectedUsers: number;
        newPermissions: string[];
        removedPermissions: string[];
      };
      permissionImpact: {
        affectedPermissions: number;
        riskAssessment: 'low' | 'medium' | 'high' | 'critical';
        warnings: string[];
      };
    }>(`${this.apiPath}/${groupId}/preview-changes`, changes);
  }

  /**
   * Build query parameters for group requests
   */
  private buildGroupParams(request: GetGroupsRequest): Record<string, any> {
    const params: Record<string, any> = {};

    if (request['page'] !== undefined) params['page'] = request['page'];
    if (request['pageSize'] !== undefined) params['pageSize'] = request['pageSize'];
    if (request['search']) params['search'] = request['search'];
    if (request['includeSystemGroups'] !== undefined) params['includeSystemGroups'] = request['includeSystemGroups'];
    if (request['hasMembers'] !== undefined) params['hasMembers'] = request['hasMembers'];
    if (request['sortBy']) params['sortBy'] = request['sortBy'];
    if (request['sortDirection']) params['sortDirection'] = request['sortDirection'];

    return params;
  }
}