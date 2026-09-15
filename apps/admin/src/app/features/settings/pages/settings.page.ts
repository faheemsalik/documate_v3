import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Textarea } from 'primeng/textarea';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { Tag } from 'primeng/tag';
import {
  AdminApiService,
  type AdminProvider,
  type SystemSetting,
} from '../../../core/admin-api.service';

type SettingMeta = {
  title: string;
  description: string;
  icon: string;
  input: 'text' | 'json' | 'provider-llm' | 'provider-ocr';
  hint?: string;
};

type SettingGroup = {
  id: string;
  title: string;
  description: string;
  icon: string;
  keys: string[];
};

type ProviderOption = { label: string; value: string };

const SETTING_META: Record<string, SettingMeta> = {
  'EmailIntake:DefaultDomain': {
    title: 'Default intake domain',
    description: 'Domain used when minting mailbox addresses for email intake (e.g. docsintake.com).',
    icon: 'pi pi-globe',
    input: 'text',
  },
  'EmailIntake:MaxAttachmentBytes': {
    title: 'Max attachment size',
    description: 'Maximum size in bytes for a single email attachment.',
    icon: 'pi pi-file',
    input: 'text',
    hint: 'Bytes (e.g. 26214400 ≈ 25 MB)',
  },
  'EmailIntake:MaxTotalAttachmentBytes': {
    title: 'Max total attachments',
    description: 'Maximum combined size in bytes of all attachments on one email.',
    icon: 'pi pi-clone',
    input: 'text',
    hint: 'Bytes',
  },
  'EmailIntake:MaxAttachments': {
    title: 'Max attachment count',
    description: 'Maximum number of attachments accepted on a single inbound email.',
    icon: 'pi pi-list',
    input: 'text',
  },
  'EmailIntake:AllowedExtensions': {
    title: 'Allowed file extensions',
    description: 'JSON array of file extensions accepted for email attachments.',
    icon: 'pi pi-check-square',
    input: 'json',
    hint: 'JSON array, e.g. [".pdf",".png"]',
  },
  'EmailIntake:RateLimitPerMailboxPerMinute': {
    title: 'Rate limit (per minute)',
    description: 'Max inbound emails processed per mailbox per minute.',
    icon: 'pi pi-bolt',
    input: 'text',
  },
  'EmailIntake:RateLimitPerMailboxPerHour': {
    title: 'Rate limit (per hour)',
    description: 'Max inbound emails processed per mailbox per hour.',
    icon: 'pi pi-clock',
    input: 'text',
  },
  'EmailIntake:S3Bucket': {
    title: 'MIME S3 bucket',
    description: 'S3 bucket name where raw inbound MIME objects are stored (not credentials).',
    icon: 'pi pi-box',
    input: 'text',
  },
  'EmailIntake:S3Prefix': {
    title: 'MIME S3 prefix',
    description: 'Object-key prefix inside the bucket for inbound MIME (e.g. inbound/).',
    icon: 'pi pi-folder',
    input: 'text',
  },
  'EmailIntake:AwsRegion': {
    title: 'Email AWS region',
    description:
      'AWS region for email intake S3/SES resources (region name only — credentials stay in AWS Secrets Manager).',
    icon: 'pi pi-map-marker',
    input: 'text',
  },
  'EmailIntake:MimeRetentionDays': {
    title: 'MIME retention (days)',
    description: 'How long to keep raw inbound MIME in S3 before lifecycle cleanup.',
    icon: 'pi pi-calendar',
    input: 'text',
    hint: 'Default 30',
  },
  'EmailIntake:BodyExcerptMaxChars': {
    title: 'Body excerpt max chars',
    description: 'Max characters of email body stored/stamped for ops visibility.',
    icon: 'pi pi-align-left',
    input: 'text',
  },
  'Pipeline:SyncWaitTimeoutSeconds': {
    title: 'Sync wait timeout',
    description: 'Seconds to wait for sync upload processing before returning a timeout.',
    icon: 'pi pi-hourglass',
    input: 'text',
    hint: 'Seconds',
  },
  'Pipeline:SyncMaxPages': {
    title: 'Sync max pages',
    description: 'Files with more pages than this are rejected for sync processing.',
    icon: 'pi pi-copy',
    input: 'text',
  },
  'Pipeline:SyncMaxBytes': {
    title: 'Sync max bytes',
    description: 'Files larger than this are rejected for sync processing.',
    icon: 'pi pi-database',
    input: 'text',
    hint: 'Bytes',
  },
  'Pipeline:IntelligenceT1ProviderKey': {
    title: 'Intelligence T1 model',
    description:
      'Cheap first-pass model for page intelligence (split/classify). Must match a CorProvider.ProviderKey. API credentials stay in AWS Secrets Manager.',
    icon: 'pi pi-sparkles',
    input: 'provider-llm',
  },
  'Pipeline:IntelligenceFallbackProviderKey': {
    title: 'Intelligence fallback model',
    description:
      'Escalation model when T1 is unrecognized/ambiguous. Must match a CorProvider.ProviderKey. API credentials stay in AWS Secrets Manager.',
    icon: 'pi pi-replay',
    input: 'provider-llm',
  },
  'Pipeline:ExtractProviderKey': {
    title: 'Extract model',
    description:
      'Default LLM used for document field extraction when the agent does not override. Must match a CorProvider.ProviderKey. API credentials stay in AWS Secrets Manager.',
    icon: 'pi pi-pencil',
    input: 'provider-llm',
  },
  'Pipeline:IntelligenceMaxCallsPerFile': {
    title: 'Intelligence max calls / file',
    description: 'Cap on intelligence LLM calls (T1 + fallback) per File to control cost.',
    icon: 'pi pi-chart-bar',
    input: 'text',
    hint: 'Default 40',
  },
  'Pipeline:MaxConcurrentFiles': {
    title: 'Max concurrent files',
    description: 'How many Files the pipeline may process in parallel.',
    icon: 'pi pi-th-large',
    input: 'text',
  },
  'Pipeline:MaxConcurrentWebhooks': {
    title: 'Max concurrent webhooks',
    description: 'How many outbound webhook deliveries may run in parallel.',
    icon: 'pi pi-send',
    input: 'text',
  },
  'Pipeline:StubStageDelayMs': {
    title: 'Stub stage delay',
    description: 'Artificial delay (ms) injected into stub pipeline stages for local timing tests. 0 = realtime.',
    icon: 'pi pi-clock',
    input: 'text',
    hint: 'Milliseconds (0 = off)',
  },
  'Storage:Provider': {
    title: 'Storage provider',
    description: 'Object-storage backend used for artifacts and downloads.',
    icon: 'pi pi-cloud',
    input: 'text',
    hint: 'local | s3',
  },
  'Storage:BucketOrContainer': {
    title: 'Bucket / container',
    description: 'S3 bucket or container name for document artifacts (not credentials).',
    icon: 'pi pi-box',
    input: 'text',
  },
  'Storage:LocalRootPath': {
    title: 'Local root path',
    description: 'Filesystem root when Storage:Provider is local.',
    icon: 'pi pi-folder-open',
    input: 'text',
  },
  'Storage:Region': {
    title: 'Storage region',
    description: 'AWS region for S3 storage (region name only — credentials stay in AWS Secrets Manager).',
    icon: 'pi pi-map-marker',
    input: 'text',
  },
  'Storage:ServiceUrl': {
    title: 'Service URL',
    description: 'Optional custom endpoint (e.g. LocalStack / MinIO). Leave empty for real AWS.',
    icon: 'pi pi-link',
    input: 'text',
  },
  'Storage:SignedUrlMinutes': {
    title: 'Signed URL lifetime',
    description: 'Minutes before generated download URLs expire and are refreshed.',
    icon: 'pi pi-clock',
    input: 'text',
    hint: 'Minutes',
  },
  'Ocr:PrimaryProviderKey': {
    title: 'Primary OCR provider',
    description:
      'First OCR engine tried during normalize. Must match a CorProvider.ProviderKey (OCR category). Credentials stay in AWS Secrets Manager.',
    icon: 'pi pi-eye',
    input: 'provider-ocr',
  },
  'Ocr:SecondaryProviderKey': {
    title: 'Secondary OCR provider',
    description:
      'Fallback OCR engine when primary fails or is unavailable. Must match a CorProvider.ProviderKey. Credentials stay in AWS Secrets Manager.',
    icon: 'pi pi-replay',
    input: 'provider-ocr',
  },
  'Ocr:SyncMaxPages': {
    title: 'OCR sync max pages',
    description: 'Page count above which sync OCR is skipped in favor of async / fallback paths.',
    icon: 'pi pi-copy',
    input: 'text',
  },
  'Ocr:Textract:Region': {
    title: 'Textract region',
    description: 'AWS region for Textract calls (region only — access keys stay in AWS Secrets Manager).',
    icon: 'pi pi-map-marker',
    input: 'text',
  },
  'Ocr:GoogleDocumentAi:Location': {
    title: 'Google Document AI location',
    description: 'Processor location (e.g. us). CredentialsJson stays in AWS Secrets Manager.',
    icon: 'pi pi-globe',
    input: 'text',
  },
  'Ocr:GoogleDocumentAi:ProjectId': {
    title: 'Google Document AI project id',
    description: 'GCP project id for Document AI (non-secret identifier).',
    icon: 'pi pi-id-card',
    input: 'text',
  },
  'Ocr:GoogleDocumentAi:ProcessorId': {
    title: 'Google Document AI processor id',
    description: 'Document AI processor id (non-secret identifier).',
    icon: 'pi pi-hashtag',
    input: 'text',
  },
  'Notifications:Enabled': {
    title: 'Ops notifications enabled',
    description: 'When true, failed pipeline alerts may be emailed via SMTP. SMTP password stays in AWS Secrets Manager.',
    icon: 'pi pi-bell',
    input: 'text',
    hint: 'true | false',
  },
  'Notifications:ToAddress': {
    title: 'Ops alert To address',
    description: 'Destination email for ops failure alerts.',
    icon: 'pi pi-envelope',
    input: 'text',
  },
  'Notifications:Smtp:Host': {
    title: 'SMTP host',
    description: 'SMTP server hostname for ops alerts (non-secret). Password stays in AWS Secrets Manager.',
    icon: 'pi pi-server',
    input: 'text',
  },
  'Notifications:Smtp:Port': {
    title: 'SMTP port',
    description: 'SMTP server port.',
    icon: 'pi pi-sort-numeric-up',
    input: 'text',
  },
  'Notifications:Smtp:User': {
    title: 'SMTP user',
    description: 'SMTP username (non-secret identity). Password stays in AWS Secrets Manager.',
    icon: 'pi pi-user',
    input: 'text',
  },
  'Notifications:Smtp:From': {
    title: 'SMTP From address',
    description: 'From address used when sending ops alert email.',
    icon: 'pi pi-inbox',
    input: 'text',
  },
  'Admin:HangfireDashboardUrl': {
    title: 'Hangfire dashboard URL',
    description: 'Link shown in admin to the Hangfire jobs dashboard (path or absolute URL).',
    icon: 'pi pi-briefcase',
    input: 'text',
    hint: 'e.g. /hangfire',
  },
  'Admin:DatadogDashboardUrl': {
    title: 'Datadog dashboard URL',
    description: 'Optional link to an external Datadog ops dashboard.',
    icon: 'pi pi-chart-line',
    input: 'text',
  },
};

const SETTING_GROUPS: SettingGroup[] = [
  {
    id: 'email',
    title: 'Email intake',
    description: 'Inbound mailbox limits, retention, and non-secret S3 naming for raw MIME.',
    icon: 'pi pi-envelope',
    keys: [
      'EmailIntake:DefaultDomain',
      'EmailIntake:MaxAttachmentBytes',
      'EmailIntake:MaxTotalAttachmentBytes',
      'EmailIntake:MaxAttachments',
      'EmailIntake:AllowedExtensions',
      'EmailIntake:RateLimitPerMailboxPerMinute',
      'EmailIntake:RateLimitPerMailboxPerHour',
      'EmailIntake:S3Bucket',
      'EmailIntake:S3Prefix',
      'EmailIntake:AwsRegion',
      'EmailIntake:MimeRetentionDays',
      'EmailIntake:BodyExcerptMaxChars',
    ],
  },
  {
    id: 'pipeline-sync',
    title: 'Pipeline sync gates',
    description: 'Limits that decide whether an upload can complete synchronously.',
    icon: 'pi pi-sitemap',
    keys: [
      'Pipeline:SyncWaitTimeoutSeconds',
      'Pipeline:SyncMaxPages',
      'Pipeline:SyncMaxBytes',
    ],
  },
  {
    id: 'pipeline-models',
    title: 'Pipeline models',
    description:
      'Select which catalog providers run intelligence (T1 / fallback) and extract. API keys stay in AWS Secrets Manager (Plan 17) — not in these settings.',
    icon: 'pi pi-sparkles',
    keys: [
      'Pipeline:IntelligenceT1ProviderKey',
      'Pipeline:IntelligenceFallbackProviderKey',
      'Pipeline:ExtractProviderKey',
      'Pipeline:IntelligenceMaxCallsPerFile',
    ],
  },
  {
    id: 'pipeline-concurrency',
    title: 'Pipeline concurrency',
    description: 'Parallelism and stub timing knobs for the processing pipeline.',
    icon: 'pi pi-sliders-h',
    keys: [
      'Pipeline:MaxConcurrentFiles',
      'Pipeline:MaxConcurrentWebhooks',
      'Pipeline:StubStageDelayMs',
    ],
  },
  {
    id: 'storage',
    title: 'Storage',
    description: 'Artifact storage backend and signed download URL lifetime (credentials stay in AWS Secrets Manager).',
    icon: 'pi pi-cloud',
    keys: [
      'Storage:Provider',
      'Storage:BucketOrContainer',
      'Storage:LocalRootPath',
      'Storage:Region',
      'Storage:ServiceUrl',
      'Storage:SignedUrlMinutes',
    ],
  },
  {
    id: 'ocr',
    title: 'OCR',
    description:
      'Primary/secondary OCR engines and non-secret region/location knobs. Provider credentials stay in AWS Secrets Manager.',
    icon: 'pi pi-eye',
    keys: [
      'Ocr:PrimaryProviderKey',
      'Ocr:SecondaryProviderKey',
      'Ocr:SyncMaxPages',
      'Ocr:Textract:Region',
      'Ocr:GoogleDocumentAi:Location',
      'Ocr:GoogleDocumentAi:ProjectId',
      'Ocr:GoogleDocumentAi:ProcessorId',
    ],
  },
  {
    id: 'notifications',
    title: 'Notifications',
    description: 'Ops alert routing and SMTP identity. SMTP password stays in AWS Secrets Manager.',
    icon: 'pi pi-bell',
    keys: [
      'Notifications:Enabled',
      'Notifications:ToAddress',
      'Notifications:Smtp:Host',
      'Notifications:Smtp:Port',
      'Notifications:Smtp:User',
      'Notifications:Smtp:From',
    ],
  },
  {
    id: 'admin',
    title: 'Admin links',
    description: 'Deep links to Hangfire and Datadog dashboards from the backoffice.',
    icon: 'pi pi-external-link',
    keys: ['Admin:HangfireDashboardUrl', 'Admin:DatadogDashboardUrl'],
  },
];

@Component({
  selector: 'app-settings-page',
  imports: [FormsModule, Button, Textarea, InputText, Message, Select, Tag],
  template: `
    <div class="page">
      <header class="page-head">
        <div class="page-head-text">
          <h1>System settings</h1>
          <p class="lede">
            Platform operational knobs (Plan 15). Secrets — AWS, LLM API keys, SMTP, auth — live in
            <strong>AWS Secrets Manager</strong> (Plan 17) or local user-secrets, not in these settings.
          </p>
        </div>
        <p-button label="Reload" icon="pi pi-refresh" [outlined]="true" (onClick)="reload()" [loading]="loading()" />
      </header>

      @if (error()) {
        <p-message severity="error" [text]="error()!" />
      }
      @if (ok()) {
        <p-message severity="success" [text]="ok()!" />
      }

      @if (!loading() && settings().length === 0) {
        <p class="empty">No settings returned.</p>
      }

      @for (group of visibleGroups(); track group.id) {
        <section class="group" [class.is-open]="isOpen(group.id)">
          <button
            type="button"
            class="group-toggle"
            (click)="toggle(group.id)"
            [attr.aria-expanded]="isOpen(group.id)"
            [attr.aria-controls]="'settings-group-' + group.id"
          >
            <span class="group-icon" aria-hidden="true"><i [class]="group.icon"></i></span>
            <div class="group-text">
              <span class="group-label">Settings group</span>
              <h2>{{ group.title }}</h2>
              <p>{{ group.description }}</p>
            </div>
            <p-tag [value]="groupCount(group) + ' settings'" severity="secondary" />
            <i class="pi chevron" [class.pi-chevron-down]="isOpen(group.id)" [class.pi-chevron-right]="!isOpen(group.id)" aria-hidden="true"></i>
          </button>

          @if (isOpen(group.id)) {
            <div class="cards" [id]="'settings-group-' + group.id">
              @for (key of group.keys; track key) {
                @if (hasKey(key)) {
                  <article class="card">
                    <div class="card-head">
                      <span class="setting-icon" aria-hidden="true"><i [class]="meta(key).icon"></i></span>
                      <div class="card-titles">
                        <h3>{{ meta(key).title }}</h3>
                        <p class="desc">{{ meta(key).description }}</p>
                        <code class="key">{{ key }}</code>
                      </div>
                    </div>

                    <div class="card-body">
                      @if (meta(key).input === 'json') {
                        <textarea
                          pTextarea
                          [(ngModel)]="edits[key]"
                          rows="3"
                          class="val"
                          [attr.aria-label]="meta(key).title"
                        ></textarea>
                      } @else if (meta(key).input === 'provider-llm') {
                        <p-select
                          [options]="llmProviders()"
                          [(ngModel)]="edits[key]"
                          optionLabel="label"
                          optionValue="value"
                          [filter]="true"
                          placeholder="Select LLM provider"
                          class="val"
                          [attr.aria-label]="meta(key).title"
                        />
                      } @else if (meta(key).input === 'provider-ocr') {
                        <p-select
                          [options]="ocrProviders()"
                          [(ngModel)]="edits[key]"
                          optionLabel="label"
                          optionValue="value"
                          [filter]="true"
                          placeholder="Select OCR provider"
                          class="val"
                          [attr.aria-label]="meta(key).title"
                        />
                      } @else {
                        <input
                          pInputText
                          [(ngModel)]="edits[key]"
                          class="val"
                          [attr.aria-label]="meta(key).title"
                        />
                      }
                      @if (meta(key).hint) {
                        <span class="hint">{{ meta(key).hint }}</span>
                      }
                    </div>

                    <div class="card-actions">
                      <p-button
                        label="Save"
                        icon="pi pi-save"
                        size="small"
                        (onClick)="save(key)"
                        [loading]="savingKey() === key"
                      />
                    </div>
                  </article>
                }
              }
            </div>
          }
        </section>
      }

      @if (ungrouped().length > 0) {
        <section class="group" [class.is-open]="isOpen('other')">
          <button
            type="button"
            class="group-toggle"
            (click)="toggle('other')"
            [attr.aria-expanded]="isOpen('other')"
            aria-controls="settings-group-other"
          >
            <span class="group-icon" aria-hidden="true"><i class="pi pi-sliders-h"></i></span>
            <div class="group-text">
              <span class="group-label">Settings group</span>
              <h2>Other</h2>
              <p>Allowlisted keys not yet assigned to a group.</p>
            </div>
            <p-tag [value]="ungrouped().length + ' settings'" severity="secondary" />
            <i class="pi chevron" [class.pi-chevron-down]="isOpen('other')" [class.pi-chevron-right]="!isOpen('other')" aria-hidden="true"></i>
          </button>
          @if (isOpen('other')) {
            <div class="cards" id="settings-group-other">
              @for (s of ungrouped(); track s.key) {
                <article class="card">
                  <div class="card-head">
                    <span class="setting-icon" aria-hidden="true"><i class="pi pi-cog"></i></span>
                    <div class="card-titles">
                      <h3>{{ s.key }}</h3>
                      <code class="key">{{ s.key }}</code>
                    </div>
                  </div>
                  <div class="card-body">
                    <textarea pTextarea [(ngModel)]="edits[s.key]" rows="2" class="val"></textarea>
                  </div>
                  <div class="card-actions">
                    <p-button
                      label="Save"
                      icon="pi pi-save"
                      size="small"
                      (onClick)="save(s.key)"
                      [loading]="savingKey() === s.key"
                    />
                  </div>
                </article>
              }
            </div>
          }
        </section>
      }
    </div>
  `,
  styles: `
    .page {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
      max-width: 56rem;
    }
    .page-head {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      align-items: flex-start;
    }
    .page-head-text {
      flex: 1;
      min-width: 0;
    }
    h1 {
      margin: 0;
      font-size: 1.35rem;
      font-weight: 650;
    }
    .lede {
      margin: 0.35rem 0 0;
      color: var(--p-text-muted-color);
      font-size: 0.9rem;
      line-height: 1.45;
      max-width: 40rem;
    }
    .group {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      margin-top: 0.35rem;
      padding-top: 0.85rem;
      border-top: 2px solid color-mix(in srgb, var(--p-primary-color) 35%, var(--p-content-border-color));
    }
    .group:first-of-type {
      margin-top: 0.15rem;
      padding-top: 0;
      border-top: none;
    }
    .group-toggle {
      display: grid;
      grid-template-columns: auto 1fr auto auto;
      align-items: center;
      gap: 0.85rem;
      width: 100%;
      padding: 0.85rem 1rem;
      border: 1px solid color-mix(in srgb, var(--p-primary-color) 28%, var(--p-content-border-color));
      border-left: 4px solid var(--p-primary-color);
      border-radius: 10px;
      background: color-mix(in srgb, var(--p-primary-color) 7%, var(--p-content-background));
      color: inherit;
      text-align: left;
      cursor: pointer;
      transition: background 0.15s ease, border-color 0.15s ease;
    }
    .group-toggle:hover {
      background: color-mix(in srgb, var(--p-primary-color) 12%, var(--p-content-background));
      border-color: color-mix(in srgb, var(--p-primary-color) 45%, var(--p-content-border-color));
    }
    .group.is-open .group-toggle {
      background: color-mix(in srgb, var(--p-primary-color) 11%, var(--p-content-background));
      box-shadow: inset 0 -1px 0 color-mix(in srgb, var(--p-primary-color) 18%, transparent);
    }
    .group-text {
      min-width: 0;
    }
    .group-label {
      display: block;
      font-size: 0.68rem;
      font-weight: 700;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: var(--p-primary-color);
      margin-bottom: 0.15rem;
    }
    .group-toggle h2 {
      margin: 0;
      font-size: 1.12rem;
      font-weight: 700;
      letter-spacing: -0.01em;
    }
    .group-toggle p {
      margin: 0.25rem 0 0;
      color: var(--p-text-muted-color);
      font-size: 0.82rem;
      line-height: 1.4;
      max-width: 36rem;
    }
    .group-icon,
    .setting-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 2.4rem;
      height: 2.4rem;
      border-radius: 0.55rem;
      flex-shrink: 0;
      background: color-mix(in srgb, var(--p-primary-color) 16%, transparent);
      color: var(--p-primary-color);
      font-size: 1.05rem;
    }
    .setting-icon {
      width: 2rem;
      height: 2rem;
      border-radius: 0.45rem;
      font-size: 0.95rem;
    }
    .chevron {
      color: var(--p-text-muted-color);
      font-size: 0.9rem;
    }
    .cards {
      display: flex;
      flex-direction: column;
      gap: 0.65rem;
    }
    .card {
      display: grid;
      grid-template-columns: 1fr auto;
      grid-template-areas:
        'head actions'
        'body actions';
      gap: 0.55rem 0.85rem;
      padding: 0.85rem 1rem;
      border: 1px solid var(--p-content-border-color);
      border-radius: 10px;
      background: var(--p-content-background);
    }
    .card-head {
      grid-area: head;
      display: flex;
      gap: 0.75rem;
      align-items: flex-start;
    }
    .card-titles h3 {
      margin: 0;
      font-size: 0.95rem;
      font-weight: 600;
    }
    .desc {
      margin: 0.25rem 0 0;
      color: var(--p-text-muted-color);
      font-size: 0.8rem;
      line-height: 1.4;
    }
    .key {
      display: inline-block;
      margin-top: 0.35rem;
      font-family: ui-monospace, monospace;
      font-size: 0.7rem;
      color: var(--p-text-muted-color);
      background: color-mix(in srgb, var(--p-content-border-color) 55%, transparent);
      padding: 0.1rem 0.35rem;
      border-radius: 4px;
    }
    .card-body {
      grid-area: body;
      display: flex;
      flex-direction: column;
      gap: 0.3rem;
      min-width: 0;
    }
    .card-actions {
      grid-area: actions;
      display: flex;
      align-items: flex-start;
      padding-top: 0.15rem;
    }
    .val {
      width: 100%;
      font-family: ui-monospace, monospace;
      font-size: 0.82rem;
    }
    .hint {
      font-size: 0.72rem;
      color: var(--p-text-muted-color);
    }
    .empty {
      color: var(--p-text-muted-color);
    }
    @media (max-width: 720px) {
      .page-head {
        flex-direction: column;
      }
      .card {
        grid-template-columns: 1fr;
        grid-template-areas:
          'head'
          'body'
          'actions';
      }
      .group-toggle {
        grid-template-columns: auto 1fr auto;
      }
      .group-toggle :is(p-tag) {
        display: none;
      }
    }
  `,
})
export class SettingsPage implements OnInit {
  private readonly api = inject(AdminApiService);
  readonly settings = signal<SystemSetting[]>([]);
  readonly llmProviders = signal<ProviderOption[]>([]);
  readonly ocrProviders = signal<ProviderOption[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly savingKey = signal<string | null>(null);
  /** Collapsed by default — only ids present here are open. */
  readonly openGroups = signal<ReadonlySet<string>>(new Set());
  edits: Record<string, string> = {};

  private readonly knownKeys = new Set(Object.keys(SETTING_META));

  readonly visibleGroups = computed(() => {
    const present = new Set(this.settings().map((s) => s.key));
    return SETTING_GROUPS.filter((g) => g.keys.some((k) => present.has(k)));
  });

  readonly ungrouped = computed(() =>
    this.settings().filter((s) => !this.knownKeys.has(s.key)),
  );

  ngOnInit(): void {
    this.reload();
  }

  isOpen(id: string): boolean {
    return this.openGroups().has(id);
  }

  toggle(id: string): void {
    const next = new Set(this.openGroups());
    if (next.has(id)) next.delete(id);
    else next.add(id);
    this.openGroups.set(next);
  }

  hasKey(key: string): boolean {
    return this.settings().some((s) => s.key === key);
  }

  groupCount(group: SettingGroup): number {
    return group.keys.filter((k) => this.hasKey(k)).length;
  }

  meta(key: string): SettingMeta {
    return (
      SETTING_META[key] ?? {
        title: key,
        description: '',
        icon: 'pi pi-cog',
        input: 'json' as const,
      }
    );
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.ok.set(null);

    let settingsDone = false;
    let providersDone = false;
    let failed = false;

    const finish = () => {
      if (settingsDone && providersDone) {
        this.loading.set(false);
      }
    };

    this.api.listSettings().subscribe({
      next: (list) => {
        this.settings.set(list);
        this.edits = Object.fromEntries(
          list.map((s) => [s.key, this.formatForEdit(s.key, s.valueJson)]),
        );
        settingsDone = true;
        finish();
      },
      error: () => {
        if (!failed) {
          failed = true;
          this.error.set('Failed to load settings.');
        }
        settingsDone = true;
        finish();
      },
    });

    this.api.listProviders().subscribe({
      next: (providers) => {
        this.applyProviders(providers);
        providersDone = true;
        finish();
      },
      error: () => {
        this.llmProviders.set([]);
        this.ocrProviders.set([]);
        if (!failed) {
          // Settings may still load; do not block the page on provider catalog failure.
          this.error.set('Failed to load provider catalog.');
        }
        providersDone = true;
        finish();
      },
    });
  }

  save(key: string): void {
    const raw = this.edits[key] ?? 'null';
    let valueJson: string;
    try {
      valueJson = this.normalizeForSave(key, raw);
    } catch {
      this.error.set(`Invalid JSON for ${this.meta(key).title}`);
      return;
    }
    this.savingKey.set(key);
    this.error.set(null);
    this.ok.set(null);
    this.api.putSetting(key, valueJson).subscribe({
      next: () => {
        this.savingKey.set(null);
        this.ok.set(`Saved ${this.meta(key).title}`);
        this.edits[key] = this.formatForEdit(key, valueJson);
      },
      error: (err) => {
        this.savingKey.set(null);
        this.error.set(err?.error?.error ?? `Failed to save ${key}`);
      },
    });
  }

  private applyProviders(providers: AdminProvider[]): void {
    const toOption = (p: AdminProvider): ProviderOption => ({
      label: `${p.name} (${p.providerKey})`,
      value: p.providerKey,
    });

    this.llmProviders.set(
      providers.filter((p) => p.categoryKey === 'llm').map(toOption),
    );
    this.ocrProviders.set(
      providers.filter((p) => p.categoryKey === 'ocr').map(toOption),
    );
  }

  /** Strip surrounding quotes from JSON string values for friendlier text / provider inputs. */
  private formatForEdit(key: string, valueJson: string): string {
    if (this.meta(key).input === 'json') {
      try {
        return JSON.stringify(JSON.parse(valueJson), null, 2);
      } catch {
        return valueJson;
      }
    }
    try {
      const parsed = JSON.parse(valueJson);
      if (typeof parsed === 'string' || typeof parsed === 'number' || typeof parsed === 'boolean') {
        return String(parsed);
      }
      return valueJson;
    } catch {
      return valueJson;
    }
  }

  private normalizeForSave(key: string, raw: string): string {
    const trimmed = raw.trim();
    if (this.meta(key).input === 'json') {
      JSON.parse(trimmed); // throw early if invalid; API would reject too
      return trimmed;
    }
    // Already valid JSON literal?
    try {
      JSON.parse(trimmed);
      return trimmed;
    } catch {
      // Treat as string or number
      if (/^-?\d+(\.\d+)?$/.test(trimmed)) {
        return trimmed;
      }
      return JSON.stringify(trimmed);
    }
  }
}
