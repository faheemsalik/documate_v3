import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { AdminApiService, type AdminSupportHit } from '../../../core/admin-api.service';

@Component({
  selector: 'app-support-page',
  imports: [FormsModule, Button, InputText, Message],
  template: `
    <div class="page">
      <header>
        <h1>Support workspace</h1>
        <p class="meta">INT-11 · lookup tenant, business, file, or document</p>
      </header>
      <div class="search">
        <input pInputText [(ngModel)]="q" placeholder="Name, Iden id, or Guid…" (keyup.enter)="lookup()" class="grow" />
        <p-button label="Lookup" (onClick)="lookup()" />
      </div>
      @if (error()) { <p-message severity="error" [text]="error()!" /> }
      @if (searched() && hits().length === 0) {
        <p class="empty">No matches.</p>
      }
      <div class="hits">
        @for (h of hits(); track h.kind + h.id) {
          <button type="button" class="hit" (click)="open(h)">
            <span class="kind">{{ h.kind }}</span>
            <div>
              <div class="label">{{ h.label }}</div>
              <div class="sec mono">{{ h.secondary }} · {{ h.id }}</div>
            </div>
          </button>
        }
      </div>
    </div>
  `,
  styles: `
    .page { display: flex; flex-direction: column; gap: 0.85rem; max-width: 40rem; }
    h1 { margin: 0; font-size: 1.25rem; }
    .meta { margin: 0.2rem 0 0; color: var(--p-text-muted-color); font-family: ui-monospace, monospace; font-size: 0.75rem; }
    .search { display: flex; gap: 0.5rem; }
    .grow { flex: 1; }
    .hits { display: flex; flex-direction: column; gap: 0.4rem; }
    .hit { display: flex; gap: 0.75rem; align-items: flex-start; padding: 0.65rem 0.75rem; border: 1px solid var(--p-content-border-color); border-radius: 8px; text-align: left; cursor: pointer; color: inherit; background: var(--p-content-background); width: 100%; }
    .hit:hover { background: var(--p-content-hover-background); }
    .kind { font-size: 0.7rem; text-transform: uppercase; letter-spacing: 0.04em; color: var(--p-primary-color); font-weight: 650; min-width: 4.5rem; padding-top: 0.15rem; }
    .label { font-weight: 600; }
    .sec { color: var(--p-text-muted-color); font-size: 0.75rem; margin-top: 0.15rem; }
    .mono { font-family: ui-monospace, monospace; }
    .empty { color: var(--p-text-muted-color); }
  `,
})
export class SupportPage {
  private readonly api = inject(AdminApiService);
  private readonly router = inject(Router);
  readonly hits = signal<AdminSupportHit[]>([]);
  readonly error = signal<string | null>(null);
  readonly searched = signal(false);
  q = '';

  lookup(): void {
    this.error.set(null);
    this.searched.set(true);
    this.api.supportLookup(this.q.trim()).subscribe({
      next: (res) => this.hits.set(res.hits),
      error: () => this.error.set('Lookup failed.'),
    });
  }

  open(h: AdminSupportHit): void {
    const link = h.deepLink ?? '/support';
    const [path, qs] = link.split('?');
    const queryParams: Record<string, string> = {};
    if (qs) {
      for (const part of qs.split('&')) {
        const [k, v] = part.split('=');
        if (k) queryParams[k] = decodeURIComponent(v ?? '');
      }
    }
    void this.router.navigateByUrl(path + (qs ? `?${qs}` : ''));
  }
}
