import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import type { LoginRequest } from '../../../shared/models/auth.models';

import { AuthService } from '../../../core/auth.service';

/**
 * Material-backed credential capture surface aligning with SPA-first NodeScope onboarding experiences.
 */
@Component({
  standalone: true,
  selector: 'app-login-page',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly feedback = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  /** Submits sanitized credentials upstream and hydrates SPA session primitives on success. */
  submitCredentials(): void {
    this.feedback.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload: LoginRequest = this.form.getRawValue();
    this.submitting.set(true);

    this.auth
      .login(payload)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: async () => {
          await this.router.navigateByUrl('/dashboard');
        },
        error: (error: unknown) => {
          const message =
            error instanceof HttpErrorResponse
              ? error.status === StatusCodesUnauthorized
                  ? 'Invalid email or password.'
                  : error.status === StatusCodesUnreachable
                    ? 'Authentication service unreachable.'
                    : 'Unexpected rejection from NodeScope.'
              : 'Authentication request failed unexpectedly.';

          this.feedback.set(message);
        },
      });
  }
}

const StatusCodesUnauthorized = 401;
const StatusCodesUnreachable = 0;
