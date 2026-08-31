import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { API_BASE_URL } from '../../../core/api-base';
import type {
  DocumentDetail,
  DocumentListItem,
  FileDetail,
  FileDownloadUrl,
  FileSummary,
  ListFilesParams,
  PagedFileList,
  PagedFileSchemaSearch,
  SchemaSearchParams,
} from '../models/file.models';

@Injectable({ providedIn: 'root' })
export class FilesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE_URL}/api/app/queues`;

  listFiles(queueId: string, params: ListFilesParams = {}) {
    let httpParams = new HttpParams();
    if (params.page != null) httpParams = httpParams.set('page', String(params.page));
    if (params.pageSize != null) httpParams = httpParams.set('pageSize', String(params.pageSize));
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.createdFrom) httpParams = httpParams.set('createdFrom', params.createdFrom);
    if (params.createdTo) httpParams = httpParams.set('createdTo', params.createdTo);
    return this.http.get<PagedFileList>(`${this.base}/${queueId}/files`, { params: httpParams });
  }

  getFile(queueId: string, fileId: string) {
    return this.http.get<FileDetail>(`${this.base}/${queueId}/files/${fileId}`);
  }

  getDownloadUrl(queueId: string, fileId: string) {
    return this.http.get<FileDownloadUrl>(`${this.base}/${queueId}/files/${fileId}/download-url`);
  }

  listDocuments(queueId: string, fileId: string) {
    return this.http.get<DocumentListItem[]>(`${this.base}/${queueId}/files/${fileId}/documents`);
  }

  getDocument(queueId: string, documentId: string) {
    return this.http.get<DocumentDetail>(`${this.base}/${queueId}/documents/${documentId}`);
  }

  uploadFile(queueId: string, file: File, documentTypeKey?: string) {
    const form = new FormData();
    form.append('file', file);
    if (documentTypeKey) {
      form.append('documentTypeKey', documentTypeKey);
    }
    return this.http.post<FileDetail>(`${this.base}/${queueId}/files`, form);
  }

  searchFiles(queueId: string, params: SchemaSearchParams) {
    let httpParams = new HttpParams()
      .set('fieldKey', params.fieldKey)
      .set('value', params.value);
    if (params.matchMode) httpParams = httpParams.set('matchMode', params.matchMode);
    if (params.page != null) httpParams = httpParams.set('page', String(params.page));
    if (params.pageSize != null) httpParams = httpParams.set('pageSize', String(params.pageSize));
    return this.http.get<PagedFileSchemaSearch>(`${this.base}/${queueId}/files/search`, {
      params: httpParams,
    });
  }

  getSummary(queueId: string) {
    return this.http.get<FileSummary>(`${this.base}/${queueId}/files/summary`);
  }
}
