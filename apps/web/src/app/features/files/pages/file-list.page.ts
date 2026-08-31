import { Component, effect, inject, signal, viewChild, ElementRef } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AgGridAngular } from 'ag-grid-angular';
import {
  AllCommunityModule,
  ModuleRegistry,
  type ColDef,
  type RowClickedEvent,
} from 'ag-grid-community';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { documateAgTheme } from '../../../core/ag-grid-theme';
import { AppContextService } from '../../../core/app-context.service';
import { FilesApiService } from '../data/files-api.service';
import type { FileListItem } from '../models/file.models';
import { FileStatusCellComponent } from '../components/file-status-cell.component';
import { FILE_STATUS_FILTER_OPTIONS } from '../utils/file-status.util';

ModuleRegistry.registerModules([AllCommunityModule]);

@Component({
  selector: 'app-file-list-page',
  imports: [
    AgGridAngular,
    Button,
    Message,
    Select,
    FormsModule,
    RouterLink,
  ],
  templateUrl: './file-list.page.html',
  styleUrl: './file-list.page.scss',
})
export class FileListPage {
  private readonly filesApi = inject(FilesApiService);
  readonly ctx = inject(AppContextService);
  private readonly router = inject(Router);

  readonly grid = viewChild(AgGridAngular);
  readonly fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');
  readonly theme = documateAgTheme;
  readonly statusOptions = FILE_STATUS_FILTER_OPTIONS;

  readonly loading = signal(false);
  readonly uploading = signal(false);
  readonly error = signal<string | null>(null);
  readonly rows = signal<FileListItem[]>([]);
  readonly totalCount = signal(0);
  statusFilter: string | null = null;

  readonly columnDefs: ColDef<FileListItem>[] = [
    {
      field: 'originalFileName',
      headerName: 'File',
      flex: 2,
      minWidth: 180,
      valueFormatter: (p) => p.value ?? 'Untitled',
    },
    {
      field: 'publicStatusKey',
      headerName: 'Status',
      width: 140,
      cellRenderer: FileStatusCellComponent,
    },
    {
      field: 'documentCount',
      headerName: 'Docs',
      width: 90,
      type: 'numericColumn',
    },
    {
      field: 'createdAt',
      headerName: 'Uploaded',
      width: 170,
      valueFormatter: (p) => this.formatDate(p.value),
    },
    {
      field: 'completedAt',
      headerName: 'Completed',
      width: 170,
      valueFormatter: (p) => (p.value ? this.formatDate(p.value) : '—'),
    },
    {
      field: 'sizeBytes',
      headerName: 'Size',
      width: 110,
      valueFormatter: (p) => this.formatBytes(Number(p.value ?? 0)),
    },
  ];

  readonly defaultColDef: ColDef = {
    sortable: true,
    resizable: true,
    filter: false,
  };

  constructor() {
    effect(() => {
      if (!this.ctx.loading() && this.ctx.defaultQueueId()) {
        this.loadFiles();
      }
    });
  }

  onStatusFilterChange(): void {
    this.loadFiles();
  }

  refresh(): void {
    this.loadFiles();
  }

  onRowClicked(event: RowClickedEvent<FileListItem>): void {
    const id = event.data?.id;
    if (id) {
      void this.router.navigate(['/files', id]);
    }
  }

  triggerUpload(): void {
    this.fileInput()?.nativeElement.click();
  }

  onUploadSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    const queueId = this.ctx.defaultQueueId();
    if (!queueId) {
      this.error.set('No default queue configured.');
      return;
    }

    this.uploading.set(true);
    this.error.set(null);
    this.filesApi.uploadFile(queueId, file).subscribe({
      next: (created) => {
        this.uploading.set(false);
        void this.router.navigate(['/files', created.id]);
      },
      error: () => {
        this.uploading.set(false);
        this.error.set('Upload failed.');
      },
    });
  }

  private loadFiles(): void {
    const queueId = this.ctx.defaultQueueId();
    if (!queueId) {
      if (!this.ctx.loading()) {
        this.error.set(this.ctx.loadError() ?? 'No default queue configured.');
      }
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.filesApi
      .listFiles(queueId, {
        page: 1,
        pageSize: 50,
        status: this.statusFilter ?? undefined,
      })
      .subscribe({
        next: (page) => {
          this.rows.set(page.items);
          this.totalCount.set(page.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.error.set('Could not load files.');
        },
      });
  }

  private formatDate(value: unknown): string {
    if (!value) return '—';
    return new Date(String(value)).toLocaleString();
  }

  private formatBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
