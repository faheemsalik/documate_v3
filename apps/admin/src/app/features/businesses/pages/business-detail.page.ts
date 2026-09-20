import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Message } from 'primeng/message';
import { AdminApiService, type AdminBusinessDetail } from '../../../core/admin-api.service';

@Component({
  selector: 'app-business-detail-page',
  imports: [Message, RouterLink],
  template: `
    <div class="page">
      <a routerLink="/businesses">← Businesses</a>
      @if (error()) { <p-message severity="error" [text]="error()!" /> }
      @if (biz(); as b) {
        <header>
          <h1>{{ b.name }}</h1>
          <p class="meta">INT-05 · {{ b.idenBusinessId }}</p>
          <p class="sub">Tenant: <a [routerLink]="['/tenants', b.tenantId]">{{ b.tenantName }}</a> · {{ b.idenTenantId }}</p>
        </header>

        <div class="kpis">
          <div class="kpi"><div class="lbl">Files (7d)</div><div class="val">{{ b.recentFileStats.total }}</div></div>
          <div class="kpi"><div class="lbl">Failed (7d)</div><div class="val">{{ b.recentFileStats.failed }}</div></div>
          <div class="kpi"><div class="lbl">Last file</div><div class="val small">{{ b.recentFileStats.lastCreatedAt?.slice(0, 19) ?? '—' }}</div></div>
        </div>

        <p>
          <a [routerLink]="['/ops']" [queryParams]="{ businessId: b.idenBusinessId }">Open in Files &amp; documents →</a>
          ·
          <a [routerLink]="['/actions']" [queryParams]="{ businessId: b.idenBusinessId }">Action bindings →</a>
          ·
          <a [routerLink]="['/agents']" [queryParams]="{ businessId: b.idenBusinessId }">Agents →</a>
        </p>

        <div class="cols">
          <section>
            <h2>Queues</h2>
            @for (q of b.queues; track q.id) {
              <div class="row">{{ q.name }} @if (q.isDefault) { <span class="tag">default</span> }</div>
            } @empty { <p class="empty">No queues.</p> }
          </section>
          <section>
            <h2>Agents</h2>
            @for (a of b.agents; track a.id) {
              <div class="row">
                <a [routerLink]="['/agents', a.id]">{{ a.name }}</a>
                · {{ a.isActive ? 'active' : 'inactive' }}
              </div>
            } @empty { <p class="empty">No agents.</p> }
          </section>
        </div>
      }
    </div>
  `,
  styles: `
    .page { display: flex; flex-direction: column; gap: 0.75rem; }
    h1 { margin: 0; font-size: 1.25rem; }
    h2 { margin: 0 0 0.4rem; font-size: 0.95rem; }
    .meta { margin: 0.2rem 0 0; color: var(--p-text-muted-color); font-family: ui-monospace, monospace; font-size: 0.75rem; }
    .sub { margin: 0.35rem 0 0; font-size: 0.85rem; }
    .kpis { display: flex; gap: 0.6rem; flex-wrap: wrap; }
    .kpi { background: var(--p-content-background); border: 1px solid var(--p-content-border-color); border-radius: 8px; padding: 0.65rem 0.8rem; min-width: 7rem; }
    .lbl { font-size: 0.7rem; color: var(--p-text-muted-color); text-transform: uppercase; }
    .val { font-size: 1.15rem; font-weight: 650; }
    .val.small { font-size: 0.8rem; font-family: ui-monospace, monospace; }
    .cols { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .row { padding: 0.35rem 0; border-bottom: 1px solid var(--p-content-border-color); font-size: 0.85rem; }
    .tag { margin-left: 0.4rem; font-size: 0.7rem; background: var(--p-surface-200); padding: 0.1rem 0.35rem; border-radius: 4px; }
    .empty { color: var(--p-text-muted-color); font-size: 0.85rem; }
    a { color: var(--p-primary-color); text-decoration: none; }
    @media (max-width: 700px) { .cols { grid-template-columns: 1fr; } }
  `,
})
export class BusinessDetailPage implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  readonly biz = signal<AdminBusinessDetail | null>(null);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('businessId');
    if (!id) {
      this.error.set('Missing business id.');
      return;
    }
    this.api.getBusiness(id).subscribe({
      next: (b) => this.biz.set(b),
      error: () => this.error.set('Business not found.'),
    });
  }
}
