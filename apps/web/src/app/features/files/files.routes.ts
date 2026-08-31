import { Routes } from '@angular/router';

export const FILES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/file-list.page').then((m) => m.FileListPage),
  },
  {
    path: 'search',
    loadComponent: () => import('./pages/schema-search.page').then((m) => m.SchemaSearchPage),
  },
  {
    path: ':fileId/documents/:documentId',
    loadComponent: () =>
      import('./pages/document-detail.page').then((m) => m.DocumentDetailPage),
  },
  {
    path: ':fileId',
    loadComponent: () => import('./pages/file-detail.page').then((m) => m.FileDetailPage),
  },
];
