import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
  CompareImportsResponse,
  DatasetRecordRow,
  ImportJobQueued,
  ImportJobSummary,
  PagedResult,
  ValidationIssueRow,
} from '../shared/models/import.models';
import { trimTrailingSlash } from '../shared/utils/api-helpers';
import { API_BASE_URL } from './api.tokens';

@Injectable({
  providedIn: 'root',
})
export class ImportsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = trimTrailingSlash(inject(API_BASE_URL));

  list(projectId: string): Observable<ImportJobSummary[]> {
    return this.http.get<ImportJobSummary[]>(`${this.base}/api/projects/${projectId}/imports`);
  }

  upload(projectId: string, file: File): Observable<ImportJobQueued> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<ImportJobQueued>(`${this.base}/api/projects/${projectId}/imports`, formData);
  }

  summary(importId: string): Observable<ImportJobSummary> {
    return this.http.get<ImportJobSummary>(`${this.base}/api/imports/${importId}/summary`);
  }

  getArtifactBlob(importId: string, kind: 'report' | 'normalized' | 'issues'): Observable<Blob> {
    const segment =
      kind === 'report' ? 'artifacts/report' : kind === 'normalized' ? 'artifacts/normalized-json' : 'artifacts/issues-csv';

    return this.http.get(`${this.base}/api/imports/${importId}/${segment}`, { responseType: 'blob' });
  }

  listValidationIssues(
    importId: string,
    page: number,
    pageSize: number,
    severity?: string | null,
    search?: string | null,
  ): Observable<PagedResult<ValidationIssueRow>> {
    let params = new HttpParams().set('page', String(page)).set('pageSize', String(pageSize));
    if (severity) {
      params = params.set('severity', severity);
    }
    if (search?.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<PagedResult<ValidationIssueRow>>(`${this.base}/api/imports/${importId}/validation-issues`, {
      params,
    });
  }

  listDatasetRecords(importId: string, page: number, pageSize: number, search?: string | null): Observable<PagedResult<DatasetRecordRow>> {
    let params = new HttpParams().set('page', String(page)).set('pageSize', String(pageSize));
    if (search?.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<PagedResult<DatasetRecordRow>>(`${this.base}/api/imports/${importId}/dataset-records`, {
      params,
    });
  }

  reprocess(projectId: string, importId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/api/projects/${projectId}/imports/${importId}/reprocess`, {});
  }

  compare(projectId: string, leftImportId: string, rightImportId: string): Observable<CompareImportsResponse> {
    const params = new HttpParams().set('leftImportId', leftImportId).set('rightImportId', rightImportId);
    return this.http.get<CompareImportsResponse>(`${this.base}/api/projects/${projectId}/imports/compare`, { params });
  }
}
