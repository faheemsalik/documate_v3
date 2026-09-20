import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { Tag } from 'primeng/tag';
import { Textarea } from 'primeng/textarea';
import { forkJoin } from 'rxjs';
import {
  AdminApiService,
  type AdminAgentDetail,
  type AdminAgentPromptPreview,
} from '../../../core/admin-api.service';

@Component({
  selector: 'app-agent-detail-page',
  imports: [DatePipe, FormsModule, RouterLink, Button, Message, Tag, Textarea],
  templateUrl: './agent-detail.page.html',
  styleUrl: './agent-detail.page.scss',
})
export class AgentDetailPage implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly info = signal<string | null>(null);
  readonly agent = signal<AdminAgentDetail | null>(null);
  readonly preview = signal<AdminAgentPromptPreview | null>(null);
  readonly generating = signal(false);
  readonly saving = signal(false);
  readonly previewOpen = signal(false);

  systemPromptDraft = '';
  private agentId: string | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      this.error.set('Missing agent id.');
      return;
    }
    this.agentId = id;
    this.load(id);
  }

  prettySchema(json: string | null | undefined): string {
    if (!json) return '{}';
    try {
      return JSON.stringify(JSON.parse(json), null, 2);
    } catch {
      return json;
    }
  }

  togglePreview(): void {
    const next = !this.previewOpen();
    this.previewOpen.set(next);
    if (next) this.refreshPreview();
  }

  generateSystemPrompt(): void {
    const id = this.agentId;
    if (!id) return;
    this.generating.set(true);
    this.error.set(null);
    this.info.set(null);
    this.api.suggestAgentSystemPrompt(id).subscribe({
      next: (res) => {
        this.systemPromptDraft = res.systemPrompt;
        this.generating.set(false);
        this.info.set('Draft system prompt generated — review and Save to persist.');
        this.refreshPreview();
      },
      error: () => {
        this.generating.set(false);
        this.error.set('Failed to generate system prompt.');
      },
    });
  }

  saveSystemPrompt(): void {
    const id = this.agentId;
    const agent = this.agent();
    if (!id || !agent) return;
    this.saving.set(true);
    this.error.set(null);
    this.info.set(null);
    this.api.updateAgentSystemPrompt(id, { systemPrompt: this.systemPromptDraft }).subscribe({
      next: (updated) => {
        this.agent.set(updated);
        this.systemPromptDraft = updated.systemPrompt;
        this.saving.set(false);
        this.info.set('System prompt saved.');
        this.refreshPreview();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err?.error?.error ?? 'Save failed.');
      },
    });
  }

  private refreshPreview(): void {
    const id = this.agentId;
    const agent = this.agent();
    if (!id || !agent) return;
    this.api
      .previewAgentPrompt(id, {
        systemPrompt: this.systemPromptDraft,
        instructions: agent.instructions,
        outputSchemaJson: agent.outputSchemaJson,
        additionalDocumentInstructions: agent.additionalDocumentInstructions,
      })
      .subscribe({
        next: (p) => this.preview.set(p),
        error: () => {
          /* keep last preview */
        },
      });
  }

  private load(id: string): void {
    this.loading.set(true);
    this.error.set(null);
    forkJoin({
      agent: this.api.getAgent(id),
      preview: this.api.getAgentPromptPreview(id),
    }).subscribe({
      next: ({ agent, preview }) => {
        this.agent.set(agent);
        this.systemPromptDraft = agent.systemPrompt ?? '';
        this.preview.set(preview);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Agent not found.');
      },
    });
  }
}
