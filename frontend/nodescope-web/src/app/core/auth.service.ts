import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';

import type { AuthResponse, LoginRequest } from '../shared/models/auth.models';
import { trimTrailingSlash } from '../shared/utils/api-helpers';
import { API_BASE_URL } from './api.tokens';
import { AuthTokenStorage } from './auth-token.storage';

/**
 * Coordinates SPA authentication flows backed by CQRS-aligned ASP.NET controllers.
 */
@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storage = inject(AuthTokenStorage);
  private readonly apiBase = trimTrailingSlash(inject(API_BASE_URL));

  /**
   * Mirrors {@link POST} `/api/auth/login` contract.
   */
  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiBase}/api/auth/login`, request)
      .pipe(tap((response) => this.storage.persist(response)));
  }

  /** Clears local session context (signals + persistence). */
  logout(): void {
    this.storage.clear();
  }

  /** Whether a usable (non-expired) access token exists. */
  isAuthenticated(): boolean {
    return this.storage.hasValidSession();
  }
}
