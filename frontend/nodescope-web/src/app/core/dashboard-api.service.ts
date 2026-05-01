import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type { DashboardStatistics } from '../shared/models/dashboard.models';
import { trimTrailingSlash } from '../shared/utils/api-helpers';
import { API_BASE_URL } from './api.tokens';

@Injectable({
  providedIn: 'root',
})
export class DashboardApiService {
  private readonly http = inject(HttpClient);
  private readonly base = trimTrailingSlash(inject(API_BASE_URL));

  statistics(): Observable<DashboardStatistics> {
    return this.http.get<DashboardStatistics>(`${this.base}/api/dashboard/stats`);
  }
}
