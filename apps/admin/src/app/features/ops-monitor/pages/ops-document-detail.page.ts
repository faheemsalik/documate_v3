import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Card } from 'primeng/card';
import { Message } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { AdminApiService, type AdminDocumentDetail } from '../../../core/admin-api.service';

@Component({
  selector: 'app-ops-document-detail-page',
  imports: [RouterLink, Button, Card, Message, TableModule, DatePipe],
  templateUrl: './ops-document-detail.page.html',
  styleUrl: './ops-document-detail.page.scss',
})
export class OpsDocumentDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(AdminApiService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly previewLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly document = signal<AdminDocumentDetail | null>(null);
  readonly previewObjectUrl = signal<string | null>(null);
  readonly resultJsonOpen = signal(true);
  readonly copied = signal(false);

  readonly parsedResult = computed(() => parseResultJson(this.document()?.resultJson ?? null));

  readonly formattedResultJson = computed(() => {
    const raw = this.document()?.resultJson;
    if (raw == null) return null;
    try {
      return JSON.stringify(raw, null, 2);
    } catch {
      return String(raw);
    }
  });

  readonly canPreviewInline = computed(() => {
    const d = this.document();
    return isPreviewable(d?.contentType, d?.originalFileName);
  });

  readonly isPdf = computed(() =>
    mimeIsPdf(fMime(this.document()?.contentType, this.document()?.originalFileName)),
  );

  readonly safePreviewUrl = computed(() => {
    const url = this.previewObjectUrl();
    return url ? this.sanitizer.bypassSecurityTrustResourceUrl(url) : null;
  });

  /** Friendly H1 — document type name, not an internal id/key as-is. */
  readonly pageTitle = computed(() => {
    const key = this.document()?.documentTypeKey;
    const label = humanizeKey(key);
    return label || 'Document';
  });

  readonly typeLabel = computed(() => humanizeKey(this.document()?.documentTypeKey) || '—');
  readonly statusLabel = computed(() => humanizeKey(this.document()?.publicStatusKey) || '—');
  readonly stageLabel = computed(() => humanizeKey(this.document()?.internalStageKey) || '—');

  readonly formatFieldValue = formatFieldValue;

  ngOnInit(): void {
    this.destroyRef.onDestroy(() => this.revokePreview());
    const documentId = this.route.snapshot.paramMap.get('documentId');
    if (!documentId) {
      this.loading.set(false);
      this.error.set('Missing document id.');
      return;
    }
    this.load(documentId);
  }

  refresh(): void {
    const documentId = this.route.snapshot.paramMap.get('documentId');
    if (documentId) this.load(documentId);
  }

  toggleResultJson(): void {
    this.resultJsonOpen.update((open) => !open);
  }

  copyResultJson(): void {
    const json = this.formattedResultJson();
    if (!json || typeof navigator === 'undefined' || !navigator.clipboard?.writeText) return;
    void navigator.clipboard.writeText(json).then(() => {
      this.copied.set(true);
      window.setTimeout(() => this.copied.set(false), 1500);
    });
  }

  private load(documentId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.api
      .getDocument(documentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (doc) => {
          this.document.set(doc);
          this.loading.set(false);
          this.loadPreview(documentId);
        },
        error: () => {
          this.loading.set(false);
          this.error.set('Document not found.');
        },
      });
  }

  private loadPreview(documentId: string): void {
    this.revokePreview();
    this.previewLoading.set(true);
    this.api
      .getDocumentContentBlob(documentId)
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

function parseResultJson(result: Record<string, unknown> | null | undefined): {
  scalars: { key: string; value: unknown }[];
  tables: { key: string; rows: Record<string, unknown>[]; columns: string[] }[];
} {
  if (!result) return { scalars: [], tables: [] };
  const scalars: { key: string; value: unknown }[] = [];
  const tables: { key: string; rows: Record<string, unknown>[]; columns: string[] }[] = [];
  for (const [key, value] of Object.entries(result)) {
    if (
      Array.isArray(value) &&
      value.length > 0 &&
      value.every((row) => row !== null && typeof row === 'object' && !Array.isArray(row))
    ) {
      const rows = value as Record<string, unknown>[];
      const columns: string[] = [];
      const seen = new Set<string>();
      for (const row of rows) {
        for (const col of Object.keys(row)) {
          if (!seen.has(col)) {
            seen.add(col);
            columns.push(col);
          }
        }
      }
      tables.push({ key, rows, columns });
    } else {
      scalars.push({ key, value });
    }
  }
  return { scalars, tables };
}

function formatFieldValue(value: unknown): string {
  if (value == null) return '—';
  if (typeof value === 'object') return JSON.stringify(value);
  return String(value);
}

/** Turn snake/kebab/camel keys into readable labels for layman users. */
function humanizeKey(key?: string | null): string {
  if (!key?.trim()) return '';
  const spaced = key
    .trim()
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .replace(/[_\-.]+/g, ' ')
    .replace(/\s+/g, ' ');
  return spaced
    .split(' ')
    .filter(Boolean)
    .map((w) => w.charAt(0).toUpperCase() + w.slice(1).toLowerCase())
    .join(' ');
}
