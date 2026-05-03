import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';

/**
 * Hub de contexto operativo (planta / MES / SCADA): enlaces al producto real y huecos claros para integraciones futuras.
 */
@Component({
  standalone: true,
  selector: 'app-operations-hub-page',
  imports: [RouterLink, MatCardModule, MatButtonModule, MatDividerModule],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Operaciones</p>
          <h1>Planta, MES y datos OT</h1>
          <p class="muted">
            NodeScope encaja como capa de análisis y validación sobre datasets técnicos que suelen circular entre
            SCADA, historiadores, MES y herramientas de ingeniería. Desde aquí enlazamos al flujo que ya está
            implementado y dejamos visibles los próximos frentes típicos en IT/OT.
          </p>
        </div>
      </header>

      <div class="grid">
        <mat-card appearance="outlined" class="tile">
          <mat-card-content>
            <h2 class="tile-title">Ingesta de planta</h2>
            <p class="muted">
              Libros Excel de nodos OPC UA, CSV exportados desde SCADA y JSON de telemetría se procesan en cola,
              generan informes HTML interactivos, filas normalizadas y CSV de incidencias.
            </p>
          </mat-card-content>
          <mat-divider />
          <mat-card-actions align="end">
            <a mat-flat-button color="primary" routerLink="/projects">Ir a proyectos</a>
          </mat-card-actions>
        </mat-card>

        <mat-card appearance="outlined" class="tile">
          <mat-card-content>
            <h2 class="tile-title">MES y calidad de datos</h2>
            <p class="muted">
              El MES se apoya en datos consistentes: reglas de validación, conteo de filas y comparación entre
              importaciones ayudan a detectar deriva entre baseline y candidato antes de publicar a producción.
            </p>
          </mat-card-content>
          <mat-divider />
          <mat-card-actions align="end">
            <a mat-stroked-button routerLink="/dashboard">Ver panel</a>
          </mat-card-actions>
        </mat-card>

        <mat-card appearance="outlined" class="tile">
          <mat-card-content>
            <h2 class="tile-title">Integraciones (hoja de ruta)</h2>
            <ul class="roadmap">
              <li>Conectores de lectura programada desde historiador (REST/OPC UA pub/sub).</li>
              <li>Etiquetado de contexto de línea / centro de trabajo para alinear con órdenes MES.</li>
              <li>Salida firmada o empaquetado ZIP para entornos air-gapped.</li>
            </ul>
            <p class="muted small">
              Estas líneas son orientativas; priorízalas con tu equipo de OT y ciberseguridad.
            </p>
          </mat-card-content>
        </mat-card>
      </div>
    </section>
  `,
  styles: `
    .page {
      display: grid;
      gap: 22px;
      padding-bottom: 48px;
    }

    .page-header h1 {
      margin: 0 0 8px;
      font-size: clamp(1.5rem, 2.2vw, 1.9rem);
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
      line-height: 1.55;
    }

    .muted.small {
      margin-top: 12px;
      font-size: 0.875rem;
    }

    .grid {
      display: grid;
      gap: 18px;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    }

    .tile {
      border-radius: 14px !important;
      display: flex;
      flex-direction: column;
      height: 100%;
    }

    .tile-title {
      margin: 0 0 10px;
      font-size: 1.05rem;
      font-weight: 800;
      color: #004254;
    }

    .roadmap {
      margin: 0;
      padding-left: 1.15rem;
      color: rgb(15 26 29 / 0.78);
      line-height: 1.5;
    }

    .roadmap li + li {
      margin-top: 6px;
    }

    mat-card-actions {
      padding: 12px 16px 16px !important;
      margin-top: auto;
    }
  `,
})
export class OperationsHubPageComponent {}
