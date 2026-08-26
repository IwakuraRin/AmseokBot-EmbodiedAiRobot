import { Routes } from '@angular/router';

export const WEB_USER_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./lib/pages/web-user-list-page/web-user-list-page').then(
        ({ WebUserListPage }) => WebUserListPage,
      ),
  },
];
