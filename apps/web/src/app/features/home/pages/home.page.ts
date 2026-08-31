import { Component, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Card } from 'primeng/card';
import { Message } from 'primeng/message';
import { AppContextService } from '../../../core/app-context.service';
import { FilesApiService } from '../../files/data/files-api.service';
import { formatStatusLabel, statusSeverity } from '../../files/utils/file-status.util';
import { Tag } from 'primeng/tag';

@Component({
  selector: 'app-home-page',
  imports: [RouterLink, Card, Message, Tag],
  templateUrl: './home.page.html',
  styleUrl: './home.page.scss',
})
export class HomePage {
  private readonly filesApi = inject(FilesApiService);
  readonly ctx = inject(AppContextService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly total = signal(0);
  readonly byStatus = signal<[string, number][]>([]);

  readonly formatStatusLabel = formatStatusLabel;
  readonly statusSeverity = statusSeverity;

  constructor() {
    effect(() => {
      const qid = this.ctx.defaultQueueId();
      if (qid && !this.ctx.loading()) this.load(qid);
    });
  }

  private load(queueId: string): void {
    this.loading.set(true);
    this.filesApi.getSummary(queueId).subscribe({
      next: (s) => {
        this.total.set(s.total);
        this.byStatus.set(Object.entries(s.byPublicStatus));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load dashboard metrics.');
      },
    });
  }
}
