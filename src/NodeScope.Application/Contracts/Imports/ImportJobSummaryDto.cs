using NodeScope.Domain.Enums;

namespace NodeScope.Application.Contracts.Imports;

/// <summary>
/// Import projection returned after ingestion registration or poll-friendly summary reads.
/// </summary>
/// <param name="Id">Import surrogate key.</param>
/// <param name="ProjectId">Attached workspace aggregate.</param>
/// <param name="OriginalFileName">Uploaded filename sanitized for auditing.</param>
/// <param name="Status">Current lifecycle sentinel.</param>
/// <param name="RowCount">Counted rows upon completion.</param>
/// <param name="IssueCount">Structured validation cardinality.</param>
/// <param name="DominantType">Dominant heuristic datatype surfaced from Python summaries.</param>
/// <param name="DominantNamespace">Dominant namespace fragment when OPC UA heuristic applies.</param>
/// <param name="CompletedAt">Completion instant when finalized.</param>
public sealed record ImportJobSummaryDto(
    Guid Id,
    Guid ProjectId,
    string OriginalFileName,
    ImportJobStatus Status,
    int? RowCount,
    int? IssueCount,
    string? DominantType,
    string? DominantNamespace,
    DateTimeOffset? CompletedAt);
