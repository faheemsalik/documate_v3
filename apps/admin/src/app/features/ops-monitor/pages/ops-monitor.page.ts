import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AgGridAngular } from 'ag-grid-angular';
import {
  AllCommunityModule,
  ModuleRegistry,
  type CellClickedEvent,
  type ColDef,
  type GridApi,
} from 'ag-grid-community';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Tabs, TabList, Tab, TabPanels, TabPanel } from 'primeng/tabs';
import { MultiSelect } from 'primeng/multiselect';
import { Message } from 'primeng/message';
import { documateAgTheme } from '../../../core/ag-grid-theme';
import {
  AdminApiService,
  type AdminDocumentListItem,
  type AdminFileListItem,
} from '../../../core/admin-api.service';

ModuleRegistry.registerModules([AllCommunityModule]);

const FILE_COLS_KEY = 'documate.admin.ops.files.columns';
const DOC_COLS_KEY = 'documate.admin.ops.docs.columns';

@Component({
  selector: 'app-ops-monitor-page',
  imports: [
    AgGridAngular,
    FormsModule,
    Button,
    InputText,
    Select,
    Tabs,
    TabList,
    Tab,
    TabPanels,
    TabPanel,
    MultiSelect,
    Message,
  ],
  templateUrl: './ops-monitor.page.html',
  styleUrl: './ops-monitor.page.scss',
})
export class OpsMonitorPage implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly theme = documateAgTheme;
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly info = signal<string | null>(null);
  readonly resettingIds = signal<ReadonlySet<string>>(new Set());
  readonly fileRows = signal<AdminFileListItem[]>([]);
  readonly docRows = signal<AdminDocumentListItem[]>([]);
  readonly fileTotal = signal(0);
  readonly docTotal = signal(0);
  readonly page = signal(1);
  readonly pageSize = 50;

  activeTab: string | number = '0';
  businessId = '';
  status = '';
  stage = '';
  name = '';
  webhookStatus = '';
  createdFrom = '';
  createdTo = '';

  readonly statusOptions = [
    { label: 'Any status', value: '' },
    { label: 'received', value: 'received' },
    { label: 'processing', value: 'processing' },
    { label: 'ready', value: 'ready' },
    { label: 'partial_ready', value: 'partial_ready' },
    { label: 'failed', value: 'failed' },
    { label: 'rejected', value: 'rejected' },
    { label: 'cancelled', value: 'cancelled' },
  ];

  readonly webhookOptions = [
    { label: 'Any webhook', value: '' },
    { label: 'pending', value: 'pending' },
    { label: 'succeeded', value: 'succeeded' },
    { label: 'exhausted', value: 'exhausted' },
    { label: 'skipped', value: 'skipped' },
    { label: 'not_configured', value: 'not_configured' },
  ];

  readonly fileColumnDefs: ColDef<AdminFileListItem>[] = [
    {
      colId: 'org',
      headerName: 'Business',
      flex: 1.4,
      minWidth: 160,
      valueGetter: (p) => p.data?.businessName ?? '',
      cellRenderer: (p: { data?: AdminFileListItem }) =>
        this.orgCell(p.data?.businessName, p.data?.tenantName),
    },
    {
      field: 'originalFileName',
      headerName: 'File',
      flex: 1.6,
      minWidth: 160,
      valueFormatter: (p) => p.value ?? 'Untitled',
    },
    { field: 'publicStatusKey', headerName: 'Status', width: 120 },
    { field: 'internalStageKey', headerName: 'Stage', width: 110, hide: true },
    { field: 'sourceKey', headerName: 'Source', width: 100, hide: true },
    { field: 'documentCount', headerName: 'Docs', width: 80 },
    {
      field: 'createdAt',
      headerName: 'Uploaded',
      width: 160,
      valueFormatter: (p) => this.fmt(p.value),
    },
    {
      field: 'completedAt',
      headerName: 'Completed',
      width: 160,
      hide: true,
      valueFormatter: (p) => (p.value ? this.fmt(p.value) : '—'),
    },
    {
      field: 'sizeBytes',
      headerName: 'Size',
      width: 90,
      hide: true,
      valueFormatter: (p) => this.fmtBytes(Number(p.value ?? 0)),
    },
    { field: 'emailFrom', headerName: 'Email from', width: 160, hide: true },
    { field: 'errorMessage', headerName: 'Error', flex: 1, minWidth: 120, hide: true },
    { field: 'businessId', headerName: 'Business ID', width: 140, hide: true },
    { field: 'id', headerName: 'File ID', width: 140, hide: true },
    {
      colId: 'actions',
      headerName: '',
      width: 100,
      pinned: 'right',
      sortable: false,
      filter: false,
      suppressHeaderMenuButton: true,
      cellRenderer: (p: { data?: AdminFileListItem }) => this.resetCell(p.data?.id),
    },
  ];

  readonly docColumnDefs: ColDef<AdminDocumentListItem>[] = [
    {
      colId: 'org',
      headerName: 'Business',
      flex: 1.4,
      minWidth: 160,
      cellRenderer: (p: { data?: AdminDocumentListItem }) =>
        this.orgCell(p.data?.businessName, p.data?.tenantName),
    },
    {
      field: 'originalFileName',
      headerName: 'File',
      flex: 1.4,
      minWidth: 140,
      valueFormatter: (p) => p.value ?? 'Untitled',
    },
    { field: 'documentTypeKey', headerName: 'Type', width: 120 },
    { field: 'publicStatusKey', headerName: 'Status', width: 110 },
    {
      colId: 'webhook',
      headerName: 'Webhook',
      flex: 1.2,
      minWidth: 180,
      cellRenderer: (p: { data?: AdminDocumentListItem }) => this.webhookCell(p.data),
    },
    { field: 'agentName', headerName: 'Agent', width: 120, hide: true },
    { field: 'internalStageKey', headerName: 'Stage', width: 110, hide: true },
    {
      field: 'createdAt',
      headerName: 'Created',
      width: 150,
      valueFormatter: (p) => this.fmt(p.value),
    },
    { field: 'errorMessage', headerName: 'Error', flex: 1, minWidth: 120, hide: true },
    { field: 'id', headerName: 'Doc ID', width: 140, hide: true },
    {
      colId: 'actions',
      headerName: '',
      width: 100,
      pinned: 'right',
      sortable: false,
      filter: false,
      suppressHeaderMenuButton: true,
      cellRenderer: (p: { data?: AdminDocumentListItem }) => this.resetCell(p.data?.id),
    },
  ];

  readonly defaultColDef: ColDef = { sortable: true, resizable: true, filter: false };

  fileVisibleCols: string[] = [];
  docVisibleCols: string[] = [];
  fileColOptions: { label: string; value: string }[] = [];
  docColOptions: { label: string; value: string }[] = [];

  private fileGridApi: GridApi | null = null;
  private docGridApi: GridApi | null = null;

  ngOnInit(): void {
    this.fileColOptions = this.fileColumnDefs
      .map((c) => ({
        label: c.headerName ?? String(c.field ?? c.colId),
        value: String(c.colId ?? c.field),
      }))
      .filter((c) => c.label);
    this.docColOptions = this.docColumnDefs
      .map((c) => ({
        label: c.headerName ?? String(c.field ?? c.colId),
        value: String(c.colId ?? c.field),
      }))
      .filter((c) => c.label);

    const q = this.route.snapshot.queryParamMap;
    this.businessId = q.get('businessId') ?? '';
    const tab = q.get('tab');
    this.activeTab = tab === 'documents' ? '1' : '0';
    this.createdFrom = q.get('date') ? `${q.get('date')}T00:00:00.000Z` : '';
    this.createdTo = q.get('date') ? `${q.get('date')}T23:59:59.999Z` : '';

    this.fileVisibleCols = this.loadCols(FILE_COLS_KEY, this.fileColumnDefs);
    this.docVisibleCols = this.loadCols(DOC_COLS_KEY, this.docColumnDefs);
    if (!this.fileVisibleCols.includes('actions')) {
      this.fileVisibleCols = [...this.fileVisibleCols, 'actions'];
    }
    if (!this.docVisibleCols.includes('actions')) {
      this.docVisibleCols = [...this.docVisibleCols, 'actions'];
    }

    this.reload();
  }

  onTabChange(value: string | number | undefined): void {
    this.activeTab = value ?? '0';
    this.page.set(1);
    this.reload();
  }

  applyFilters(): void {
    this.page.set(1);
    this.reload();
  }

  nextPage(): void {
    this.page.update((p) => p + 1);
    this.reload();
  }

  prevPage(): void {
    this.page.update((p) => Math.max(1, p - 1));
    this.reload();
  }

  onFileGridReady(e: { api: GridApi }): void {
    this.fileGridApi = e.api;
    this.applyVisible(this.fileGridApi, this.fileVisibleCols);
  }

  onDocGridReady(e: { api: GridApi }): void {
    this.docGridApi = e.api;
    this.applyVisible(this.docGridApi, this.docVisibleCols);
  }

  onFileColsChange(): void {
    localStorage.setItem(FILE_COLS_KEY, JSON.stringify(this.fileVisibleCols));
    this.applyVisible(this.fileGridApi, this.fileVisibleCols);
  }

  onDocColsChange(): void {
    localStorage.setItem(DOC_COLS_KEY, JSON.stringify(this.docVisibleCols));
    this.applyVisible(this.docGridApi, this.docVisibleCols);
  }

  onFileCellClicked(event: CellClickedEvent<AdminFileListItem>): void {
    if (this.isResetClick(event)) {
      const row = event.data;
      if (!row) return;
      this.resetFile(row);
      return;
    }
    if (event.column?.getColId() === 'actions') return;
    const id = event.data?.id;
    if (id) void this.router.navigate(['/ops/files', id]);
  }

  onDocCellClicked(event: CellClickedEvent<AdminDocumentListItem>): void {
    if (this.isResetClick(event)) {
      const row = event.data;
      if (!row) return;
      this.resetDocument(row);
      return;
    }
    if (event.column?.getColId() === 'actions') return;
    const id = event.data?.id;
    if (id) void this.router.navigate(['/ops/documents', id]);
  }

  private resetFile(row: AdminFileListItem): void {
    const name = row.originalFileName ?? row.id;
    if (
      !confirm(
        `Reset file "${name}"?\n\nReuses the same file entry: clears prior documents, sets status to received, and re-runs the pipeline. Webhooks will fire again when docs complete.`,
      )
    ) {
      return;
    }
    if (this.resettingIds().has(row.id)) return;
    this.setResetting(row.id, true);
    this.error.set(null);
    this.info.set(null);
    this.api.resetFile(row.id).subscribe({
      next: (res) => {
        this.setResetting(row.id, false);
        this.info.set(
          `Reset queued for ${res.fileId}` +
            (res.softDeletedDocumentCount
              ? ` (cleared ${res.softDeletedDocumentCount} prior doc${res.softDeletedDocumentCount === 1 ? '' : 's'})`
              : ''),
        );
        this.reload();
      },
      error: (err: { error?: { error?: string }; message?: string }) => {
        this.setResetting(row.id, false);
        this.error.set(err?.error?.error ?? err?.message ?? 'Reset failed.');
      },
    });
  }

  private resetDocument(row: AdminDocumentListItem): void {
    const name = row.originalFileName ?? row.fileId;
    if (
      !confirm(
        `Reset parent file for document "${name}"?\n\nResets the same file entry (whole PDF), not just this document. Prior docs are cleared; pipeline re-runs; webhooks fire again on completion.`,
      )
    ) {
      return;
    }
    if (this.resettingIds().has(row.id)) return;
    this.setResetting(row.id, true);
    this.error.set(null);
    this.info.set(null);
    this.api.resetDocument(row.id).subscribe({
      next: (res) => {
        this.setResetting(row.id, false);
        this.info.set(`Reset queued for file ${res.fileId}`);
        this.reload();
      },
      error: (err: { error?: { error?: string }; message?: string }) => {
        this.setResetting(row.id, false);
        this.error.set(err?.error?.error ?? err?.message ?? 'Reset failed.');
      },
    });
  }

  private setResetting(id: string, busy: boolean): void {
    const next = new Set(this.resettingIds());
    if (busy) next.add(id);
    else next.delete(id);
    this.resettingIds.set(next);
    this.fileGridApi?.refreshCells({ force: true, columns: ['actions'] });
    this.docGridApi?.refreshCells({ force: true, columns: ['actions'] });
  }

  private isResetClick(event: CellClickedEvent): boolean {
    if (event.colDef.colId !== 'actions') return false;
    const target = event.event?.target as HTMLElement | null;
    return !!target?.closest('[data-action="reset"]');
  }

  private reload(): void {
    this.loading.set(true);
    this.error.set(null);
    const common = {
      page: this.page(),
      pageSize: this.pageSize,
      businessIds: this.businessId ? [this.businessId] : undefined,
      status: this.status || undefined,
      stage: this.stage || undefined,
      createdFrom: this.createdFrom || undefined,
      createdTo: this.createdTo || undefined,
    };

    if (this.activeTab === '1' || this.activeTab === 1) {
      this.api
        .listDocuments({
          ...common,
          fileName: this.name || undefined,
          webhookStatus: this.webhookStatus || undefined,
        })
        .subscribe({
          next: (res) => {
            this.docRows.set(res.items);
            this.docTotal.set(res.totalCount);
            this.loading.set(false);
          },
          error: () => {
            this.error.set('Failed to load documents.');
            this.loading.set(false);
          },
        });
    } else {
      this.api
        .listFiles({
          ...common,
          name: this.name || undefined,
        })
        .subscribe({
          next: (res) => {
            this.fileRows.set(res.items);
            this.fileTotal.set(res.totalCount);
            this.loading.set(false);
          },
          error: () => {
            this.error.set('Failed to load files.');
            this.loading.set(false);
          },
        });
    }
  }

  private loadCols(key: string, defs: ColDef[]): string[] {
    try {
      const raw = localStorage.getItem(key);
      if (raw) {
        const parsed = JSON.parse(raw) as string[];
        if (Array.isArray(parsed) && parsed.length) return parsed;
      }
    } catch {
      /* ignore */
    }
    return defs.filter((d) => !d.hide).map((d) => String(d.colId ?? d.field));
  }

  private applyVisible(api: GridApi | null, visible: string[]): void {
    if (!api) return;
    const set = new Set(visible);
    set.add('actions');
    const allIds = [
      ...this.fileColumnDefs.map((d) => String(d.colId ?? d.field)),
      ...this.docColumnDefs.map((d) => String(d.colId ?? d.field)),
    ];
    api.applyColumnState({
      state: allIds.map((colId) => ({ colId, hide: !set.has(colId) })),
    });
  }

  private resetCell(id?: string): string {
    if (!id) return '';
    const busy = this.resettingIds().has(id);
    const label = busy ? '…' : 'Reset';
    const disabled = busy ? ' disabled' : '';
    return `<button type="button" class="ops-reset-btn" data-action="reset"${disabled}>${label}</button>`;
  }

  private orgCell(business?: string, tenant?: string): string {
    const b = business ?? '—';
    const t = tenant ?? '';
    return `<div class="org-cell"><div class="org-primary">${escapeHtml(b)}</div><div class="org-secondary">${escapeHtml(t)}</div></div>`;
  }

  private webhookCell(d?: AdminDocumentListItem): string {
    if (!d) return '';
    const status = d.webhookStatusKey ?? '—';
    const line = [
      d.webhookLastAt ? this.fmt(d.webhookLastAt) : '—',
      d.webhookLastHttpStatus != null ? `HTTP ${d.webhookLastHttpStatus}` : null,
      `${d.webhookAttempts} attempts`,
    ]
      .filter(Boolean)
      .join(' · ');
    return `<div class="wh-cell"><span class="wh-pill">${escapeHtml(status)}</span><div class="wh-line">${escapeHtml(line)}</div></div>`;
  }

  private fmt(v: string): string {
    try {
      return new Date(v).toISOString().replace('T', ' ').slice(0, 19) + 'Z';
    } catch {
      return v;
    }
  }

  private fmtBytes(n: number): string {
    if (n < 1024) return `${n} B`;
    if (n < 1024 * 1024) return `${(n / 1024).toFixed(1)} KB`;
    return `${(n / (1024 * 1024)).toFixed(1)} MB`;
  }
}

function escapeHtml(s: string): string {
  return s
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;');
}
