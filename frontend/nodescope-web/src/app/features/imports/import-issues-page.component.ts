import { CommonModule } from '@angular/common';
import {
  Component,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { catchError, finalize, of } from 'rxjs';

import { ImportsApiService } from '../../core/imports-api.service';
import type { ValidationIssueRow } from '../../shared/models/import.models';

@Component({
  standalone: true,
  selector: 'app-import-issues-page',
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <section class="page">
      <p class="eyebrow">
        <a [routerLink]="['/projects']">Projects</a>
        /
        <a [routerLink]="['/projects', projectId()]">Workspace</a>
        / Issues
      </p>
      <h1>Validation issues</h1>
      <p class="muted">Import <code>{{ importId() }}</code></p>

      @if (error()) {
        <mat-card appearance="outlined" class="banner">{{ error() }}</mat-card>
      }

      <mat-card appearance="outlined" class="filters">
        <div class="filter-row">
          <mat-form-field appearance="outline">
            <mat-label>Severity</mat-label>
            <mat-select [(ngModel)]="severityModel" (selectionChange)="onFilterChange()">
              <mat-option value="">All</mat-option>
              <mat-option value="Info">Info</mat-option>
              <mat-option value="Warning">Warning</mat-option>
              <mat-option value="Error">Error</mat-option>
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline" class="grow">
            <mat-label>Search code or message</mat-label>
            <input matInput [(ngModel)]="searchModel" (keyup.enter)="reload()" />
          </mat-form-field>
          <button type="button" mat-flat-button color="primary" (click)="reload()">Apply</button>
        </div>
      </mat-card>

      @if (loading()) {
        <div class="loading">
          <mat-progress-spinner diameter="40" mode="indeterminate" />
        </div>
      } @else {
        <mat-card appearance="outlined" class="table-card">
          <table mat-table [dataSource]="rows()" class="grid">
            <ng-container matColumnDef="severity">
              <th mat-header-cell *matHeaderCellDef>Severity</th>
              <td mat-cell *matCellDef="let row">{{ row.severity }}</td>
            </ng-container>
            <ng-container matColumnDef="code">
              <th mat-header-cell *matHeaderCellDef>Code</th>
              <td mat-cell *matCellDef="let row"><code>{{ row.code }}</code></td>
            </ng-container>
            <ng-container matColumnDef="message">
              <th mat-header-cell *matHeaderCellDef>Message</th>
              <td mat-cell *matCellDef="let row">{{ row.message }}</td>
            </ng-container>
            <ng-container matColumnDef="column">
              <th mat-header-cell *matHeaderCellDef>Column</th>
              <td mat-cell *matCellDef="let row">{{ row.columnName ?? '—' }}</td>
            </ng-container>
            <ng-container matColumnDef="row">
              <th mat-header-cell *matHeaderCellDef>Row</th>
              <td mat-cell *matCellDef="let row">{{ row.rowIndex ?? '—' }}</td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="columns"></tr>
            <tr mat-row *matRowDef="let row; columns: columns"></tr>
          </table>
          <mat-paginator
            [length]="total()"
            [pageIndex]="pageIndex()"
            [pageSize]="pageSize()"
            [pageSizeOptions]="[10, 25, 50]"
            (page)="onPage($event)"
          />
        </mat-card>
      }
    </section>
  `,
  styles: `
    .page {
      display: grid;
      gap: 20px;
      padding-bottom: 48px;
      max-width: 1200px;
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
      }
      a:hover {
        color: var(--mat-sys-primary);
      }
    }
    .muted {
      margin: 0;
      color: rgba(15, 23, 42, 0.62);
    }
    .banner {
      border-color: #f97316;
      color: #c2410c;
      padding: 16px 20px;
    }
    .filters {
      padding: 16px 20px;
    }
    .filter-row {
      display: flex;
      flex-wrap: wrap;
      gap: 12px;
      align-items: center;
    }
    .grow {
      flex: 1;
      min-width: 200px;
    }
    .table-card {
      padding: 0;
      overflow: hidden;
    }
    .grid {
      width: 100%;
    }
    .loading {
      display: flex;
      justify-content: center;
      padding: 40px;
    }
  `,
})
export class ImportIssuesPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly importsApi = inject(ImportsApiService);
  private routeSub?: Subscription;

  protected readonly projectId = signal('');
  protected readonly importId = signal('');

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly rows = signal<ValidationIssueRow[]>([]);
  protected readonly total = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(25);

  protected severityModel = '';
  protected searchModel = '';

  protected readonly columns = ['severity', 'code', 'message', 'column', 'row'] as const;

  ngOnInit(): void {
    this.routeSub = this.route.paramMap.subscribe((params) => {
      this.projectId.set(params.get('projectId') ?? '');
      this.importId.set(params.get('importId') ?? '');
      this.pageIndex.set(0);
      this.reload();
    });
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  protected reload(): void {
    const iid = this.importId();
    if (!iid) {
      this.error.set('Missing import identifier.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.importsApi
      .listValidationIssues(iid, this.pageIndex() + 1, this.pageSize(), this.severityModel || null, this.searchModel || null)
      .pipe(
        catchError(() => {
          this.error.set('Could not load validation issues.');
          return of({ items: [], totalCount: 0, page: 1, pageSize: this.pageSize() });
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe((page) => {
        this.rows.set(page.items ?? []);
        this.total.set(page.totalCount);
      });
  }

  protected onPage(ev: PageEvent): void {
    this.pageIndex.set(ev.pageIndex);
    this.pageSize.set(ev.pageSize);
    this.reload();
  }

  protected onFilterChange(): void {
    this.pageIndex.set(0);
    this.reload();
  }
}
