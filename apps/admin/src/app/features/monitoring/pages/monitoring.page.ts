import { Component, OnInit, inject, signal } from '@angular/core';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { AdminApiService, type AdminMonitoringSnapshot } from '../../../core/admin-api.service';
import { AppConfigService } from '../../../core/app-config.service';

@Component({
  selector: 'app-monitoring-page',
  imports: [Button, Message],
  template: `
    <div class="page">
      <header class="page-head">
        <div>
          <h1>API monitoring</h1>
          <p class="meta">INT-09 · health + deep links</p>
        </div>
        <p-button label="Refresh" icon="pi pi-refresh" [outlined]="true" (onClick)="reload()" />
      </header>
      @if (error()) { <p-message severity="error" [text]="error()!" /> }
      @if (snap(); as s) {
        <div class="status-card" [class.ok]="s.healthStatus === 'Healthy'" [class.bad]="s.healthStatus !== 'Healthy'">
          <div class="lbl">Overall</div>
          <div class="val">{{ s.healthStatus }}</div>
        </div>
        <div class="checks">
          @for (c of s.checks; track c.name) {
            <div class="check">
              <div class="name">{{ c.name }}</div>
              <div class="st">{{ c.status }}</div>
              @if (c.description) { <div class="desc">{{ c.description }}</div> }
            </div>
          } @empty {
            <p class="empty">No health check entries.</p>
          }
        </div>
        <div class="links">
          @if (s.hangfireDashboardUrl) {
            <a class="link" [href]="abs(s.hangfireDashboardUrl)" target="_blank" rel="noopener">Hangfire dashboard ↗</a>
          }
          @if (s.datadogDashboardUrl) {
            <a class="link" [href]="s.datadogDashboardUrl" target="_blank" rel="noopener">Datadog dashboard ↗</a>
          } @else {
            <span class="empty">Datadog URL not configured (Admin:DatadogDashboardUrl).</span>
          }
        </div>
        <p class="note">Request-log table deferred — use Hangfire/Datadog for deep diagnostics.</p>
      }
    </div>
  `,
  styles: `
    .page { display: flex; flex-direction: column; gap: 0.85rem; max-width: 40rem; }
    .page-head { display: flex; justify-content: space-between; }
    h1 { margin: 0; font-size: 1.25rem; }
    .meta { margin: 0.2rem 0 0; color: var(--p-text-muted-color); font-family: ui-monospace, monospace; font-size: 0.75rem; }
    .status-card { border-radius: 8px; padding: 0.85rem 1rem; border: 1px solid var(--p-content-border-color); }
    .status-card.ok { border-color: #3a9a5a; background: color-mix(in srgb, #3a9a5a 12%, transparent); }
    .status-card.bad { border-color: #c44; background: color-mix(in srgb, #c44 12%, transparent); }
    .lbl { font-size: 0.7rem; text-transform: uppercase; color: var(--p-text-muted-color); }
    .val { font-size: 1.35rem; font-weight: 700; }
    .checks { display: flex; flex-direction: column; gap: 0.4rem; }
    .check { padding: 0.55rem 0.7rem; border: 1px solid var(--p-content-border-color); border-radius: 6px; }
    .name { font-weight: 600; font-size: 0.85rem; }
    .st { font-family: ui-monospace, monospace; font-size: 0.8rem; }
    .desc { color: var(--p-text-muted-color); font-size: 0.75rem; }
    .links { display: flex; flex-direction: column; gap: 0.4rem; }
    .link { color: var(--p-primary-color); }
    .note, .empty { color: var(--p-text-muted-color); font-size: 0.8rem; }
  `,
})
export class MonitoringPage implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly config = inject(AppConfigService);
  readonly snap = signal<AdminMonitoringSnapshot | null>(null);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.error.set(null);
    this.api.monitoringSnapshot().subscribe({
      next: (s) => this.snap.set(s),
      error: () => this.error.set('Failed to load monitoring snapshot.'),
    });
  }

  abs(path: string): string {
    if (path.startsWith('http')) return path;
    return `${this.config.apiBaseUrl}${path.startsWith('/') ? '' : '/'}${path}`;
  }
}
