namespace NodeScope.Application.Contracts.Imports;

/// <summary>
/// One side of a two-import comparison pane.
/// </summary>
public sealed record ImportComparisonSideDto(
    Guid ImportId,
    string OriginalFileName,
    string Status,
    int? RowCount,
    int? IssueCount,
    string? DominantType,
    string? DominantNamespace,
    IReadOnlyList<string> IssueCodesDistinct);

/// <summary>
/// Operational diff between two ingestions in the same workspace.
/// </summary>
public sealed record CompareImportsResponseDto(
    ImportComparisonSideDto Left,
    ImportComparisonSideDto Right,
    int? RowCountDelta,
    int? IssueCountDelta,
    IReadOnlyList<string> IssueCodesOnlyInLeft,
    IReadOnlyList<string> IssueCodesOnlyInRight);
