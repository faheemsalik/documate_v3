import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { Tag } from 'primeng/tag';
import { AdminApiService, type AdminAgentTemplate } from '../../../core/admin-api.service';

@Component({
  selector: 'app-agent-template-list-page',
  imports: [RouterLink, Button, Message, Tag],
  templateUrl: './agent-template-list.page.html',
  styleUrl: './agent-template-list.page.scss',
})
export class AgentTemplateListPage implements OnInit {
  private readonly api = inject(AdminApiService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly rows = signal<AdminAgentTemplate[]>([]);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listAgentTemplates().subscribe({
      next: (items) => {
        this.rows.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load agent templates.');
        this.loading.set(false);
      },
    });
  }
}
