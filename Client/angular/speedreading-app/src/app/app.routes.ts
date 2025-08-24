import { Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/home', pathMatch: 'full' },
  { 
    path: 'home', 
    loadComponent: () => import('./pages/home/home.component').then(c => c.HomeComponent)
  },
  {
    path: 'auth',
    children: [
      {
        path: 'login',
        loadComponent: () => import('./features/auth/login/login.component').then(c => c.LoginComponent)
      },
      {
        path: 'register',
        loadComponent: () => import('./features/auth/register/register.component').then(c => c.RegisterComponent)
      },
      {
        path: 'forgot-password',
        loadComponent: () => import('./features/auth/forgot-password/forgot-password.component').then(c => c.ForgotPasswordComponent)
      },
      {
        // Primary route expected by backend redirect
        path: 'google-callback',
        loadComponent: () => import('./features/auth/google-callback/google-callback.component').then(c => c.GoogleCallbackComponent)
      },
      {
        // Backward-compat alias
        path: 'google/callback',
        loadComponent: () => import('./features/auth/google-callback/google-callback.component').then(c => c.GoogleCallbackComponent)
      },
      {
        path: 'confirm-email',
        loadComponent: () => import('./features/auth/confirm-email/confirm-email.component').then(c => c.ConfirmEmailComponent)
      }
    ]
  },
  // Public alias to support email links
  {
    path: 'reset-password',
    loadComponent: () => import('./features/auth/reset-password/reset-password.component').then(c => c.ResetPasswordComponent)
  },
  {
    path: 'dashboard',
    canActivate: [AuthGuard],
    loadComponent: () => import('./features/dashboard/dashboard.component').then(c => c.DashboardComponent)
  },
  {
    path: 'statistics',
    canActivate: [AuthGuard],
    loadComponent: () => import('./features/statistics/statistics.component').then(c => c.StatisticsComponent)
  },
  {
    path: 'reading',
    canActivate: [AuthGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./features/reading/components/reading-interface.component').then(c => c.ReadingInterfaceComponent)
      },
      {
        path: 'settings',
        loadComponent: () => import('./features/reading/components/reading-settings.component').then(c => c.ReadingSettingsComponent)
      },
      {
        path: 'start',
        loadComponent: () => import('./features/reading/components/reading-page.component').then(c => c.ReadingPageComponent)
      },
      {
        path: 'texts',
        loadComponent: () => import('./features/reading/components/texts-list.component').then(c => c.TextsListComponent)
      }
    ]
  },
  {
    path: 'muscle',
    canActivate: [AuthGuard],
    loadComponent: () => import('./features/reading/components/reading-interface.component').then(c => c.ReadingInterfaceComponent)
  },
  { path: '**', redirectTo: '/home' }
];
