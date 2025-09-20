import { Routes } from '@angular/router';
import { authGuard } from './core/auth/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/auth/login',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.authRoutes)
  },
  {
    path: '',
    loadComponent: () => import('./layout/admin/admin-layout.component').then(m => m.AdminLayoutComponent),
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
        title: 'Dashboard - PlatformV1'
      },
      // Simple User Management - Clean and functional
      {
        path: 'users',
        loadChildren: () => import('./features/user-management/simple-user-management.routes').then(m => m.simpleUserManagementRoutes)
      },
      {
        path: 'groups',
        loadChildren: () => import('./features/group-management/group-management.routes').then(m => m.groupManagementRoutes)
      },
      {
        path: 'speed-reading',
        loadChildren: () => import('./features/speed-reading/speed-reading.routes').then(m => m.speedReadingRoutes),
        title: 'Hızlı Okuma - PlatformV1'
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
        title: 'Profil - PlatformV1'
      },
      {
        path: 'settings',
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
        title: 'Ayarlar - PlatformV1'
      }
    ]
  },
  {
    path: '403',
    loadComponent: () => import('./shared/components/error-pages/forbidden/forbidden.component').then(m => m.ForbiddenComponent),
    title: 'Erişim Reddedildi - PlatformV1'
  },
  {
    path: '404',
    loadComponent: () => import('./shared/components/error-pages/not-found/not-found.component').then(m => m.NotFoundComponent),
    title: 'Sayfa Bulunamadı - PlatformV1'
  },
  {
    path: '**',
    redirectTo: '/404'
  }
];
