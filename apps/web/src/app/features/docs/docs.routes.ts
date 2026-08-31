import { Routes } from '@angular/router';

export const DOCS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/docs.page').then((m) => m.DocsPage),
  },
];
