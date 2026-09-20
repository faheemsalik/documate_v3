import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { Tag } from 'primeng/tag';
import { Textarea } from 'primeng/textarea';
import {
  AdminApiService,
  type AdminActionBindingListItem,
  type AdminBusinessListItem,
  type AdminPublicEvent,
} from '../../../core/admin-api.service';

type FilterOption = { label: string; value: string | boolean | null };

@Component({
  selector: 'app-actions-page',
  imports: [
    FormsModule,
    Button,
    Checkbox,
    Dialog,
    InputText,
    Message,
    Select,
    Tag,
    Textarea,
    RouterLink,
  ],
  templateUrl: './actions.page.html',
  styleUrl: './actions.page.scss',
})
export class ActionsPage implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly rows = signal<AdminActionBindingListItem[]>([]);
  readonly total = signal(0);
  readonly businesses = signal<AdminBusinessListItem[]>([]);
  readonly createQueues = signal<{ id: string; name: string; isDefault: boolean }[]>([]);
  readonly catalogEvents = signal<AdminPublicEvent[]>([]);

  search = '';
  actionTypeKey: string | null = null;
  enabled: boolean | null = null;
  scope: string | null = null;
  businessId: string | null = null;

  readonly actionTypeOptions: FilterOption[] = [
    { label: 'Webhook', value: 'webhook' },
    { label: 'Email', value: 'email' },
    { label: 'In-app', value: 'in_app' },
  ];
  readonly enabledOptions: FilterOption[] = [
    { label: 'Enabled', value: true },
    { label: 'Disabled', value: false },
  ];
  readonly scopeOptions: FilterOption[] = [
    { label: 'Business default', value: 'business' },
    { label: 'Queue override', value: 'queue' },
  ];
  readonly audienceOptions = [
    { label: 'Partner recipients', value: 'partner' },
    { label: 'Documate support (platform)', value: 'platform' },
  ];

  showCreate = false;
  createBusinessId = '';
  createQueueId: string | null = null;
  createActionType = 'webhook';
  createEnabled = true;
  createEventToggles: { eventKey: string; enabled: boolean }[] = [];
  createWebhookUrl = '';
  createWebhookSecret = '';
  createEmailAudience = 'partner';
  createEmailRecipients = '';
  createError = '';
  creating = false;

  ngOnInit(): void {
    const q = this.route.snapshot.queryParamMap;
    this.search = q.get('search') ?? '';
    this.actionTypeKey = q.get('actionTypeKey');
    this.scope = q.get('scope');
    this.businessId = q.get('businessId');
    const enabledRaw = q.get('enabled');
    this.enabled = enabledRaw === 'true' ? true : enabledRaw === 'false' ? false : null;
    this.reload();
    this.api.listBusinesses({ pageSize: 100 }).subscribe({
      next: (res) => this.businesses.set(res.items),
    });
    this.api.getPublicEventsCatalog().subscribe({
      next: (cat) => {
        this.catalogEvents.set(cat.events);
        this.resetCreateEvents();
      },
    });
  }

  fmt(iso: string): string {
    return iso.slice(0, 19).replace('T', ' ');
  }

  actionLabel(key: string): string {
    return this.actionTypeOptions.find((o) => o.value === key)?.label ?? key;
  }

  openRow(id: string, event?: Event): void {
    const t = event?.target as HTMLElement | undefined;
    if (t?.closest('a')) return;
    void this.router.navigate(['/actions', id]);
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api
      .listActionBindings({
        search: this.search || undefined,
        actionTypeKey: this.actionTypeKey || undefined,
        enabled: this.enabled ?? undefined,
        scope: this.scope || undefined,
        businessId: this.businessId || undefined,
        pageSize: 100,
      })
      .subscribe({
        next: (res) => {
          this.rows.set(res.items);
          this.total.set(res.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Failed to load action bindings.');
          this.loading.set(false);
        },
      });
  }

  openCreate(): void {
    this.createBusinessId = this.businessId ?? this.businesses()[0]?.idenBusinessId ?? '';
    this.createQueueId = null;
    this.createActionType = 'webhook';
    this.createEnabled = true;
    this.createWebhookUrl = '';
    this.createWebhookSecret = '';
    this.createEmailAudience = 'partner';
    this.createEmailRecipients = '';
    this.createError = '';
    this.createQueues.set([]);
    this.resetCreateEvents();
    this.showCreate = true;
    if (this.createBusinessId) {
      this.onCreateBusinessChange();
    }
  }

  onCreateBusinessChange(): void {
    this.createQueueId = null;
    this.createQueues.set([]);
    if (!this.createBusinessId) return;
    this.api.getBusiness(this.createBusinessId).subscribe({
      next: (b) => this.createQueues.set(b.queues),
      error: () => this.createQueues.set([]),
    });
  }

  onCreateActionTypeChange(): void {
    this.resetCreateEvents();
  }

  submitCreate(): void {
    if (!this.createBusinessId) {
      this.createError = 'Business is required.';
      return;
    }
    if (!this.createActionType) {
      this.createError = 'Action type is required.';
      return;
    }
    if (this.createActionType === 'webhook' && this.createEnabled && !this.createWebhookUrl.trim()) {
      this.createError = 'Webhook URL is required when enabled.';
      return;
    }
    if (this.createActionType === 'email' && this.createEmailAudience === 'partner') {
      const recipients = this.parseRecipients(this.createEmailRecipients);
      if (recipients.length === 0) {
        this.createError = 'At least one recipient email is required for partner audience.';
        return;
      }
    }

    const eventKeys = this.createEventToggles.filter((e) => e.enabled).map((e) => e.eventKey);
    if (eventKeys.length === 0) {
      this.createError = 'Select at least one event.';
      return;
    }

    this.creating = true;
    this.createError = '';
    this.api
      .createActionBinding({
        businessId: this.createBusinessId,
        queueId: this.createQueueId || null,
        actionTypeKey: this.createActionType,
        enabled: this.createEnabled,
        eventKeys,
        url: this.createActionType === 'webhook' ? this.createWebhookUrl.trim() || null : undefined,
        secret:
          this.createActionType === 'webhook' ? this.createWebhookSecret.trim() || null : undefined,
        audience: this.createActionType === 'email' ? this.createEmailAudience : undefined,
        recipients:
          this.createActionType === 'email'
            ? this.parseRecipients(this.createEmailRecipients)
            : undefined,
      })
      .subscribe({
        next: (row) => {
          this.creating = false;
          this.showCreate = false;
          void this.router.navigate(['/actions', row.id]);
        },
        error: (err) => {
          this.creating = false;
          this.createError = err?.error?.error ?? 'Create failed.';
        },
      });
  }

  private parseRecipients(text: string): string[] {
    return text
      .split(/[,;\s]+/)
      .map((x) => x.trim())
      .filter(Boolean);
  }

  private resetCreateEvents(): void {
    const defaults = this.defaultEventKeys(this.createActionType);
    this.createEventToggles = this.catalogEvents().map((ev) => ({
      eventKey: ev.eventKey,
      enabled: defaults.includes(ev.eventKey),
    }));
  }

  private defaultEventKeys(actionType: string): string[] {
    if (actionType === 'webhook') {
      return ['File received', 'Document ready', 'Document failed', 'Document cancelled'];
    }
    if (actionType === 'email') {
      return ['Document failed'];
    }
    if (actionType === 'in_app') {
      return ['Document failed', 'File received'];
    }
    return [];
  }
}
