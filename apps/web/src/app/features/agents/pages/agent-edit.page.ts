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
import { CatalogsApiService, type DocumentType, type Provider } from '../../../core/api/catalogs-api.service';
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

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly agent = signal<Agent | null>(null);
  readonly documentTypes = signal<DocumentType[]>([]);
  readonly providers = signal<Provider[]>([]);

  name = '';
  description = '';
  documentTypeId: number | null = null;
  instructions = '';
  outputSchemaJson = '';
  schemaVersion = 1;
  isActive = true;
  defaultWorkflowId: number | null = null;
  defaultProviderId: number | null = null;
  postProcessEnabled = true;

  private readonly agentId = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('id') ?? '')),
    { initialValue: this.route.snapshot.paramMap.get('id') ?? '' },
  );

  ngOnInit(): void {
    this.catalogsApi.listDocumentTypes().subscribe((d) => this.documentTypes.set(d));
    this.catalogsApi.listProviders().subscribe((p) => this.providers.set(p));
    this.loadAgent();
  }

  onSchemaChange(json: string): void {
    this.outputSchemaJson = json;
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
        schemaVersion: this.schemaVersion,
        isActive: this.isActive,
        defaultWorkflowId: this.postProcessEnabled ? this.defaultWorkflowId : null,
        defaultProviderId: this.defaultProviderId,
      })
      .subscribe({
        next: (updated) => {
          this.agent.set(updated);
          this.schemaVersion = updated.schemaVersion;
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
        this.defaultWorkflowId = a.defaultWorkflowId ?? null;
        this.defaultProviderId = a.defaultProviderId ?? null;
        this.postProcessEnabled = a.defaultWorkflowId != null;
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Agent not found.');
      },
    });
  }
}
