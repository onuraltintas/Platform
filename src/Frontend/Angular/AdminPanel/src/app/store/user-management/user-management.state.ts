import {
  User,
  Role,
  Permission,
  Group,
  UserStatistics,
  RoleStatistics,
  PermissionStatistics,
  GroupStatistics,
  PagedResult,
  UserQuery,
  RoleQuery,
  PermissionQuery,
  GroupQuery
} from '../../features/user-management/models/user-management.models';

export interface EntityState<T> {
  entities: { [id: string]: T };
  ids: string[];
  loading: boolean;
  error: string | null;
  lastQuery?: any;
  lastResult?: PagedResult<T> | null;
}

export interface UserManagementState {
  users: EntityState<User> & {
    statistics: UserStatistics | null;
    selectedUser: User | null;
    creating: boolean;
    updating: boolean;
    deleting: boolean;
  };
  roles: EntityState<Role> & {
    statistics: RoleStatistics | null;
    selectedRole: Role | null;
    rolePermissions: { [roleId: string]: Permission[] };
    creating: boolean;
    updating: boolean;
    deleting: boolean;
  };
  permissions: EntityState<Permission> & {
    statistics: PermissionStatistics | null;
    selectedPermission: Permission | null;
    categories: any[];
  };
  groups: EntityState<Group> & {
    statistics: GroupStatistics | null;
    selectedGroup: Group | null;
    creating: boolean;
    updating: boolean;
    deleting: boolean;
  };
}

const createInitialEntityState = <T>(): EntityState<T> => ({
  entities: {},
  ids: [],
  loading: false,
  error: null,
  lastQuery: null,
  lastResult: null
});

export const initialUserManagementState: UserManagementState = {
  users: {
    ...createInitialEntityState<User>(),
    statistics: null,
    selectedUser: null,
    creating: false,
    updating: false,
    deleting: false
  },
  roles: {
    ...createInitialEntityState<Role>(),
    statistics: null,
    selectedRole: null,
    rolePermissions: {},
    creating: false,
    updating: false,
    deleting: false
  },
  permissions: {
    ...createInitialEntityState<Permission>(),
    statistics: null,
    selectedPermission: null,
    categories: []
  },
  groups: {
    ...createInitialEntityState<Group>(),
    statistics: null,
    selectedGroup: null,
    creating: false,
    updating: false,
    deleting: false
  }
};