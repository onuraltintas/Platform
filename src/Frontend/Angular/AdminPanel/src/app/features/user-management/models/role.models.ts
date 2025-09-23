export interface RoleDto {
  id: string;
  name: string;
  displayName: string;
  description?: string;
  isSystemRole: boolean;
  isDefault?: boolean;
  userCount: number;
  permissions: PermissionDto[];
  createdAt: Date;
  updatedAt?: Date;
  createdBy?: string;
}

export interface CreateRoleRequest {
  name: string;
  displayName: string;
  description?: string;
  permissionIds: string[];
}

export interface UpdateRoleRequest {
  displayName?: string;
  description?: string;
  permissionIds?: string[];
}

export interface CloneRoleRequest {
  sourceRoleId: string;
  newRoleName: string;
  newDisplayName: string;
  description?: string;
  permissionIds?: string[];
}

export interface RolePermissionDiff {
  added: PermissionDto[];
  removed: PermissionDto[];
  unchanged: PermissionDto[];
}

export interface RoleStatistics {
  totalRoles: number;
  systemRoles: number;
  customRoles: number;
  rolesWithUsers: number;
  averagePermissionsPerRole: number;
  mostUsedRole: RoleDto;
  leastUsedRole: RoleDto;
}

export interface GetRolesRequest {
  page?: number;
  pageSize?: number;
  search?: string;
  includeSystemRoles?: boolean;
  hasUsers?: boolean;
  includePermissions?: boolean;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  ids?: string[];
}

export interface RoleComparisonResult {
  role1: RoleDto;
  role2: RoleDto;
  commonPermissions: PermissionDto[];
  role1OnlyPermissions: PermissionDto[];
  role2OnlyPermissions: PermissionDto[];
  similarityPercentage: number;
}

// Re-export from permission models for convenience
export interface PermissionDto {
  id: string;
  name: string;
  displayName: string;
  description?: string;
  service: string;
  resource: string;
  action: string;
  isSystemPermission: boolean;
  category: string;
}