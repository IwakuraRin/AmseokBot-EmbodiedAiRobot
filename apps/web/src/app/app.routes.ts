import { Routes } from '@angular/router';
import { authenticatedGuard, permissionGuard } from './core/authorization/authorization';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: '',
    loadChildren: () =>
      import('./features/authentication/routes').then(
        ({ AUTHENTICATION_ROUTES }) => AUTHENTICATION_ROUTES,
      ),
  },
  {
    path: '',
    canActivate: [authenticatedGuard],
    loadComponent: () => import('./layout/shell').then(({ AppShell }) => AppShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        canActivate: [permissionGuard('system.overview.read')],
        loadChildren: () =>
          import('./features/dashboard/routes').then(({ DASHBOARD_ROUTES }) => DASHBOARD_ROUTES),
      },
      {
        path: 'disks',
        canActivate: [permissionGuard('storage.read')],
        loadChildren: () =>
          import('./features/disks/routes').then(({ DISK_ROUTES }) => DISK_ROUTES),
      },
      {
        path: 'plugins',
        canActivate: [permissionGuard('plugins.manage')],
        loadChildren: () =>
          import('./plugin-host/routes').then(({ PLUGIN_HOST_ROUTES }) => PLUGIN_HOST_ROUTES),
      },
      {
        path: 'web-users',
        canActivate: [permissionGuard('web.users.manage')],
        loadChildren: () =>
          import('./features/web-users/routes').then(({ WEB_USER_ROUTES }) => WEB_USER_ROUTES),
      },
      {
        path: 'audit',
        canActivate: [permissionGuard('audit.read')],
        loadChildren: () =>
          import('./features/audit/routes').then(({ AUDIT_ROUTES }) => AUDIT_ROUTES),
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
