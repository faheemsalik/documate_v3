import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { Tag } from 'primeng/tag';
import { Textarea } from 'primeng/textarea';
import {
  AdminApiService,
  type AdminActionBindingDetail,
  type AdminPublicEventToggle,
} from '../../../core/admin-api.service';

@Component({
  selector: 'app-action-binding-edit-page',
  imports: [FormsModule, Button, Checkbox, InputText, Message, Select, Tag, Textarea, RouterLink],
  templateUrl: './action-binding-edit.page.html',
  styleUrl: './action-binding-edit.page.scss',
})
export class ActionBindingEditPage implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly info = signal<string | null>(null);
  readonly detail = signal<AdminActionBindingDetail | null>(null);

  enabled = false;
  events: AdminPublicEventToggle[] = [];
  webhookUrl = '';
  webhookSecret = '';
  emailAudience = 'partner';
  emailRecipientsText = '';

  readonly audienceOptions = [
    { label: 'Partner recipients', value: 'partner' },
    { label: 'Documate support (platform)', value: 'platform' },
  ];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Missing binding id.');
      this.loading.set(false);
      return;
    }
    this.api.getActionBinding(id).subscribe({
      next: (row) => {
        this.apply(row);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Action binding not found.');
        this.loading.set(false);
      },
    });
  }

  actionLabel(key: string): string {
    if (key === 'webhook') return 'Webhook';
    if (key === 'email') return 'Email';
    if (key === 'in_app') return 'In-app';
    return key;
  }

  fmt(iso: string): string {
    return iso.slice(0, 19).replace('T', ' ');
  }

  save(): void {
    const row = this.detail();
    if (!row) return;
    this.saving.set(true);
    this.error.set(null);
    this.info.set(null);
    this.api
      .updateActionBinding(row.id, {
        enabled: this.enabled,
        eventKeys: this.events.filter((e) => e.enabled).map((e) => e.eventKey),
        url: row.actionTypeKey === 'webhook' ? this.webhookUrl || null : undefined,
        secret: row.actionTypeKey === 'webhook' ? this.webhookSecret || null : undefined,
        audience: row.actionTypeKey === 'email' ? this.emailAudience : undefined,
        recipients:
          row.actionTypeKey === 'email'
            ? this.emailRecipientsText
                .split(/[,;\s]+/)
                .map((x) => x.trim())
                .filter(Boolean)
            : undefined,
      })
      .subscribe({
        next: (updated) => {
          this.apply(updated);
          this.webhookSecret = '';
          this.saving.set(false);
          this.info.set('Saved.');
        },
        error: (err) => {
          this.saving.set(false);
          this.error.set(err?.error?.error ?? 'Save failed.');
        },
      });
  }

  private apply(row: AdminActionBindingDetail): void {
    this.detail.set(row);
    this.enabled = row.enabled;
    this.events = row.events.map((e) => ({ ...e }));
    this.webhookUrl = row.webhookUrl ?? '';
    this.emailAudience = row.emailAudience ?? 'partner';
    this.emailRecipientsText = row.recipients.join(', ');
  }
}
