import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { apiBaseUrl } from '../api-base';

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

export interface CatalogEnum {
  id: number;
  enumKey: string;
  displayName: string;
}

@Injectable({ providedIn: 'root' })
export class CatalogsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${apiBaseUrl()}/api/app/catalogs`;

  listDocumentTypes() {
    return this.http.get<DocumentType[]>(`${this.base}/document-types`);
  }

  listProviders(category?: string) {
    const params = category ? { category } : undefined;
    return this.http.get<Provider[]>(`${this.base}/providers`, { params });
  }

  listAgentTemplates() {
    return this.http.get<AgentTemplate[]>(`${this.base}/agent-templates`);
  }

  getAgentTemplate(key: string) {
    return this.http.get<AgentTemplate>(`${this.base}/agent-templates/${encodeURIComponent(key)}`);
  }

  listEnums(typeKey: string) {
    return this.http.get<CatalogEnum[]>(`${this.base}/enums/${encodeURIComponent(typeKey)}`);
  }
}
