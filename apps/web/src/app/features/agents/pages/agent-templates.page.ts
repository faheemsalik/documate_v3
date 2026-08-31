import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Card } from 'primeng/card';
import { Message } from 'primeng/message';
import { AgentsApiService } from '../../../core/api/agents-api.service';
import { CatalogsApiService, type AgentTemplate } from '../../../core/api/catalogs-api.service';

@Component({
  selector: 'app-agent-templates-page',
  imports: [RouterLink, Button, Card, Message],
  templateUrl: './agent-templates.page.html',
  styleUrl: './agent-templates.page.scss',
})
export class AgentTemplatesPage {
  private readonly catalogsApi = inject(CatalogsApiService);
  private readonly agentsApi = inject(AgentsApiService);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly cloning = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly templates = signal<AgentTemplate[]>([]);

  constructor() {
    this.catalogsApi.listAgentTemplates().subscribe({
      next: (items) => {
        this.templates.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load templates.');
      },
    });
  }

  clone(template: AgentTemplate): void {
    this.cloning.set(template.agentTemplateKey);
    this.error.set(null);
    this.agentsApi
      .cloneFromTemplate({ agentTemplateKey: template.agentTemplateKey, name: template.name })
      .subscribe({
        next: (agent) => {
          this.cloning.set(null);
          void this.router.navigate(['/agents', agent.id]);
        },
        error: () => {
          this.cloning.set(null);
          this.error.set('Clone failed.');
        },
      });
  }
}
