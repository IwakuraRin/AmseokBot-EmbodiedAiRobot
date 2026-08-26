import { Routes } from '@angular/router';
import { setupGuard } from '../../core/authorization/authorization';

export const AUTHENTICATION_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./lib/pages/login-page/login-page').then(({ LoginPage }) => LoginPage),
  },
  {
    path: 'setup',
    canActivate: [setupGuard],
    loadComponent: () =>
      import('./lib/pages/setup-owner-page/setup-owner-page').then(
        ({ SetupOwnerPage }) => SetupOwnerPage,
      ),
  },
];
