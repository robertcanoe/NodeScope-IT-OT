import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';

import { AuthTokenStorage } from './auth-token.storage';

/** Blocks lazy routes whenever JWT material is unavailable in session primitives. */
export const authGuard: CanActivateFn = () => {
  const storage = inject(AuthTokenStorage);
  const router = inject(Router);

  return storage.hasSession() ? true : router.parseUrl('/login');
};
