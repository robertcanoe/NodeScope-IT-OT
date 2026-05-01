import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
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
    }

    .muted {
      color: rgba(15, 23, 42, 0.64);
      margin: 0;
    }

    .composer {
      padding: 20px 24px;
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
    }

    .project-table {
      width: 100%;
    }
  `,
})
export class ProjectsPageComponent implements OnInit {
  private readonly projectsApi = inject(ProjectsApiService);
  private readonly fb = inject(FormBuilder);

  protected readonly SourceType = ProjectSourceType;
  protected readonly projects = signal<ProjectResponse[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly showComposer = signal(false);

  protected readonly displayedColumns = ['name', 'source', 'updated'] as const;

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
