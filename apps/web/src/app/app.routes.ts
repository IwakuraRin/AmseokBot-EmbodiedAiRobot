import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./layout/shell').then(({ AppShell }) => AppShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadChildren: () =>
          import('./features/dashboard/routes').then(({ DASHBOARD_ROUTES }) => DASHBOARD_ROUTES),
      },
      {
        path: 'disks',
        loadChildren: () =>
          import('./features/disks/routes').then(({ DISK_ROUTES }) => DISK_ROUTES),
      },
      {
        path: 'plugins',
        loadChildren: () =>
          import('./plugin-host/routes').then(({ PLUGIN_HOST_ROUTES }) => PLUGIN_HOST_ROUTES),
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
