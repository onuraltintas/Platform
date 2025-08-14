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
import { SrTextsListComponent } from './features/speed-reading/ui/texts/sr-texts-list.component';
import { SrTextsFormComponent } from './features/speed-reading/ui/texts/sr-texts-form.component';
import { SrExercisesListComponent } from './features/speed-reading/ui/exercises/sr-exercises-list.component';
import { SrExercisesFormComponent } from './features/speed-reading/ui/exercises/sr-exercises-form.component';
import { SrQuestionsListComponent } from './features/speed-reading/ui/questions/sr-questions-list.component';
import { SrLevelsListComponent } from './features/speed-reading/ui/levels/sr-levels-list.component';
import { SrReportsComponent } from './features/speed-reading/ui/reports/sr-reports.component';
import { SrQuestionsFormComponent } from './features/speed-reading/ui/questions/sr-questions-form.component';
import { SrLevelsFormComponent } from './features/speed-reading/ui/levels/sr-levels-form.component';

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
      // Speed Reading Admin
      { path: 'sr/texts', component: SrTextsListComponent, canActivate: [permissionGuard], data: { requiredPermissions: ['sr.content.manage'] } },
      { path: 'sr/texts/new', component: SrTextsFormComponent, canActivate: [permissionGuard], data: { requiredPermissions: ['sr.content.manage'] } },
      { path: 'sr/texts/:id', component: SrTextsFormComponent, canActivate: [permissionGuard], data: { requiredPermissions: ['sr.content.manage'] } },
      { path: 'sr/exercises', component: SrExercisesListComponent, canActivate: [permissionGuard], data: { requiredPermissions: ['sr.content.manage'] } },
      { path: 'sr/exercises/new', component: SrExercisesFormComponent, canActivate: [permissionGuard], data: { requiredPermissions: ['sr.content.manage'] } },
      { path: 'sr/exercises/:id', component: SrExercisesFormComponent, canActivate: [permissionGuard], data: { requiredPermissions: ['sr.content.manage'] } },
      { path: 'sr/questions', component: SrQuestionsListComponent, canActivate: [permissionGuard], data: { requiredPermissions: ['sr.content.manage'] } },
      { path: 'sr/questions/new', component: SrQuestionsFormComponent, canActivate: [permissionGuard], data: { requiredPermissions: ['sr.content.manage'] } },
      { path: 'sr/questions/:id', component: SrQuestionsFormComponent, canActivate: [permissionGuard], data: { requiredPermissions: ['sr.content.manage'] } },
      { path: 'sr/levels', component: SrLevelsListComponent, canActivate: [permissionGuard], data: { requiredPermissions: ['sr.content.manage'] } },
      { path: 'sr/levels/new', component: SrLevelsFormComponent, canActivate: [permissionGuard], data: { requiredPermissions: ['sr.content.manage'] } },
      { path: 'sr/levels/:id', component: SrLevelsFormComponent, canActivate: [permissionGuard], data: { requiredPermissions: ['sr.content.manage'] } },
      { path: 'sr/reports', component: SrReportsComponent, canActivate: [permissionGuard], data: { requiredPermissions: ['sr.progress.read.all'] } },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
