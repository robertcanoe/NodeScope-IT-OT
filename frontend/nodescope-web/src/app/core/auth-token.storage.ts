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

  readonly accessToken = this.persistedToken.asReadonly();

  readonly currentUser = this.persistedUser.asReadonly();

  readonly hasSession = computed(() => !!this.persistedToken());

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
  }
}
