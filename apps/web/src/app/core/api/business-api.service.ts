import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { apiBaseUrl } from '../api-base';

export interface BusinessProfile {
  businessId: string;
  tenantId: string;
  tenantName: string;
  name: string;
  isActive: boolean;
}

export interface BusinessListItem {
  businessId: string;
  name: string;
  tenantName: string;
  isActive: boolean;
  isCurrent: boolean;
}

@Injectable({ providedIn: 'root' })
export class BusinessApiService {
  private readonly http = inject(HttpClient);
  private readonly profileBase = `${apiBaseUrl()}/api/app/business/profile`;
  private readonly listBase = `${apiBaseUrl()}/api/app/businesses`;

  get() {
    return this.http.get<BusinessProfile>(this.profileBase);
  }

  update(name: string) {
    return this.http.put<BusinessProfile>(this.profileBase, { name });
  }

  list() {
    return this.http.get<BusinessListItem[]>(this.listBase);
  }

  create(name: string) {
    return this.http.post<BusinessListItem>(this.listBase, { name });
  }
}
