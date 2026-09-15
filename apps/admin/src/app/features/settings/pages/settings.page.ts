import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Textarea } from 'primeng/textarea';
import { Message } from 'primeng/message';
import { AdminApiService, type SystemSetting } from '../../../core/admin-api.service';

@Component({
  selector: 'app-settings-page',
  imports: [FormsModule, Button, Textarea, Message],
  template: `
    <div class="page">
      <header class="page-head">
        <div>
          <h1>System settings</h1>
          <p class="meta">INT-08 · Plan 15 allowlisted keys</p>
        </div>
        <p-button label="Reload" icon="pi pi-refresh" [outlined]="true" (onClick)="reload()" />
      </header>
      @if (error()) { <p-message severity="error" [text]="error()!" /> }
      @if (ok()) { <p-message severity="success" [text]="ok()!" /> }
      @if (settings().length === 0 && !loading()) {
        <p class="empty">No settings returned.</p>
      }
      <div class="list">
        @for (s of settings(); track s.key) {
          <div class="row">
            <div class="key mono">{{ s.key }}</div>
            <textarea pTextarea [(ngModel)]="edits[s.key]" rows="2" class="val"></textarea>
            <p-button label="Save" size="small" (onClick)="save(s.key)" [loading]="savingKey() === s.key" />
          </div>
        }
      </div>
    </div>
  `,
  styles: `
    .page { display: flex; flex-direction: column; gap: 0.85rem; }
    .page-head { display: flex; justify-content: space-between; }
    h1 { margin: 0; font-size: 1.25rem; }
    .meta { margin: 0.2rem 0 0; color: var(--p-text-muted-color); font-family: ui-monospace, monospace; font-size: 0.75rem; }
    .list { display: flex; flex-direction: column; gap: 0.65rem; }
    .row { display: grid; grid-template-columns: minmax(12rem, 18rem) 1fr auto; gap: 0.6rem; align-items: start; padding: 0.65rem; border: 1px solid var(--p-content-border-color); border-radius: 8px; background: var(--p-content-background); }
    .key { font-size: 0.78rem; word-break: break-all; padding-top: 0.35rem; }
    .val { width: 100%; font-family: ui-monospace, monospace; font-size: 0.8rem; }
    .mono { font-family: ui-monospace, monospace; }
    .empty { color: var(--p-text-muted-color); }
    @media (max-width: 800px) { .row { grid-template-columns: 1fr; } }
  `,
})
export class SettingsPage implements OnInit {
  private readonly api = inject(AdminApiService);
  readonly settings = signal<SystemSetting[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);
  readonly savingKey = signal<string | null>(null);
  edits: Record<string, string> = {};

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.ok.set(null);
    this.api.listSettings().subscribe({
      next: (list) => {
        this.settings.set(list);
        this.edits = Object.fromEntries(list.map((s) => [s.key, s.valueJson]));
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load settings.');
        this.loading.set(false);
      },
    });
  }

  save(key: string): void {
    const valueJson = this.edits[key] ?? 'null';
    this.savingKey.set(key);
    this.error.set(null);
    this.ok.set(null);
    this.api.putSetting(key, valueJson).subscribe({
      next: () => {
        this.savingKey.set(null);
        this.ok.set(`Saved ${key}`);
      },
      error: (err) => {
        this.savingKey.set(null);
        this.error.set(err?.error?.error ?? `Failed to save ${key}`);
      },
    });
  }
}
