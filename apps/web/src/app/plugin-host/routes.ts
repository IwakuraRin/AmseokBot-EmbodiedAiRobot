import { Routes } from '@angular/router';

export const PLUGIN_HOST_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./lib/pages/plugin-catalog-page/plugin-catalog-page').then(
        ({ PluginCatalogPage }) => PluginCatalogPage,
      ),
  },
  {
    path: ':pluginId/:pageId',
    loadComponent: () =>
      import('./lib/pages/plugin-page/plugin-page').then(({ PluginPage }) => PluginPage),
  },
];
