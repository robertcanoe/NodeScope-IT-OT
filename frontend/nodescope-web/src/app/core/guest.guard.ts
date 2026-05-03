import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';

import { AuthTokenStorage } from './auth-token.storage';

/** Prevents revisiting credential capture while a non-expired session exists locally. */
export const guestGuard: CanActivateFn = () => {
  const storage = inject(AuthTokenStorage);
  const router = inject(Router);

  return storage.hasValidSession() ? router.parseUrl('/dashboard') : true;
};
