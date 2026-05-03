import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';

import { AuthTokenStorage } from './auth-token.storage';

/** Blocks lazy routes when there is no valid (non-expired) access token. */
export const authGuard: CanActivateFn = () => {
  const storage = inject(AuthTokenStorage);
  const router = inject(Router);

  if (storage.hasValidSession()) {
    return true;
  }
  if (storage.hasSession()) {
    storage.clear();
  }
  return router.parseUrl('/login');
};
