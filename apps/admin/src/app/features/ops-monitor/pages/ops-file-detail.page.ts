import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Card } from 'primeng/card';
import { Message } from 'primeng/message';
import { TableModule } from 'primeng/table';
import {
  AdminApiService,
  type AdminFileDetail,
  type AdminFileDocumentItem,
} from '../../../core/admin-api.service';

@Component({
  selector: 'app-ops-file-detail-page',
  imports: [RouterLink, Button, Card, Message, TableModule, DatePipe],
  templateUrl: './ops-file-detail.page.html',
  styleUrl: './ops-file-detail.page.scss',
})
export class OpsFileDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(AdminApiService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly previewLoading = signal(false);
  readonly resetting = signal(false);
  readonly error = signal<string | null>(null);
  readonly info = signal<string | null>(null);
  readonly file = signal<AdminFileDetail | null>(null);
  readonly documents = signal<AdminFileDocumentItem[]>([]);
  readonly previewObjectUrl = signal<string | null>(null);

  readonly canPreviewInline = computed(() => {
    const f = this.file();
    return isPreviewable(f?.contentType, f?.originalFileName);
  });

  readonly isPdf = computed(() => mimeIsPdf(fMime(this.file()?.contentType, this.file()?.originalFileName)));

  readonly canReset = computed(
    () => (this.file()?.publicStatusKey ?? '').toLowerCase() === 'failed',
  );

  readonly safePreviewUrl = computed(() => {
    const url = this.previewObjectUrl();
    return url ? this.sanitizer.bypassSecurityTrustResourceUrl(url) : null;
  });

  ngOnInit(): void {
    this.destroyRef.onDestroy(() => this.revokePreview());
    const fileId = this.route.snapshot.paramMap.get('fileId');
    if (!fileId) {
      this.loading.set(false);
      this.error.set('Missing file id.');
      return;
    }
    this.load(fileId);
  }

  refresh(): void {
    const fileId = this.route.snapshot.paramMap.get('fileId');
    if (fileId) this.load(fileId);
  }

  reset(): void {
    const file = this.file();
    if (!file || !this.canReset() || this.resetting()) return;

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
    this.api.resetFile(file.id).subscribe({
      next: (res) => {
        this.resetting.set(false);
        this.info.set(
          `Reset queued` +
            (res.softDeletedDocumentCount
              ? ` — cleared ${res.softDeletedDocumentCount} document(s)`
              : ''),
        );
        this.refresh();
      },
      error: (err: { error?: { error?: string }; message?: string }) => {
        this.resetting.set(false);
        this.error.set(err?.error?.error ?? err?.message ?? 'Reset failed.');
      },
    });
  }

  private load(fileId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.api
      .getFile(fileId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (f) => {
          this.file.set(f);
          this.loading.set(false);
          this.loadDocs(fileId);
          this.loadPreview(fileId);
        },
        error: () => {
          this.loading.set(false);
          this.error.set('File not found.');
        },
      });
  }

  private loadDocs(fileId: string): void {
    this.api
      .listFileDocuments(fileId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (docs) => this.documents.set(docs),
        error: () => this.documents.set([]),
      });
  }

  private loadPreview(fileId: string): void {
    this.revokePreview();
    this.previewLoading.set(true);
    this.api
      .getFileContentBlob(fileId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const typed = blobForPreview(blob, this.file()?.contentType, this.file()?.originalFileName);
          this.previewObjectUrl.set(URL.createObjectURL(typed));
          this.previewLoading.set(false);
        },
        error: () => {
          this.previewObjectUrl.set(null);
          this.previewLoading.set(false);
        },
      });
  }

  private revokePreview(): void {
    const url = this.previewObjectUrl();
    if (url) {
      URL.revokeObjectURL(url);
      this.previewObjectUrl.set(null);
    }
  }
}

function fMime(contentType?: string | null, fileName?: string | null): string {
  const ct = (contentType ?? '').trim().toLowerCase();
  if (ct && ct !== 'application/octet-stream' && ct !== 'binary/octet-stream') return ct;
  const name = (fileName ?? '').toLowerCase();
  const ext = name.includes('.') ? name.slice(name.lastIndexOf('.')) : '';
  switch (ext) {
    case '.pdf':
      return 'application/pdf';
    case '.png':
      return 'image/png';
    case '.jpg':
    case '.jpeg':
      return 'image/jpeg';
    case '.gif':
      return 'image/gif';
    case '.webp':
      return 'image/webp';
    default:
      return ct || 'application/octet-stream';
  }
}

function mimeIsPdf(mime: string): boolean {
  return mime.includes('pdf');
}

function isPreviewable(contentType?: string | null, fileName?: string | null): boolean {
  const mime = fMime(contentType, fileName);
  return mime.includes('pdf') || mime.startsWith('image/');
}

function blobForPreview(blob: Blob, contentType?: string | null, fileName?: string | null): Blob {
  const mime = fMime(contentType || blob.type, fileName);
  return !mime || mime === blob.type ? blob : new Blob([blob], { type: mime });
}
