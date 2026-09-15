import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { apiBaseUrl } from '../api-base';

export interface Queue {
  id: string;
  name: string;
  description?: string | null;
  isDefault: boolean;
  isActive: boolean;
  routingLocked: boolean;
  routingLockedAt?: string | null;
  webhookEnabled: boolean;
  webhookUrl?: string | null;
  hasWebhookSecret: boolean;
  emailIntakeEnabled: boolean;
  emailAddress?: string | null;
  emailLocalPart?: string | null;
  emailDomain?: string | null;
  emailAddressVersion: number;
  allowlistModeEnumId: number;
  allowlistModeKey?: string | null;
  workflowModeEnumId: number;
  workflowModeKey?: string | null;
  workflowId?: number | null;
}

export interface QueueRoute {
  id: number;
  documentTypeId: number;
  documentTypeKey?: string | null;
  agentId: string;
  agentName?: string | null;
}

export interface AllowlistEntry {
  id: number;
  matchTypeEnumId: number;
  matchTypeKey?: string | null;
  value: string;
}

export interface QueueDetail {
  queue: Queue;
  routes: QueueRoute[];
  allowlist: AllowlistEntry[];
}

export interface QueueEmailAddress {
  emailAddress: string;
  localPart: string;
  domain: string;
  version: number;
}

@Injectable({ providedIn: 'root' })
export class QueuesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${apiBaseUrl()}/api/app/queues`;

  list() {
    return this.http.get<Queue[]>(this.base);
  }

  get(id: string) {
    return this.http.get<QueueDetail>(`${this.base}/${id}`);
  }

  listRoutes(id: string) {
    return this.http.get<QueueRoute[]>(`${this.base}/${id}/routes`);
  }

  replaceRoutes(id: string, routes: { documentTypeId: number; agentId: string }[]) {
    return this.http.put<QueueRoute[]>(`${this.base}/${id}/routes`, { routes });
  }

  lockRouting(id: string) {
    return this.http.post<Queue>(`${this.base}/${id}/routing/lock`, {});
  }

  unlockRouting(id: string) {
    return this.http.post<Queue>(`${this.base}/${id}/routing/unlock`, {});
  }

  updateWebhook(id: string, body: { enabled: boolean; url?: string | null; secret?: string | null }) {
    return this.http.put<Queue>(`${this.base}/${id}/webhook`, body);
  }

  mintEmail(id: string) {
    return this.http.post<QueueEmailAddress>(`${this.base}/${id}/email/mint`, {});
  }

  updateEmailSettings(id: string, body: { emailIntakeEnabled: boolean; allowlistModeEnumId: number }) {
    return this.http.put<Queue>(`${this.base}/${id}/email`, body);
  }

  listAllowlist(id: string) {
    return this.http.get<AllowlistEntry[]>(`${this.base}/${id}/allowlist`);
  }

  addAllowlist(id: string, body: { matchTypeKey: string; value: string }) {
    return this.http.post<AllowlistEntry>(`${this.base}/${id}/allowlist`, body);
  }

  deleteAllowlist(id: string, entryId: number) {
    return this.http.delete<void>(`${this.base}/${id}/allowlist/${entryId}`);
  }
}
