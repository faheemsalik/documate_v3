import { Component } from '@angular/core';
import type { ICellRendererAngularComp } from 'ag-grid-angular';
import type { ICellRendererParams } from 'ag-grid-community';
import { StatusPillComponent } from './status-pill.component';

@Component({
  selector: 'app-file-status-cell',
  imports: [StatusPillComponent],
  template: `<app-status-pill [status]="status" />`,
})
export class FileStatusCellComponent implements ICellRendererAngularComp {
  status = '';

  agInit(params: ICellRendererParams): void {
    this.status = String(params.value ?? '');
  }

  refresh(params: ICellRendererParams): boolean {
    this.status = String(params.value ?? '');
    return true;
  }
}
