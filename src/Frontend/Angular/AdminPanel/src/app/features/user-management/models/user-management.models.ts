export interface User {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  isActive: boolean;
  emailConfirmed: boolean;
  phoneNumberConfirmed: boolean;
  twoFactorEnabled: boolean;
  lockoutEnabled: boolean;
  lockoutEnd?: Date;
  accessFailedCount: number;
  createdAt: Date;
  updatedAt: Date;
  lastLoginAt?: Date;
  profilePicture?: string;
  roles: Role[];
  groups: Group[];
  permissions: Permission[];
  claims: UserClaim[];
}

export interface Role {
  id: string;
  name: string;
  normalizedName: string;
  description?: string;
  isActive: boolean;
  isDefault: boolean;
  createdAt: Date;
  updatedAt: Date;
  permissions: Permission[];
  userCount?: number;
}

export interface Permission {
  id: string;
  name: string;
  normalizedName: string;
  description?: string;
  category: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export interface Group {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
  users: User[];
  roles: Role[];
  userCount?: number;
}

export interface UserClaim {
  id: string;
  userId: string;
  claimType: string;
  claimValue: string;
  createdAt: Date;
}

// Request/Response Models
export interface CreateUserRequest {
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  password: string;
  confirmPassword: string;
  isActive?: boolean;
  emailConfirmed?: boolean;
  roleIds?: string[];
  groupIds?: string[];
}

export interface UpdateUserRequest {
  id: string;
  userName?: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  isActive?: boolean;
  emailConfirmed?: boolean;
  phoneNumberConfirmed?: boolean;
  twoFactorEnabled?: boolean;
  lockoutEnabled?: boolean;
  roleIds?: string[];
  groupIds?: string[];
}

export interface CreateRoleRequest {
  name: string;
  description?: string;
  isActive?: boolean;
  isDefault?: boolean;
  permissionIds?: string[];
}

export interface UpdateRoleRequest {
  id: string;
  name?: string;
  description?: string;
  isActive?: boolean;
  isDefault?: boolean;
  permissionIds?: string[];
}

export interface CreateGroupRequest {
  name: string;
  description?: string;
  isActive?: boolean;
  userIds?: string[];
  roleIds?: string[];
}

export interface UpdateGroupRequest {
  id: string;
  name?: string;
  description?: string;
  isActive?: boolean;
  userIds?: string[];
  roleIds?: string[];
}

export interface ChangePasswordRequest {
  userId: string;
  newPassword: string;
  confirmPassword: string;
  sendEmail?: boolean;
}

export interface ResetPasswordRequest {
  userId: string;
  sendEmail?: boolean;
}

export interface BulkUserOperationRequest {
  userIds: string[];
  operation: 'activate' | 'deactivate' | 'delete' | 'assignRole' | 'removeRole' | 'assignGroup' | 'removeGroup';
  roleId?: string;
  groupId?: string;
}

// Query Models
export interface UserQuery {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  isActive?: boolean;
  emailConfirmed?: boolean;
  roleId?: string;
  groupId?: string;
  sortBy?: 'userName' | 'email' | 'firstName' | 'lastName' | 'createdAt' | 'lastLoginAt';
  sortDirection?: 'asc' | 'desc';
}

export interface RoleQuery {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  isActive?: boolean;
  isDefault?: boolean;
  sortBy?: 'name' | 'createdAt' | 'userCount';
  sortDirection?: 'asc' | 'desc';
}

export interface GroupQuery {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  isActive?: boolean;
  sortBy?: 'name' | 'createdAt' | 'userCount';
  sortDirection?: 'asc' | 'desc';
}

export interface PermissionQuery {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  category?: string;
  isActive?: boolean;
  sortBy?: 'name' | 'category' | 'createdAt';
  sortDirection?: 'asc' | 'desc';
}

// Response Models
export interface PagedResult<T> {
  data: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface UserStatistics {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  emailConfirmedUsers: number;
  emailUnconfirmedUsers: number;
  lockedOutUsers: number;
  twoFactorEnabledUsers: number;
  newUsersThisMonth: number;
  lastLoginActivity: {
    last24Hours: number;
    last7Days: number;
    last30Days: number;
  };
}

export interface RoleStatistics {
  totalRoles: number;
  activeRoles: number;
  defaultRoles: number;
  customRoles: number;
}

export interface PermissionStatistics {
  totalPermissions: number;
  activePermissions: number;
  categoriesCount: number;
  systemPermissions: number;
  userPermissions: number;
  modulePermissions: number;
  resourcePermissions: number;
  categoryCount: number;
  categories: {
    name: string;
    count: number;
  }[];
}

export interface GroupStatistics {
  totalGroups: number;
  activeGroups: number;
  inactiveGroups: number;
  departmentGroups: number;
  teamGroups: number;
  projectGroups: number;
  averageUsersPerGroup: number;
  averageRolesPerGroup: number;
}

// Table Column Definitions
export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  type?: 'text' | 'number' | 'date' | 'boolean' | 'badge' | 'avatar' | 'actions';
  width?: string;
  align?: 'left' | 'center' | 'right';
  format?: string;
}

// Filter Definitions
export interface FilterOption {
  label: string;
  value: any;
  count?: number;
}

export interface FilterGroup {
  label: string;
  key: string;
  type: 'select' | 'multiselect' | 'date' | 'boolean' | 'text';
  options?: FilterOption[];
  value?: any;
}

// Action Definitions
export interface ActionButton {
  label: string;
  icon: string;
  action: string;
  permission?: string;
  type?: 'primary' | 'secondary' | 'success' | 'warning' | 'danger' | 'info';
  disabled?: boolean;
}

export interface BulkAction {
  label: string;
  icon: string;
  action: string;
  permission?: string;
  confirmMessage?: string;
  type?: 'primary' | 'secondary' | 'success' | 'warning' | 'danger' | 'info';
}

// Audit Log Models
export interface UserAuditLog {
  id: string;
  userId: string;
  action: string;
  description: string;
  timestamp: Date;
  performedBy: string;
  performedById: string;
  ipAddress?: string;
  userAgent?: string;
  details?: string;
}

export interface RoleAuditLog {
  id: string;
  roleId: string;
  action: string;
  description: string;
  timestamp: Date;
  performedBy: string;
  performedById: string;
  ipAddress?: string;
  userAgent?: string;
  details?: string;
}

export interface PermissionAuditLog {
  id: string;
  permissionId: string;
  action: string;
  description: string;
  timestamp: Date;
  performedBy: string;
  performedById: string;
  ipAddress?: string;
  userAgent?: string;
  details?: string;
}

export interface GroupAuditLog {
  id: string;
  groupId: string;
  action: string;
  description: string;
  timestamp: Date;
  performedBy: string;
  performedById: string;
  ipAddress?: string;
  userAgent?: string;
  details?: string;
}

// Extended Permission Models
export interface PermissionCategory {
  name: string;
  displayName: string;
  description: string;
  icon?: string;
  permissions: Permission[];
}