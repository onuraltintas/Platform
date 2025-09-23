import { Routes } from '@angular/router';
import { authGuard } from './core/auth/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/auth/login',
    pathMatch: 'full'
  },
  {
    path: 'terms',
    loadComponent: () => import('./features/terms/terms.component').then(m => m.TermsComponent),
    title: 'Kullanım Şartları - OnAl'
  },
  {
    path: 'privacy',
    loadComponent: () => import('./features/privacy/privacy.component').then(m => m.PrivacyComponent),
    title: 'Gizlilik Politikası - OnAl'
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.authRoutes)
  },
  {
    path: 'admin',
    loadComponent: () => import('./layout/modern-admin/modern-admin-layout.component').then(m => m.ModernAdminLayoutComponent),
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./features/home/module-hub.component').then(m => m.ModuleHubComponent),
        title: 'Ana - OnAl'
      },
      // User Management System - Complete with Users, Roles, Permissions, and Groups
      {
        path: 'user-management',
        loadChildren: () => import('./features/user-management/user-management.routes').then(m => m.USER_MANAGEMENT_ROUTES),
        title: 'Kullanıcı Yönetimi - OnAl'
      },
      // Backward compatibility routes
      {
        path: 'users',
        redirectTo: 'user-management/users',
        pathMatch: 'prefix'
      },
      {
        path: 'groups',
        redirectTo: 'user-management/groups',
        pathMatch: 'prefix'
      },
      {
        path: 'speed-reading',
        loadChildren: () => import('./features/speed-reading/speed-reading.routes').then(m => m.speedReadingRoutes),
        title: 'Hızlı Okuma - OnAl'
      },
      {
        path: 'profile',
        loadChildren: () => import('./features/profile/profile.routes').then(m => m.profileRoutes),
        title: 'Profil - OnAl'
      },
      {
        path: 'settings',
        loadChildren: () => import('./features/settings/settings.routes').then(m => m.settingsRoutes),
        title: 'Ayarlar - OnAl'
      }
    ]
  },
  {
    path: '403',
    loadComponent: () => import('./shared/components/error-pages/forbidden/forbidden.component').then(m => m.ForbiddenComponent),
    title: 'Erişim Reddedildi - OnAl'
  },
  {
    path: '404',
    loadComponent: () => import('./shared/components/error-pages/not-found/not-found.component').then(m => m.NotFoundComponent),
    title: 'Sayfa Bulunamadı - OnAl'
  },
  {
    path: '**',
    redirectTo: '/404'
  }
];
