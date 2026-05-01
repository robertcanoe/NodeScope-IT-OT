/** Mirrors `ImportJobStatus` on the API. */
export enum ImportJobStatus {
  Pending = 0,
  Processing = 1,
  Completed = 2,
  Failed = 3,
}

export interface ImportJobSummary {
  id: string;
  projectId: string;
  originalFileName: string;
  status: ImportJobStatus;
  rowCount: number | null;
  issueCount: number | null;
  dominantType: string | null;
  dominantNamespace: string | null;
  completedAt: string | null;
  failureMessage?: string | null;
}

export interface ImportJobQueued {
  importId: string;
  statusText: string;
}

/** API `PagedResultDto<T>` camelCase serialization. */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ValidationIssueRow {
  id: string;
  severity: string;
  code: string;
  message: string;
  columnName: string | null;
  rowIndex: number | null;
  rawValue: string | null;
  createdAt: string;
}

export interface DatasetRecordRow {
  recordIndex: number;
  payloadJson: string;
}

export interface ImportComparisonSide {
  importId: string;
  originalFileName: string;
  status: string;
  rowCount: number | null;
  issueCount: number | null;
  dominantType: string | null;
  dominantNamespace: string | null;
  issueCodesDistinct: string[];
}

export interface CompareImportsResponse {
  left: ImportComparisonSide;
  right: ImportComparisonSide;
  rowCountDelta: number | null;
  issueCountDelta: number | null;
  issueCodesOnlyInLeft: string[];
  issueCodesOnlyInRight: string[];
}
