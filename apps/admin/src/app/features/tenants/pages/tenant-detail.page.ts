import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Message } from 'primeng/message';
import { AdminApiService, type AdminTenantDetail } from '../../../core/admin-api.service';

@Component({
  selector: 'app-tenant-detail-page',
  imports: [Message, RouterLink],
  template: `
    <div class="page">
      <a routerLink="/tenants">← Tenants</a>
      @if (error()) {
        <p-message severity="error" [text]="error()!" />
      }
      @if (tenant(); as t) {
        <header>
          <h1>{{ t.name }}</h1>
          <p class="meta">INT-03 · {{ t.idenTenantId }} · {{ t.providerModeKey }}</p>
        </header>
        <h2>Businesses</h2>
        @if (t.businesses.length === 0) {
          <p class="empty">No businesses under this tenant.</p>
        } @else {
          <table class="table">
            <thead>
              <tr><th>Name</th><th>IdenBusinessId</th><th>Slug</th><th>Active</th></tr>
            </thead>
            <tbody>
              @for (b of t.businesses; track b.id) {
                <tr>
                  <td><a [routerLink]="['/businesses', b.idenBusinessId]">{{ b.name }}</a></td>
                  <td class="mono">{{ b.idenBusinessId }}</td>
                  <td>{{ b.intakeEmailSlug ?? '—' }}</td>
                  <td>{{ b.isActive ? 'yes' : 'no' }}</td>
                </tr>
              }
            </tbody>
          </table>
        }
      }
    </div>
  `,
  styles: `
    .page { display: flex; flex-direction: column; gap: 0.75rem; }
    h1 { margin: 0; font-size: 1.25rem; }
    h2 { margin: 0.5rem 0 0; font-size: 0.95rem; }
    .meta { margin: 0.2rem 0 0; color: var(--p-text-muted-color); font-family: ui-monospace, monospace; font-size: 0.75rem; }
    .table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
    .table th, .table td { text-align: left; padding: 0.45rem 0.35rem; border-bottom: 1px solid var(--p-content-border-color); }
    .mono { font-family: ui-monospace, monospace; font-size: 0.8rem; }
    .empty { color: var(--p-text-muted-color); }
    a { color: var(--p-primary-color); text-decoration: none; }
  `,
})
export class TenantDetailPage implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  readonly tenant = signal<AdminTenantDetail | null>(null);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Missing tenant id.');
      return;
    }
    this.api.getTenant(id).subscribe({
      next: (t) => this.tenant.set(t),
      error: () => this.error.set('Tenant not found.'),
    });
  }
}
