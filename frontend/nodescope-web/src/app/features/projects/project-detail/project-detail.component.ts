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
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { catchError, finalize, of, take } from 'rxjs';

import { ImportsApiService } from '../../../core/imports-api.service';
import { ProjectsApiService } from '../../../core/projects-api.service';
import type { ProjectResponse } from '../../../shared/models/project.models';
import { ImportJobStatus, type CompareImportsResponse, type ImportJobSummary } from '../../../shared/models/import.models';

@Component({
  standalone: true,
  selector: 'app-project-detail',
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatDividerModule,
    MatTableModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
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
            @if (live.status === ImportJobStatus.Failed && live.failureMessage) {
              <div class="failure-panel">
                <h3 class="failure-heading">Failure detail</h3>
                <pre class="failure-body">{{ live.failureMessage }}</pre>
              </div>
            }
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
                <td mat-cell *matCellDef="let row">
                  <span
                    class="status-cell"
                    [matTooltip]="row.failureMessage ?? ''"
                    [matTooltipDisabled]="row.status !== ImportJobStatus.Failed || !row.failureMessage"
                    matTooltipShowDelay="200"
                  >
                    {{ describe(row.status) }}
                  </span>
                </td>
              </ng-container>
              <ng-container matColumnDef="completed">
                <th mat-header-cell *matHeaderCellDef>Finished</th>
                <td mat-cell *matCellDef="let row">{{ row.completedAt ? (row.completedAt | date: 'short') : '—' }}</td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef>Outputs</th>
                <td mat-cell *matCellDef="let row">
                  <div class="row-actions">
                    @if (row.status === ImportJobStatus.Completed || row.status === ImportJobStatus.Failed) {
                      <a mat-stroked-button [routerLink]="['/projects', ws.id, 'imports', row.id, 'issues']">Issues</a>
                      <a mat-stroked-button [routerLink]="['/projects', ws.id, 'imports', row.id, 'records']">Rows</a>
                    }
                    @if (row.status === ImportJobStatus.Completed) {
                      <button
                        type="button"
                        mat-stroked-button
                        matTooltip="Informe HTML: KPIs, gráficas Chart.js, tabla filtrable y export CSV (mismo motor que generarInformeNodos)."
                        matTooltipShowDelay="300"
                        (click)="openReport(row.id)"
                      >
                        Informe nodos
                      </button>
                      <button type="button" mat-stroked-button (click)="downloadNormalized(row.id)">JSON</button>
                      <button type="button" mat-stroked-button (click)="downloadIssues(row.id)">Issues CSV</button>
                    }
                    @if (
                      row.status === ImportJobStatus.Completed || row.status === ImportJobStatus.Failed
                    ) {
                      <button type="button" mat-stroked-button color="primary" (click)="reprocessImport(ws.id, row.id)">
                        Re-run analysis
                      </button>
                    } @else if (row.status !== ImportJobStatus.Completed && row.status !== ImportJobStatus.Failed) {
                      <span class="muted">—</span>
                    }
                  </div>
                </td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="importColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: importColumns"></tr>
            </table>
          }
        </mat-card>

        @if (imports().length >= 2) {
          <mat-card appearance="outlined" class="compare-card">
            <div class="card-header">
              <h2>Compare two imports</h2>
              <p class="muted">Pick historical runs to inspect row/issue deltas and differing issue codes.</p>
            </div>
            <mat-divider />
            <div class="compare-controls">
              <label class="field">
                <span>Baseline</span>
                <select class="native-select" [(ngModel)]="compareLeftId">
                  @for (opt of imports(); track opt.id) {
                    <option [value]="opt.id">{{ opt.originalFileName }} — {{ describe(opt.status) }}</option>
                  }
                </select>
              </label>
              <label class="field">
                <span>Candidate</span>
                <select class="native-select" [(ngModel)]="compareRightId">
                  @for (opt of imports(); track opt.id) {
                    <option [value]="opt.id">{{ opt.originalFileName }} — {{ describe(opt.status) }}</option>
                  }
                </select>
              </label>
              <button
                type="button"
                mat-flat-button
                color="primary"
                [disabled]="compareLoading()"
                (click)="runCompare(ws.id)"
              >
                @if (compareLoading()) {
                  Comparing…
                } @else {
                  Compare
                }
              </button>
            </div>
            @if (compareError()) {
              <p class="compare-error">{{ compareError() }}</p>
            }
            @if (comparison(); as cmp) {
              <div class="compare-grid">
                <div>
                  <h3>Baseline</h3>
                  <p class="muted">{{ cmp.left.originalFileName }}</p>
                  <dl>
                    <dt>Rows</dt>
                    <dd>{{ cmp.left.rowCount ?? '—' }}</dd>
                    <dt>Issues</dt>
                    <dd>{{ cmp.left.issueCount ?? '—' }}</dd>
                    <dt>Dominant type</dt>
                    <dd>{{ cmp.left.dominantType ?? 'n/a' }}</dd>
                  </dl>
                </div>
                <div>
                  <h3>Candidate</h3>
                  <p class="muted">{{ cmp.right.originalFileName }}</p>
                  <dl>
                    <dt>Rows</dt>
                    <dd>{{ cmp.right.rowCount ?? '—' }}</dd>
                    <dt>Issues</dt>
                    <dd>{{ cmp.right.issueCount ?? '—' }}</dd>
                    <dt>Dominant type</dt>
                    <dd>{{ cmp.right.dominantType ?? 'n/a' }}</dd>
                  </dl>
                </div>
              </div>
              <div class="deltas muted">
                <p>Δ Rows: {{ cmp.rowCountDelta ?? 'n/a' }} · Δ Issues: {{ cmp.issueCountDelta ?? 'n/a' }}</p>
                <div class="code-lists">
                  <div>
                    <strong>Codes only in baseline</strong>
                    <pre>{{ cmp.issueCodesOnlyInLeft.length ? cmp.issueCodesOnlyInLeft.join('\n') : '—' }}</pre>
                  </div>
                  <div>
                    <strong>Codes only in candidate</strong>
                    <pre>{{ cmp.issueCodesOnlyInRight.length ? cmp.issueCodesOnlyInRight.join('\n') : '—' }}</pre>
                  </div>
                </div>
              </div>
            }
          </mat-card>
        }
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

      h1 {
        margin: 0 0 6px;
        font-weight: 800;
        letter-spacing: -0.025em;
        color: #08252c;
      }
    }

    .eyebrow {
      margin: 0;
      font-size: 13px;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      color: rgb(15 37 44 / 0.45);

      a {
        color: inherit;
        text-decoration: none;
      }

      a:hover {
        color: var(--ns-accent);
      }
    }

    .muted {
      color: rgb(15 26 29 / 0.58);
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

    .row-actions {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      padding: 8px 0;
    }

    .compare-card {
      padding: 0 0 20px;

      .card-header {
        padding: 16px 20px 12px;
      }

      .compare-controls {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
        gap: 12px;
        padding: 16px 20px;
        align-items: flex-end;

        .field {
          display: flex;
          flex-direction: column;
          gap: 6px;
          font-size: 12px;
          text-transform: uppercase;
          letter-spacing: 0.04em;
          color: rgba(15, 23, 42, 0.5);
        }

        .native-select {
          padding: 10px 12px;
          border-radius: 8px;
          border: 1px solid rgba(15, 23, 42, 0.2);
          font: inherit;
        }
      }

      .compare-error {
        color: #b91c1c;
        margin: 0 20px 12px;
      }

      .compare-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        gap: 16px;
        padding: 0 20px;
      }

      .compare-grid h3 {
        margin: 0 0 4px;
      }

      dl {
        margin: 0;
        display: grid;
        grid-template-columns: 120px 1fr;
        gap: 4px 8px;
      }

      .deltas {
        padding: 16px 20px 0;
      }

      .code-lists {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
        gap: 12px;

        pre {
          margin: 8px 0 0;
          padding: 10px;
          background: rgba(248, 250, 252, 0.9);
          border-radius: 8px;
          font-size: 12px;
          max-height: 160px;
          overflow: auto;
        }
      }
    }

    .status-cell {
      cursor: default;
      border-bottom: 1px dotted transparent;
    }

    .status-cell:hover {
      border-bottom-color: rgba(15, 23, 42, 0.25);
    }

    .banner {
      border-color: #f97316;
      color: #c2410c;
      padding: 16px 20px;
    }

    .inspector {
      padding: 18px 22px;

      .failure-panel {
        margin-top: 16px;
        padding-top: 16px;
        border-top: 1px solid rgba(15, 23, 42, 0.1);
      }

      .failure-heading {
        margin: 0 0 8px;
        font-size: 13px;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: rgba(185, 28, 28, 0.85);
      }

      .failure-body {
        margin: 0;
        padding: 12px 14px;
        background: rgba(254, 242, 242, 0.8);
        border-radius: 8px;
        font-size: 12px;
        line-height: 1.45;
        white-space: pre-wrap;
        word-break: break-word;
        max-height: 280px;
        overflow: auto;
        color: #7f1d1d;
        border: 1px solid rgba(248, 113, 113, 0.35);
      }

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
      border-radius: 14px !important;
      border: 1px solid rgb(0 66 84 / 0.1);
      box-shadow: 0 2px 20px rgb(0 54 71 / 0.06);
    }
  `,
})
export class ProjectDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly projectsApi = inject(ProjectsApiService);
  private readonly importsApi = inject(ImportsApiService);

  protected readonly ImportJobStatus = ImportJobStatus;

  @ViewChild('datasetPicker')
  protected readonly pickerRef?: ElementRef<HTMLInputElement>;

  protected readonly project = signal<ProjectResponse | null>(null);
  protected readonly imports = signal<ImportJobSummary[]>([]);
  protected readonly importsLoading = signal(false);
  protected readonly uploading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly latestSummary = signal<ImportJobSummary | null>(null);
  protected readonly comparison = signal<CompareImportsResponse | null>(null);
  protected readonly compareLoading = signal(false);
  protected readonly compareError = signal<string | null>(null);

  protected compareLeftId = '';
  protected compareRightId = '';

  protected readonly importColumns = ['file', 'status', 'completed', 'actions'] as const;

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

  protected openReport(importId: string): void {
    this.importsApi
      .getArtifactBlob(importId, 'report')
      .pipe(
        take(1),
        catchError(() => {
          this.error.set('Could not open the HTML report (missing file or network error).');
          return of(null);
        }),
      )
      .subscribe((blob) => {
        if (!blob) {
          return;
        }

        const url = URL.createObjectURL(blob);
        window.open(url, '_blank', 'noopener,noreferrer');
        window.setTimeout(() => URL.revokeObjectURL(url), 120_000);
      });
  }

  protected downloadNormalized(importId: string): void {
    this.saveBlob(importId, 'normalized', `${importId}-normalized.json`);
  }

  protected downloadIssues(importId: string): void {
    this.saveBlob(importId, 'issues', `${importId}-issues.csv`);
  }

  private saveBlob(importId: string, kind: 'normalized' | 'issues', fileName: string): void {
    this.importsApi
      .getArtifactBlob(importId, kind)
      .pipe(
        take(1),
        catchError(() => {
          this.error.set('Download failed (artifact may be missing).');
          return of(null);
        }),
      )
      .subscribe((blob) => {
        if (!blob) {
          return;
        }

        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = fileName;
        anchor.rel = 'noopener';
        anchor.click();
        window.setTimeout(() => URL.revokeObjectURL(url), 30_000);
      });
  }

  protected runCompare(projectId: string): void {
    if (!this.compareLeftId || !this.compareRightId || this.compareLeftId === this.compareRightId) {
      this.compareError.set('Select two different imports to compare.');
      return;
    }

    this.compareLoading.set(true);
    this.compareError.set(null);
    this.importsApi
      .compare(projectId, this.compareLeftId, this.compareRightId)
      .pipe(
        take(1),
        catchError(() => {
          this.compareError.set('Comparison failed — verify both imports belong to this workspace.');
          return of(null);
        }),
        finalize(() => this.compareLoading.set(false)),
      )
      .subscribe((dto) => {
        this.comparison.set(dto);
      });
  }

  protected reprocessImport(projectId: string, importId: string): void {
    this.error.set(null);
    this.importsApi
      .reprocess(projectId, importId)
      .pipe(
        take(1),
        catchError(() => {
          this.error.set('Re-run rejected (already queued or unreachable).');
          return of(undefined);
        }),
      )
      .subscribe(() => {
        this.latestSummary.set(null);
        this.startPollingLatest(importId);
        this.reloadImports(projectId);
      });
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
        const list = rows ?? [];
        this.imports.set(list);
        if (list.length >= 2 && !this.compareLeftId) {
          this.compareLeftId = list[0]!.id;
          this.compareRightId = list[1]!.id;
        }

        if (list.length < 2) {
          this.compareLeftId = '';
          this.compareRightId = '';
          this.comparison.set(null);
        }
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
