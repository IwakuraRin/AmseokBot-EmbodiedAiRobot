import { Routes } from '@angular/router';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./lib/pages/dashboard-page/dashboard-page').then(
        ({ DashboardPage }) => DashboardPage,
      ),
  },
];
