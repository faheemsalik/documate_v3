import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import {
  AdminApiService,
  type AdminAgentTemplate,
  type AdminDocumentType,
  type AdminProvider,
} from '../../../core/admin-api.service';
import { SchemaFormBuilderComponent } from '../components/schema-form-builder.component';

const DEFAULT_SYSTEM_PROMPT =
  'You extract structured data from documents. Return ONLY a JSON object matching the schema. No markdown.';

const EMPTY_SCHEMA = JSON.stringify({ type: 'object', properties: {}, required: [] }, null, 2);

@Component({
  selector: 'app-agent-template-edit-page',
  imports: [
    RouterLink,
    FormsModule,
    Button,
    Checkbox,
    Dialog,
    InputText,
    Message,
    Select,
    Textarea,
    SchemaFormBuilderComponent,
  ],
  templateUrl: './agent-template-edit.page.html',
  styleUrl: './agent-template-edit.page.scss',
})
export class AgentTemplateEditPage implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly isCreate = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly documentTypes = signal<AdminDocumentType[]>([]);
  readonly providers = signal<AdminProvider[]>([]);
  readonly template = signal<AdminAgentTemplate | null>(null);
  readonly showPushDialog = signal(false);

  agentTemplateKey = '';
  name = '';
  description = '';
  documentTypeId: number | null = null;
  defaultProviderId: number | null = null;
  isPublished = false;
  systemPrompt = DEFAULT_SYSTEM_PROMPT;
  defaultInstructions = '';
  defaultPostProcessPrompt = '';
  defaultAdditionalDocumentInstructions = '';
  defaultSchemaJson = EMPTY_SCHEMA;

  private loadedSystemPrompt = DEFAULT_SYSTEM_PROMPT;

  ngOnInit(): void {
    this.api.listDocumentTypes().subscribe({
      next: (types) => this.documentTypes.set(types),
      error: () => this.error.set('Could not load document types.'),
    });
    this.api.listProviders('llm').subscribe({
      next: (p) => this.providers.set(p),
    });

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const idParam = params.get('id');
      if (!idParam) {
        this.isCreate.set(true);
        this.template.set(null);
        this.loading.set(false);
        return;
      }

      const id = Number(idParam);
      if (!Number.isFinite(id)) {
        this.error.set('Invalid template id.');
        this.loading.set(false);
        return;
      }

      this.isCreate.set(false);
      this.loading.set(true);
      this.api.getAgentTemplate(id).subscribe({
        next: (t) => {
          this.applyTemplate(t);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Template not found.');
          this.loading.set(false);
        },
      });
    });
  }

  onSchemaChange(json: string): void {
    this.defaultSchemaJson = json;
  }

  requestSave(): void {
    this.error.set(null);
    if (!this.name.trim() || this.documentTypeId == null) {
      this.error.set('Name and document type are required.');
      return;
    }
    if (this.isCreate() && !this.agentTemplateKey.trim()) {
      this.error.set('Template key is required.');
      return;
    }

    const clones = this.template()?.clonedAgentCount ?? 0;
    const systemChanged = this.systemPrompt.trim() !== this.loadedSystemPrompt.trim();
    if (!this.isCreate() && clones > 0 && systemChanged) {
      this.showPushDialog.set(true);
      return;
    }

    this.save(false);
  }

  confirmPush(push: boolean): void {
    this.showPushDialog.set(false);
    this.save(push);
  }

  cancelPush(): void {
    this.showPushDialog.set(false);
  }

  private save(pushSystemPrompt: boolean): void {
    this.saving.set(true);
    this.error.set(null);

    if (this.isCreate()) {
      this.api
        .createAgentTemplate({
          agentTemplateKey: this.agentTemplateKey.trim(),
          name: this.name.trim(),
          description: this.description.trim() || null,
          documentTypeId: this.documentTypeId!,
          defaultSchemaJson: this.defaultSchemaJson,
          defaultInstructions: this.defaultInstructions,
          systemPrompt: this.systemPrompt,
          defaultPostProcessPrompt: this.defaultPostProcessPrompt,
          defaultAdditionalDocumentInstructions: this.defaultAdditionalDocumentInstructions,
          defaultProviderId: this.defaultProviderId,
          isPublished: this.isPublished,
        })
        .subscribe({
          next: (t) => {
            this.saving.set(false);
            void this.router.navigate(['/agent-templates', t.id]);
          },
          error: (err) => {
            this.saving.set(false);
            this.error.set(err?.error?.error ?? 'Create failed.');
          },
        });
      return;
    }

    const id = this.template()?.id;
    if (id == null) return;

    this.api
      .updateAgentTemplate(id, {
        name: this.name.trim(),
        description: this.description.trim() || null,
        documentTypeId: this.documentTypeId!,
        defaultSchemaJson: this.defaultSchemaJson,
        defaultInstructions: this.defaultInstructions,
        systemPrompt: this.systemPrompt,
        defaultPostProcessPrompt: this.defaultPostProcessPrompt,
        defaultAdditionalDocumentInstructions: this.defaultAdditionalDocumentInstructions,
        defaultProviderId: this.defaultProviderId,
        isPublished: this.isPublished,
        pushSystemPrompt,
      })
      .subscribe({
        next: (res) => {
          this.applyTemplate(res.template);
          this.saving.set(false);
        },
        error: (err) => {
          this.saving.set(false);
          this.error.set(err?.error?.error ?? 'Save failed.');
        },
      });
  }

  private applyTemplate(t: AdminAgentTemplate): void {
    this.template.set(t);
    this.agentTemplateKey = t.agentTemplateKey;
    this.name = t.name;
    this.description = t.description ?? '';
    this.documentTypeId = t.documentTypeId;
    this.defaultProviderId = t.defaultProviderId ?? null;
    this.isPublished = t.isPublished;
    this.systemPrompt = t.systemPrompt;
    this.loadedSystemPrompt = t.systemPrompt;
    this.defaultInstructions = t.defaultInstructions;
    this.defaultPostProcessPrompt = t.defaultPostProcessPrompt;
    this.defaultAdditionalDocumentInstructions = t.defaultAdditionalDocumentInstructions ?? '';
    this.defaultSchemaJson = t.defaultSchemaJson || EMPTY_SCHEMA;
  }
}
