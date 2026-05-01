import { CommonModule } from '@angular/common';
import {
  Component,
  ElementRef,
  inject,
  OnDestroy,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { catchError, finalize, of } from 'rxjs';

import { ImportsApiService } from '../../../core/imports-api.service';
import { ProjectsApiService } from '../../../core/projects-api.service';
import type { ProjectResponse } from '../../../shared/models/project.models';
import { ImportJobStatus, type ImportJobSummary } from '../../../shared/models/import.models';

@Component({
  standalone: true,
  selector: 'app-project-detail',
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatDividerModule,
    MatTableModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <section class="page">
      @if (!project()) {
        <mat-card appearance="outlined" class="padded">
          <p class="muted">Loading workspace…</p>
        </mat-card>
      } @else {
        @let ws = project()!;
        <header class="page-header">
          <div>
            <p class="eyebrow">
              <a routerLink="/projects">Projects</a> / {{ ws.name }}
            </p>
            <h1>{{ ws.name }}</h1>
            <p class="muted">{{ ws.description || 'No description supplied for this ingestion workspace.' }}</p>
          </div>
          <button
            type="button"
            mat-flat-button
            color="primary"
            [disabled]="uploading()"
            (click)="openPicker()"
          >
            @if (uploading()) {
              <span>Ingest in progress…</span>
            } @else {
              <span>Ingest dataset</span>
            }
          </button>
        </header>

        <input
          #datasetPicker
          type="file"
          class="hidden"
          (change)="onDatasetChange()"
          accept=".csv,.xlsx,.xls,.json"
        />

        @if (error()) {
          <mat-card appearance="outlined" class="banner">{{ error() }}</mat-card>
        }

        @if (latestSummary(); as live) {
          <mat-card appearance="outlined" class="inspector">
            <h2>Active import telemetry</h2>
            <p class="muted">Status snapshot for import <code>{{ live.id }}</code></p>
            <div class="grid">
              <div>
                <span class="label">Status</span>
                <div class="value">{{ describe(live.status) }}</div>
              </div>
              <div>
                <span class="label">Rows</span>
                <div class="value">{{ live.rowCount ?? '…' }}</div>
              </div>
              <div>
                <span class="label">Issues</span>
                <div class="value">{{ live.issueCount ?? '…' }}</div>
              </div>
              <div>
                <span class="label">Dominant type</span>
                <div class="value">{{ live.dominantType ?? 'n/a' }}</div>
              </div>
              <div>
                <span class="label">Dominant NS</span>
                <div class="value">{{ live.dominantNamespace ?? 'n/a' }}</div>
              </div>
            </div>
          </mat-card>
        }

        <mat-card appearance="outlined" class="table-card">
          <div class="card-header">
            <h2>Ingest history</h2>
          </div>
          <mat-divider />

          @if (importsLoading()) {
            <div class="loading padded">
              <mat-progress-spinner diameter="44" mode="indeterminate" />
            </div>
          } @else if (imports().length === 0) {
            <p class="muted padded">Upload an Excel workbook, OPC UA CSV, or telemetry JSON artifact to initialise history.</p>
          } @else {
            <table mat-table [dataSource]="imports()" class="imports-table">
              <ng-container matColumnDef="file">
                <th mat-header-cell *matHeaderCellDef>File</th>
                <td mat-cell *matCellDef="let row">{{ row.originalFileName }}</td>
              </ng-container>
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>Status</th>
                <td mat-cell *matCellDef="let row">{{ describe(row.status) }}</td>
              </ng-container>
              <ng-container matColumnDef="completed">
                <th mat-header-cell *matHeaderCellDef>Finished</th>
                <td mat-cell *matCellDef="let row">{{ row.completedAt ? (row.completedAt | date: 'short') : '—' }}</td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="importColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: importColumns"></tr>
            </table>
          }
        </mat-card>
      }
    </section>
  `,
  styles: `
    .page {
      display: grid;
      gap: 24px;
      padding-bottom: 48px;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      gap: 16px;
      flex-wrap: wrap;
      align-items: flex-start;
    }

    .eyebrow {
      margin: 0;
      font-size: 13px;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      color: rgba(15, 23, 42, 0.55);

      a {
        color: inherit;
        text-decoration: none;

        &:hover {
          color: var(--mat-sys-primary);
        }
      }
    }

    .muted {
      color: rgba(15, 23, 42, 0.64);
      margin: 0;
    }

    .hidden {
      display: none;
    }

    .padded {
      padding: 20px;
    }

    .card-header {
      padding: 16px 20px 12px;
    }

    .table-card {
      padding: 0;
      overflow: hidden;
    }

    .imports-table {
      width: 100%;
    }

    .banner {
      border-color: #f97316;
      color: #c2410c;
      padding: 16px 20px;
    }

    .inspector {
      padding: 18px 22px;

      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
        gap: 12px;

        .label {
          font-size: 12px;
          text-transform: uppercase;
          color: rgba(15, 23, 42, 0.5);
          letter-spacing: 0.04em;
        }

        .value {
          font-size: 22px;
          font-weight: 600;
          color: #0f172a;
        }
      }
    }

    .loading {
      display: flex;
      justify-content: center;
      padding: 36px;
    }

    mat-card.mat-mdc-card {
      overflow: visible;
    }
  `,
})
export class ProjectDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly projectsApi = inject(ProjectsApiService);
  private readonly importsApi = inject(ImportsApiService);

  @ViewChild('datasetPicker')
  protected readonly pickerRef?: ElementRef<HTMLInputElement>;

  protected readonly project = signal<ProjectResponse | null>(null);
  protected readonly imports = signal<ImportJobSummary[]>([]);
  protected readonly importsLoading = signal(false);
  protected readonly uploading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly latestSummary = signal<ImportJobSummary | null>(null);

  protected readonly importColumns = ['file', 'status', 'completed'] as const;

  private activePoll?: ReturnType<typeof setInterval>;
  private routedProjectId: string | null = null;

  ngOnInit(): void {
    const projectId = this.route.snapshot.paramMap.get('projectId');

    if (!projectId) {
      this.error.set('Missing routing context for workspace identifier.');
      return;
    }

    this.routedProjectId = projectId;

    this.projectsApi
      .get(projectId)
      .pipe(catchError(() => of(null)))
      .subscribe((detail) => {
        if (!detail) {
          this.error.set('Project not accessible for the signed-in tenant.');
          return;
        }

        this.project.set(detail);
      });

    this.reloadImports(projectId);
  }

  ngOnDestroy(): void {
    this.disposePolling();
  }

  protected openPicker(): void {
    this.pickerRef?.nativeElement.click();
  }

  protected describe(status: ImportJobStatus): string {
    switch (status) {
      case ImportJobStatus.Pending:
        return 'Queued';
      case ImportJobStatus.Processing:
        return 'Processing';
      case ImportJobStatus.Completed:
        return 'Completed';
      case ImportJobStatus.Failed:
        return 'Failed';
      default:
        return String(status);
    }
  }

  protected onDatasetChange(): void {
    const picker = this.pickerRef?.nativeElement;

    if (!picker) {
      return;
    }

    const projectId = this.project()?.id;
    const fileList = picker.files;

    if (!projectId || !fileList?.length) {
      picker.value = '';
      return;
    }

    const [payload] = Array.from(fileList);

    this.uploading.set(true);
    this.error.set(null);
    this.importsApi
      .upload(projectId, payload)
      .pipe(
        catchError(() => {
          this.error.set('Upload failed due to gateway rejection or malformed payload sizing.');
          return of(null);
        }),
        finalize(() => {
          this.uploading.set(false);
          picker.value = '';
        }),
      )
      .subscribe((response) => {
        if (!response) {
          return;
        }

        this.latestSummary.set(null);
        this.startPollingLatest(response.importId);
        this.reloadImports(projectId);
      });
  }

  private reloadImports(projectId: string): void {
    this.importsLoading.set(true);
    this.importsApi
      .list(projectId)
      .pipe(
        catchError(() => {
          this.error.set('Imports API unreachable while listing historical uploads.');
          return of([]);
        }),
        finalize(() => this.importsLoading.set(false)),
      )
      .subscribe((rows) => {
        this.imports.set(rows ?? []);
      });
  }

  private startPollingLatest(importId: string): void {
    this.disposePolling();

    const evaluate = (): void => {
      this.importsApi.summary(importId).subscribe({
        next: (summary) => {
          this.latestSummary.set(summary);

          const finishedStatuses = new Set<number>([
            ImportJobStatus.Completed as number,
            ImportJobStatus.Failed as number,
          ]);

          if (finishedStatuses.has(summary.status as number)) {
            if (this.routedProjectId) {
              this.reloadImports(this.routedProjectId);
            }

            this.disposePolling();
          }
        },
        error: () => {
          this.disposePolling();
        },
      });
    };

    evaluate();
    this.activePoll = window.setInterval(evaluate, 1500);
  }

  private disposePolling(): void {
    if (this.activePoll) {
      clearInterval(this.activePoll);
      this.activePoll = undefined;
    }
  }
}
