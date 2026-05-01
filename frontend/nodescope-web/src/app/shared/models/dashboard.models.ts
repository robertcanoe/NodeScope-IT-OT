export interface DashboardRecentImport {
  importId: string;
  projectId: string;
  projectName: string;
  originalFileName: string;
  status: string;
  completedAt: string | null;
}

export interface DashboardStatistics {
  projectCount: number;
  importCount: number;
  completedCount: number;
  failedCount: number;
  processingCount: number;
  recentImports: DashboardRecentImport[];
}
