import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { ApiKeysApiService, type ApiKeyListItem, type CreatedApiKey } from '../../../core/api/api-keys-api.service';

@Component({
  selector: 'app-api-keys-page',
  imports: [FormsModule, Button, InputText, Message, TableModule, DatePipe],
  templateUrl: './api-keys.page.html',
  styleUrl: './api-keys.page.scss',
})
export class ApiKeysPage implements OnInit {
  private readonly apiKeysApi = inject(ApiKeysApiService);

  readonly loading = signal(true);
  readonly creating = signal(false);
  readonly error = signal<string | null>(null);
  readonly keys = signal<ApiKeyListItem[]>([]);
  readonly createdKey = signal<CreatedApiKey | null>(null);
  newKeyName = '';

  ngOnInit(): void {
    this.reload();
  }

  create(): void {
    if (!this.newKeyName.trim()) return;
    this.creating.set(true);
    this.apiKeysApi.create(this.newKeyName.trim()).subscribe({
      next: (created) => {
        this.createdKey.set(created);
        this.newKeyName = '';
        this.creating.set(false);
        this.reload();
      },
      error: () => {
        this.creating.set(false);
        this.error.set('Create failed.');
      },
    });
  }

  revoke(id: string): void {
    this.apiKeysApi.revoke(id).subscribe({
      next: () => this.reload(),
      error: () => this.error.set('Revoke failed.'),
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.apiKeysApi.list().subscribe({
      next: (items) => {
        this.keys.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load API keys.');
      },
    });
  }
}
