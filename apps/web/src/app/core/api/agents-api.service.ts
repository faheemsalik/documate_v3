import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { apiBaseUrl } from '../api-base';

export interface Agent {
  id: string;
  name: string;
  description?: string | null;
  documentTypeId: number;
  documentTypeKey?: string | null;
  outputSchemaJson: string;
  schemaVersion: number;
  instructions: string;
  sourceTemplateId?: number | null;
  defaultWorkflowId?: number | null;
  defaultProviderId?: number | null;
  isActive: boolean;
}

export interface CreateAgentBody {
  name: string;
  description?: string | null;
  documentTypeId: number;
  outputSchemaJson: string;
  instructions: string;
  defaultWorkflowId?: number | null;
  defaultProviderId?: number | null;
  schemaVersion?: number;
}

export interface UpdateAgentBody extends CreateAgentBody {
  schemaVersion: number;
  isActive: boolean;
}

export interface CloneFromTemplateBody {
  agentTemplateKey: string;
  name?: string | null;
  description?: string | null;
}

@Injectable({ providedIn: 'root' })
export class AgentsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${apiBaseUrl()}/api/app/agents`;

  list() {
    return this.http.get<Agent[]>(this.base);
  }

  get(id: string) {
    return this.http.get<Agent>(`${this.base}/${id}`);
  }

  create(body: CreateAgentBody) {
    return this.http.post<Agent>(this.base, body);
  }

  update(id: string, body: UpdateAgentBody) {
    return this.http.put<Agent>(`${this.base}/${id}`, body);
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  cloneFromTemplate(body: CloneFromTemplateBody) {
    return this.http.post<Agent>(`${this.base}/clone-from-template`, body);
  }
}
