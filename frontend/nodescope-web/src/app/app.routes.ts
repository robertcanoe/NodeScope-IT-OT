import { Routes } from '@angular/router';

import { authGuard } from './core/auth.guard';
import { guestGuard } from './core/guest.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/login/login.component').then((module) => module.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./core/layout/main-layout.component').then((module) => module.MainLayoutComponent),
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard-page.component').then(
            (module) => module.DashboardPageComponent,
          ),
      },
      {
        path: 'projects',
        loadComponent: () =>
          import('./features/projects/projects-page.component').then(
            (module) => module.ProjectsPageComponent,
          ),
      },
      {
        path: 'projects/:projectId',
        loadComponent: () =>
          import('./features/projects/project-detail/project-detail.component').then(
            (module) => module.ProjectDetailComponent,
          ),
      },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', redirectTo: '' },
];
