import { Routes } from '@angular/router';

export const DISK_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./lib/pages/disk-list-page/disk-list-page').then(
        ({ DiskListPage }) => DiskListPage,
      ),
  },
];
