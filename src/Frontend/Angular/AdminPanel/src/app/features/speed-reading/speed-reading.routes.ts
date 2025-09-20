import { Routes } from '@angular/router';

export const speedReadingRoutes: Routes = [
  {
    path: '',
    redirectTo: 'texts',
    pathMatch: 'full'
  },
  {
    path: 'texts',
    loadComponent: () => import('./components/text-library/text-library.component').then(m => m.TextLibraryComponent),
    title: 'Metin Kütüphanesi'
  },
  {
    path: 'texts/create',
    loadComponent: () => import('./components/text-form/text-form.component').then(m => m.TextFormComponent),
    title: 'Yeni Metin'
  },
  {
    path: 'texts/:id/edit',
    loadComponent: () => import('./components/text-form/text-form.component').then(m => m.TextFormComponent),
    title: 'Metin Düzenle'
  },
  {
    path: 'exercises',
    loadComponent: () => import('./components/exercise-builder/exercise-builder.component').then(m => m.ExerciseBuilderComponent),
    title: 'Egzersiz Yönetimi'
  },
  {
    path: 'exercises/create',
    loadComponent: () => import('./components/exercise-form/exercise-form.component').then(m => m.ExerciseFormComponent),
    title: 'Yeni Egzersiz'
  },
  {
    path: 'exercises/:id/edit',
    loadComponent: () => import('./components/exercise-form/exercise-form.component').then(m => m.ExerciseFormComponent),
    title: 'Egzersiz Düzenle'
  },
  {
    path: 'progress',
    loadComponent: () => import('./components/progress-dashboard/progress-dashboard.component').then(m => m.ProgressDashboardComponent),
    title: 'İlerleme Takibi'
  },
  {
    path: 'analytics',
    loadComponent: () => import('./components/analytics-report/analytics-report.component').then(m => m.AnalyticsReportComponent),
    title: 'Analitik Raporlar'
  }
];