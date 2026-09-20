import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Textarea } from 'primeng/textarea';
import {
  PublicActionsApiService,
  type PublicActionsSettings,
  type PublicEventToggle,
} from '../../../core/api/public-actions-api.service';

@Component({
  selector: 'app-business-integrations-page',
  imports: [FormsModule, Button, Checkbox, InputText, Message, Textarea],
  templateUrl: './business-integrations.page.html',
  styleUrl: './business-integrations.page.scss',
})
export class BusinessIntegrationsPage implements OnInit {
  private readonly api = inject(PublicActionsApiService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly info = signal<string | null>(null);

  webhookEnabled = false;
  webhookUrl = '';
  webhookSecret = '';
  webhookEvents: PublicEventToggle[] = [];

  emailEnabled = false;
  emailRecipientsText = '';
  emailEvents: PublicEventToggle[] = [];

  inAppEnabled = false;
  inAppEvents: PublicEventToggle[] = [];

  ngOnInit(): void {
    this.api.getBusiness().subscribe({
      next: (s) => {
        this.apply(s);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load integrations settings.');
      },
    });
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);
    this.info.set(null);
    this.api
      .putBusiness({
        webhook: {
          enabled: this.webhookEnabled,
          url: this.webhookUrl || null,
          secret: this.webhookSecret || null,
          eventKeys: this.webhookEvents.filter((e) => e.enabled).map((e) => e.eventKey),
        },
        email: {
          enabled: this.emailEnabled,
          // Partner recipients only — Documate support is admin/platform (DR-EA6).
          audience: 'partner',
          recipients: this.emailRecipientsText
            .split(/[,;\s]+/)
            .map((x) => x.trim())
            .filter(Boolean),
          eventKeys: this.emailEvents.filter((e) => e.enabled).map((e) => e.eventKey),
        },
        inApp: {
          enabled: this.inAppEnabled,
          eventKeys: this.inAppEvents.filter((e) => e.enabled).map((e) => e.eventKey),
        },
      })
      .subscribe({
        next: (s) => {
          this.apply(s);
          this.webhookSecret = '';
          this.saving.set(false);
          this.info.set('Saved Business defaults. Queues that inherit will use these settings.');
        },
        error: () => {
          this.saving.set(false);
          this.error.set('Save failed.');
        },
      });
  }

  private apply(s: PublicActionsSettings): void {
    this.webhookEnabled = s.webhook.enabled;
    this.webhookUrl = s.webhook.url ?? '';
    this.webhookEvents = s.webhook.events.map((e) => ({ ...e }));
    this.emailEnabled = s.email.enabled;
    this.emailRecipientsText = s.email.recipients.join(', ');
    this.emailEvents = s.email.events.map((e) => ({ ...e }));
    this.inAppEnabled = s.inApp.enabled;
    this.inAppEvents = s.inApp.events.map((e) => ({ ...e }));
  }
}
