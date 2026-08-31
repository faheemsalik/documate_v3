import { Injectable, computed, inject, signal } from '@angular/core';
import { MeApiService, type MeResponse } from './me-api.service';

@Injectable({ providedIn: 'root' })
export class AppContextService {
  private readonly meApi = inject(MeApiService);

  readonly me = signal<MeResponse | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);

  readonly defaultQueueId = computed(() => this.me()?.defaultQueueId ?? null);
  readonly businessName = computed(() => this.me()?.businessName ?? 'Business');

  constructor() {
    this.refreshMe();
  }

  refreshMe(): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.meApi.getMe().subscribe({
      next: (m) => {
        this.me.set(m);
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set('Could not load account context.');
        this.loading.set(false);
      },
    });
  }
}
