import { CommonModule } from '@angular/common';
import { Component, Inject, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { catchError, finalize, of } from 'rxjs';

import { ProjectsApiService } from '../../core/projects-api.service';
import { ProjectSourceType, type ProjectResponse } from '../../shared/models/project.models';

@Component({
  standalone: true,
  selector: 'app-projects-page',
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    MatCardModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTableModule,
  ],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <h1>Projects</h1>
          <p class="muted">Structured workspaces organising technical datasets awaiting validation.</p>
        </div>
        <button type="button" mat-stroked-button color="primary" (click)="toggleComposer()">
          {{ showComposer() ? 'Hide composer' : 'New project' }}
        </button>
      </header>

      @if (error()) {
        <mat-card appearance="outlined" class="banner">{{ error() }}</mat-card>
      }

      @if (showComposer()) {
        <mat-card appearance="outlined" class="composer">
          <h2>Create workspace</h2>
          <form [formGroup]="form" (ngSubmit)="submitProject()" class="composer-grid">
            <mat-form-field appearance="outline">
              <mat-label>Name</mat-label>
              <input matInput formControlName="name" />
            </mat-form-field>

            <mat-form-field appearance="outline" class="span-2">
              <mat-label>Description</mat-label>
              <textarea matInput rows="3" formControlName="description"></textarea>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Declared source</mat-label>
              <mat-select formControlName="sourceType">
                <mat-option [value]="SourceType.OpcUa">OPC UA export</mat-option>
                <mat-option [value]="SourceType.ExcelSignals">Excel signals</mat-option>
                <mat-option [value]="SourceType.GenericCsv">CSV / logs</mat-option>
                <mat-option [value]="SourceType.Logs">Telemetry logs</mat-option>
              </mat-select>
            </mat-form-field>

            <div class="actions">
              <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">
                Provision workspace
              </button>
            </div>
          </form>
        </mat-card>
      }

      <mat-card appearance="outlined">
        @if (loading()) {
          <p class="muted padded">Fetching projects…</p>
        } @else if (projects().length === 0) {
          <p class="muted padded">No projects yet — create one to begin uploading industrial datasets.</p>
        } @else {
          <table mat-table [dataSource]="projects()" class="mat-elevation-z0 project-table">
            <ng-container matColumnDef="name">
              <th mat-header-cell *matHeaderCellDef>Name</th>
              <td mat-cell *matCellDef="let row">
                <a [routerLink]="['/projects', row.id]">{{ row.name }}</a>
              </td>
            </ng-container>

            <ng-container matColumnDef="source">
              <th mat-header-cell *matHeaderCellDef>Source</th>
              <td mat-cell *matCellDef="let row">{{ describeSource(row.sourceType) }}</td>
            </ng-container>

            <ng-container matColumnDef="updated">
              <th mat-header-cell *matHeaderCellDef>Updated</th>
              <td mat-cell *matCellDef="let row">{{ row.updatedAt | date: 'short' }}</td>
            </ng-container>

            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let row">
                <div class="row-actions">
                  <button type="button" mat-stroked-button (click)="editProject(row)">Edit</button>
                  <button type="button" mat-stroked-button color="warn" (click)="deleteProject(row)">Delete</button>
                </div>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
          </table>
        }
      </mat-card>
    </section>
  `,
  styles: `
    .page {
      display: grid;
      gap: 24px;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      gap: 16px;
      flex-wrap: wrap;
      align-items: center;

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

    .composer {
      padding: 22px 26px !important;
      border-radius: 14px !important;
      border-left: 4px solid rgb(0 66 84 / 0.35);
      box-shadow: 0 2px 20px rgb(0 54 71 / 0.07) !important;
    }

    .composer h2 {
      margin: 0 0 14px;
      font-size: 1.0625rem;
      font-weight: 700;
    }

    .composer-grid {
      display: grid;
      gap: 16px;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
    }

    .span-2 {
      grid-column: 1 / -1;
    }

    .actions {
      display: flex;
      align-items: center;
      grid-column: 1 / -1;
    }

    .banner {
      border-color: #f97316;
      color: #c2410c;
      padding: 16px;
    }

    .padded {
      padding: 20px;
    }

      mat-card {
        padding: 0;
        overflow: hidden;
        border-radius: 14px !important;
        border: 1px solid rgb(0 66 84 / 0.1) !important;
        box-shadow: 0 2px 20px rgb(0 54 71 / 0.06) !important;
      }

    .project-table {
      width: 100%;
    }

    .row-actions {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }
  `,
})
export class ProjectsPageComponent implements OnInit {
  private readonly projectsApi = inject(ProjectsApiService);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);

  protected readonly SourceType = ProjectSourceType;
  protected readonly projects = signal<ProjectResponse[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly showComposer = signal(false);

  protected readonly displayedColumns = ['name', 'source', 'updated', 'actions'] as const;

  protected readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: [''],
    sourceType: this.fb.nonNullable.control<ProjectSourceType>(ProjectSourceType.OpcUa),
  });

  ngOnInit(): void {
    this.refresh();
  }

  protected toggleComposer(): void {
    this.showComposer.update((value) => !value);
  }

  protected submitProject(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.projectsApi
      .create({
        name: raw.name.trim(),
        description: raw.description?.trim() || null,
        sourceType: raw.sourceType,
      })
      .pipe(
        catchError(() => {
          this.error.set('Provisioning failed. Inspect API logs for validation faults.');
          return of(null);
        }),
      )
      .subscribe((created) => {
        if (!created) {
          return;
        }

        this.error.set(null);
        this.refresh();
        this.form.reset({
          name: '',
          description: '',
          sourceType: ProjectSourceType.OpcUa,
        });
      });
  }

  protected describeSource(kind: ProjectSourceType): string {
    switch (kind) {
      case ProjectSourceType.OpcUa:
        return 'OPC UA';
      case ProjectSourceType.ExcelSignals:
        return 'Excel';
      case ProjectSourceType.GenericCsv:
        return 'CSV';
      case ProjectSourceType.Logs:
        return 'Logs';
      default:
        return 'Unknown';
    }
  }

  protected editProject(project: ProjectResponse): void {
    const ref = this.dialog.open(ProjectEditDialogComponent, {
      width: '560px',
      maxWidth: '95vw',
      data: project,
    });

    ref.afterClosed().subscribe((payload: { name: string; description: string | null } | undefined) => {
      if (!payload) {
        return;
      }

      this.projectsApi
        .update(project.id, {
          name: payload.name,
          description: payload.description,
          sourceType: project.sourceType,
        })
        .pipe(
          catchError(() => {
            this.error.set('Could not update project. Check API logs for details.');
            return of(null);
          }),
        )
        .subscribe((updated) => {
          if (!updated) {
            return;
          }

          this.error.set(null);
          this.projects.update((items) => items.map((p) => (p.id === updated.id ? updated : p)));
        });
    });
  }

  protected deleteProject(project: ProjectResponse): void {
    const ref = this.dialog.open(ProjectDeleteDialogComponent, {
      width: '440px',
      maxWidth: '95vw',
      data: project,
    });

    ref.afterClosed().subscribe((accepted: boolean | undefined) => {
      if (!accepted) {
        return;
      }

      this.projectsApi
        .delete(project.id)
        .pipe(
          catchError(() => {
            this.error.set('Could not delete project.');
            return of(null);
          }),
        )
        .subscribe((result) => {
          if (result === null) {
            return;
          }

          this.error.set(null);
          this.projects.update((items) => items.filter((p) => p.id !== project.id));
        });
    });
  }

  private refresh(): void {
    this.loading.set(true);
    this.projectsApi
      .list()
      .pipe(
        catchError(() => {
          this.error.set('Projects API unreachable. Confirm NodeScope.API is executing with PostgreSQL online.');
          return of([]);
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe((items) => {
        if (items) {
          this.projects.set(items);
        }
      });
  }
}

@Component({
  standalone: true,
  selector: 'app-project-edit-dialog',
  imports: [CommonModule, ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Edit project</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="dialog-form">
        <mat-form-field appearance="outline">
          <mat-label>Name</mat-label>
          <input matInput formControlName="name" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Description</mat-label>
          <textarea matInput rows="4" formControlName="description"></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="dialogRef.close()">Cancel</button>
      <button mat-flat-button color="primary" type="button" [disabled]="form.invalid" (click)="save()">Save</button>
    </mat-dialog-actions>
  `,
  styles: `
    .dialog-form {
      display: grid;
      gap: 12px;
      padding-top: 6px;
    }
    mat-form-field {
      width: 100%;
    }
  `,
})
export class ProjectEditDialogComponent {
  private readonly fb = inject(FormBuilder);
  protected readonly form;

  constructor(
    readonly dialogRef: MatDialogRef<ProjectEditDialogComponent>,
    @Inject(MAT_DIALOG_DATA) protected readonly data: ProjectResponse,
  ) {
    this.form = this.fb.nonNullable.group({
      name: [data.name, Validators.required],
      description: [data.description ?? ''],
    });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.dialogRef.close({
      name: raw.name.trim(),
      description: raw.description.trim() || null,
    });
  }
}

@Component({
  standalone: true,
  selector: 'app-project-delete-dialog',
  imports: [CommonModule, MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Delete project</h2>
    <mat-dialog-content>
      <p>
        Delete <strong>{{ data.name }}</strong>?
      </p>
      <p class="muted">This removes imports, issues, and generated artifacts for this workspace.</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="dialogRef.close(false)">Cancel</button>
      <button mat-flat-button color="warn" type="button" (click)="dialogRef.close(true)">Delete</button>
    </mat-dialog-actions>
  `,
  styles: `
    .muted {
      color: rgb(15 26 29 / 0.6);
    }
  `,
})
export class ProjectDeleteDialogComponent {
  constructor(
    readonly dialogRef: MatDialogRef<ProjectDeleteDialogComponent>,
    @Inject(MAT_DIALOG_DATA) protected readonly data: ProjectResponse,
  ) {}
}
