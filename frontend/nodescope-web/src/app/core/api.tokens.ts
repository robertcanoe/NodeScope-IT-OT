import { InjectionToken } from '@angular/core';

/** Base URL passed from {@link bootstrapApplication}; defaults avoid magic strings sprinkled across repositories. */
export const API_BASE_URL = new InjectionToken<string>('NODE_SCOPE_API_BASE_URL');
