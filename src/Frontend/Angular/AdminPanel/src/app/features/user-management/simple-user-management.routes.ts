import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/guards/auth.guard';

export const simpleUserManagementRoutes: Routes = [
  {
    path: '',
    redirectTo: 'users',
    pathMatch: 'full'
  },
  // Users
  {
    path: 'users',
    loadComponent: () =>
      import('./components/users/user-list.component').then(m => m.UserListComponent),
    canActivate: [authGuard],
    data: {
      breadcrumb: 'Kullanıcılar'
    },
    title: 'Kullanıcılar - PlatformV1'
  },
  {
    path: 'users/create',
    loadComponent: () =>
      import('./components/users/user-form.component').then(m => m.UserFormComponent),
    canActivate: [authGuard],
    data: {
      breadcrumb: 'Yeni Kullanıcı'
    },
    title: 'Yeni Kullanıcı - PlatformV1'
  },
  {
    path: 'users/:id/edit',
    loadComponent: () =>
      import('./components/users/user-form.component').then(m => m.UserFormComponent),
    canActivate: [authGuard],
    data: {
      breadcrumb: 'Kullanıcı Düzenle'
    },
    title: 'Kullanıcı Düzenle - PlatformV1'
  },
  // Groups
  {
    path: 'groups',
    loadComponent: () =>
      import('./components/groups/group-list.component').then(m => m.GroupListComponent),
    canActivate: [authGuard],
    data: {
      breadcrumb: 'Gruplar'
    },
    title: 'Gruplar - PlatformV1'
  },
  {
    path: 'groups/create',
    loadComponent: () =>
      import('./components/groups/group-form.component').then(m => m.GroupFormComponent),
    canActivate: [authGuard],
    data: {
      breadcrumb: 'Yeni Grup'
    },
    title: 'Yeni Grup - PlatformV1'
  },
  {
    path: 'groups/:id/edit',
    loadComponent: () =>
      import('./components/groups/group-form.component').then(m => m.GroupFormComponent),
    canActivate: [authGuard],
    data: {
      breadcrumb: 'Grup Düzenle'
    },
    title: 'Grup Düzenle - PlatformV1'
  }
];