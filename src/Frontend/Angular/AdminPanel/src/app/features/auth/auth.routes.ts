import { Routes } from '@angular/router';
import { AuthLayoutComponent } from '../../layout/auth-layout/auth-layout.component';

export const authRoutes: Routes = [
  {
    path: '',
    component: AuthLayoutComponent,
    children: [
      {
        path: '',
        redirectTo: 'login',
        pathMatch: 'full'
      },
      {
        path: 'login',
        loadComponent: () => import('./components/login/login.component').then(m => m.LoginComponent),
        title: 'Giriş Yap - PlatformV1'
      },
      {
        path: 'register',
        loadComponent: () => import('./components/register/register.component').then(m => m.RegisterComponent),
        title: 'Kayıt Ol - PlatformV1'
      },
      {
        path: 'forgot-password',
        loadComponent: () => import('./components/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent),
        title: 'Şifre Sıfırlama - PlatformV1'
      },
      {
        path: 'reset-password',
        loadComponent: () => import('./components/reset-password/reset-password.component').then(m => m.ResetPasswordComponent),
        title: 'Şifre Yenileme - PlatformV1'
      },
      {
        path: 'verify-email',
        loadComponent: () => import('./components/verify-email/verify-email.component').then(m => m.VerifyEmailComponent),
        title: 'E-posta Doğrulama - PlatformV1'
      },
      {
        path: 'google/callback',
        loadComponent: () => import('./components/google-callback/google-callback.component').then(m => m.GoogleCallbackComponent),
        title: 'Google Girişi - PlatformV1'
      }
    ]
  }
];