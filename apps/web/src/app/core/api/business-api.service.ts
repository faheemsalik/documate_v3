import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { API_BASE_URL } from '../api-base';

export interface BusinessProfile {
  businessId: string;
  tenantId: string;
  tenantName: string;
  name: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class BusinessApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE_URL}/api/app/business/profile`;

  get() {
    return this.http.get<BusinessProfile>(this.base);
  }

  update(name: string) {
    return this.http.put<BusinessProfile>(this.base, { name });
  }
}
