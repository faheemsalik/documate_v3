import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { API_BASE_URL } from '../api-base';

export interface DocumentType {
  id: number;
  documentTypeKey: string;
  name: string;
  description?: string | null;
}

export interface Provider {
  id: number;
  providerKey: string;
  name: string;
  vendorHint?: string | null;
  categoryEnumId: number;
}

export interface AgentTemplate {
  id: number;
  agentTemplateKey: string;
  name: string;
  description?: string | null;
  documentTypeId: number;
  documentTypeKey: string;
  defaultSchemaJson: string;
  defaultInstructions: string;
  defaultProviderId?: number | null;
  version: number;
}

@Injectable({ providedIn: 'root' })
export class CatalogsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE_URL}/api/app/catalogs`;

  listDocumentTypes() {
    return this.http.get<DocumentType[]>(`${this.base}/document-types`);
  }

  listProviders() {
    return this.http.get<Provider[]>(`${this.base}/providers`);
  }

  listAgentTemplates() {
    return this.http.get<AgentTemplate[]>(`${this.base}/agent-templates`);
  }

  getAgentTemplate(key: string) {
    return this.http.get<AgentTemplate>(`${this.base}/agent-templates/${encodeURIComponent(key)}`);
  }
}
