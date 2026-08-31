import { Routes } from '@angular/router';

export const BUSINESS_ROUTES: Routes = [
  {
    path: 'profile',
    loadComponent: () =>
      import('./pages/business-profile.page').then((m) => m.BusinessProfilePage),
  },
  {
    path: 'switch',
    loadComponent: () => import('./pages/business-switch.page').then((m) => m.BusinessSwitchPage),
  },
  { path: '', pathMatch: 'full', redirectTo: 'profile' },
];
