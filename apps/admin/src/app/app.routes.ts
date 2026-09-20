import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth.guard';

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
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/pages/dashboard.page').then((m) => m.DashboardPage),
      },
      {
        path: 'ops',
        loadComponent: () =>
          import('./features/ops-monitor/pages/ops-monitor.page').then((m) => m.OpsMonitorPage),
      },
      {
        path: 'ops/files/:fileId',
        loadComponent: () =>
          import('./features/ops-monitor/pages/ops-file-detail.page').then((m) => m.OpsFileDetailPage),
      },
      {
        path: 'ops/documents/:documentId',
        loadComponent: () =>
          import('./features/ops-monitor/pages/ops-document-detail.page').then(
            (m) => m.OpsDocumentDetailPage,
          ),
      },
      {
        path: 'support',
        loadComponent: () =>
          import('./features/support/pages/support.page').then((m) => m.SupportPage),
      },
      {
        path: 'tenants',
        loadComponent: () =>
          import('./features/tenants/pages/tenants.page').then((m) => m.TenantsPage),
      },
      {
        path: 'tenants/:id',
        loadComponent: () =>
          import('./features/tenants/pages/tenant-detail.page').then((m) => m.TenantDetailPage),
      },
      {
        path: 'businesses',
        loadComponent: () =>
          import('./features/businesses/pages/businesses.page').then((m) => m.BusinessesPage),
      },
      {
        path: 'businesses/:businessId',
        loadComponent: () =>
          import('./features/businesses/pages/business-detail.page').then((m) => m.BusinessDetailPage),
      },
      {
        path: 'agent-templates',
        loadComponent: () =>
          import('./features/agent-templates/pages/agent-template-list.page').then(
            (m) => m.AgentTemplateListPage,
          ),
      },
      {
        path: 'agent-templates/new',
        loadComponent: () =>
          import('./features/agent-templates/pages/agent-template-edit.page').then(
            (m) => m.AgentTemplateEditPage,
          ),
      },
      {
        path: 'agent-templates/:id',
        loadComponent: () =>
          import('./features/agent-templates/pages/agent-template-edit.page').then(
            (m) => m.AgentTemplateEditPage,
          ),
      },
      {
        path: 'agents',
        loadComponent: () =>
          import('./features/agents/pages/agent-list.page').then((m) => m.AgentListPage),
      },
      {
        path: 'agents/:id',
        loadComponent: () =>
          import('./features/agents/pages/agent-detail.page').then((m) => m.AgentDetailPage),
      },
      {
        path: 'events',
        loadComponent: () =>
          import('./features/events/pages/events.page').then((m) => m.EventsPage),
      },
      {
        path: 'actions',
        loadComponent: () =>
          import('./features/actions/pages/actions.page').then((m) => m.ActionsPage),
      },
      {
        path: 'actions/:id',
        loadComponent: () =>
          import('./features/actions/pages/action-binding-edit.page').then(
            (m) => m.ActionBindingEditPage,
          ),
      },
      {
        path: 'monitoring',
        loadComponent: () =>
          import('./features/monitoring/pages/monitoring.page').then((m) => m.MonitoringPage),
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./features/settings/pages/settings.page').then((m) => m.SettingsPage),
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
