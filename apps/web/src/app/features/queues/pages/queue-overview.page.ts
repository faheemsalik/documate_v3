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
import { QueuesApiService, type Queue, type QueueRoute } from '../../../core/api/queues-api.service';
import {
  IntakeMailboxesApiService,
  type IntakeMailbox,
  type MailboxAllowlistEntry,
} from '../../../core/api/intake-mailboxes-api.service';

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
  private readonly mailboxesApi = inject(IntakeMailboxesApiService);
  readonly ctx = inject(AppContextService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly queue = signal<Queue | null>(null);
  readonly routes = signal<QueueRoute[]>([]);
  readonly allowlist = signal<MailboxAllowlistEntry[]>([]);
  readonly agents = signal<Agent[]>([]);
  readonly documentTypes = signal<DocumentType[]>([]);
  readonly mailboxes = signal<IntakeMailbox[]>([]);
  readonly selectedMailbox = signal<IntakeMailbox | null>(null);

  webhookEnabled = false;
  webhookUrl = '';
  webhookSecret = '';
  emailIntakeEnabled = false;
  newRouteDocTypeId: number | null = null;
  newRouteAgentId: string | null = null;
  allowlistMatchType = 'email';
  allowlistValue = '';
  allowlistModeEnumId: number | null = null;
  readonly allowlistModes = signal<{ label: string; value: number }[]>([]);

  constructor() {
    effect(() => {
      const qid = this.ctx.defaultQueueId();
      if (qid && !this.ctx.loading()) this.load(qid);
    });
    this.agentsApi.list().subscribe((a) => this.agents.set(a));
    this.catalogsApi.listDocumentTypes().subscribe((d) => this.documentTypes.set(d));
    this.catalogsApi.listEnums('allowlist_mode').subscribe({
      next: (modes) =>
        this.allowlistModes.set(modes.map((m) => ({ label: m.displayName || m.enumKey, value: m.id }))),
    });
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

  createMultiMailbox(): void {
    this.mailboxesApi.createMulti().subscribe({
      next: () => this.reloadMailboxes(),
      error: (err) => this.error.set(err?.error?.error ?? 'Could not create multi mailbox.'),
    });
  }

  selectMailbox(mb: IntakeMailbox): void {
    this.selectedMailbox.set(mb);
    this.allowlistModeEnumId = mb.allowlistModeEnumId;
    this.mailboxesApi.listAllowlist(mb.id).subscribe({
      next: (entries) => this.allowlist.set(entries),
      error: () => this.error.set('Could not load allowlist.'),
    });
  }

  saveAllowlistMode(): void {
    const mb = this.selectedMailbox();
    if (!mb || this.allowlistModeEnumId == null) return;
    this.mailboxesApi
      .update(mb.id, { enabled: mb.enabled, allowlistModeEnumId: this.allowlistModeEnumId })
      .subscribe({
        next: (updated) => {
          this.selectedMailbox.set(updated);
          this.reloadMailboxes();
        },
        error: () => this.error.set('Could not update allowlist mode.'),
      });
  }

  rotateMailbox(id: string): void {
    this.mailboxesApi.rotate(id).subscribe({
      next: () => this.reloadMailboxes(),
      error: () => this.error.set('Rotate failed.'),
    });
  }

  toggleMailbox(mb: IntakeMailbox): void {
    this.mailboxesApi
      .update(mb.id, { enabled: !mb.enabled, allowlistModeEnumId: mb.allowlistModeEnumId })
      .subscribe({
        next: () => this.reloadMailboxes(),
        error: () => this.error.set('Could not update mailbox.'),
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
    const mb = this.selectedMailbox();
    if (!mb || !this.allowlistValue.trim()) return;
    this.mailboxesApi
      .addAllowlist(mb.id, { matchTypeKey: this.allowlistMatchType, value: this.allowlistValue.trim() })
      .subscribe({
        next: (entry) => {
          this.allowlist.update((a) => [...a, entry]);
          this.allowlistValue = '';
        },
        error: () => this.error.set('Could not add allowlist entry.'),
      });
  }

  removeAllowlist(entryId: number): void {
    const mb = this.selectedMailbox();
    if (!mb) return;
    this.mailboxesApi.deleteAllowlist(mb.id, entryId).subscribe({
      next: () => this.allowlist.update((a) => a.filter((e) => e.id !== entryId)),
      error: () => this.error.set('Could not remove entry.'),
    });
  }

  private reloadMailboxes(): void {
    this.mailboxesApi.list().subscribe({
      next: (list) => {
        const multi = list.filter((m) => m.kindKey === 'multi_type');
        this.mailboxes.set(multi);
        const sel = this.selectedMailbox();
        if (sel) {
          const refreshed = multi.find((m) => m.id === sel.id) ?? null;
          this.selectedMailbox.set(refreshed);
          if (refreshed) this.selectMailbox(refreshed);
        }
      },
    });
  }

  private load(queueId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.queuesApi.get(queueId).subscribe({
      next: (detail) => {
        this.queue.set(detail.queue);
        this.routes.set(detail.routes);
        this.webhookEnabled = detail.queue.webhookEnabled;
        this.webhookUrl = detail.queue.webhookUrl ?? '';
        this.emailIntakeEnabled = detail.queue.emailIntakeEnabled;
        this.loading.set(false);
        this.reloadMailboxes();
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load channel.');
      },
    });
  }
}
