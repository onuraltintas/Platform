import { Routes } from '@angular/router';

export const groupManagementRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('../user-management/components/group-list/group-list.component').then(m => m.GroupListComponent),
    title: 'Grup Yönetimi - PlatformV1'
  }
];