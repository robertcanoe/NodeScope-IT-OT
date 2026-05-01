import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';

import { DashboardApiService } from '../../core/dashboard-api.service';
import type { DashboardStatistics } from '../../shared/models/dashboard.models';

@Component({
  standalone: true,
  selector: 'app-dashboard-page',
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatDividerModule,
    MatTableModule,
  ],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <h1>Operations dashboard</h1>
          <p class="muted">Live workspace telemetry aligned with ingestion queue health.</p>
        </div>
        <a mat-stroked-button color="primary" routerLink="/projects">Manage projects</a>
      </header>

      @if (error()) {
        <mat-card appearance="outlined" class="banner">{{ error() }}</mat-card>
      }

      @if (stats(); as snapshot) {
        <div class="kpi-grid">
          <mat-card appearance="outlined">
            <h3>Projects</h3>
            <div class="kpi-value">{{ snapshot.projectCount }}</div>
            <p class="muted">Active analysis workspaces visible to your account.</p>
          </mat-card>
          <mat-card appearance="outlined">
            <h3>Imports</h3>
            <div class="kpi-value">{{ snapshot.importCount }}</div>
            <p class="muted">Total ingestion attempts tracked historically.</p>
          </mat-card>
          <mat-card appearance="outlined">
            <h3>Completed</h3>
            <div class="kpi-value accent">{{ snapshot.completedCount }}</div>
          </mat-card>
          <mat-card appearance="outlined">
            <h3>In flight</h3>
            <div class="kpi-value warn">{{ snapshot.processingCount }}</div>
          </mat-card>
          <mat-card appearance="outlined">
            <h3>Failed</h3>
            <div class="kpi-value danger">{{ snapshot.failedCount }}</div>
          </mat-card>
        </div>

        <mat-card appearance="outlined" class="table-card">
          <div class="card-header">
            <h2>Latest ingestions</h2>
          </div>
          <mat-divider />
          <table mat-table [dataSource]="snapshot.recentImports" class="mat-elevation-z0 ingest-table">
            <ng-container matColumnDef="file">
              <th mat-header-cell *matHeaderCellDef>File</th>
              <td mat-cell *matCellDef="let row">{{ row.originalFileName }}</td>
            </ng-container>
            <ng-container matColumnDef="project">
              <th mat-header-cell *matHeaderCellDef>Workspace</th>
              <td mat-cell *matCellDef="let row">
                <a [routerLink]="['/projects', row.projectId]">{{ row.projectName }}</a>
              </td>
            </ng-container>
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>Status</th>
              <td mat-cell *matCellDef="let row">{{ row.status }}</td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayColumns"></tr>
          </table>
          @if (snapshot.recentImports.length === 0) {
            <p class="empty muted">Upload a dataset from any project workspace to populate this feed.</p>
          }
        </mat-card>
      } @else if (!loading()) {
        <p class="muted">No dashboard statistics available yet.</p>
      }

      @if (loading()) {
        <p class="muted">Loading dashboard…</p>
      }
    </section>
  `,
  styles: `
    .page {
      display: grid;
      gap: 24px;
      padding: 8px 0 48px;
    }

    .page-header {
      display: flex;
      gap: 16px;
      align-items: center;
      justify-content: space-between;
      flex-wrap: wrap;

      h1 {
        margin: 0 0 8px;
        font-size: clamp(1.625rem, 2.5vw, 2rem);
        font-weight: 800;
        letter-spacing: -0.025em;
        color: #08252c;
      }
    }

    .muted {
      color: rgb(15 26 29 / 0.58);
      margin: 0;
    }

    .kpi-grid {
      display: grid;
      gap: 16px;
      grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    }

    .kpi-grid mat-card {
      border-radius: 14px !important;
      border-left: 4px solid rgb(0 66 84 / 0.45) !important;
      box-shadow: 0 2px 16px rgb(0 45 58 / 0.06) !important;
    }

    mat-card {
      padding: 16px 20px;
    }

    h2,
    h3 {
      margin-top: 0;
    }

    h3 {
      font-size: 0.75rem;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      color: rgb(8 37 44 / 0.55);
      font-weight: 700;
    }

    .kpi-value {
      font-size: 2.125rem;
      font-weight: 800;
      letter-spacing: -0.02em;
      line-height: 1.15;
      color: var(--ns-brand);
    }

    .accent {
      color: #0a6b80;
    }

    .warn {
      color: #a85a0a;
    }

    .danger {
      color: #b4232c;
    }

    .table-card {
      padding: 0;
      overflow: hidden;
    }

    .card-header {
      padding: 16px 20px 12px;
    }

    .ingest-table {
      width: 100%;
      background-color: transparent;
    }

    .banner {
      border-color: #f97316;
      color: #c2410c;
    }

    .empty {
      padding: 0 20px 20px;
    }
  `,
})
export class DashboardPageComponent implements OnInit {
  private readonly dashboardApi = inject(DashboardApiService);

  protected readonly loading = signal(false);
  protected readonly stats = signal<DashboardStatistics | null>(null);
  protected readonly error = signal<string | null>(null);

  protected readonly displayColumns = ['file', 'project', 'status'] as const;

  ngOnInit(): void {
    this.loading.set(true);
    this.dashboardApi
      .statistics()
      .pipe(
        catchError(() => {
          this.error.set('Dashboard statistics unavailable. Confirm the NodeScope API and PostgreSQL are running.');
          return of(null);
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe((payload) => {
        if (payload) {
          this.stats.set(payload);
          this.error.set(null);
        }
      });
  }
}
