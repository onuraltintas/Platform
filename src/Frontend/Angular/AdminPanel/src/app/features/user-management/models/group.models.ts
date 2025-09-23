import type { PermissionDto } from './permission.models';

export interface GroupDto {
  id: string;
  name: string;
  description?: string;
  memberCount: number;
  isSystemGroup: boolean;
  members: GroupMemberDto[];
  permissions: PermissionDto[];
  createdAt: Date;
  updatedAt?: Date;
  createdBy?: string;
}

export interface GroupMemberDto {
  userId: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  profilePictureUrl?: string;
  joinedAt: Date;
  role: GroupMemberRole;
  isActive: boolean;
}

export interface CreateGroupRequest {
  name: string;
  description?: string;
  memberIds?: string[];
  permissionIds?: string[];
}

export interface UpdateGroupRequest {
  name?: string;
  description?: string;
  memberIds?: string[];
  permissionIds?: string[];
}

export interface GroupMemberRequest {
  userId: string;
  role: GroupMemberRole;
}

export interface BulkGroupMemberOperation {
  userIds: string[];
  operation: 'add' | 'remove' | 'changeRole';
  role?: GroupMemberRole;
}

export interface GetGroupsRequest {
  page?: number;
  pageSize?: number;
  search?: string;
  includeSystemGroups?: boolean;
  hasMembers?: boolean;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface GroupStatistics {
  totalGroups: number;
  systemGroups: number;
  customGroups: number;
  totalMembers: number;
  averageMembersPerGroup: number;
  largestGroup: GroupDto;
  emptyGroups: number;
}

export interface GroupPermissionAssignment {
  groupId: string;
  permissionIds: string[];
  operation: 'assign' | 'revoke';
}

export type GroupMemberRole = 'Member' | 'Admin' | 'Owner';

// PermissionDto and UserDto are imported from their respective model files to avoid conflicts