// User models - selective exports to avoid conflicts
export type {
  UserDto,
  CreateUserRequest,
  UpdateUserRequest,
  GetUsersRequest,
  UserFilter,
  BulkUserOperation,
  UserStatistics,
  DateRange,
  PaginationState,
  PagedResponse
} from './user.models';

// Role models - selective exports
export type {
  RoleDto,
  CreateRoleRequest,
  UpdateRoleRequest,
  CloneRoleRequest,
  RolePermissionDiff,
  RoleStatistics,
  GetRolesRequest,
  RoleComparisonResult
} from './role.models';

// Permission models - selective exports
export type {
  PermissionDto,
  CreatePermissionRequest,
  UpdatePermissionRequest,
  GetPermissionsRequest,
  PermissionStatistics,
  PermissionMatrixItem,
  PermissionsByService,
  BulkPermissionAssignment
} from './permission.models';

// Group models - selective exports
export type {
  GroupDto,
  GroupMemberDto,
  CreateGroupRequest,
  UpdateGroupRequest,
  GroupMemberRequest,
  BulkGroupMemberOperation,
  GetGroupsRequest,
  GroupStatistics,
  GroupPermissionAssignment,
  GroupMemberRole
} from './group.models';

// Re-export core API models with export type to avoid conflicts
export type {
  ApiResponse,
  PagedResponse as PaginatedResponse,
  PageRequest,
  BulkOperationRequest,
  BulkOperationResult
} from '../../../core/api/models/api.models';

export interface SortOptions {
  field: string;
  direction: 'asc' | 'desc';
}

export interface FilterOptions {
  search?: string;
  dateRange?: { start?: Date; end?: Date; };
  status?: string[];
  tags?: string[];
}

export interface BulkOperation<T = any> {
  ids: string[];
  operation: string;
  data?: T;
}

export interface AuditInfo {
  createdAt: Date;
  createdBy?: string;
  updatedAt?: Date;
  updatedBy?: string;
}

export interface SelectOption<T = string> {
  label: string;
  value: T;
  disabled?: boolean;
  description?: string;
}

export interface TableColumn {
  key: string;
  label: string;
  title?: string;
  sortable?: boolean;
  filterable?: boolean;
  width?: string;
  align?: 'left' | 'center' | 'right';
  type?: 'text' | 'number' | 'date' | 'boolean' | 'badge' | 'avatar' | 'actions';
}

export interface ActionButton {
  icon: string;
  label: string;
  action: string;
  variant?: 'primary' | 'secondary' | 'success' | 'warning' | 'danger';
  visible?: boolean;
  disabled?: boolean;
  permission?: string;
}