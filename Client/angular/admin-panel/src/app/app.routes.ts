import { Routes } from '@angular/router';
import { LoginComponent } from './auth/login/login.component';
import { RegisterComponent } from './auth/register/register.component';
import { ForgotPasswordComponent } from './auth/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './auth/reset-password/reset-password.component';
import { GoogleCallbackComponent } from './auth/google-callback/google-callback.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { LayoutComponent } from './layout/layout.component';
import { authGuard } from './guards/auth.guard';
import { permissionGuard } from './guards/permission.guard';
import { UsersListComponent } from './users/users-list.component';
import { ConfirmEmailComponent } from './auth/confirm-email/confirm-email.component';
import { UserFormComponent } from './users/user-form.component';
import { RolesListComponent } from './roles/roles-list.component';
import { RoleFormComponent } from './roles/role-form.component';
import { PermissionsListComponent } from './permissions/permissions-list.component';
import { PermissionFormComponent } from './permissions/permission-form.component';
import { CategoriesListComponent } from './categories/categories-list.component';
import { CategoryFormComponent } from './categories/category-form.component';
import { ProfileComponent } from './profile/profile.component';
import { SettingsComponent } from './profile/settings.component';

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
