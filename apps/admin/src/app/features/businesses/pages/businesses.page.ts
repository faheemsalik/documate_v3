import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { AdminApiService, type AdminBusinessListItem } from '../../../core/admin-api.service';

@Component({
  selector: 'app-businesses-page',
  imports: [FormsModule, Button, InputText, Message, RouterLink],
  template: `
    <div class="page">
      <header class="page-head">
        <div>
          <h1>Businesses</h1>
          <p class="meta">INT-04 · {{ total() }} total</p>
        </div>
        <div class="actions">
          <input pInputText [(ngModel)]="search" placeholder="Search" (keyup.enter)="reload()" />
          <p-button label="Search" [outlined]="true" (onClick)="reload()" />
        </div>
      </header>
      @if (error()) { <p-message severity="error" [text]="error()!" /> }
      @if (!loading() && rows().length === 0) {
        <p class="empty">No businesses found.</p>
      } @else {
        <table class="table">
          <thead>
            <tr><th>Business</th><th>Tenant</th><th>Slug</th><th>Active</th></tr>
          </thead>
          <tbody>
            @for (b of rows(); track b.id) {
              <tr>
                <td>
                  <a [routerLink]="['/businesses', b.idenBusinessId]">{{ b.name }}</a>
                  <div class="sec mono">{{ b.idenBusinessId }}</div>
                </td>
                <td>
                  <div>{{ b.tenantName }}</div>
                  <div class="sec mono">{{ b.idenTenantId }}</div>
                </td>
                <td>{{ b.intakeEmailSlug ?? '—' }}</td>
                <td>{{ b.isActive ? 'yes' : 'no' }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `,
  styles: `
    .page { display: flex; flex-direction: column; gap: 0.85rem; }
    .page-head { display: flex; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
    h1 { margin: 0; font-size: 1.25rem; }
    .meta { margin: 0.2rem 0 0; color: var(--p-text-muted-color); font-family: ui-monospace, monospace; font-size: 0.75rem; }
    .actions { display: flex; gap: 0.5rem; }
    .table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
    .table th, .table td { text-align: left; padding: 0.5rem 0.4rem; border-bottom: 1px solid var(--p-content-border-color); vertical-align: top; }
    .sec { color: var(--p-text-muted-color); font-size: 0.72rem; margin-top: 0.15rem; }
    .mono { font-family: ui-monospace, monospace; }
    .empty { color: var(--p-text-muted-color); }
    a { color: var(--p-primary-color); text-decoration: none; }
  `,
})
export class BusinessesPage implements OnInit {
  private readonly api = inject(AdminApiService);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly rows = signal<AdminBusinessListItem[]>([]);
  readonly total = signal(0);
  search = '';

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listBusinesses({ search: this.search || undefined, pageSize: 100 }).subscribe({
      next: (res) => {
        this.rows.set(res.items);
        this.total.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load businesses.');
        this.loading.set(false);
      },
    });
  }
}
