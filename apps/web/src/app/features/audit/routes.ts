import { Routes } from '@angular/router';

export const AUDIT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./lib/pages/audit-log-page/audit-log-page').then(
        ({ AuditLogPage }) => AuditLogPage,
      ),
  },
];
