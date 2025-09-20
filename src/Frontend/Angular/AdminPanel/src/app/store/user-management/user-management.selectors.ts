import { createFeatureSelector, createSelector } from '@ngrx/store';
import { UserManagementState } from './user-management.state';
import { User, Role, Permission, Group } from '../../features/user-management/models/user-management.models';

// Feature Selector
export const selectUserManagementState = createFeatureSelector<UserManagementState>('userManagement');

// ================================
// USER SELECTORS
// ================================

export const selectUsersState = createSelector(
  selectUserManagementState,
  (state) => state.users
);

export const selectAllUsers = createSelector(
  selectUsersState,
  (state) => state.ids.map(id => state.entities[id]).filter(Boolean) as User[]
);

export const selectUsersEntities = createSelector(
  selectUsersState,
  (state) => state.entities
);

export const selectUsersLoading = createSelector(
  selectUsersState,
  (state) => state.loading
);

export const selectUsersError = createSelector(
  selectUsersState,
  (state) => state.error
);

export const selectSelectedUser = createSelector(
  selectUsersState,
  (state) => state.selectedUser
);

export const selectUserStatistics = createSelector(
  selectUsersState,
  (state) => state.statistics
);

export const selectUsersLastResult = createSelector(
  selectUsersState,
  (state) => state.lastResult
);

export const selectUserById = (id: string) => createSelector(
  selectUsersEntities,
  (entities) => entities[id] || null
);

export const selectUsersCreating = createSelector(
  selectUsersState,
  (state) => state.creating
);

export const selectUsersUpdating = createSelector(
  selectUsersState,
  (state) => state.updating
);

export const selectUsersDeleting = createSelector(
  selectUsersState,
  (state) => state.deleting
);

// ================================
// ROLE SELECTORS
// ================================

export const selectRolesState = createSelector(
  selectUserManagementState,
  (state) => state.roles
);

export const selectAllRoles = createSelector(
  selectRolesState,
  (state) => state.ids.map(id => state.entities[id]).filter(Boolean) as Role[]
);

export const selectRolesEntities = createSelector(
  selectRolesState,
  (state) => state.entities
);

export const selectRolesLoading = createSelector(
  selectRolesState,
  (state) => state.loading
);

export const selectRolesError = createSelector(
  selectRolesState,
  (state) => state.error
);

export const selectSelectedRole = createSelector(
  selectRolesState,
  (state) => state.selectedRole
);

export const selectRoleStatistics = createSelector(
  selectRolesState,
  (state) => state.statistics
);

export const selectRolesLastResult = createSelector(
  selectRolesState,
  (state) => state.lastResult
);

export const selectRoleById = (id: string) => createSelector(
  selectRolesEntities,
  (entities) => entities[id] || null
);

export const selectRolePermissions = (roleId: string) => createSelector(
  selectRolesState,
  (state) => state.rolePermissions[roleId] || []
);

export const selectActiveRoles = createSelector(
  selectAllRoles,
  (roles) => roles.filter(role => role.isActive)
);

export const selectDefaultRoles = createSelector(
  selectAllRoles,
  (roles) => roles.filter(role => role.isDefault)
);

export const selectRolesCreating = createSelector(
  selectRolesState,
  (state) => state.creating
);

export const selectRolesUpdating = createSelector(
  selectRolesState,
  (state) => state.updating
);

export const selectRolesDeleting = createSelector(
  selectRolesState,
  (state) => state.deleting
);

// ================================
// PERMISSION SELECTORS
// ================================

export const selectPermissionsState = createSelector(
  selectUserManagementState,
  (state) => state.permissions
);

export const selectAllPermissions = createSelector(
  selectPermissionsState,
  (state) => state.ids.map(id => state.entities[id]).filter(Boolean) as Permission[]
);

export const selectPermissionsEntities = createSelector(
  selectPermissionsState,
  (state) => state.entities
);

export const selectPermissionsLoading = createSelector(
  selectPermissionsState,
  (state) => state.loading
);

export const selectPermissionsError = createSelector(
  selectPermissionsState,
  (state) => state.error
);

export const selectSelectedPermission = createSelector(
  selectPermissionsState,
  (state) => state.selectedPermission
);

export const selectPermissionStatistics = createSelector(
  selectPermissionsState,
  (state) => state.statistics
);

export const selectPermissionCategories = createSelector(
  selectPermissionsState,
  (state) => state.categories
);

export const selectPermissionsLastResult = createSelector(
  selectPermissionsState,
  (state) => state.lastResult
);

export const selectPermissionById = (id: string) => createSelector(
  selectPermissionsEntities,
  (entities) => entities[id] || null
);

export const selectPermissionsByCategory = (category: string) => createSelector(
  selectAllPermissions,
  (permissions) => permissions.filter(permission => permission.category === category)
);

// ================================
// GROUP SELECTORS
// ================================

export const selectGroupsState = createSelector(
  selectUserManagementState,
  (state) => state.groups
);

export const selectAllGroups = createSelector(
  selectGroupsState,
  (state) => state.ids.map(id => state.entities[id]).filter(Boolean) as Group[]
);

export const selectGroupsEntities = createSelector(
  selectGroupsState,
  (state) => state.entities
);

export const selectGroupsLoading = createSelector(
  selectGroupsState,
  (state) => state.loading
);

export const selectGroupsError = createSelector(
  selectGroupsState,
  (state) => state.error
);

export const selectSelectedGroup = createSelector(
  selectGroupsState,
  (state) => state.selectedGroup
);

export const selectGroupStatistics = createSelector(
  selectGroupsState,
  (state) => state.statistics
);

export const selectGroupsLastResult = createSelector(
  selectGroupsState,
  (state) => state.lastResult
);

export const selectGroupById = (id: string) => createSelector(
  selectGroupsEntities,
  (entities) => entities[id] || null
);

export const selectActiveGroups = createSelector(
  selectAllGroups,
  (groups) => groups.filter(group => group.isActive)
);

export const selectGroupsCreating = createSelector(
  selectGroupsState,
  (state) => state.creating
);

export const selectGroupsUpdating = createSelector(
  selectGroupsState,
  (state) => state.updating
);

export const selectGroupsDeleting = createSelector(
  selectGroupsState,
  (state) => state.deleting
);

// ================================
// CROSS-ENTITY SELECTORS
// ================================

export const selectUsersByRole = (roleId: string) => createSelector(
  selectAllUsers,
  (users) => users.filter(user => user.roles.some(role => role.id === roleId))
);

export const selectUsersByGroup = (groupId: string) => createSelector(
  selectAllUsers,
  (users) => users.filter(user => user.groups.some(group => group.id === groupId))
);

export const selectRolesByUser = (userId: string) => createSelector(
  selectAllUsers,
  selectAllRoles,
  (users, roles) => {
    const user = users.find(u => u.id === userId);
    if (!user) return [];
    return user.roles || [];
  }
);

export const selectGroupsByUser = (userId: string) => createSelector(
  selectAllUsers,
  selectAllGroups,
  (users, groups) => {
    const user = users.find(u => u.id === userId);
    if (!user) return [];
    return user.groups || [];
  }
);

// ================================
// COMPUTED SELECTORS
// ================================

export const selectTotalEntitiesCount = createSelector(
  selectUsersState,
  selectRolesState,
  selectPermissionsState,
  selectGroupsState,
  (users, roles, permissions, groups) => ({
    users: users.ids.length,
    roles: roles.ids.length,
    permissions: permissions.ids.length,
    groups: groups.ids.length
  })
);

export const selectIsLoading = createSelector(
  selectUsersLoading,
  selectRolesLoading,
  selectPermissionsLoading,
  selectGroupsLoading,
  (usersLoading, rolesLoading, permissionsLoading, groupsLoading) =>
    usersLoading || rolesLoading || permissionsLoading || groupsLoading
);

export const selectHasErrors = createSelector(
  selectUsersError,
  selectRolesError,
  selectPermissionsError,
  selectGroupsError,
  (usersError, rolesError, permissionsError, groupsError) =>
    !!(usersError || rolesError || permissionsError || groupsError)
);

export const selectAllErrors = createSelector(
  selectUsersError,
  selectRolesError,
  selectPermissionsError,
  selectGroupsError,
  (usersError, rolesError, permissionsError, groupsError) => {
    const errors: string[] = [];
    if (usersError) errors.push(`Users: ${usersError}`);
    if (rolesError) errors.push(`Roles: ${rolesError}`);
    if (permissionsError) errors.push(`Permissions: ${permissionsError}`);
    if (groupsError) errors.push(`Groups: ${groupsError}`);
    return errors;
  }
);

// ================================
// PERFORMANCE SELECTORS
// ================================

export const selectEntitiesLoadingStates = createSelector(
  selectUsersState,
  selectRolesState,
  selectPermissionsState,
  selectGroupsState,
  (users, roles, permissions, groups) => ({
    users: {
      loading: users.loading,
      creating: users.creating,
      updating: users.updating,
      deleting: users.deleting
    },
    roles: {
      loading: roles.loading,
      creating: roles.creating,
      updating: roles.updating,
      deleting: roles.deleting
    },
    permissions: {
      loading: permissions.loading
    },
    groups: {
      loading: groups.loading,
      creating: groups.creating,
      updating: groups.updating,
      deleting: groups.deleting
    }
  })
);