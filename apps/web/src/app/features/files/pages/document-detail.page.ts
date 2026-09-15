import { DatePipe } from '@angular/common';
import { Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Card } from 'primeng/card';
import { Message } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { map } from 'rxjs';
import { AppContextService } from '../../../core/app-context.service';
import { FilesApiService } from '../data/files-api.service';
import type { DocumentDetail } from '../models/file.models';
import { StatusPillComponent } from '../components/status-pill.component';
import { formatFieldValue, parseResultJson } from '../utils/result-json.util';
import { blobForPreview, isInlinePreviewable, isPdfMime } from '../utils/preview-mime.util';

@Component({
  selector: 'app-document-detail-page',
  imports: [RouterLink, Card, Message, TableModule, StatusPillComponent, DatePipe],
  templateUrl: './document-detail.page.html',
  styleUrl: './document-detail.page.scss',
})
export class DocumentDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly filesApi = inject(FilesApiService);
  readonly ctx = inject(AppContextService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly previewLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly document = signal<DocumentDetail | null>(null);
  readonly previewObjectUrl = signal<string | null>(null);
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

  readonly canPreviewInline = computed(() => {
    const d = this.document();
    return isInlinePreviewable(d?.contentType, d?.originalFileName);
  });

  readonly isPdf = computed(() => {
    const d = this.document();
    return isPdfMime(d?.contentType, d?.originalFileName);
  });

  readonly safePreviewUrl = computed(() => {
    const url = this.previewObjectUrl();
    return url ? this.sanitizer.bypassSecurityTrustResourceUrl(url) : null;
  });

  private lastLoadedKey: string | null = null;

  constructor() {
    this.destroyRef.onDestroy(() => this.revokePreviewUrl());

    effect(() => {
      const queueId = this.ctx.defaultQueueId();
      const documentId = this.documentId();
      const ctxLoading = this.ctx.loading();

      if (ctxLoading) {
        this.loading.set(true);
        return;
      }

      if (!queueId || !documentId) {
        this.loading.set(false);
        this.error.set(
          this.ctx.loadError() ??
            (!documentId ? 'Missing document id.' : 'No default queue configured.'),
        );
        return;
      }

      const key = `${queueId}:${documentId}`;
      if (this.lastLoadedKey === key && this.document()) {
        return;
      }
      this.lastLoadedKey = key;
      this.load(queueId, documentId);
    });
  }

  private load(queueId: string, documentId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.filesApi
      .getDocument(queueId, documentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (doc) => {
          this.document.set(doc);
          this.loading.set(false);
          this.loadPreview(queueId, documentId);
        },
        error: () => {
          this.loading.set(false);
          this.document.set(null);
          this.error.set('Document not found.');
        },
      });
  }

  private loadPreview(queueId: string, documentId: string): void {
    this.revokePreviewUrl();
    this.previewLoading.set(true);
    this.filesApi
      .getDocumentContentBlob(queueId, documentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const typed = blobForPreview(
            blob,
            this.document()?.contentType,
            this.document()?.originalFileName,
          );
          this.previewObjectUrl.set(URL.createObjectURL(typed));
          this.previewLoading.set(false);
        },
        error: () => {
          this.previewObjectUrl.set(null);
          this.previewLoading.set(false);
        },
      });
  }

  private revokePreviewUrl(): void {
    const url = this.previewObjectUrl();
    if (url) {
      URL.revokeObjectURL(url);
      this.previewObjectUrl.set(null);
    }
  }
}
