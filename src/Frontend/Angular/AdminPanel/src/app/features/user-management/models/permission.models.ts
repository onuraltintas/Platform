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
  createdAt: Date;
  roleCount: number;
  userCount: number;
}

export interface PermissionMatrixItem {
  service: string;
  resource: string;
  permissions: PermissionMatrixEntry[];
}

export interface PermissionMatrixEntry {
  permission: PermissionDto;
  roles: RolePermissionEntry[];
}

export interface RolePermissionEntry {
  roleId: string;
  roleName: string;
  hasPermission: boolean;
  isInherited: boolean;
}

export interface PermissionsByService {
  serviceName: string;
  resources: PermissionsByResource[];
  totalPermissions: number;
}

export interface PermissionsByResource {
  resourceName: string;
  permissions: PermissionDto[];
  actions: string[];
}

export interface GetPermissionsRequest {
  page?: number;
  pageSize?: number;
  search?: string;
  service?: string;
  resource?: string;
  action?: string;
  category?: string;
  includeSystemPermissions?: boolean;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface PermissionStatistics {
  totalPermissions: number;
  systemPermissions: number;
  customPermissions: number;
  serviceCount: number;
  resourceCount: number;
  actionCount: number;
  categoryCount: number;
  unassignedPermissions: number;
  mostUsedPermission: PermissionDto;
}

export interface CreatePermissionRequest {
  name: string;
  displayName: string;
  description?: string;
  service: string;
  resource: string;
  action: string;
  category: string;
}

export interface UpdatePermissionRequest {
  displayName?: string;
  description?: string;
  category?: string;
}

export interface BulkPermissionAssignment {
  roleIds: string[];
  permissionIds: string[];
  operation: 'assign' | 'revoke';
}

// RoleDto is imported from role.models.ts to avoid conflicts