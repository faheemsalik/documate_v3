import { Routes } from '@angular/router';

export const AGENTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/agent-list.page').then((m) => m.AgentListPage),
  },
  {
    path: 'templates',
    loadComponent: () => import('./pages/agent-templates.page').then((m) => m.AgentTemplatesPage),
  },
  {
    path: ':id',
    loadComponent: () => import('./pages/agent-edit.page').then((m) => m.AgentEditPage),
  },
];
