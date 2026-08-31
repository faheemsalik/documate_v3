import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { Button } from 'primeng/button';
import { Card } from 'primeng/card';
import { Message } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { map } from 'rxjs';
import { AppContextService } from '../../../core/app-context.service';
import { FilesApiService } from '../data/files-api.service';
import type { DocumentListItem, FileDetail } from '../models/file.models';
import { StatusPillComponent } from '../components/status-pill.component';

@Component({
  selector: 'app-file-detail-page',
  imports: [
    RouterLink,
    Button,
    Card,
    Message,
    TableModule,
    StatusPillComponent,
    DatePipe,
    DecimalPipe,
  ],
  templateUrl: './file-detail.page.html',
  styleUrl: './file-detail.page.scss',
})
export class FileDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly filesApi = inject(FilesApiService);
  private readonly ctx = inject(AppContextService);
  private readonly sanitizer = inject(DomSanitizer);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly file = signal<FileDetail | null>(null);
  readonly documents = signal<DocumentListItem[]>([]);
  readonly previewUrl = signal<string | null>(null);

  private readonly fileId = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('fileId') ?? '')),
    { initialValue: this.route.snapshot.paramMap.get('fileId') ?? '' },
  );

  readonly safePreviewUrl = computed(() => {
    const url = this.previewUrl();
    return url ? this.sanitizer.bypassSecurityTrustResourceUrl(url) : null;
  });

  readonly canPreviewInline = computed(() => {
    const ct = this.file()?.contentType?.toLowerCase() ?? '';
    return ct.includes('pdf') || ct.startsWith('image/');
  });

  constructor() {
    this.load();
  }

  refresh(): void {
    this.load();
  }

  private load(): void {
    const queueId = this.ctx.defaultQueueId();
    const fileId = this.fileId();
    if (!queueId || !fileId) {
      this.loading.set(false);
      this.error.set('Missing queue or file id.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.filesApi.getFile(queueId, fileId).subscribe({
      next: (f) => {
        this.file.set(f);
        this.loadDocuments(queueId, fileId);
        this.loadPreview(queueId, fileId);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('File not found.');
      },
    });
  }

  private loadDocuments(queueId: string, fileId: string): void {
    this.filesApi.listDocuments(queueId, fileId).subscribe({
      next: (docs) => {
        this.documents.set(docs);
        this.loading.set(false);
      },
      error: () => {
        this.documents.set([]);
        this.loading.set(false);
      },
    });
  }

  private loadPreview(queueId: string, fileId: string): void {
    this.filesApi.getDownloadUrl(queueId, fileId).subscribe({
      next: (d) => this.previewUrl.set(d.url),
      error: () => this.previewUrl.set(null),
    });
  }
}
