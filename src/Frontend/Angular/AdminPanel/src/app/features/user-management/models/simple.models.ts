// Simplified User Management Models
// Focused on essential functionality only

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  isActive: boolean;
  emailConfirmed: boolean;
  createdAt: Date;
  roles: Role[];
  groups: Group[];
}

export interface Role {
  id: string;
  name: string;
  description?: string;
  isSystemRole: boolean;
  permissions: Permission[];
}

export interface Permission {
  id: string;
  name: string;
  resource: string;
  action: string;
  description?: string;
}

export interface Group {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  color?: string; // For visual distinction
  memberCount: number;
  createdAt: Date;
  users?: User[]; // For group details
}

// Simple request/response types
export interface CreateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  password: string;
  roleIds: string[];
  groupIds?: string[];
}

export interface UpdateUserRequest {
  id: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  isActive: boolean;
  roleIds: string[];
  groupIds?: string[];
}

export interface CreateRoleRequest {
  name: string;
  description?: string;
  permissionIds: string[];
}

export interface UpdateRoleRequest {
  id: string;
  name: string;
  description?: string;
  permissionIds: string[];
}

export interface CreateGroupRequest {
  name: string;
  description?: string;
  color?: string;
  userIds?: string[];
}

export interface UpdateGroupRequest {
  id: string;
  name: string;
  description?: string;
  color?: string;
  isActive: boolean;
  userIds?: string[];
}

// Simple pagination
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}