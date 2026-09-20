import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppConfigService } from './app-config.service';

export interface Paged<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AdminFileListItem {
  id: string;
  businessId: string;
  businessName: string;
  tenantId: string;
  tenantName: string;
  idenTenantId: string;
  queueId: string;
  queueName: string | null;
  originalFileName: string | null;
  publicStatusKey: string | null;
  internalStageKey: string | null;
  sourceKey: string | null;
  documentCount: number;
  sizeBytes: number;
  emailFrom: string | null;
  emailSubject: string | null;
  errorCode: string | null;
  errorMessage: string | null;
  isReprocess: boolean;
  isCancelled: boolean;
  createdAt: string;
  completedAt: string | null;
}

export interface AdminDocumentListItem {
  id: string;
  fileId: string;
  originalFileName: string | null;
  businessId: string;
  businessName: string;
  tenantId: string;
  tenantName: string;
  idenTenantId: string;
  queueId: string;
  documentTypeKey: string | null;
  agentName: string | null;
  publicStatusKey: string | null;
  internalStageKey: string | null;
  webhookStatusKey: string | null;
  webhookLastAt: string | null;
  webhookLastHttpStatus: number | null;
  webhookAttempts: number;
  hasResultJson: boolean;
  errorCode: string | null;
  errorMessage: string | null;
  isCancelled: boolean;
  createdAt: string;
  completedAt: string | null;
  fileCreatedAt: string;
}

export interface AdminResetResult {
  fileId: string;
  businessId: string;
  originalFileName: string | null;
  softDeletedDocumentCount: number;
}

export interface AdminFileDetail {
  id: string;
  businessId: string;
  businessName: string;
  tenantId: string;
  tenantName: string;
  queueId: string;
  queueName: string | null;
  originalFileName: string | null;
  contentType: string | null;
  sizeBytes: number;
  publicStatusKey: string | null;
  internalStageKey: string | null;
  sourceKey: string | null;
  documentCount: number;
  errorCode: string | null;
  errorMessage: string | null;
  isReprocess: boolean;
  isCancelled: boolean;
  createdAt: string;
  completedAt: string | null;
}

export interface AdminFileDocumentItem {
  id: string;
  fileId: string;
  documentTypeKey: string | null;
  publicStatusKey: string | null;
  internalStageKey: string | null;
  pageStart: number | null;
  pageEnd: number | null;
  createdAt: string;
  completedAt: string | null;
  hasExtractPrompt: boolean;
}

export interface AdminDocumentDetail {
  id: string;
  fileId: string;
  businessId: string;
  businessName: string;
  queueId: string;
  originalFileName: string | null;
  contentType: string | null;
  documentTypeKey: string | null;
  publicStatusKey: string | null;
  internalStageKey: string | null;
  pageStart: number | null;
  pageEnd: number | null;
  errorCode: string | null;
  errorMessage: string | null;
  resultJson: Record<string, unknown> | null;
  webhookStatusKey: string | null;
  webhookAttempts: number;
  createdAt: string;
  completedAt: string | null;
  extractSystemPrompt: string | null;
  extractUserPrompt: string | null;
  extractPromptCapturedAt: string | null;
}

export interface AdminAnalyticsSummary {
  files: number;
  documents: number;
  failedFilesPct: number;
  failedDocumentsPct: number;
  webhookSuccessPct: number;
  avgFileE2eMs: number | null;
  avgDocE2eMs: number | null;
}

export interface AdminVolumePoint {
  bucketStartUtc: string;
  files: number;
  documents: number;
}

export interface AdminByBusinessRow {
  businessId: string;
  businessName: string;
  tenantName: string;
  files: number;
  documents: number;
  failedPct: number;
  peakHourUtc: number | null;
}

export interface AdminHourlyPoint {
  hourUtc: number;
  files: number;
  documents: number;
}

export interface AdminStageTimingItem {
  stageKey: string;
  avgDurationMs: number;
  sampleCount: number;
}

export interface AdminStageTimings {
  fileStages: AdminStageTimingItem[];
  documentStages: AdminStageTimingItem[];
  note: string;
}

export interface AdminTenantListItem {
  id: string;
  idenTenantId: string;
  name: string;
  providerModeKey: string | null;
  isActive: boolean;
  businessCount: number;
  createdAt: string;
}

export interface AdminBusinessSummary {
  id: string;
  idenBusinessId: string;
  name: string;
  isActive: boolean;
  intakeEmailSlug: string | null;
  createdAt: string;
}

export interface AdminTenantDetail {
  id: string;
  idenTenantId: string;
  name: string;
  providerModeKey: string | null;
  isActive: boolean;
  createdAt: string;
  businesses: AdminBusinessSummary[];
}

export interface AdminBusinessListItem {
  id: string;
  idenBusinessId: string;
  name: string;
  tenantId: string;
  tenantName: string;
  idenTenantId: string;
  isActive: boolean;
  intakeEmailSlug: string | null;
  createdAt: string;
}

export interface AdminBusinessDetail {
  id: string;
  idenBusinessId: string;
  name: string;
  tenantId: string;
  tenantName: string;
  idenTenantId: string;
  isActive: boolean;
  intakeEmailSlug: string | null;
  createdAt: string;
  queues: { id: string; name: string; isDefault: boolean }[];
  agents: { id: string; name: string; isActive: boolean }[];
  recentFileStats: { total: number; failed: number; lastCreatedAt: string | null };
}

export interface AdminSupportHit {
  kind: string;
  id: string;
  label: string;
  secondary: string | null;
  deepLink: string | null;
}

export interface AdminSupportLookup {
  query: string;
  hits: AdminSupportHit[];
}

export interface AdminMonitoringSnapshot {
  healthStatus: string;
  checks: { name: string; status: string; description: string | null }[];
  hangfireDashboardUrl: string | null;
  datadogDashboardUrl: string | null;
}

export interface SystemSetting {
  key: string;
  valueJson: string;
}

export interface AdminProvider {
  id: number;
  providerKey: string;
  name: string;
  vendorHint: string | null;
  categoryKey: string;
}

export interface AdminDocumentType {
  id: number;
  documentTypeKey: string;
  name: string;
}

export interface AdminAgentTemplate {
  id: number;
  agentTemplateKey: string;
  name: string;
  description?: string | null;
  documentTypeId: number;
  documentTypeKey: string;
  defaultSchemaJson: string;
  defaultInstructions: string;
  systemPrompt: string;
  defaultPostProcessPrompt: string;
  defaultAdditionalDocumentInstructions: string;
  defaultProviderId?: number | null;
  isPublished: boolean;
  version: number;
  clonedAgentCount: number;
}

export interface AdminAgentTemplateUpdateResult {
  template: AdminAgentTemplate;
  clonedAgentCount: number;
  pushedSystemPrompt: boolean;
}

export interface AdminAgentListItem {
  id: string;
  name: string;
  businessId: string;
  businessName: string;
  tenantId: string;
  tenantName: string;
  documentTypeKey: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface AdminAgentDetail {
  id: string;
  name: string;
  description: string | null;
  businessId: string;
  businessName: string;
  tenantId: string;
  tenantName: string;
  documentTypeId: number;
  documentTypeKey: string | null;
  outputSchemaJson: string;
  schemaVersion: number;
  instructions: string;
  systemPrompt: string;
  postProcessPrompt: string;
  additionalDocumentInstructions: string;
  sourceTemplateId: number | null;
  defaultProviderId: number | null;
  isActive: boolean;
  createdAt: string;
}

export interface AdminAgentPromptPreview {
  systemPrompt: string;
  userPrompt: string;
}

export interface AdminAgentSuggestSystemPrompt {
  systemPrompt: string;
}

export interface UpdateAdminAgentSystemPromptBody {
  systemPrompt: string;
}

export interface CreateAdminAgentTemplateBody {
  agentTemplateKey: string;
  name: string;
  description?: string | null;
  documentTypeId: number;
  defaultSchemaJson: string;
  defaultInstructions: string;
  systemPrompt?: string | null;
  defaultPostProcessPrompt?: string | null;
  defaultAdditionalDocumentInstructions?: string | null;
  defaultProviderId?: number | null;
  isPublished: boolean;
}

export interface UpdateAdminAgentTemplateBody {
  name: string;
  description?: string | null;
  documentTypeId: number;
  defaultSchemaJson: string;
  defaultInstructions: string;
  systemPrompt?: string | null;
  defaultPostProcessPrompt?: string | null;
  defaultAdditionalDocumentInstructions?: string | null;
  defaultProviderId?: number | null;
  isPublished: boolean;
  pushSystemPrompt: boolean;
}

export interface AdminPublicEvent {
  eventKey: string;
  resourceTypeKey: string;
}

export interface AdminPublicActionType {
  actionTypeKey: string;
  name: string;
}

export interface AdminPublicEventsCatalog {
  events: AdminPublicEvent[];
  actionTypes: AdminPublicActionType[];
}

export interface AdminPublicEventToggle {
  eventKey: string;
  enabled: boolean;
}

export interface AdminActionBindingListItem {
  id: string;
  businessId: string;
  businessName: string;
  tenantId: string;
  tenantName: string;
  queueId: string | null;
  queueName: string | null;
  actionTypeKey: string;
  enabled: boolean;
  eventKeys: string[];
  webhookUrl: string | null;
  hasSecret: boolean;
  emailAudience: string | null;
  recipientCount: number;
  queuePublicActionsInherit: boolean | null;
  createdAt: string;
  updatedAt: string;
}

export interface AdminActionBindingDetail {
  id: string;
  businessId: string;
  businessName: string;
  tenantId: string;
  tenantName: string;
  queueId: string | null;
  queueName: string | null;
  actionTypeKey: string;
  enabled: boolean;
  events: AdminPublicEventToggle[];
  webhookUrl: string | null;
  hasSecret: boolean;
  emailAudience: string | null;
  recipients: string[];
  queuePublicActionsInherit: boolean | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAdminActionBindingBody {
  businessId: string;
  queueId?: string | null;
  actionTypeKey: string;
  enabled: boolean;
  eventKeys?: string[];
  url?: string | null;
  secret?: string | null;
  audience?: string | null;
  recipients?: string[];
}

export interface UpdateAdminActionBindingBody {
  enabled: boolean;
  eventKeys: string[];
  url?: string | null;
  secret?: string | null;
  audience?: string | null;
  recipients?: string[];
}

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(AppConfigService);

  private url(path: string): string {
    return `${this.config.apiBaseUrl}${path}`;
  }

  private params(obj: Record<string, string | number | boolean | null | undefined | string[]>): HttpParams {
    let p = new HttpParams();
    for (const [k, v] of Object.entries(obj)) {
      if (v === null || v === undefined || v === '') continue;
      if (Array.isArray(v)) {
        for (const item of v) {
          if (item) p = p.append(k, item);
        }
      } else {
        p = p.set(k, String(v));
      }
    }
    return p;
  }

  listFiles(query: Record<string, unknown>): Observable<Paged<AdminFileListItem>> {
    return this.http.get<Paged<AdminFileListItem>>(this.url('/api/admin/ops/files'), {
      params: this.params(query as Record<string, string | number | boolean | null | undefined | string[]>),
    });
  }

  listDocuments(query: Record<string, unknown>): Observable<Paged<AdminDocumentListItem>> {
    return this.http.get<Paged<AdminDocumentListItem>>(this.url('/api/admin/ops/documents'), {
      params: this.params(query as Record<string, string | number | boolean | null | undefined | string[]>),
    });
  }

  resetFile(fileId: string): Observable<AdminResetResult> {
    return this.http.post<AdminResetResult>(this.url(`/api/admin/ops/files/${fileId}/reset`), {});
  }

  resetDocument(documentId: string): Observable<AdminResetResult> {
    return this.http.post<AdminResetResult>(this.url(`/api/admin/ops/documents/${documentId}/reset`), {});
  }

  getFile(fileId: string): Observable<AdminFileDetail> {
    return this.http.get<AdminFileDetail>(this.url(`/api/admin/ops/files/${fileId}`));
  }

  getFileContentBlob(fileId: string): Observable<Blob> {
    return this.http.get(this.url(`/api/admin/ops/files/${fileId}/content`), {
      responseType: 'blob',
    });
  }

  listFileDocuments(fileId: string): Observable<AdminFileDocumentItem[]> {
    return this.http.get<AdminFileDocumentItem[]>(this.url(`/api/admin/ops/files/${fileId}/documents`));
  }

  getDocument(documentId: string): Observable<AdminDocumentDetail> {
    return this.http.get<AdminDocumentDetail>(this.url(`/api/admin/ops/documents/${documentId}`));
  }

  getDocumentContentBlob(documentId: string): Observable<Blob> {
    return this.http.get(this.url(`/api/admin/ops/documents/${documentId}/content`), {
      responseType: 'blob',
    });
  }

  analyticsSummary(query: Record<string, unknown> = {}): Observable<AdminAnalyticsSummary> {
    return this.http.get<AdminAnalyticsSummary>(this.url('/api/admin/analytics/summary'), {
      params: this.params(query as Record<string, string | number | boolean | null | undefined | string[]>),
    });
  }

  analyticsVolume(query: Record<string, unknown>): Observable<AdminVolumePoint[]> {
    return this.http.get<AdminVolumePoint[]>(this.url('/api/admin/analytics/volume'), {
      params: this.params(query as Record<string, string | number | boolean | null | undefined | string[]>),
    });
  }

  analyticsByBusiness(query: Record<string, unknown>): Observable<AdminByBusinessRow[]> {
    return this.http.get<AdminByBusinessRow[]>(this.url('/api/admin/analytics/by-business'), {
      params: this.params(query as Record<string, string | number | boolean | null | undefined | string[]>),
    });
  }

  analyticsHourly(query: Record<string, unknown>): Observable<AdminHourlyPoint[]> {
    return this.http.get<AdminHourlyPoint[]>(this.url('/api/admin/analytics/hourly'), {
      params: this.params(query as Record<string, string | number | boolean | null | undefined | string[]>),
    });
  }

  analyticsStageTimings(query: Record<string, unknown> = {}): Observable<AdminStageTimings> {
    return this.http.get<AdminStageTimings>(this.url('/api/admin/analytics/stage-timings'), {
      params: this.params(query as Record<string, string | number | boolean | null | undefined | string[]>),
    });
  }

  listTenants(query: Record<string, unknown> = {}): Observable<Paged<AdminTenantListItem>> {
    return this.http.get<Paged<AdminTenantListItem>>(this.url('/api/admin/tenants'), {
      params: this.params(query as Record<string, string | number | boolean | null | undefined | string[]>),
    });
  }

  getTenant(id: string): Observable<AdminTenantDetail> {
    return this.http.get<AdminTenantDetail>(this.url(`/api/admin/tenants/${id}`));
  }

  createTenant(body: {
    name: string;
    idenTenantId?: string | null;
    providerModeKey?: string | null;
    initialBusinessName?: string | null;
  }): Observable<AdminTenantDetail> {
    return this.http.post<AdminTenantDetail>(this.url('/api/admin/tenants'), body);
  }

  listBusinesses(query: Record<string, unknown> = {}): Observable<Paged<AdminBusinessListItem>> {
    return this.http.get<Paged<AdminBusinessListItem>>(this.url('/api/admin/businesses'), {
      params: this.params(query as Record<string, string | number | boolean | null | undefined | string[]>),
    });
  }

  getBusiness(businessId: string): Observable<AdminBusinessDetail> {
    return this.http.get<AdminBusinessDetail>(this.url(`/api/admin/businesses/${encodeURIComponent(businessId)}`));
  }

  supportLookup(q: string): Observable<AdminSupportLookup> {
    return this.http.get<AdminSupportLookup>(this.url('/api/admin/support/lookup'), {
      params: this.params({ q }),
    });
  }

  monitoringSnapshot(): Observable<AdminMonitoringSnapshot> {
    return this.http.get<AdminMonitoringSnapshot>(this.url('/api/admin/monitoring/snapshot'));
  }

  listSettings(): Observable<SystemSetting[]> {
    return this.http.get<SystemSetting[]>(this.url('/api/admin/system-settings'));
  }

  listProviders(category?: string): Observable<AdminProvider[]> {
    return this.http.get<AdminProvider[]>(this.url('/api/admin/providers'), {
      params: this.params({ category }),
    });
  }

  putSetting(key: string, valueJson: string): Observable<SystemSetting> {
    return this.http.put<SystemSetting>(
      this.url(`/api/admin/system-settings/${encodeURIComponent(key)}`),
      { valueJson },
    );
  }

  listDocumentTypes(): Observable<AdminDocumentType[]> {
    return this.http.get<AdminDocumentType[]>(this.url('/api/admin/document-types'));
  }

  listAgentTemplates(): Observable<AdminAgentTemplate[]> {
    return this.http.get<AdminAgentTemplate[]>(this.url('/api/admin/agent-templates'));
  }

  getAgentTemplate(id: number): Observable<AdminAgentTemplate> {
    return this.http.get<AdminAgentTemplate>(this.url(`/api/admin/agent-templates/${id}`));
  }

  createAgentTemplate(body: CreateAdminAgentTemplateBody): Observable<AdminAgentTemplate> {
    return this.http.post<AdminAgentTemplate>(this.url('/api/admin/agent-templates'), body);
  }

  updateAgentTemplate(
    id: number,
    body: UpdateAdminAgentTemplateBody,
  ): Observable<AdminAgentTemplateUpdateResult> {
    return this.http.put<AdminAgentTemplateUpdateResult>(
      this.url(`/api/admin/agent-templates/${id}`),
      body,
    );
  }

  listAgents(query: Record<string, unknown> = {}): Observable<Paged<AdminAgentListItem>> {
    return this.http.get<Paged<AdminAgentListItem>>(this.url('/api/admin/agents'), {
      params: this.params(query as Record<string, string | number | boolean | null | undefined | string[]>),
    });
  }

  getAgent(id: string): Observable<AdminAgentDetail> {
    return this.http.get<AdminAgentDetail>(this.url(`/api/admin/agents/${id}`));
  }

  getAgentPromptPreview(id: string): Observable<AdminAgentPromptPreview> {
    return this.http.get<AdminAgentPromptPreview>(this.url(`/api/admin/agents/${id}/prompt-preview`));
  }

  previewAgentPrompt(
    id: string,
    body: {
      systemPrompt?: string | null;
      instructions?: string | null;
      outputSchemaJson?: string | null;
      additionalDocumentInstructions?: string | null;
    },
  ): Observable<AdminAgentPromptPreview> {
    return this.http.post<AdminAgentPromptPreview>(
      this.url(`/api/admin/agents/${id}/prompt-preview`),
      body,
    );
  }

  suggestAgentSystemPrompt(id: string): Observable<AdminAgentSuggestSystemPrompt> {
    return this.http.post<AdminAgentSuggestSystemPrompt>(
      this.url(`/api/admin/agents/${id}/suggest-system-prompt`),
      {},
    );
  }

  updateAgentSystemPrompt(
    id: string,
    body: UpdateAdminAgentSystemPromptBody,
  ): Observable<AdminAgentDetail> {
    return this.http.put<AdminAgentDetail>(this.url(`/api/admin/agents/${id}/system-prompt`), body);
  }

  getPublicEventsCatalog(): Observable<AdminPublicEventsCatalog> {
    return this.http.get<AdminPublicEventsCatalog>(this.url('/api/admin/public-events'));
  }

  listActionBindings(query: Record<string, unknown> = {}): Observable<Paged<AdminActionBindingListItem>> {
    return this.http.get<Paged<AdminActionBindingListItem>>(this.url('/api/admin/action-bindings'), {
      params: this.params(query as Record<string, string | number | boolean | null | undefined | string[]>),
    });
  }

  getActionBinding(id: string): Observable<AdminActionBindingDetail> {
    return this.http.get<AdminActionBindingDetail>(this.url(`/api/admin/action-bindings/${id}`));
  }

  createActionBinding(body: CreateAdminActionBindingBody): Observable<AdminActionBindingDetail> {
    return this.http.post<AdminActionBindingDetail>(this.url('/api/admin/action-bindings'), body);
  }

  updateActionBinding(id: string, body: UpdateAdminActionBindingBody): Observable<AdminActionBindingDetail> {
    return this.http.put<AdminActionBindingDetail>(this.url(`/api/admin/action-bindings/${id}`), body);
  }
}
