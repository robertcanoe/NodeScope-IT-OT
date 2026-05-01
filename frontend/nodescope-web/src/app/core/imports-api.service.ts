import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type { ImportJobQueued, ImportJobSummary } from '../shared/models/import.models';
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
}
