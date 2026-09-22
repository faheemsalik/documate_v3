import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth.guard';
import { ComingSoonPage } from './shared/pages/coming-soon.page';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/login/pages/login.page').then((m) => m.LoginPage),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./shared/layout/app-shell.component').then((m) => m.AppShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'home' },
      {
        path: 'home',
        loadChildren: () => import('./features/home/home.routes').then((m) => m.HOME_ROUTES),
      },
      {
        path: 'agents',
        loadChildren: () => import('./features/agents/agents.routes').then((m) => m.AGENTS_ROUTES),
      },
      {
        path: 'files',
        loadChildren: () => import('./features/files/files.routes').then((m) => m.FILES_ROUTES),
      },
      {
        path: 'queues',
        loadChildren: () => import('./features/queues/queues.routes').then((m) => m.QUEUES_ROUTES),
      },
      {
        path: 'business',
        loadChildren: () =>
          import('./features/business/business.routes').then((m) => m.BUSINESS_ROUTES),
      },
      {
        path: 'docs',
        loadChildren: () => import('./features/docs/docs.routes').then((m) => m.DOCS_ROUTES),
      },
      {
        path: 'api-keys',
        loadChildren: () =>
          import('./features/api-keys/api-keys.routes').then((m) => m.API_KEYS_ROUTES),
      },
      {
        path: 'users',
        loadChildren: () => import('./features/users/users.routes').then((m) => m.USERS_ROUTES),
      },
      {
        path: 'coming-soon/usage',
        component: ComingSoonPage,
        data: { title: 'Usage stats' },
      },
      {
        path: 'coming-soon/support',
        component: ComingSoonPage,
        data: { title: 'Support requests' },
      },
      {
        path: 'coming-soon/workflows',
        component: ComingSoonPage,
        data: { title: 'Workflows' },
      },
    ],
  },
  { path: '**', redirectTo: 'home' },
];
