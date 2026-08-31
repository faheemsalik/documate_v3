import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Card } from 'primeng/card';
import { Message } from 'primeng/message';
import { Tag } from 'primeng/tag';
import { AgentsApiService, type Agent } from '../../../core/api/agents-api.service';

@Component({
  selector: 'app-agent-list-page',
  imports: [RouterLink, Button, Card, Message, Tag],
  templateUrl: './agent-list.page.html',
  styleUrl: './agent-list.page.scss',
})
export class AgentListPage implements OnInit {
  private readonly agentsApi = inject(AgentsApiService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly agents = signal<Agent[]>([]);

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.agentsApi.list().subscribe({
      next: (items) => {
        this.agents.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load agents.');
      },
    });
  }
}
