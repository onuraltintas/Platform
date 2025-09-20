import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { map, exhaustMap, catchError, switchMap, mergeMap } from 'rxjs/operators';

import { UserManagementService } from '../../features/user-management/services/user-management.service';
import { NotificationService } from '../../shared/services/notification.service';
import { UserActions, RoleActions, PermissionActions, GroupActions } from './user-management.actions';

@Injectable()
export class UserManagementEffects {
  private readonly actions$ = inject(Actions);
  private readonly userManagementService = inject(UserManagementService);
  private readonly notificationService = inject(NotificationService);

  // ================================
  // USER EFFECTS
  // ================================

  loadUsers$ = createEffect(() =>
    this.actions$.pipe(
      ofType(UserActions.loadUsers),
      switchMap(({ query }) =>
        this.userManagementService.getUsers(query).pipe(
          map(result => UserActions.loadUsersSuccess({ result })),
          catchError(error => {
            console.error('Load users error:', error);
            return of(UserActions.loadUsersFailure({ error: error.message || 'Failed to load users' }));
          })
        )
      )
    )
  );

  getUser$ = createEffect(() =>
    this.actions$.pipe(
      ofType(UserActions.getUser),
      switchMap(({ id }) =>
        this.userManagementService.getUserById(id).pipe(
          map(user => UserActions.getUserSuccess({ user })),
          catchError(error => {
            console.error('Get user error:', error);
            return of(UserActions.getUserFailure({ error: error.message || 'Failed to get user' }));
          })
        )
      )
    )
  );

  createUser$ = createEffect(() =>
    this.actions$.pipe(
      ofType(UserActions.createUser),
      exhaustMap(({ request }) =>
        this.userManagementService.createUser(request).pipe(
          map(user => {
            this.notificationService.success('Kullanıcı başarıyla oluşturuldu', 'İşlem Başarılı');
            return UserActions.createUserSuccess({ user });
          }),
          catchError(error => {
            console.error('Create user error:', error);
            this.notificationService.error('Kullanıcı oluşturulurken bir hata oluştu', 'Hata');
            return of(UserActions.createUserFailure({ error: error.message || 'Failed to create user' }));
          })
        )
      )
    )
  );

  updateUser$ = createEffect(() =>
    this.actions$.pipe(
      ofType(UserActions.updateUser),
      exhaustMap(({ request }) =>
        this.userManagementService.updateUser(request).pipe(
          map(user => {
            this.notificationService.success('Kullanıcı başarıyla güncellendi', 'İşlem Başarılı');
            return UserActions.updateUserSuccess({ user });
          }),
          catchError(error => {
            console.error('Update user error:', error);
            this.notificationService.error('Kullanıcı güncellenirken bir hata oluştu', 'Hata');
            return of(UserActions.updateUserFailure({ error: error.message || 'Failed to update user' }));
          })
        )
      )
    )
  );

  deleteUser$ = createEffect(() =>
    this.actions$.pipe(
      ofType(UserActions.deleteUser),
      exhaustMap(({ id }) =>
        this.userManagementService.deleteUser(id).pipe(
          map(() => {
            this.notificationService.success('Kullanıcı başarıyla silindi', 'İşlem Başarılı');
            return UserActions.deleteUserSuccess({ id });
          }),
          catchError(error => {
            console.error('Delete user error:', error);
            this.notificationService.error('Kullanıcı silinirken bir hata oluştu', 'Hata');
            return of(UserActions.deleteUserFailure({ error: error.message || 'Failed to delete user' }));
          })
        )
      )
    )
  );

  bulkUserOperation$ = createEffect(() =>
    this.actions$.pipe(
      ofType(UserActions.bulkUserOperation),
      exhaustMap(({ request }) =>
        this.userManagementService.bulkUserOperation(request).pipe(
          map(() => {
            this.notificationService.success('Toplu işlem başarıyla tamamlandı', 'İşlem Başarılı');
            return UserActions.bulkUserOperationSuccess({ message: 'Bulk operation completed successfully' });
          }),
          catchError(error => {
            console.error('Bulk user operation error:', error);
            this.notificationService.error('Toplu işlem sırasında bir hata oluştu', 'Hata');
            return of(UserActions.bulkUserOperationFailure({ error: error.message || 'Failed to perform bulk operation' }));
          })
        )
      )
    )
  );

  loadUserStatistics$ = createEffect(() =>
    this.actions$.pipe(
      ofType(UserActions.loadUserStatistics),
      switchMap(() =>
        this.userManagementService.getUserStatistics().pipe(
          map(statistics => UserActions.loadUserStatisticsSuccess({ statistics })),
          catchError(error => {
            console.error('Load user statistics error:', error);
            return of(UserActions.loadUserStatisticsFailure({ error: error.message || 'Failed to load user statistics' }));
          })
        )
      )
    )
  );

  // ================================
  // ROLE EFFECTS
  // ================================

  loadRoles$ = createEffect(() =>
    this.actions$.pipe(
      ofType(RoleActions.loadRoles),
      switchMap(({ query }) =>
        this.userManagementService.getRoles(query).pipe(
          map(result => RoleActions.loadRolesSuccess({ result })),
          catchError(error => {
            console.error('Load roles error:', error);
            return of(RoleActions.loadRolesFailure({ error: error.message || 'Failed to load roles' }));
          })
        )
      )
    )
  );

  getRole$ = createEffect(() =>
    this.actions$.pipe(
      ofType(RoleActions.getRole),
      switchMap(({ id }) =>
        this.userManagementService.getRoleById(id).pipe(
          map(role => RoleActions.getRoleSuccess({ role })),
          catchError(error => {
            console.error('Get role error:', error);
            return of(RoleActions.getRoleFailure({ error: error.message || 'Failed to get role' }));
          })
        )
      )
    )
  );

  createRole$ = createEffect(() =>
    this.actions$.pipe(
      ofType(RoleActions.createRole),
      exhaustMap(({ request }) =>
        this.userManagementService.createRole(request).pipe(
          map(role => {
            this.notificationService.success('Rol başarıyla oluşturuldu', 'İşlem Başarılı');
            return RoleActions.createRoleSuccess({ role });
          }),
          catchError(error => {
            console.error('Create role error:', error);
            this.notificationService.error('Rol oluşturulurken bir hata oluştu', 'Hata');
            return of(RoleActions.createRoleFailure({ error: error.message || 'Failed to create role' }));
          })
        )
      )
    )
  );

  updateRole$ = createEffect(() =>
    this.actions$.pipe(
      ofType(RoleActions.updateRole),
      exhaustMap(({ request }) =>
        this.userManagementService.updateRole(request).pipe(
          map(role => {
            this.notificationService.success('Rol başarıyla güncellendi', 'İşlem Başarılı');
            return RoleActions.updateRoleSuccess({ role });
          }),
          catchError(error => {
            console.error('Update role error:', error);
            this.notificationService.error('Rol güncellenirken bir hata oluştu', 'Hata');
            return of(RoleActions.updateRoleFailure({ error: error.message || 'Failed to update role' }));
          })
        )
      )
    )
  );

  deleteRole$ = createEffect(() =>
    this.actions$.pipe(
      ofType(RoleActions.deleteRole),
      exhaustMap(({ id }) =>
        this.userManagementService.deleteRole(id).pipe(
          map(() => {
            this.notificationService.success('Rol başarıyla silindi', 'İşlem Başarılı');
            return RoleActions.deleteRoleSuccess({ id });
          }),
          catchError(error => {
            console.error('Delete role error:', error);
            this.notificationService.error('Rol silinirken bir hata oluştu', 'Hata');
            return of(RoleActions.deleteRoleFailure({ error: error.message || 'Failed to delete role' }));
          })
        )
      )
    )
  );

  loadRolePermissions$ = createEffect(() =>
    this.actions$.pipe(
      ofType(RoleActions.loadRolePermissions),
      switchMap(({ roleId }) =>
        this.userManagementService.getRolePermissions(roleId).pipe(
          map(permissions => RoleActions.loadRolePermissionsSuccess({ roleId, permissions })),
          catchError(error => {
            console.error('Load role permissions error:', error);
            return of(RoleActions.loadRolePermissionsFailure({ error: error.message || 'Failed to load role permissions' }));
          })
        )
      )
    )
  );

  loadRoleStatistics$ = createEffect(() =>
    this.actions$.pipe(
      ofType(RoleActions.loadRoleStatistics),
      switchMap(() =>
        this.userManagementService.getRoleStatistics().pipe(
          map(statistics => RoleActions.loadRoleStatisticsSuccess({ statistics })),
          catchError(error => {
            console.error('Load role statistics error:', error);
            return of(RoleActions.loadRoleStatisticsFailure({ error: error.message || 'Failed to load role statistics' }));
          })
        )
      )
    )
  );

  // ================================
  // PERMISSION EFFECTS
  // ================================

  loadPermissions$ = createEffect(() =>
    this.actions$.pipe(
      ofType(PermissionActions.loadPermissions),
      switchMap(({ query }) =>
        this.userManagementService.getPermissions(query).pipe(
          map(result => PermissionActions.loadPermissionsSuccess({ result })),
          catchError(error => {
            console.error('Load permissions error:', error);
            return of(PermissionActions.loadPermissionsFailure({ error: error.message || 'Failed to load permissions' }));
          })
        )
      )
    )
  );

  getPermission$ = createEffect(() =>
    this.actions$.pipe(
      ofType(PermissionActions.getPermission),
      switchMap(({ id }) =>
        this.userManagementService.getPermissionById(id).pipe(
          map(permission => PermissionActions.getPermissionSuccess({ permission })),
          catchError(error => {
            console.error('Get permission error:', error);
            return of(PermissionActions.getPermissionFailure({ error: error.message || 'Failed to get permission' }));
          })
        )
      )
    )
  );

  loadPermissionCategories$ = createEffect(() =>
    this.actions$.pipe(
      ofType(PermissionActions.loadPermissionCategories),
      switchMap(() =>
        this.userManagementService.getPermissionCategories().pipe(
          map(categories => PermissionActions.loadPermissionCategoriesSuccess({ categories })),
          catchError(error => {
            console.error('Load permission categories error:', error);
            return of(PermissionActions.loadPermissionCategoriesFailure({ error: error.message || 'Failed to load permission categories' }));
          })
        )
      )
    )
  );

  loadPermissionStatistics$ = createEffect(() =>
    this.actions$.pipe(
      ofType(PermissionActions.loadPermissionStatistics),
      switchMap(() =>
        this.userManagementService.getPermissionStatistics().pipe(
          map(statistics => PermissionActions.loadPermissionStatisticsSuccess({ statistics })),
          catchError(error => {
            console.error('Load permission statistics error:', error);
            return of(PermissionActions.loadPermissionStatisticsFailure({ error: error.message || 'Failed to load permission statistics' }));
          })
        )
      )
    )
  );

  // ================================
  // GROUP EFFECTS
  // ================================

  loadGroups$ = createEffect(() =>
    this.actions$.pipe(
      ofType(GroupActions.loadGroups),
      switchMap(({ query }) =>
        this.userManagementService.getGroups(query).pipe(
          map(result => GroupActions.loadGroupsSuccess({ result })),
          catchError(error => {
            console.error('Load groups error:', error);
            return of(GroupActions.loadGroupsFailure({ error: error.message || 'Failed to load groups' }));
          })
        )
      )
    )
  );

  getGroup$ = createEffect(() =>
    this.actions$.pipe(
      ofType(GroupActions.getGroup),
      switchMap(({ id }) =>
        this.userManagementService.getGroupById(id).pipe(
          map(group => GroupActions.getGroupSuccess({ group })),
          catchError(error => {
            console.error('Get group error:', error);
            return of(GroupActions.getGroupFailure({ error: error.message || 'Failed to get group' }));
          })
        )
      )
    )
  );

  createGroup$ = createEffect(() =>
    this.actions$.pipe(
      ofType(GroupActions.createGroup),
      exhaustMap(({ request }) =>
        this.userManagementService.createGroup(request).pipe(
          map(group => {
            this.notificationService.success('Grup başarıyla oluşturuldu', 'İşlem Başarılı');
            return GroupActions.createGroupSuccess({ group });
          }),
          catchError(error => {
            console.error('Create group error:', error);
            this.notificationService.error('Grup oluşturulurken bir hata oluştu', 'Hata');
            return of(GroupActions.createGroupFailure({ error: error.message || 'Failed to create group' }));
          })
        )
      )
    )
  );

  updateGroup$ = createEffect(() =>
    this.actions$.pipe(
      ofType(GroupActions.updateGroup),
      exhaustMap(({ request }) =>
        this.userManagementService.updateGroup(request).pipe(
          map(group => {
            this.notificationService.success('Grup başarıyla güncellendi', 'İşlem Başarılı');
            return GroupActions.updateGroupSuccess({ group });
          }),
          catchError(error => {
            console.error('Update group error:', error);
            this.notificationService.error('Grup güncellenirken bir hata oluştu', 'Hata');
            return of(GroupActions.updateGroupFailure({ error: error.message || 'Failed to update group' }));
          })
        )
      )
    )
  );

  deleteGroup$ = createEffect(() =>
    this.actions$.pipe(
      ofType(GroupActions.deleteGroup),
      exhaustMap(({ id }) =>
        this.userManagementService.deleteGroup(id).pipe(
          map(() => {
            this.notificationService.success('Grup başarıyla silindi', 'İşlem Başarılı');
            return GroupActions.deleteGroupSuccess({ id });
          }),
          catchError(error => {
            console.error('Delete group error:', error);
            this.notificationService.error('Grup silinirken bir hata oluştu', 'Hata');
            return of(GroupActions.deleteGroupFailure({ error: error.message || 'Failed to delete group' }));
          })
        )
      )
    )
  );

  loadGroupStatistics$ = createEffect(() =>
    this.actions$.pipe(
      ofType(GroupActions.loadGroupStatistics),
      switchMap(() =>
        this.userManagementService.getGroupStatistics().pipe(
          map(statistics => GroupActions.loadGroupStatisticsSuccess({ statistics })),
          catchError(error => {
            console.error('Load group statistics error:', error);
            return of(GroupActions.loadGroupStatisticsFailure({ error: error.message || 'Failed to load group statistics' }));
          })
        )
      )
    )
  );

  // ================================
  // SIDE EFFECTS (Auto-refresh after operations)
  // ================================

  refreshUsersAfterOperation$ = createEffect(() =>
    this.actions$.pipe(
      ofType(
        UserActions.createUserSuccess,
        UserActions.updateUserSuccess,
        UserActions.deleteUserSuccess,
        UserActions.bulkUserOperationSuccess
      ),
      map(() => UserActions.loadUserStatistics())
    )
  );

  refreshRolesAfterOperation$ = createEffect(() =>
    this.actions$.pipe(
      ofType(
        RoleActions.createRoleSuccess,
        RoleActions.updateRoleSuccess,
        RoleActions.deleteRoleSuccess
      ),
      map(() => RoleActions.loadRoleStatistics())
    )
  );

  refreshGroupsAfterOperation$ = createEffect(() =>
    this.actions$.pipe(
      ofType(
        GroupActions.createGroupSuccess,
        GroupActions.updateGroupSuccess,
        GroupActions.deleteGroupSuccess
      ),
      map(() => GroupActions.loadGroupStatistics())
    )
  );
}