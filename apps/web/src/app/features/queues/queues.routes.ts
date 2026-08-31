import { Routes } from '@angular/router';

export const QUEUES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/queue-overview.page').then((m) => m.QueueOverviewPage),
  },
];
