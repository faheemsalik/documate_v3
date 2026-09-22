import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AppConfigService } from './app-config.service';

export interface MeResponse {
  userId: string;
  tenantId: string;
  businessId: string;
  tenantName?: string | null;
  businessName?: string | null;
  defaultQueueId?: string | null;
  buContextId?: string | null;
  identityClass?: string | null;
}

@Injectable({ providedIn: 'root' })
export class MeApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(AppConfigService);

  getMe() {
    return this.http.get<MeResponse>(`${this.config.apiBaseUrl}/api/app/me`);
  }
}
