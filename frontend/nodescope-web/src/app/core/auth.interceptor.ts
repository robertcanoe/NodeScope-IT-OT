import { HttpInterceptorFn } from '@angular/common/http';

import { inject } from '@angular/core';

import { AuthTokenStorage } from './auth-token.storage';

/** Attaches Bearer credentials for protected REST calls emitted by SPA features. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const storage = inject(AuthTokenStorage);
  const token = storage.accessToken();

  if (!token) {
    return next(req);
  }

  const authHeaders = req.headers.set('Authorization', `Bearer ${token}`);
  const authorized = req.clone({ headers: authHeaders });
  return next(authorized);
};
