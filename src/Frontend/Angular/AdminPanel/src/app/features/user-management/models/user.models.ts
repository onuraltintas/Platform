export interface UserGroupInfo {
  groupId: string;
  groupName: string;
  groupType: string;
  userRole: string;
  joinedAt: string;
  isDefault: boolean;
}

export interface UserDto {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phoneNumber?: string;
  isEmailConfirmed: boolean;
  isActive: boolean;
  lastLoginAt?: string;
  createdAt?: string;
  roles: string[];
  groups: UserGroupInfo[];
  defaultGroup?: UserGroupInfo;
  permissions: string[];
}

export interface CreateUserRequest {
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  about?: string;
  password: string;
  confirmPassword: string;
  isActive: boolean;
  emailConfirmed?: boolean;
  roleIds?: string[];
  groupIds?: string[];
}

export interface UpdateUserRequest {
  userName?: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  about?: string;
  profilePictureUrl?: string;
  isActive?: boolean;
  emailConfirmed?: boolean;
  phoneNumberConfirmed?: boolean;
  twoFactorEnabled?: boolean;
  lockoutEnabled?: boolean;
  roleIds?: string[];
  groupIds?: string[];
}

export interface GetUsersRequest {
  page?: number;
  pageSize?: number;
  search?: string;
  roleId?: string;
  roleIds?: string[];
  groupId?: string;
  groupIds?: string[];
  isActive?: boolean;
  isEmailConfirmed?: boolean;
  createdFrom?: Date;
  createdTo?: Date;
  lastLoginFrom?: Date;
  lastLoginTo?: Date;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  format?: string;
}


export interface UserFilter {
  search: string;
  isActive?: boolean;
  roles: string[];
  groups: string[];
  createdDateRange: DateRange;
  lastLoginDateRange: DateRange;
}

export interface BulkUserOperation {
  userIds: string[];
  operation: 'activate' | 'deactivate' | 'delete' | 'assignRole' | 'removeRole' | 'update';
  data?: any;
}

export interface UserStatistics {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  newUsersThisMonth: number;
  onlineUsers: number;
  usersWithTwoFactor: number;
  lockedUsers: number;
  unverifiedEmails: number;
  userGrowthRate: number;
  totalRoles: number;
  systemRoles: number;
  totalGroups: number;
  averageGroupSize: number;
  todayActiveUsers: number;
  weeklyNewUsers: number;
  pendingApprovals: number;
  dailyActiveUsers?: number[];
}

// Shared interfaces
export interface RoleDto {
  id: string;
  name: string;
  displayName: string;
  description?: string;
  isSystemRole: boolean;
  userCount: number;
  permissions: string[];
  createdAt: Date;
}

// Import GroupDto from group.models.ts to avoid conflicts
export type { GroupDto } from './group.models';

export interface DateRange {
  start?: Date;
  end?: Date;
}

export interface PaginationState {
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

export interface PagedResponse<T> {
  users?: T[];  // For users endpoint
  data?: T[];   // Generic data field
  totalCount: number;
  currentPage: number;
  pageSize: number;
  totalPages?: number;
  pagination?: {
    total: number;
    page: number;
    pageSize: number;
    totalPages: number;
  };
}