import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { apiBaseUrl } from '../api-base';

export interface PublicEventToggle {
  eventKey: string;
  enabled: boolean;
}

export interface WebhookActionSettings {
  enabled: boolean;
  url?: string | null;
  hasSecret: boolean;
  events: PublicEventToggle[];
}

export interface EmailActionSettings {
  enabled: boolean;
  audience: string;
  recipients: string[];
  events: PublicEventToggle[];
}

export interface InAppActionSettings {
  enabled: boolean;
  events: PublicEventToggle[];
}

export interface PublicActionsSettings {
  webhook: WebhookActionSettings;
  email: EmailActionSettings;
  inApp: InAppActionSettings;
}

export interface QueuePublicActionsSettings {
  inherit: boolean;
  effective: PublicActionsSettings;
  override?: PublicActionsSettings | null;
}

export interface InAppNotification {
  id: string;
  eventName: string;
  eventId: string;
  title: string;
  body: string;
  createdAt: string;
  readAt?: string | null;
}

@Injectable({ providedIn: 'root' })
export class PublicActionsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${apiBaseUrl()}/api/app`;

  getBusiness() {
    return this.http.get<PublicActionsSettings>(`${this.base}/business/public-actions`);
  }

  putBusiness(body: unknown) {
    return this.http.put<PublicActionsSettings>(`${this.base}/business/public-actions`, body);
  }

  getQueue(queueId: string) {
    return this.http.get<QueuePublicActionsSettings>(`${this.base}/queues/${queueId}/public-actions`);
  }

  putQueue(queueId: string, body: unknown) {
    return this.http.put<QueuePublicActionsSettings>(`${this.base}/queues/${queueId}/public-actions`, body);
  }

  listInApp(unreadOnly = false) {
    return this.http.get<InAppNotification[]>(`${this.base}/notifications/in-app`, {
      params: { unreadOnly: String(unreadOnly) },
    });
  }

  markRead(id: string) {
    return this.http.post(`${this.base}/notifications/in-app/${id}/read`, {});
  }
}
