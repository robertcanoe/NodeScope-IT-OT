/**
 * Mirrors ASP.NET JSON contracts (camelCase). Enums deserialize as integers unless `JsonStringEnumConverter` ships server-side—default NodeScope Phase 2 uses numeric payloads.
 */

export enum UserRole {
  User = 0,
  Admin = 1,
}

/** POST /api/auth/login request contract. */
export interface LoginRequest {
  email: string;
  password: string;
}

/** User slice embedded into successful JWT login responses. */
export interface UserSummary {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
}

/** Alias used by layout chrome when surfacing persisted operators. */
export interface User extends UserSummary {}

/** Successful login envelope emitted by ASP.NET controllers. */
export interface AuthResponse {
  accessToken: string;
  expiresUtc: string;
  user: UserSummary;
}
