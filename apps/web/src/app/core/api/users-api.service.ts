import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { apiBaseUrl } from '../api-base';

export interface AppUser {
  memberId: string;
  identityId: string;
  email: string;
  displayName: string;
  roleSysKey?: string | null;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${apiBaseUrl()}/api/app/users`;

  list() {
    return this.http.get<AppUser[]>(this.base);
  }

  invite(body: { email: string; displayName?: string; roleSysKey?: string }) {
    return this.http.post<AppUser>(this.base, body);
  }

  assignRole(memberId: string, roleSysKey: string) {
    return this.http.post<void>(`${this.base}/${encodeURIComponent(memberId)}/role`, { roleSysKey });
  }

  deactivate(memberId: string) {
    return this.http.post<void>(`${this.base}/${encodeURIComponent(memberId)}/deactivate`, {});
  }
}
