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
}

export interface ImportJobQueued {
  importId: string;
  statusText: string;
}
