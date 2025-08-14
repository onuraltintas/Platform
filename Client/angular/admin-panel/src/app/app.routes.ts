import { Routes } from '@angular/router';
import { LoginComponent } from './features/access-control/feature/auth/login/login.component';
import { RegisterComponent } from './features/access-control/feature/auth/register/register.component';
import { ForgotPasswordComponent } from './features/access-control/feature/auth/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './features/access-control/feature/auth/reset-password/reset-password.component';
import { GoogleCallbackComponent } from './features/access-control/feature/auth/google-callback/google-callback.component';
import { ConfirmEmailComponent } from './features/access-control/feature/auth/confirm-email/confirm-email.component';
import { DashboardComponent } from './features/dashboard/feature/dashboard/dashboard.component';
import { LayoutComponent } from './core/layout/layout/layout.component';
import { authGuard } from './core/guards/guards/auth.guard';
import { permissionGuard } from './core/guards/guards/permission.guard';
import { UsersListComponent } from './features/users/ui/users/users-list.component';
import { UserFormComponent } from './features/users/ui/users/user-form.component';
import { RolesListComponent } from './features/access-control/ui/roles/roles-list.component';
import { RoleFormComponent } from './features/access-control/ui/roles/role-form.component';
import { PermissionsListComponent } from './features/access-control/ui/permissions/permissions-list.component';
import { PermissionFormComponent } from './features/access-control/ui/permissions/permission-form.component';
import { CategoriesListComponent } from './features/categories/ui/categories/categories-list.component';
import { CategoryFormComponent } from './features/categories/ui/categories/category-form.component';
import { ProfileComponent } from './features/profile/feature/profile/profile.component';
import { SettingsComponent } from './features/profile/feature/profile/settings.component';

export const routes: Routes = [
  // Public auth routes (with /auth prefix)
  {
    path: 'auth',
    children: [
      { path: 'login', component: LoginComponent },
      { path: 'register', component: RegisterComponent },
      { path: 'forgot-password', component: ForgotPasswordComponent },
      { path: 'reset-password', component: ResetPasswordComponent },
      { path: 'confirm-email', component: ConfirmEmailComponent },
      { path: 'google-callback', component: GoogleCallbackComponent },
    ],
  },
  // Public aliases (without /auth prefix) to support links coming from emails
  { path: 'reset-password', component: ResetPasswordComponent },
  { path: 'confirm-email', component: ConfirmEmailComponent },

  // Protected app routes
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'users', component: UsersListComponent, canActivate: [permissionGuard] },
      { path: 'users/new', component: UserFormComponent, canActivate: [permissionGuard] },
      { path: 'users/:id', component: UserFormComponent, canActivate: [permissionGuard] },
      { path: 'roles', component: RolesListComponent, canActivate: [permissionGuard] },
      { path: 'roles/new', component: RoleFormComponent, canActivate: [permissionGuard] },
      { path: 'roles/:id', component: RoleFormComponent, canActivate: [permissionGuard] },
      { path: 'permissions', component: PermissionsListComponent, canActivate: [permissionGuard] },
      { path: 'permissions/new', component: PermissionFormComponent, canActivate: [permissionGuard] },
      { path: 'permissions/:id', component: PermissionFormComponent, canActivate: [permissionGuard] },
      { path: 'categories', component: CategoriesListComponent, canActivate: [permissionGuard] },
      { path: 'categories/new', component: CategoryFormComponent, canActivate: [permissionGuard] },
      { path: 'categories/:id', component: CategoryFormComponent, canActivate: [permissionGuard] },
      { path: 'profile', component: ProfileComponent },
      { path: 'settings', component: SettingsComponent },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
