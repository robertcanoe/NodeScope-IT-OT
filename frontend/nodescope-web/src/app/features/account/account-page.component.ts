import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';

import { AuthService } from '../../core/auth.service';
import { AuthTokenStorage } from '../../core/auth-token.storage';
import { UserRole } from '../../shared/models/auth.models';

@Component({
  standalone: true,
  selector: 'app-account-page',
  imports: [CommonModule, RouterLink, MatCardModule, MatButtonModule, MatDividerModule],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Cuenta</p>
          <h1>Perfil y sesión</h1>
          <p class="muted">
            Gestiona tu identidad en el espacio de trabajo y el ciclo de vida del token de acceso (útil en
            entornos OT con estaciones compartidas).
          </p>
        </div>
      </header>

      <mat-card appearance="outlined" class="card">
        <mat-card-content>
          @if (user(); as u) {
            <dl class="kv">
              <dt>Nombre</dt>
              <dd>{{ u.displayName }}</dd>
              <dt>Correo</dt>
              <dd>{{ u.email }}</dd>
              <dt>Rol</dt>
              <dd>{{ roleLabel(u.role) }}</dd>
            </dl>
            <mat-divider class="sep" />
            <dl class="kv">
              <dt>Caducidad del token</dt>
              <dd>
                @if (expiresDisplay(); as exp) {
                  {{ exp }}
                } @else {
                  <span class="muted">No disponible</span>
                }
              </dd>
            </dl>
          } @else {
            <p class="muted">No hay datos de usuario en esta sesión.</p>
          }
        </mat-card-content>
        <mat-divider />
        <mat-card-actions align="end" class="actions">
          <a mat-stroked-button routerLink="/dashboard">Volver al panel</a>
          <button type="button" mat-flat-button color="warn" (click)="signOut()">Cerrar sesión</button>
        </mat-card-actions>
      </mat-card>

      <mat-card appearance="outlined" class="card hint">
        <mat-card-content>
          <h2 class="h2">Seguridad en planta</h2>
          <p class="muted block">
            En puestos SCADA/MES compartidos, cierra sesión al abandonar la consola. El token se guarda en el
            navegador de esta máquina; no sustituye a las políticas de dominio ni al bastionado de red de tu
            arquitectura OT.
          </p>
        </mat-card-content>
      </mat-card>
    </section>
  `,
  styles: `
    .page {
      display: grid;
      gap: 20px;
      padding-bottom: 48px;
      max-width: 720px;
    }

    .page-header h1 {
      margin: 0 0 8px;
      font-size: clamp(1.5rem, 2.2vw, 1.85rem);
      font-weight: 800;
      letter-spacing: -0.02em;
      color: #08252c;
    }

    .eyebrow {
      margin: 0 0 6px;
      font-size: 12px;
      font-weight: 700;
      letter-spacing: 0.12em;
      text-transform: uppercase;
      color: rgb(15 37 44 / 0.45);
    }

    .muted {
      margin: 0;
      color: rgb(15 26 29 / 0.58);
      line-height: 1.5;
    }

    .muted.block {
      margin-top: 8px;
    }

    .card {
      border-radius: 14px !important;
    }

    .kv {
      margin: 0;
      display: grid;
      grid-template-columns: 160px 1fr;
      gap: 10px 16px;
      align-items: baseline;
    }

    .kv dt {
      margin: 0;
      font-size: 12px;
      font-weight: 700;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      color: rgb(15 37 44 / 0.5);
    }

    .kv dd {
      margin: 0;
      font-size: 1rem;
      color: #0f172a;
    }

    .sep {
      margin: 18px 0;
    }

    .actions {
      padding: 12px 16px 16px !important;
      gap: 10px;
      flex-wrap: wrap;
    }

    .h2 {
      margin: 0 0 4px;
      font-size: 1rem;
      font-weight: 700;
      color: #08252c;
    }

    .hint mat-card-content {
      padding-top: 20px !important;
    }
  `,
})
export class AccountPageComponent {
  private readonly storage = inject(AuthTokenStorage);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly user = this.storage.currentUser;

  protected readonly expiresDisplay = computed(() => {
    const raw = this.storage.sessionExpiresUtc();
    if (!raw) {
      return null;
    }
    const ms = Date.parse(raw);
    if (!Number.isFinite(ms)) {
      return raw;
    }
    return new Date(ms).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });
  });

  protected roleLabel(role: UserRole): string {
    return role === UserRole.Admin ? 'Administrador' : 'Operador';
  }

  protected signOut(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
