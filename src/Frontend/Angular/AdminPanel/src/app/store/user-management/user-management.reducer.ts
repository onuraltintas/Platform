import { createReducer, on } from '@ngrx/store';
import { UserManagementState, initialUserManagementState, EntityState } from './user-management.state';
import { UserActions, RoleActions, PermissionActions, GroupActions } from './user-management.actions';
import { User, Role, Permission, Group, PagedResult } from '../../features/user-management/models/user-management.models';

// Helper function to normalize entities
function normalizeEntities<T extends { id: string }>(items: T[]): { entities: { [id: string]: T }, ids: string[] } {
  const entities: { [id: string]: T } = {};
  const ids: string[] = [];

  items.forEach(item => {
    entities[item.id] = item;
    ids.push(item.id);
  });

  return { entities, ids };
}

// Helper function to update entity state
function updateEntityState<T>(
  state: EntityState<T>,
  result: PagedResult<T>
): EntityState<T> {
  const { entities, ids } = normalizeEntities(result.data);

  return {
    ...state,
    entities,
    ids,
    loading: false,
    error: null,
    lastResult: result
  };
}

export const userManagementReducer = createReducer(
  initialUserManagementState,

  // ================================
  // USER REDUCERS
  // ================================

  // Load Users
  on(UserActions.loadUsers, (state, { query }) => ({
    ...state,
    users: {
      ...state.users,
      loading: true,
      error: null,
      lastQuery: query
    }
  })),

  on(UserActions.loadUsersSuccess, (state, { result }) => ({
    ...state,
    users: updateEntityState(state.users, result)
  })),

  on(UserActions.loadUsersFailure, (state, { error }) => ({
    ...state,
    users: {
      ...state.users,
      loading: false,
      error
    }
  })),

  // Get User
  on(UserActions.getUser, (state) => ({
    ...state,
    users: {
      ...state.users,
      loading: true,
      error: null
    }
  })),

  on(UserActions.getUserSuccess, (state, { user }) => ({
    ...state,
    users: {
      ...state.users,
      loading: false,
      selectedUser: user,
      entities: {
        ...state.users.entities,
        [user.id]: user
      }
    }
  })),

  on(UserActions.getUserFailure, (state, { error }) => ({
    ...state,
    users: {
      ...state.users,
      loading: false,
      error
    }
  })),

  // Create User
  on(UserActions.createUser, (state) => ({
    ...state,
    users: {
      ...state.users,
      creating: true,
      error: null
    }
  })),

  on(UserActions.createUserSuccess, (state, { user }) => ({
    ...state,
    users: {
      ...state.users,
      creating: false,
      entities: {
        ...state.users.entities,
        [user.id]: user
      },
      ids: [...state.users.ids, user.id]
    }
  })),

  on(UserActions.createUserFailure, (state, { error }) => ({
    ...state,
    users: {
      ...state.users,
      creating: false,
      error
    }
  })),

  // Update User
  on(UserActions.updateUser, (state) => ({
    ...state,
    users: {
      ...state.users,
      updating: true,
      error: null
    }
  })),

  on(UserActions.updateUserSuccess, (state, { user }) => ({
    ...state,
    users: {
      ...state.users,
      updating: false,
      selectedUser: state.users.selectedUser?.id === user.id ? user : state.users.selectedUser,
      entities: {
        ...state.users.entities,
        [user.id]: user
      }
    }
  })),

  on(UserActions.updateUserFailure, (state, { error }) => ({
    ...state,
    users: {
      ...state.users,
      updating: false,
      error
    }
  })),

  // Delete User
  on(UserActions.deleteUser, (state) => ({
    ...state,
    users: {
      ...state.users,
      deleting: true,
      error: null
    }
  })),

  on(UserActions.deleteUserSuccess, (state, { id }) => {
    const { [id]: deleted, ...remainingEntities } = state.users.entities;
    return {
      ...state,
      users: {
        ...state.users,
        deleting: false,
        entities: remainingEntities,
        ids: state.users.ids.filter(userId => userId !== id),
        selectedUser: state.users.selectedUser?.id === id ? null : state.users.selectedUser
      }
    };
  }),

  on(UserActions.deleteUserFailure, (state, { error }) => ({
    ...state,
    users: {
      ...state.users,
      deleting: false,
      error
    }
  })),

  // Load User Statistics
  on(UserActions.loadUserStatisticsSuccess, (state, { statistics }) => ({
    ...state,
    users: {
      ...state.users,
      statistics
    }
  })),

  // Set Selected User
  on(UserActions.setSelectedUser, (state, { user }) => ({
    ...state,
    users: {
      ...state.users,
      selectedUser: user
    }
  })),

  // Clear Selected User
  on(UserActions.clearSelectedUser, (state) => ({
    ...state,
    users: {
      ...state.users,
      selectedUser: null
    }
  })),

  // ================================
  // ROLE REDUCERS
  // ================================

  // Load Roles
  on(RoleActions.loadRoles, (state, { query }) => ({
    ...state,
    roles: {
      ...state.roles,
      loading: true,
      error: null,
      lastQuery: query
    }
  })),

  on(RoleActions.loadRolesSuccess, (state, { result }) => ({
    ...state,
    roles: updateEntityState(state.roles, result)
  })),

  on(RoleActions.loadRolesFailure, (state, { error }) => ({
    ...state,
    roles: {
      ...state.roles,
      loading: false,
      error
    }
  })),

  // Get Role
  on(RoleActions.getRoleSuccess, (state, { role }) => ({
    ...state,
    roles: {
      ...state.roles,
      loading: false,
      selectedRole: role,
      entities: {
        ...state.roles.entities,
        [role.id]: role
      }
    }
  })),

  // Create Role
  on(RoleActions.createRole, (state) => ({
    ...state,
    roles: {
      ...state.roles,
      creating: true,
      error: null
    }
  })),

  on(RoleActions.createRoleSuccess, (state, { role }) => ({
    ...state,
    roles: {
      ...state.roles,
      creating: false,
      entities: {
        ...state.roles.entities,
        [role.id]: role
      },
      ids: [...state.roles.ids, role.id]
    }
  })),

  // Update Role
  on(RoleActions.updateRoleSuccess, (state, { role }) => ({
    ...state,
    roles: {
      ...state.roles,
      updating: false,
      selectedRole: state.roles.selectedRole?.id === role.id ? role : state.roles.selectedRole,
      entities: {
        ...state.roles.entities,
        [role.id]: role
      }
    }
  })),

  // Delete Role
  on(RoleActions.deleteRoleSuccess, (state, { id }) => {
    const { [id]: deleted, ...remainingEntities } = state.roles.entities;
    return {
      ...state,
      roles: {
        ...state.roles,
        deleting: false,
        entities: remainingEntities,
        ids: state.roles.ids.filter(roleId => roleId !== id),
        selectedRole: state.roles.selectedRole?.id === id ? null : state.roles.selectedRole
      }
    };
  }),

  // Load Role Permissions
  on(RoleActions.loadRolePermissionsSuccess, (state, { roleId, permissions }) => ({
    ...state,
    roles: {
      ...state.roles,
      rolePermissions: {
        ...state.roles.rolePermissions,
        [roleId]: permissions
      }
    }
  })),

  // Load Role Statistics
  on(RoleActions.loadRoleStatisticsSuccess, (state, { statistics }) => ({
    ...state,
    roles: {
      ...state.roles,
      statistics
    }
  })),

  // Set Selected Role
  on(RoleActions.setSelectedRole, (state, { role }) => ({
    ...state,
    roles: {
      ...state.roles,
      selectedRole: role
    }
  })),

  // Clear Selected Role
  on(RoleActions.clearSelectedRole, (state) => ({
    ...state,
    roles: {
      ...state.roles,
      selectedRole: null
    }
  })),

  // ================================
  // PERMISSION REDUCERS
  // ================================

  // Load Permissions
  on(PermissionActions.loadPermissions, (state, { query }) => ({
    ...state,
    permissions: {
      ...state.permissions,
      loading: true,
      error: null,
      lastQuery: query
    }
  })),

  on(PermissionActions.loadPermissionsSuccess, (state, { result }) => ({
    ...state,
    permissions: updateEntityState(state.permissions, result)
  })),

  // Load Permission Categories
  on(PermissionActions.loadPermissionCategoriesSuccess, (state, { categories }) => ({
    ...state,
    permissions: {
      ...state.permissions,
      categories
    }
  })),

  // Load Permission Statistics
  on(PermissionActions.loadPermissionStatisticsSuccess, (state, { statistics }) => ({
    ...state,
    permissions: {
      ...state.permissions,
      statistics
    }
  })),

  // Set Selected Permission
  on(PermissionActions.setSelectedPermission, (state, { permission }) => ({
    ...state,
    permissions: {
      ...state.permissions,
      selectedPermission: permission
    }
  })),

  // ================================
  // GROUP REDUCERS
  // ================================

  // Load Groups
  on(GroupActions.loadGroups, (state, { query }) => ({
    ...state,
    groups: {
      ...state.groups,
      loading: true,
      error: null,
      lastQuery: query
    }
  })),

  on(GroupActions.loadGroupsSuccess, (state, { result }) => ({
    ...state,
    groups: updateEntityState(state.groups, result)
  })),

  // Get Group
  on(GroupActions.getGroupSuccess, (state, { group }) => ({
    ...state,
    groups: {
      ...state.groups,
      loading: false,
      selectedGroup: group,
      entities: {
        ...state.groups.entities,
        [group.id]: group
      }
    }
  })),

  // Create Group
  on(GroupActions.createGroupSuccess, (state, { group }) => ({
    ...state,
    groups: {
      ...state.groups,
      creating: false,
      entities: {
        ...state.groups.entities,
        [group.id]: group
      },
      ids: [...state.groups.ids, group.id]
    }
  })),

  // Update Group
  on(GroupActions.updateGroupSuccess, (state, { group }) => ({
    ...state,
    groups: {
      ...state.groups,
      updating: false,
      selectedGroup: state.groups.selectedGroup?.id === group.id ? group : state.groups.selectedGroup,
      entities: {
        ...state.groups.entities,
        [group.id]: group
      }
    }
  })),

  // Delete Group
  on(GroupActions.deleteGroupSuccess, (state, { id }) => {
    const { [id]: deleted, ...remainingEntities } = state.groups.entities;
    return {
      ...state,
      groups: {
        ...state.groups,
        deleting: false,
        entities: remainingEntities,
        ids: state.groups.ids.filter(groupId => groupId !== id),
        selectedGroup: state.groups.selectedGroup?.id === id ? null : state.groups.selectedGroup
      }
    };
  }),

  // Load Group Statistics
  on(GroupActions.loadGroupStatisticsSuccess, (state, { statistics }) => ({
    ...state,
    groups: {
      ...state.groups,
      statistics
    }
  })),

  // Set Selected Group
  on(GroupActions.setSelectedGroup, (state, { group }) => ({
    ...state,
    groups: {
      ...state.groups,
      selectedGroup: group
    }
  })),

  // Clear Selected Group
  on(GroupActions.clearSelectedGroup, (state) => ({
    ...state,
    groups: {
      ...state.groups,
      selectedGroup: null
    }
  }))
);