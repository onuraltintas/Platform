import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/guards/auth.guard';
import { permissionGuard } from '../../core/auth/guards/permission.guard';

export const USER_MANAGEMENT_ROUTES: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./components/dashboard/user-management-dashboard.component').then(
            (m) => m.UserManagementDashboardComponent
          ),
        canActivate: [permissionGuard],
        data: {
          title: 'Kullanıcı Yönetimi',
          breadcrumb: 'Ana Sayfa',
          permissions: ['Identity.Users.Read', 'Identity.Roles.Read', 'Identity.Permissions.Read', 'Identity.Groups.Read']
        }
      },

      // Users Routes
      {
        path: 'users',
        data: {
          title: 'Kullanıcılar',
          breadcrumb: 'Kullanıcılar'
        },
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./components/users/user-list/user-list.component').then(
                (m) => m.UserListComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Kullanıcı Listesi',
              permissions: ['Identity.Users.Read']
            }
          },
          {
            path: 'create',
            loadComponent: () =>
              import('./components/users/user-form/user-form.component').then(
                (m) => m.UserFormComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Yeni Kullanıcı',
              breadcrumb: 'Yeni Kullanıcı',
              permissions: ['Identity.Users.Create']
            }
          },
          {
            path: ':id',
            loadComponent: () =>
              import('./components/users/user-detail/user-detail.component').then(
                (m) => m.UserDetailComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Kullanıcı Detay',
              breadcrumb: 'Detay',
              permissions: ['Identity.Users.Read']
            }
          },
          {
            path: ':id/edit',
            loadComponent: () =>
              import('./components/users/user-form/user-form.component').then(
                (m) => m.UserFormComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Kullanıcı Düzenle',
              breadcrumb: 'Düzenle',
              permissions: ['Identity.Users.Update']
            }
          }
        ]
      },

      // Roles Routes
      {
        path: 'roles',
        data: {
          title: 'Roller',
          breadcrumb: 'Roller'
        },
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./components/roles/role-list/role-list.component').then(
                (m) => m.RoleListComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Rol Listesi',
              permissions: ['Identity.Roles.Read']
            }
          },
          {
            path: 'create',
            loadComponent: () =>
              import('./components/roles/role-form/role-form.component').then(
                (m) => m.RoleFormComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Yeni Rol',
              breadcrumb: 'Yeni Rol',
              permissions: ['Identity.Roles.Create']
            }
          },
          {
            path: ':id',
            loadComponent: () =>
              import('./components/roles/role-detail/role-detail.component').then(
                (m) => m.RoleDetailComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Rol Detay',
              breadcrumb: 'Detay',
              permissions: ['Identity.Roles.Read']
            }
          },
          {
            path: ':id/edit',
            loadComponent: () =>
              import('./components/roles/role-form/role-form.component').then(
                (m) => m.RoleFormComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Rol Düzenle',
              breadcrumb: 'Düzenle',
              permissions: ['Identity.Roles.Update']
            }
          },
          {
            path: ':id/clone',
            loadComponent: () =>
              import('./components/roles/role-clone/role-clone.component').then(
                (m) => m.RoleCloneComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Rol Kopyala',
              breadcrumb: 'Kopyala',
              permissions: ['Identity.Roles.Create']
            }
          },
          {
            path: ':id/permissions',
            loadComponent: () =>
              import('./components/roles/role-permissions/role-permissions.component').then(
                (m) => m.RolePermissionsComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Rol Yetkileri',
              breadcrumb: 'Yetkiler',
              permissions: ['Identity.Roles.Read', 'Identity.Permissions.Read']
            }
          }
        ]
      },

      // Permissions Routes
      {
        path: 'permissions',
        data: {
          title: 'Yetkiler',
          breadcrumb: 'Yetkiler'
        },
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./components/permissions/permission-list/permission-list.component').then(
                (m) => m.PermissionListComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Yetki Listesi',
              permissions: ['Identity.Permissions.Read']
            }
          },
          {
            path: 'matrix',
            loadComponent: () =>
              import('./components/permissions/permission-matrix/permission-matrix.component').then(
                (m) => m.PermissionMatrixComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Yetki Matrisi',
              breadcrumb: 'Yetki Matrisi',
              permissions: ['Identity.Permissions.Read', 'Identity.Roles.Read']
            }
          },
          {
            path: 'by-service',
            loadComponent: () =>
              import('./components/permissions/permission-by-service/permission-by-service.component').then(
                (m) => m.PermissionByServiceComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Servis Yetkileri',
              breadcrumb: 'Servis Bazında',
              permissions: ['Identity.Permissions.Read']
            }
          },
          {
            path: 'create',
            loadComponent: () =>
              import('./components/permissions/permission-form/permission-form.component').then(
                (m) => m.PermissionFormComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Yeni Yetki',
              breadcrumb: 'Yeni Yetki',
              permissions: ['Identity.Permissions.Create']
            }
          }
        ]
      },

      // Groups Routes
      {
        path: 'groups',
        data: {
          title: 'Gruplar',
          breadcrumb: 'Gruplar'
        },
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./components/groups/group-advanced-list/group-advanced-list.component').then(
                (m) => m.GroupAdvancedListComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Grup Listesi',
              permissions: ['Identity.Groups.Read']
            }
          },
          {
            path: 'create',
            loadComponent: () =>
              import('./components/groups/group-form/group-form.component').then(
                (m) => m.GroupFormComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Yeni Grup',
              breadcrumb: 'Yeni Grup',
              permissions: ['Identity.Groups.Create']
            }
          },
          {
            path: ':id',
            loadComponent: () =>
              import('./components/groups/group-detail/group-detail.component').then(
                (m) => m.GroupDetailComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Grup Detay',
              breadcrumb: 'Detay',
              permissions: ['Identity.Groups.Read']
            }
          },
          {
            path: ':id/edit',
            loadComponent: () =>
              import('./components/groups/group-form/group-form.component').then(
                (m) => m.GroupFormComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Grup Düzenle',
              breadcrumb: 'Düzenle',
              permissions: ['Identity.Groups.Update']
            }
          },
          {
            path: ':id/clone',
            loadComponent: () =>
              import('./components/groups/group-clone/group-clone.component').then(
                (m) => m.GroupCloneComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Grup Kopyala',
              breadcrumb: 'Kopyala',
              permissions: ['Identity.Groups.Create']
            }
          },
          {
            path: ':id/members',
            loadComponent: () =>
              import('./components/groups/group-advanced-list/group-advanced-list.component').then(
                (m) => m.GroupAdvancedListComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Grup Üye Yönetimi',
              breadcrumb: 'Üye Yönetimi',
              permissions: ['Identity.Groups.Read', 'Identity.Users.Read']
            }
          },
          {
            path: ':id/permissions',
            loadComponent: () =>
              import('./components/groups/group-permission-overview/group-permission-overview.component').then(
                (m) => m.GroupPermissionOverviewComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Grup İzin Yönetimi',
              breadcrumb: 'İzin Yönetimi',
              permissions: ['Identity.Groups.Read', 'Identity.Permissions.Read']
            }
          },
          {
            path: ':id/analytics',
            loadComponent: () =>
              import('./components/groups/group-analytics/group-analytics.component').then(
                (m) => m.GroupAnalyticsComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Grup Analitikleri',
              breadcrumb: 'Analitikler',
              permissions: ['Identity.Groups.Read', 'Identity.Analytics.Read']
            }
          },
          {
            path: ':id/activity',
            loadComponent: () =>
              import('./components/groups/group-activity/group-activity.component').then(
                (m) => m.GroupActivityComponent
              ),
            canActivate: [permissionGuard],
            data: {
              title: 'Grup Aktivitesi',
              breadcrumb: 'Aktivite',
              permissions: ['Identity.Groups.Read', 'Identity.Audit.Read']
            }
          }
        ]
      }
    ]
  }
];

export default USER_MANAGEMENT_ROUTES;