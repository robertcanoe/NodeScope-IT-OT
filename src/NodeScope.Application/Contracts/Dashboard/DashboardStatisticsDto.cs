namespace NodeScope.Application.Contracts.Dashboard;

/// <summary>
/// Lightweight workspace telemetry for SPA dashboard tiles sourced from tenancy-scoped aggregates.
/// </summary>
/// <param name="ProjectCount">Total projects belonging to caller.</param>
/// <param name="ImportCount">Total imports across those projects.</param>
/// <param name="CompletedCount">Imports that finished successfully.</param>
/// <param name="FailedCount">Imports terminally failed.</param>
/// <param name="ProcessingCount">Imports currently leased by workers.</param>
/// <param name="RecentImports">Most recent ingestion attempts for forensic quick-jump UX.</param>
public sealed record DashboardStatisticsDto(
    int ProjectCount,
    int ImportCount,
    int CompletedCount,
    int FailedCount,
    int ProcessingCount,
    IReadOnlyList<DashboardRecentImportDto> RecentImports);

/// <summary>
/// Narrow projection aligning dashboard tables with ingestion workspace navigation.
/// </summary>
/// <param name="ImportId">Import identifier usable for SPA drill-down routes.</param>
/// <param name="ProjectId">Parent project linkage.</param>
/// <param name="ProjectName">Human workspace label derived from aggregates.</param>
/// <param name="OriginalFileName">Uploaded filename.</param>
/// <param name="Status">Normalized lifecycle sentinel.</param>
/// <param name="CompletedAt">Completion UTC instant when available.</param>
public sealed record DashboardRecentImportDto(
    Guid ImportId,
    Guid ProjectId,
    string ProjectName,
    string OriginalFileName,
    string Status,
    DateTimeOffset? CompletedAt);
