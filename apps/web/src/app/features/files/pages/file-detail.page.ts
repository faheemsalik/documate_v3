import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Card } from 'primeng/card';
import { Message } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { map } from 'rxjs';
import { AppContextService } from '../../../core/app-context.service';
import { FilesApiService } from '../data/files-api.service';
import type { DocumentListItem, FileDetail } from '../models/file.models';
import { StatusPillComponent } from '../components/status-pill.component';
import { blobForPreview, isInlinePreviewable, isPdfMime } from '../utils/preview-mime.util';

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
  readonly ctx = inject(AppContextService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly previewLoading = signal(false);
  readonly resetting = signal(false);
  readonly error = signal<string | null>(null);
  readonly docsError = signal<string | null>(null);
  readonly info = signal<string | null>(null);
  readonly file = signal<FileDetail | null>(null);
  readonly documents = signal<DocumentListItem[]>([]);
  readonly previewObjectUrl = signal<string | null>(null);

  private readonly fileId = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('fileId') ?? '')),
    { initialValue: this.route.snapshot.paramMap.get('fileId') ?? '' },
  );

  private lastLoadedKey: string | null = null;

  readonly safePreviewUrl = computed(() => {
    const url = this.previewObjectUrl();
    return url ? this.sanitizer.bypassSecurityTrustResourceUrl(url) : null;
  });

  readonly canPreviewInline = computed(() => {
    const f = this.file();
    return isInlinePreviewable(f?.contentType, f?.originalFileName);
  });

  readonly isPdf = computed(() => {
    const f = this.file();
    return isPdfMime(f?.contentType, f?.originalFileName);
  });

  readonly canReset = computed(
    () => (this.file()?.publicStatusKey ?? '').toLowerCase() === 'failed',
  );

  readonly emailIntake = computed(() => {
    const raw = this.file()?.emailIntakeJson;
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as {
        from?: { email?: string | null; name?: string | null };
        originator?: { email?: string | null; name?: string | null } | null;
        chain?: Array<{ role?: string; email?: string | null; name?: string | null }>;
        emailBodyExcerpt?: string | null;
        skippedAttachments?: Array<{ fileName?: string; reason?: string }>;
      };
    } catch {
      return null;
    }
  });

  readonly hasEmailContext = computed(() => {
    const f = this.file();
    return !!(f?.emailFrom || f?.emailSubject || f?.emailMessageId || this.emailIntake());
  });

  constructor() {
    this.destroyRef.onDestroy(() => this.revokePreviewUrl());

    effect(() => {
      const queueId = this.ctx.defaultQueueId();
      const fileId = this.fileId();
      const ctxLoading = this.ctx.loading();

      if (ctxLoading) {
        this.loading.set(true);
        return;
      }

      if (!queueId || !fileId) {
        this.loading.set(false);
        this.error.set(
          this.ctx.loadError() ?? (!fileId ? 'Missing file id.' : 'No default queue configured.'),
        );
        return;
      }

      const key = `${queueId}:${fileId}`;
      if (this.lastLoadedKey === key && this.file()) {
        return;
      }
      this.lastLoadedKey = key;
      this.load(queueId, fileId);
    });
  }

  refresh(): void {
    const queueId = this.ctx.defaultQueueId();
    const fileId = this.fileId();
    if (!queueId || !fileId) return;
    this.lastLoadedKey = null;
    this.load(queueId, fileId);
  }

  reset(): void {
    const queueId = this.ctx.defaultQueueId();
    const file = this.file();
    if (!queueId || !file || !this.canReset() || this.resetting()) return;

    const name = file.originalFileName ?? file.id;
    if (
      !confirm(
        `Reset "${name}"?\n\nClears prior documents on this file, sets status to received, and re-runs processing on the same file.`,
      )
    ) {
      return;
    }

    this.resetting.set(true);
    this.error.set(null);
    this.info.set(null);
    this.filesApi.resetFile(queueId, file.id).subscribe({
      next: () => {
        this.resetting.set(false);
        this.info.set('Reset queued — refreshing…');
        this.lastLoadedKey = null;
        this.refresh();
      },
      error: (err: { error?: { error?: string } }) => {
        this.resetting.set(false);
        this.error.set(err?.error?.error ?? 'Reset failed.');
      },
    });
  }

  private load(queueId: string, fileId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.docsError.set(null);

    this.filesApi
      .getFile(queueId, fileId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (f) => {
          this.file.set(f);
          this.loading.set(false);
          this.loadDocuments(queueId, fileId);
          this.loadPreviewBlob(queueId, fileId);
        },
        error: () => {
          this.loading.set(false);
          this.file.set(null);
          this.error.set('File not found.');
        },
      });
  }

  private loadDocuments(queueId: string, fileId: string): void {
    this.filesApi
      .listDocuments(queueId, fileId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (docs) => {
          this.documents.set(docs);
          this.docsError.set(null);
        },
        error: () => {
          this.documents.set([]);
          this.docsError.set('Could not load documents for this file.');
        },
      });
  }

  private loadPreviewBlob(queueId: string, fileId: string): void {
    this.revokePreviewUrl();
    this.previewLoading.set(true);
    this.filesApi
      .getFileContentBlob(queueId, fileId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const typed = blobForPreview(blob, this.file()?.contentType, this.file()?.originalFileName);
          const objectUrl = URL.createObjectURL(typed);
          this.previewObjectUrl.set(objectUrl);
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
