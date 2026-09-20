import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { Tabs, TabList, Tab, TabPanels, TabPanel } from 'primeng/tabs';
import { Textarea } from 'primeng/textarea';
import { map } from 'rxjs';
import { AgentsApiService, type Agent } from '../../../core/api/agents-api.service';
import { CatalogsApiService, type DocumentType } from '../../../core/api/catalogs-api.service';
import {
  IntakeMailboxesApiService,
  type IntakeMailbox,
  type MailboxAllowlistEntry,
} from '../../../core/api/intake-mailboxes-api.service';
import { SchemaFormBuilderComponent } from '../components/schema-form-builder.component';
import { AgentTestRunnerComponent } from '../components/agent-test-runner.component';

@Component({
  selector: 'app-agent-edit-page',
  imports: [
    RouterLink,
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
    Textarea,
    SchemaFormBuilderComponent,
    AgentTestRunnerComponent,
  ],
  templateUrl: './agent-edit.page.html',
  styleUrl: './agent-edit.page.scss',
})
export class AgentEditPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly agentsApi = inject(AgentsApiService);
  private readonly catalogsApi = inject(CatalogsApiService);
  private readonly mailboxesApi = inject(IntakeMailboxesApiService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly agent = signal<Agent | null>(null);
  readonly documentTypes = signal<DocumentType[]>([]);
  readonly typedMailbox = signal<IntakeMailbox | null>(null);
  readonly mailboxBusy = signal(false);
  readonly mailboxAllowlist = signal<MailboxAllowlistEntry[]>([]);
  readonly allowlistModes = signal<{ label: string; value: number }[]>([]);

  name = '';
  description = '';
  documentTypeId: number | null = null;
  instructions = '';
  outputSchemaJson = '';
  schemaVersion = 1;
  isActive = true;
  postProcessPrompt = '';
  mailboxEnabled = true;
  mailboxAllowlistModeEnumId: number | null = null;
  mailboxAllowlistMatchType = 'email';
  mailboxAllowlistValue = '';

  private readonly agentId = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('id') ?? '')),
    { initialValue: this.route.snapshot.paramMap.get('id') ?? '' },
  );

  ngOnInit(): void {
    this.catalogsApi.listDocumentTypes().subscribe((d) => this.documentTypes.set(d));
    this.catalogsApi.listEnums('allowlist_mode').subscribe({
      next: (modes) =>
        this.allowlistModes.set(modes.map((m) => ({ label: m.displayName || m.enumKey, value: m.id }))),
    });
    this.loadAgent();
  }

  onSchemaChange(json: string): void {
    this.outputSchemaJson = json;
  }

  createTypedMailbox(): void {
    const id = this.agentId();
    if (!id) return;
    this.mailboxBusy.set(true);
    this.mailboxesApi.createTyped(id).subscribe({
      next: (mb) => {
        this.typedMailbox.set(mb);
        this.mailboxEnabled = mb.enabled;
        this.mailboxAllowlistModeEnumId = mb.allowlistModeEnumId;
        this.loadMailboxAllowlist(mb.id);
        this.mailboxBusy.set(false);
      },
      error: (err) => {
        this.mailboxBusy.set(false);
        this.error.set(err?.error?.error ?? 'Could not create intake email.');
      },
    });
  }

  rotateMailbox(): void {
    const mb = this.typedMailbox();
    if (!mb) return;
    this.mailboxBusy.set(true);
    this.mailboxesApi.rotate(mb.id).subscribe({
      next: (updated) => {
        this.typedMailbox.set(updated);
        this.mailboxBusy.set(false);
      },
      error: () => {
        this.mailboxBusy.set(false);
        this.error.set('Rotate failed.');
      },
    });
  }

  saveMailboxEnabled(): void {
    const mb = this.typedMailbox();
    if (!mb) return;
    const modeId = this.mailboxAllowlistModeEnumId ?? mb.allowlistModeEnumId;
    this.mailboxesApi
      .update(mb.id, { enabled: this.mailboxEnabled, allowlistModeEnumId: modeId })
      .subscribe({
        next: (updated) => {
          this.typedMailbox.set(updated);
          this.mailboxAllowlistModeEnumId = updated.allowlistModeEnumId;
        },
        error: () => this.error.set('Could not update mailbox.'),
      });
  }

  saveMailboxAllowlistMode(): void {
    this.saveMailboxEnabled();
  }

  addMailboxAllowlist(): void {
    const mb = this.typedMailbox();
    if (!mb || !this.mailboxAllowlistValue.trim()) return;
    this.mailboxesApi
      .addAllowlist(mb.id, {
        matchTypeKey: this.mailboxAllowlistMatchType,
        value: this.mailboxAllowlistValue.trim(),
      })
      .subscribe({
        next: (entry) => {
          this.mailboxAllowlist.update((a) => [...a, entry]);
          this.mailboxAllowlistValue = '';
        },
        error: () => this.error.set('Could not add allowlist entry.'),
      });
  }

  removeMailboxAllowlist(entryId: number): void {
    const mb = this.typedMailbox();
    if (!mb) return;
    this.mailboxesApi.deleteAllowlist(mb.id, entryId).subscribe({
      next: () => this.mailboxAllowlist.update((a) => a.filter((e) => e.id !== entryId)),
      error: () => this.error.set('Could not remove entry.'),
    });
  }

  copyMailboxAddress(): void {
    const addr = this.typedMailbox()?.emailAddress;
    if (addr) void navigator.clipboard.writeText(addr);
  }

  save(): void {
    const agent = this.agent();
    const id = this.agentId();
    if (!agent || !id || this.documentTypeId == null) return;

    this.saving.set(true);
    this.error.set(null);
    this.agentsApi
      .update(id, {
        name: this.name.trim(),
        description: this.description.trim() || null,
        documentTypeId: this.documentTypeId,
        outputSchemaJson: this.outputSchemaJson,
        instructions: this.instructions,
        postProcessPrompt: this.postProcessPrompt,
        schemaVersion: this.schemaVersion,
        isActive: this.isActive,
        defaultWorkflowId: agent.defaultWorkflowId ?? null,
        defaultProviderId: agent.defaultProviderId ?? null,
      })
      .subscribe({
        next: (updated) => {
          this.agent.set(updated);
          this.schemaVersion = updated.schemaVersion;
          this.postProcessPrompt = updated.postProcessPrompt ?? '';
          this.saving.set(false);
        },
        error: () => {
          this.saving.set(false);
          this.error.set('Save failed.');
        },
      });
  }

  private loadAgent(): void {
    const id = this.agentId();
    if (!id) {
      this.loading.set(false);
      this.error.set('Missing agent id.');
      return;
    }

    this.agentsApi.get(id).subscribe({
      next: (a) => {
        this.agent.set(a);
        this.name = a.name;
        this.description = a.description ?? '';
        this.documentTypeId = a.documentTypeId;
        this.instructions = a.instructions;
        this.outputSchemaJson = a.outputSchemaJson;
        this.schemaVersion = a.schemaVersion;
        this.isActive = a.isActive;
        this.postProcessPrompt = a.postProcessPrompt ?? '';
        this.loading.set(false);
        this.loadTypedMailbox(a.id);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Agent not found.');
      },
    });
  }

  private loadTypedMailbox(agentId: string): void {
    this.mailboxesApi.list().subscribe({
      next: (list) => {
        const mb = list.find((m) => m.agentId === agentId && m.kindKey === 'typed_agent') ?? null;
        this.typedMailbox.set(mb);
        this.mailboxEnabled = mb?.enabled ?? true;
        this.mailboxAllowlistModeEnumId = mb?.allowlistModeEnumId ?? null;
        if (mb) this.loadMailboxAllowlist(mb.id);
        else this.mailboxAllowlist.set([]);
      },
    });
  }

  private loadMailboxAllowlist(mailboxId: string): void {
    this.mailboxesApi.listAllowlist(mailboxId).subscribe({
      next: (entries) => this.mailboxAllowlist.set(entries),
      error: () => this.mailboxAllowlist.set([]),
    });
  }
}
