import { computed, Injectable, signal } from '@angular/core';

import type { AuthResponse, UserSummary } from '../shared/models/auth.models';

const STORAGE_KEY = 'nodescope.auth.session';

interface PersistedSessionPayload {
  accessToken: string;
  expiresUtc: string;
  user: UserSummary;
}

/**
 * Lightweight session holder decouples HTTP interception from {@link AuthService}/HttpClient to avoid DI cycles.
 */
@Injectable({
  providedIn: 'root',
})
export class AuthTokenStorage {
  private readonly persistedToken = signal<string | null>(null);
  private readonly persistedUser = signal<UserSummary | null>(null);
  private readonly persistedExpiresUtc = signal<string | null>(null);

  readonly accessToken = this.persistedToken.asReadonly();

  readonly currentUser = this.persistedUser.asReadonly();

  /** Server-provided access token expiry (ISO 8601), when available. */
  readonly sessionExpiresUtc = this.persistedExpiresUtc.asReadonly();

  /** True when a token string exists (may be expired). */
  readonly hasSession = computed(() => !!this.persistedToken());

  /** True when credentials exist and the access token is still within its lifetime. */
  readonly hasValidSession = computed(() => {
    const token = this.persistedToken();
    if (!token) {
      return false;
    }
    return !this.isAccessTokenExpired(token, this.persistedExpiresUtc());
  });

  constructor() {
    this.restore();
  }

  /**
   * Rehydrates SPA state via localStorage (best-effort; invalid JSON clears the slot).
   */
  restore(): void {
    try {
      const snapshot = globalThis.localStorage?.getItem(STORAGE_KEY);
      if (!snapshot) {
        this.clearSignals();
        return;
      }

      const parsed = JSON.parse(snapshot) as PersistedSessionPayload;
      if (!parsed?.accessToken) {
        this.clearSignals();
        return;
      }

      this.persistedToken.set(parsed.accessToken);
      this.persistedUser.set(parsed.user ?? null);
      this.persistedExpiresUtc.set(parsed.expiresUtc ?? null);
    } catch {
      globalThis.localStorage?.removeItem(STORAGE_KEY);
      this.clearSignals();
    }
  }

  /**
   * Persists cryptographic material emitted by {@link AuthService}.
   */
  persist(response: AuthResponse): void {
    this.persistedToken.set(response.accessToken);
    this.persistedUser.set(response.user);
    this.persistedExpiresUtc.set(response.expiresUtc);

    const payload: PersistedSessionPayload = {
      accessToken: response.accessToken,
      expiresUtc: response.expiresUtc,
      user: response.user,
    };

    globalThis.localStorage?.setItem(STORAGE_KEY, JSON.stringify(payload));
  }

  /**
   * Clears both memory-backed signals plus durable persistence.
   */
  clear(): void {
    globalThis.localStorage?.removeItem(STORAGE_KEY);
    this.clearSignals();
  }

  private clearSignals(): void {
    this.persistedToken.set(null);
    this.persistedUser.set(null);
    this.persistedExpiresUtc.set(null);
  }

  private isAccessTokenExpired(token: string, expiresUtcIso: string | null): boolean {
    const skewMs = 60_000;
    if (expiresUtcIso) {
      const ms = Date.parse(expiresUtcIso);
      if (Number.isFinite(ms)) {
        return Date.now() >= ms - skewMs;
      }
    }
    return this.isJwtExpClaimExpired(token, skewMs / 1000);
  }

  private isJwtExpClaimExpired(token: string, clockSkewSeconds: number): boolean {
    try {
      const segment = token.split('.')[1];
      if (!segment) {
        return false;
      }
      const json = globalThis.atob(segment.replace(/-/g, '+').replace(/_/g, '/'));
      const payload = JSON.parse(json) as { exp?: number };
      if (typeof payload.exp !== 'number') {
        return false;
      }
      return Date.now() / 1000 >= payload.exp - clockSkewSeconds;
    } catch {
      return false;
    }
  }
}
