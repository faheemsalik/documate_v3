import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { Card } from 'primeng/card';
import { Message } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { map } from 'rxjs';
import { AppContextService } from '../../../core/app-context.service';
import { FilesApiService } from '../data/files-api.service';
import type { DocumentDetail } from '../models/file.models';
import { StatusPillComponent } from '../components/status-pill.component';
import { formatFieldValue, parseResultJson } from '../utils/result-json.util';

@Component({
  selector: 'app-document-detail-page',
  imports: [RouterLink, Card, Message, TableModule, StatusPillComponent, DatePipe],
  templateUrl: './document-detail.page.html',
  styleUrl: './document-detail.page.scss',
})
export class DocumentDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly filesApi = inject(FilesApiService);
  private readonly ctx = inject(AppContextService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly document = signal<DocumentDetail | null>(null);
  readonly formatFieldValue = formatFieldValue;

  readonly fileId = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('fileId') ?? '')),
    { initialValue: this.route.snapshot.paramMap.get('fileId') ?? '' },
  );

  private readonly documentId = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('documentId') ?? '')),
    { initialValue: this.route.snapshot.paramMap.get('documentId') ?? '' },
  );

  readonly parsedResult = computed(() => parseResultJson(this.document()?.resultJson ?? null));

  constructor() {
    this.load();
  }

  private load(): void {
    const queueId = this.ctx.defaultQueueId();
    const documentId = this.documentId();
    if (!queueId || !documentId) {
      this.loading.set(false);
      this.error.set('Missing queue or document id.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.filesApi.getDocument(queueId, documentId).subscribe({
      next: (doc) => {
        this.document.set(doc);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Document not found.');
      },
    });
  }
}
