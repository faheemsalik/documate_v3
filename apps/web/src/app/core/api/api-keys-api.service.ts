import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { API_BASE_URL } from '../api-base';

export interface ApiKeyListItem {
  id: string;
  name: string;
  keyPrefix: string;
  isActive: boolean;
  expiresAt?: string | null;
  lastUsedAt?: string | null;
  createdAt: string;
}

export interface CreatedApiKey {
  id: string;
  name: string;
  keyPrefix: string;
  apiKey: string;
  expiresAt?: string | null;
}

@Injectable({ providedIn: 'root' })
export class ApiKeysApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE_URL}/api/app/api-keys`;

  list() {
    return this.http.get<ApiKeyListItem[]>(this.base);
  }

  create(name: string) {
    return this.http.post<CreatedApiKey>(this.base, { name });
  }

  revoke(id: string) {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
