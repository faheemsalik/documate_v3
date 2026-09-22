import { Routes } from '@angular/router';

export const USERS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/users.page').then((m) => m.UsersPage),
  },
  {
    path: ':memberId',
    loadComponent: () => import('./pages/user-detail.page').then((m) => m.UserDetailPage),
  },
];
