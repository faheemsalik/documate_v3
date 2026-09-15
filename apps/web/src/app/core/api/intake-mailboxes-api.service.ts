import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { apiBaseUrl } from '../api-base';

export interface IntakeMailbox {
  id: string;
  queueId: string;
  kindKey: string;
  agentId?: string | null;
  agentName?: string | null;
  enabled: boolean;
  emailAddress: string;
  emailLocalPart: string;
  emailDomain: string;
  emailAddressVersion: number;
  allowlistModeEnumId: number;
  allowlistModeKey?: string | null;
}

export interface MailboxAllowlistEntry {
  id: number;
  matchTypeEnumId: number;
  matchTypeKey?: string | null;
  value: string;
}

export interface IntakeMailboxDetail {
  mailbox: IntakeMailbox;
  allowlist: MailboxAllowlistEntry[];
}

export interface SimulateEmailResult {
  accepted: boolean;
  rejectCode?: string | null;
  rejectMessage?: string | null;
  intakeRejectionId?: string | null;
  batchId?: string | null;
  fileIds: string[];
}

@Injectable({ providedIn: 'root' })
export class IntakeMailboxesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${apiBaseUrl()}/api/app/intake-mailboxes`;

  list() {
    return this.http.get<IntakeMailbox[]>(this.base);
  }

  get(id: string) {
    return this.http.get<IntakeMailboxDetail>(`${this.base}/${id}`);
  }

  createTyped(agentId: string) {
    return this.http.post<IntakeMailbox>(`${this.base}/typed`, { agentId });
  }

  createMulti() {
    return this.http.post<IntakeMailbox>(`${this.base}/multi`, {});
  }

  rotate(id: string) {
    return this.http.post<IntakeMailbox>(`${this.base}/${id}/rotate`, {});
  }

  update(id: string, body: { enabled: boolean; allowlistModeEnumId: number }) {
    return this.http.put<IntakeMailbox>(`${this.base}/${id}`, body);
  }

  listAllowlist(id: string) {
    return this.http.get<MailboxAllowlistEntry[]>(`${this.base}/${id}/allowlist`);
  }

  addAllowlist(id: string, body: { matchTypeKey: string; value: string }) {
    return this.http.post<MailboxAllowlistEntry>(`${this.base}/${id}/allowlist`, body);
  }

  deleteAllowlist(id: string, entryId: number) {
    return this.http.delete(`${this.base}/${id}/allowlist/${entryId}`);
  }

  simulate(
    id: string,
    body: {
      from?: string;
      subject?: string;
      messageId?: string;
      textBody?: string;
      attachments?: { fileName: string; contentType?: string; contentBase64: string }[];
    },
  ) {
    return this.http.post<SimulateEmailResult>(`${this.base}/${id}/simulate`, body);
  }
}
