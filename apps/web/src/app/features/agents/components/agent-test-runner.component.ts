import { Component, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { ProgressSpinner } from 'primeng/progressspinner';
import { AppContextService } from '../../../core/app-context.service';
import { FilesApiService } from '../../files/data/files-api.service';
import { StatusPillComponent } from '../../files/components/status-pill.component';
import type { Agent } from '../../../core/api/agents-api.service';

@Component({
  selector: 'app-agent-test-runner',
  imports: [RouterLink, Button, Message, ProgressSpinner, StatusPillComponent],
  templateUrl: './agent-test-runner.component.html',
  styleUrl: './agent-test-runner.component.scss',
})
export class AgentTestRunnerComponent {
  private readonly filesApi = inject(FilesApiService);
  readonly ctx = inject(AppContextService);

  readonly agent = input.required<Agent>();

  readonly uploading = signal(false);
  readonly polling = signal(false);
  readonly error = signal<string | null>(null);
  readonly fileId = signal<string | null>(null);
  readonly statusKey = signal<string | null>(null);
  readonly pollCount = signal(0);

  triggerUpload(): void {
    document.getElementById('agent-test-file')?.click();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    const queueId = this.ctx.defaultQueueId();
    const docType = this.agent().documentTypeKey;
    if (!queueId) {
      this.error.set('No default queue configured.');
      return;
    }
    if (!docType) {
      this.error.set('Agent has no document type — required for route mapping.');
      return;
    }

    this.uploading.set(true);
    this.error.set(null);
    this.fileId.set(null);
    this.statusKey.set(null);
    this.filesApi.uploadFile(queueId, file, docType).subscribe({
      next: (created) => {
        this.uploading.set(false);
        this.fileId.set(created.id);
        this.statusKey.set(created.publicStatusKey ?? null);
        this.pollFile(queueId, created.id, 0);
      },
      error: () => {
        this.uploading.set(false);
        this.error.set('Upload failed.');
      },
    });
  }

  private pollFile(queueId: string, fileId: string, attempt: number): void {
    const terminal = ['ready', 'failed', 'partial_ready'];
    if (attempt >= 40) {
      this.polling.set(false);
      this.error.set('Polling timed out.');
      return;
    }

    this.polling.set(true);
    this.pollCount.set(attempt + 1);
    this.filesApi.getFile(queueId, fileId).subscribe({
      next: (file) => {
        this.statusKey.set(file.publicStatusKey ?? null);
        if (file.publicStatusKey && terminal.includes(file.publicStatusKey)) {
          this.polling.set(false);
          return;
        }
        setTimeout(() => this.pollFile(queueId, fileId, attempt + 1), 2000);
      },
      error: () => {
        this.polling.set(false);
        this.error.set('Poll failed.');
      },
    });
  }
}
