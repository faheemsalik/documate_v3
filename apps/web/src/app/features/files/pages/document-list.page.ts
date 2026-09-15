import { Component, effect, inject, signal } from '@angular/core';
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
import type { DocumentListItem } from '../models/file.models';
import { FileStatusCellComponent } from '../components/file-status-cell.component';
import { FILE_STATUS_FILTER_OPTIONS } from '../utils/file-status.util';

ModuleRegistry.registerModules([AllCommunityModule]);

@Component({
  selector: 'app-document-list-page',
  imports: [AgGridAngular, Button, Message, Select, FormsModule, RouterLink],
  templateUrl: './document-list.page.html',
  styleUrl: './document-list.page.scss',
})
export class DocumentListPage {
  private readonly filesApi = inject(FilesApiService);
  readonly ctx = inject(AppContextService);
  private readonly router = inject(Router);

  readonly theme = documateAgTheme;
  readonly statusOptions = FILE_STATUS_FILTER_OPTIONS;

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly rows = signal<DocumentListItem[]>([]);
  readonly totalCount = signal(0);
  statusFilter: string | null = null;

  readonly columnDefs: ColDef<DocumentListItem>[] = [
    {
      field: 'originalFileName',
      headerName: 'Source file',
      flex: 2,
      minWidth: 180,
      valueFormatter: (p) => p.value ?? 'Untitled',
    },
    {
      field: 'documentTypeKey',
      headerName: 'Type',
      width: 160,
      valueFormatter: (p) => p.value ?? '—',
    },
    {
      field: 'publicStatusKey',
      headerName: 'Status',
      width: 140,
      cellRenderer: FileStatusCellComponent,
    },
    {
      field: 'internalStageKey',
      headerName: 'Stage',
      width: 140,
      valueFormatter: (p) => p.value ?? '—',
    },
    {
      field: 'createdAt',
      headerName: 'Created',
      width: 170,
      valueFormatter: (p) => this.formatDate(p.value),
    },
    {
      field: 'completedAt',
      headerName: 'Completed',
      width: 170,
      valueFormatter: (p) => (p.value ? this.formatDate(p.value) : '—'),
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
        this.loadDocuments();
      }
    });
  }

  onStatusFilterChange(): void {
    this.loadDocuments();
  }

  refresh(): void {
    this.loadDocuments();
  }

  onRowClicked(event: RowClickedEvent<DocumentListItem>): void {
    const doc = event.data;
    if (doc?.id && doc.fileId) {
      void this.router.navigate(['/files', doc.fileId, 'documents', doc.id]);
    }
  }

  private loadDocuments(): void {
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
      .listQueueDocuments(queueId, {
        page: 1,
        pageSize: 100,
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
          this.error.set('Could not load documents.');
        },
      });
  }

  private formatDate(value: unknown): string {
    if (!value) return '—';
    return new Date(String(value)).toLocaleString();
  }
}
