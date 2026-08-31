import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AgGridAngular } from 'ag-grid-angular';
import { AllCommunityModule, ModuleRegistry, type ColDef, type RowClickedEvent } from 'ag-grid-community';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { documateAgTheme } from '../../../core/ag-grid-theme';
import { AppContextService } from '../../../core/app-context.service';
import { FilesApiService } from '../data/files-api.service';
import type { FileSchemaSearchItem } from '../models/file.models';
import { FileStatusCellComponent } from '../components/file-status-cell.component';

ModuleRegistry.registerModules([AllCommunityModule]);

@Component({
  selector: 'app-schema-search-page',
  imports: [AgGridAngular, Button, InputText, Message, Select, FormsModule, RouterLink],
  templateUrl: './schema-search.page.html',
  styleUrl: './schema-search.page.scss',
})
export class SchemaSearchPage {
  private readonly filesApi = inject(FilesApiService);
  readonly ctx = inject(AppContextService);
  private readonly router = inject(Router);

  readonly theme = documateAgTheme;
  readonly matchModes = [
    { label: 'Exact', value: 'exact' },
    { label: 'Contains', value: 'contains' },
  ];

  fieldKey = 'invoice_number';
  searchValue = '';
  matchMode: 'exact' | 'contains' = 'exact';

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly rows = signal<FileSchemaSearchItem[]>([]);
  readonly totalCount = signal(0);

  readonly columnDefs: ColDef<FileSchemaSearchItem>[] = [
    { field: 'originalFileName', headerName: 'File', flex: 2, minWidth: 160 },
    { field: 'fieldKey', headerName: 'Field', width: 140 },
    { field: 'matchedValue', headerName: 'Value', flex: 1, minWidth: 120 },
    {
      field: 'publicStatusKey',
      headerName: 'Status',
      width: 130,
      cellRenderer: FileStatusCellComponent,
    },
    { field: 'documentTypeKey', headerName: 'Type', width: 110 },
    {
      field: 'fileCreatedAt',
      headerName: 'Uploaded',
      width: 170,
      valueFormatter: (p) => (p.value ? new Date(String(p.value)).toLocaleString() : '—'),
    },
  ];

  search(): void {
    const queueId = this.ctx.defaultQueueId();
    if (!queueId) {
      this.error.set('No default queue configured.');
      return;
    }
    if (!this.fieldKey.trim() || !this.searchValue.trim()) {
      this.error.set('Field key and value are required.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.filesApi
      .searchFiles(queueId, {
        fieldKey: this.fieldKey.trim(),
        value: this.searchValue.trim(),
        matchMode: this.matchMode,
        page: 1,
        pageSize: 50,
      })
      .subscribe({
        next: (page) => {
          this.rows.set(page.items);
          this.totalCount.set(page.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.error.set('Search failed.');
        },
      });
  }

  onRowClicked(event: RowClickedEvent<FileSchemaSearchItem>): void {
    const id = event.data?.fileId;
    if (id) void this.router.navigate(['/files', id]);
  }
}
