/** Backend `ProjectSourceType` ordering must stay aligned with ASP.NET enum values. */
export enum ProjectSourceType {
  OpcUa = 0,
  ExcelSignals = 1,
  GenericCsv = 2,
  Logs = 3,
}

export interface ProjectResponse {
  id: string;
  ownerUserId: string;
  name: string;
  description: string | null;
  sourceType: ProjectSourceType;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProjectRequest {
  name: string;
  description?: string | null;
  sourceType: ProjectSourceType;
}

export interface UpdateProjectRequest {
  name: string;
  description?: string | null;
  sourceType?: ProjectSourceType | null;
}
