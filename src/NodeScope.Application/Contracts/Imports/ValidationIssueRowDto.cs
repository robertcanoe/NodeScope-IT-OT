namespace NodeScope.Application.Contracts.Imports;

/// <summary>
/// Single validation row for inspector grids.
/// </summary>
public sealed record ValidationIssueRowDto(
    Guid Id,
    string Severity,
    string Code,
    string Message,
    string? ColumnName,
    int? RowIndex,
    string? RawValue,
    DateTimeOffset CreatedAt);
