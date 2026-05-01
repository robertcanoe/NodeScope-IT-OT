import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type { CreateProjectRequest, ProjectResponse, UpdateProjectRequest } from '../shared/models/project.models';
import { trimTrailingSlash } from '../shared/utils/api-helpers';
import { API_BASE_URL } from './api.tokens';

@Injectable({
  providedIn: 'root',
})
export class ProjectsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = trimTrailingSlash(inject(API_BASE_URL));

  list(): Observable<ProjectResponse[]> {
    return this.http.get<ProjectResponse[]>(`${this.base}/api/projects`);
  }

  get(projectId: string): Observable<ProjectResponse> {
    return this.http.get<ProjectResponse>(`${this.base}/api/projects/${projectId}`);
  }

  create(payload: CreateProjectRequest): Observable<ProjectResponse> {
    return this.http.post<ProjectResponse>(`${this.base}/api/projects`, payload);
  }

  update(projectId: string, payload: UpdateProjectRequest): Observable<ProjectResponse> {
    return this.http.put<ProjectResponse>(`${this.base}/api/projects/${projectId}`, payload);
  }

  delete(projectId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/projects/${projectId}`);
  }
}
