import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface MeResponse {
  userId: string;
  tenantId: string;
  businessId: string;
  tenantName?: string | null;
  businessName?: string | null;
}

@Injectable({ providedIn: 'root' })
export class MeApiService {
  private readonly http = inject(HttpClient);

  /** Relative URL — configure proxy later; absolute for local API default. */
  getMe() {
    return this.http.get<MeResponse>('http://localhost:5172/api/app/me');
  }
}
