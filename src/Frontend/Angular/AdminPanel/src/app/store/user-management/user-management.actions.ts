import { createActionGroup, emptyProps, props } from '@ngrx/store';
import {
  User,
  Role,
  Permission,
  Group,
  UserQuery,
  RoleQuery,
  PermissionQuery,
  GroupQuery,
  CreateUserRequest,
  UpdateUserRequest,
  CreateRoleRequest,
  UpdateRoleRequest,
  CreateGroupRequest,
  UpdateGroupRequest,
  PagedResult,
  UserStatistics,
  RoleStatistics,
  PermissionStatistics,
  GroupStatistics,
  PermissionCategory,
  BulkUserOperationRequest
} from '../../features/user-management/models/user-management.models';

// User Actions
export const UserActions = createActionGroup({
  source: 'User Management - Users',
  events: {
    // Load Users
    'Load Users': props<{ query?: UserQuery }>(),
    'Load Users Success': props<{ result: PagedResult<User> }>(),
    'Load Users Failure': props<{ error: string }>(),

    // Get User by ID
    'Get User': props<{ id: string }>(),
    'Get User Success': props<{ user: User }>(),
    'Get User Failure': props<{ error: string }>(),

    // Create User
    'Create User': props<{ request: CreateUserRequest }>(),
    'Create User Success': props<{ user: User }>(),
    'Create User Failure': props<{ error: string }>(),

    // Update User
    'Update User': props<{ request: UpdateUserRequest }>(),
    'Update User Success': props<{ user: User }>(),
    'Update User Failure': props<{ error: string }>(),

    // Delete User
    'Delete User': props<{ id: string }>(),
    'Delete User Success': props<{ id: string }>(),
    'Delete User Failure': props<{ error: string }>(),

    // Bulk Operations
    'Bulk User Operation': props<{ request: BulkUserOperationRequest }>(),
    'Bulk User Operation Success': props<{ message: string }>(),
    'Bulk User Operation Failure': props<{ error: string }>(),

    // Load User Statistics
    'Load User Statistics': emptyProps(),
    'Load User Statistics Success': props<{ statistics: UserStatistics }>(),
    'Load User Statistics Failure': props<{ error: string }>(),

    // Clear Selected User
    'Clear Selected User': emptyProps(),

    // Set Selected User
    'Set Selected User': props<{ user: User }>()
  }
});

// Role Actions
export const RoleActions = createActionGroup({
  source: 'User Management - Roles',
  events: {
    // Load Roles
    'Load Roles': props<{ query?: RoleQuery }>(),
    'Load Roles Success': props<{ result: PagedResult<Role> }>(),
    'Load Roles Failure': props<{ error: string }>(),

    // Get Role by ID
    'Get Role': props<{ id: string }>(),
    'Get Role Success': props<{ role: Role }>(),
    'Get Role Failure': props<{ error: string }>(),

    // Create Role
    'Create Role': props<{ request: CreateRoleRequest }>(),
    'Create Role Success': props<{ role: Role }>(),
    'Create Role Failure': props<{ error: string }>(),

    // Update Role
    'Update Role': props<{ request: UpdateRoleRequest }>(),
    'Update Role Success': props<{ role: Role }>(),
    'Update Role Failure': props<{ error: string }>(),

    // Delete Role
    'Delete Role': props<{ id: string }>(),
    'Delete Role Success': props<{ id: string }>(),
    'Delete Role Failure': props<{ error: string }>(),

    // Load Role Permissions
    'Load Role Permissions': props<{ roleId: string }>(),
    'Load Role Permissions Success': props<{ roleId: string; permissions: Permission[] }>(),
    'Load Role Permissions Failure': props<{ error: string }>(),

    // Load Role Statistics
    'Load Role Statistics': emptyProps(),
    'Load Role Statistics Success': props<{ statistics: RoleStatistics }>(),
    'Load Role Statistics Failure': props<{ error: string }>(),

    // Clear Selected Role
    'Clear Selected Role': emptyProps(),

    // Set Selected Role
    'Set Selected Role': props<{ role: Role }>()
  }
});

// Permission Actions
export const PermissionActions = createActionGroup({
  source: 'User Management - Permissions',
  events: {
    // Load Permissions
    'Load Permissions': props<{ query?: PermissionQuery }>(),
    'Load Permissions Success': props<{ result: PagedResult<Permission> }>(),
    'Load Permissions Failure': props<{ error: string }>(),

    // Get Permission by ID
    'Get Permission': props<{ id: string }>(),
    'Get Permission Success': props<{ permission: Permission }>(),
    'Get Permission Failure': props<{ error: string }>(),

    // Load Permission Categories
    'Load Permission Categories': emptyProps(),
    'Load Permission Categories Success': props<{ categories: PermissionCategory[] }>(),
    'Load Permission Categories Failure': props<{ error: string }>(),

    // Load Permission Statistics
    'Load Permission Statistics': emptyProps(),
    'Load Permission Statistics Success': props<{ statistics: PermissionStatistics }>(),
    'Load Permission Statistics Failure': props<{ error: string }>(),

    // Clear Selected Permission
    'Clear Selected Permission': emptyProps(),

    // Set Selected Permission
    'Set Selected Permission': props<{ permission: Permission }>()
  }
});

// Group Actions
export const GroupActions = createActionGroup({
  source: 'User Management - Groups',
  events: {
    // Load Groups
    'Load Groups': props<{ query?: GroupQuery }>(),
    'Load Groups Success': props<{ result: PagedResult<Group> }>(),
    'Load Groups Failure': props<{ error: string }>(),

    // Get Group by ID
    'Get Group': props<{ id: string }>(),
    'Get Group Success': props<{ group: Group }>(),
    'Get Group Failure': props<{ error: string }>(),

    // Create Group
    'Create Group': props<{ request: CreateGroupRequest }>(),
    'Create Group Success': props<{ group: Group }>(),
    'Create Group Failure': props<{ error: string }>(),

    // Update Group
    'Update Group': props<{ request: UpdateGroupRequest }>(),
    'Update Group Success': props<{ group: Group }>(),
    'Update Group Failure': props<{ error: string }>(),

    // Delete Group
    'Delete Group': props<{ id: string }>(),
    'Delete Group Success': props<{ id: string }>(),
    'Delete Group Failure': props<{ error: string }>(),

    // Load Group Statistics
    'Load Group Statistics': emptyProps(),
    'Load Group Statistics Success': props<{ statistics: GroupStatistics }>(),
    'Load Group Statistics Failure': props<{ error: string }>(),

    // Clear Selected Group
    'Clear Selected Group': emptyProps(),

    // Set Selected Group
    'Set Selected Group': props<{ group: Group }>()
  }
});