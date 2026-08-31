import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { Tabs, TabList, Tab, TabPanels, TabPanel } from 'primeng/tabs';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { AppContextService } from '../../../core/app-context.service';
import { AgentsApiService, type Agent } from '../../../core/api/agents-api.service';
import { CatalogsApiService, type DocumentType } from '../../../core/api/catalogs-api.service';
import { QueuesApiService, type AllowlistEntry, type Queue, type QueueRoute } from '../../../core/api/queues-api.service';

@Component({
  selector: 'app-queue-overview-page',
  imports: [
    FormsModule,
    Button,
    Checkbox,
    InputText,
    Message,
    Select,
    Tabs,
    TabList,
    Tab,
    TabPanels,
    TabPanel,
    TableModule,
    Tag,
  ],
  templateUrl: './queue-overview.page.html',
  styleUrl: './queue-overview.page.scss',
})
export class QueueOverviewPage {
  private readonly queuesApi = inject(QueuesApiService);
  private readonly agentsApi = inject(AgentsApiService);
  private readonly catalogsApi = inject(CatalogsApiService);
  readonly ctx = inject(AppContextService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly queue = signal<Queue | null>(null);
  readonly routes = signal<QueueRoute[]>([]);
  readonly allowlist = signal<AllowlistEntry[]>([]);
  readonly agents = signal<Agent[]>([]);
  readonly documentTypes = signal<DocumentType[]>([]);

  webhookEnabled = false;
  webhookUrl = '';
  webhookSecret = '';
  emailIntakeEnabled = false;
  newRouteDocTypeId: number | null = null;
  newRouteAgentId: string | null = null;
  allowlistMatchType = 'email';
  allowlistValue = '';

  constructor() {
    effect(() => {
      const qid = this.ctx.defaultQueueId();
      if (qid && !this.ctx.loading()) this.load(qid);
    });
    this.agentsApi.list().subscribe((a) => this.agents.set(a));
    this.catalogsApi.listDocumentTypes().subscribe((d) => this.documentTypes.set(d));
  }

  saveWebhook(): void {
    const q = this.queue();
    if (!q) return;
    this.saving.set(true);
    this.queuesApi
      .updateWebhook(q.id, {
        enabled: this.webhookEnabled,
        url: this.webhookUrl || null,
        secret: this.webhookSecret || null,
      })
      .subscribe({
        next: (updated) => {
          this.queue.set(updated);
          this.saving.set(false);
        },
        error: () => {
          this.saving.set(false);
          this.error.set('Webhook update failed.');
        },
      });
  }

  mintEmail(): void {
    const q = this.queue();
    if (!q) return;
    this.queuesApi.mintEmail(q.id).subscribe({
      next: () => this.load(q.id),
      error: () => this.error.set('Could not mint email address.'),
    });
  }

  saveEmailSettings(): void {
    const q = this.queue();
    if (!q) return;
    this.queuesApi
      .updateEmailSettings(q.id, {
        emailIntakeEnabled: this.emailIntakeEnabled,
        allowlistModeEnumId: q.allowlistModeEnumId,
      })
      .subscribe({
        next: (updated) => this.queue.set(updated),
        error: () => this.error.set('Email settings update failed.'),
      });
  }

  addRoute(): void {
    const q = this.queue();
    if (!q || this.newRouteDocTypeId == null || !this.newRouteAgentId) return;
    const routes = [
      ...this.routes().map((r) => ({ documentTypeId: r.documentTypeId, agentId: r.agentId })),
      { documentTypeId: this.newRouteDocTypeId, agentId: this.newRouteAgentId },
    ];
    this.queuesApi.replaceRoutes(q.id, routes).subscribe({
      next: (updated) => {
        this.routes.set(updated);
        this.newRouteDocTypeId = null;
        this.newRouteAgentId = null;
      },
      error: () => this.error.set('Could not add route.'),
    });
  }

  lockRoutes(): void {
    const q = this.queue();
    if (!q) return;
    this.queuesApi.lockRouting(q.id).subscribe({
      next: (updated) => this.queue.set(updated),
      error: () => this.error.set('Could not lock routing.'),
    });
  }

  addAllowlist(): void {
    const q = this.queue();
    if (!q || !this.allowlistValue.trim()) return;
    this.queuesApi.addAllowlist(q.id, { matchTypeKey: this.allowlistMatchType, value: this.allowlistValue.trim() }).subscribe({
      next: (entry) => {
        this.allowlist.update((a) => [...a, entry]);
        this.allowlistValue = '';
      },
      error: () => this.error.set('Could not add allowlist entry.'),
    });
  }

  removeAllowlist(entryId: number): void {
    const q = this.queue();
    if (!q) return;
    this.queuesApi.deleteAllowlist(q.id, entryId).subscribe({
      next: () => this.allowlist.update((a) => a.filter((e) => e.id !== entryId)),
      error: () => this.error.set('Could not remove entry.'),
    });
  }

  private load(queueId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.queuesApi.get(queueId).subscribe({
      next: (detail) => {
        this.queue.set(detail.queue);
        this.routes.set(detail.routes);
        this.allowlist.set(detail.allowlist);
        this.webhookEnabled = detail.queue.webhookEnabled;
        this.webhookUrl = detail.queue.webhookUrl ?? '';
        this.emailIntakeEnabled = detail.queue.emailIntakeEnabled;
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load channel.');
      },
    });
  }
}
