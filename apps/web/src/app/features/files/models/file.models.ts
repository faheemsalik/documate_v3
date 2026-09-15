export interface FileListItem {
  id: string;
  queueId: string;
  batchId?: string | null;
  originalFileName?: string | null;
  contentType?: string | null;
  sizeBytes: number;
  publicStatusKey?: string | null;
  internalStageKey?: string | null;
  createdAt: string;
  completedAt?: string | null;
  documentCount: number;
  emailFrom?: string | null;
  emailSubject?: string | null;
}

export interface PagedFileList {
  items: FileListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface FileDetail extends FileListItem {
  storageKey: string;
  storageBucket?: string | null;
  publicStatusEnumId: number;
  internalStageEnumId?: number | null;
  emailMessageId?: string | null;
  emailIntakeJson?: string | null;
}

export interface FileDownloadUrl {
  fileId: string;
  url: string;
}

export interface DocumentListItem {
  id: string;
  fileId: string;
  documentTypeId?: number | null;
  documentTypeKey?: string | null;
  agentId?: string | null;
  publicStatusKey?: string | null;
  internalStageKey?: string | null;
  createdAt: string;
  completedAt?: string | null;
}

export interface DocumentDetail extends DocumentListItem {
  queueId: string;
  batchId?: string | null;
  publicStatusEnumId: number;
  internalStageEnumId?: number | null;
  errorCode?: string | null;
  errorMessage?: string | null;
  resultJson?: Record<string, unknown> | null;
  webhookStatusKey?: string | null;
  webhookAttempts: number;
  webhookLastHttpStatus?: number | null;
}

export interface ListFilesParams {
  page?: number;
  pageSize?: number;
  status?: string;
  createdFrom?: string;
  createdTo?: string;
}

export interface FileSchemaSearchItem {
  fileId: string;
  documentId: string;
  originalFileName?: string | null;
  publicStatusKey?: string | null;
  documentTypeKey?: string | null;
  fieldKey: string;
  matchedValue?: string | null;
  fileCreatedAt: string;
}

export interface PagedFileSchemaSearch {
  items: FileSchemaSearchItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  fieldKey: string;
  searchValue: string;
  matchMode: string;
}

export interface FileSummary {
  total: number;
  byPublicStatus: Record<string, number>;
}

export interface SchemaSearchParams {
  fieldKey: string;
  value: string;
  matchMode?: 'exact' | 'contains';
  page?: number;
  pageSize?: number;
}
