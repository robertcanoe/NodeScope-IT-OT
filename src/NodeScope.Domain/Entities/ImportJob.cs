using NodeScope.Domain.Enums;

namespace NodeScope.Domain.Entities;

/// <summary>
/// Tracks a single file upload through analysis, persistence, and artifact generation.
/// </summary>
public sealed class ImportJob : EntityBase
{
    private ImportJob()
    {
    }

    /// <summary>
    /// Initializes a new import job in <see cref="ImportJobStatus.Pending"/> state.
    /// </summary>
    /// <param name="projectId">Parent project identifier.</param>
    /// <param name="originalFileName">Filename provided by the client.</param>
    /// <param name="storedFilePath">Relative path under the configured storage root (stable before bytes are written).</param>
    /// <param name="explicitId">Optional deterministic id so paths and blobs share the same key.</param>
    public ImportJob(Guid projectId, string originalFileName, string storedFilePath, Guid? explicitId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(storedFilePath);

        Id = explicitId ?? Guid.NewGuid();
        ProjectId = projectId;
        OriginalFileName = originalFileName.Trim();
        StoredFilePath = storedFilePath.Trim();
        Status = ImportJobStatus.Pending;
    }

    /// <summary>
    /// Gets the parent project identifier.
    /// </summary>
    public Guid ProjectId { get; private set; }

    /// <summary>
    /// Gets the parent project navigation.
    /// </summary>
    public Project Project { get; private set; } = null!;

    /// <summary>
    /// Gets the original client file name (sanitized for length at infrastructure layer).
    /// </summary>
    public string OriginalFileName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the persisted storage path for the uploaded file.
    /// </summary>
    public string StoredFilePath { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the current lifecycle status.
    /// </summary>
    public ImportJobStatus Status { get; private set; }

    /// <summary>
    /// Gets when processing started, if applicable.
    /// </summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>
    /// Gets when processing finished successfully or failed, if applicable.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Gets the processor/Python pipeline version stamp used for reproducibility.
    /// </summary>
    public string? ProcessorVersion { get; private set; }

    /// <summary>
    /// Gets the number of rows detected after normalization, when known.
    /// </summary>
    public int? RowCount { get; private set; }

    /// <summary>
    /// Gets the validation issue count for quick dashboard metrics.
    /// </summary>
    public int? IssueCount { get; private set; }

    /// <summary>
    /// Gets the filesystem path for the rendered HTML report, when available.
    /// </summary>
    public string? ReportHtmlPath { get; private set; }

    /// <summary>
    /// Gets the filesystem path for canonical normalized JSON artifact, when available.
    /// </summary>
    public string? NormalizedJsonPath { get; private set; }

    /// <summary>
    /// Gets compact summary JSON persisted for API responses without loading full payloads.
    /// </summary>
    public string? SummaryJson { get; private set; }

    /// <summary>
    /// Gets a human-readable failure reason when <see cref="Status"/> is <see cref="ImportJobStatus.Failed"/>.
    /// </summary>
    public string? FailureMessage { get; private set; }

    /// <summary>
    /// Gets column profiles derived from this import.
    /// </summary>
    public ICollection<DatasetColumn> DatasetColumns { get; private set; } = new List<DatasetColumn>();

    /// <summary>
    /// Gets normalized row payloads for heterogeneous schemas.
    /// </summary>
    public ICollection<DatasetRecord> DatasetRecords { get; private set; } = new List<DatasetRecord>();

    /// <summary>
    /// Gets structured validation issues raised for this execution.
    /// </summary>
    public ICollection<ValidationIssue> ValidationIssues { get; private set; } = new List<ValidationIssue>();

    /// <summary>
    /// Gets generated artifacts tracked in the relational model.
    /// </summary>
    public ICollection<GeneratedArtifact> GeneratedArtifacts { get; private set; } = new List<GeneratedArtifact>();

    /// <summary>
    /// Transitions from <see cref="ImportJobStatus.Pending"/> to <see cref="ImportJobStatus.Processing"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">When status is not pending.</exception>
    public void MarkProcessing(DateTimeOffset? startedAtUtc = null)
    {
        if (Status != ImportJobStatus.Pending)
        {
            throw new InvalidOperationException("Only pending imports can start processing.");
        }

        Status = ImportJobStatus.Processing;
        StartedAt = startedAtUtc ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks completion with persisted metrics and artifact locations.
    /// </summary>
    /// <param name="processorVersion">Optional pipeline/version label.</param>
    /// <param name="rowCount">Total rows counted.</param>
    /// <param name="issueCount">Total structured issues counted.</param>
    /// <param name="reportHtmlPath">Optional report path.</param>
    /// <param name="normalizedJsonPath">Optional JSON path.</param>
    /// <param name="summaryJson">Compact summary JSON blob.</param>
    /// <param name="completedAtUtc">Explicit completion timestamp.</param>
    public void MarkCompleted(
        string? processorVersion,
        int rowCount,
        int issueCount,
        string? reportHtmlPath,
        string? normalizedJsonPath,
        string? summaryJson,
        DateTimeOffset? completedAtUtc = null)
    {
        if (Status != ImportJobStatus.Processing)
        {
            throw new InvalidOperationException("Only imports in processing state can complete.");
        }

        ProcessorVersion = processorVersion?.Trim();
        RowCount = rowCount;
        IssueCount = issueCount;
        ReportHtmlPath = NormalizeOptionalPath(reportHtmlPath);
        NormalizedJsonPath = NormalizeOptionalPath(normalizedJsonPath);
        SummaryJson = summaryJson;
        FailureMessage = null;
        Status = ImportJobStatus.Completed;
        CompletedAt = completedAtUtc ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks failure and optionally clears partial artifact pointers according to orchestration policy.
    /// </summary>
    /// <param name="failureMessage">Optional diagnostic text for operators and the SPA.</param>
    /// <param name="completedAtUtc">Failure timestamp.</param>
    public void MarkFailed(string? failureMessage = null, DateTimeOffset? completedAtUtc = null)
    {
        if (Status is ImportJobStatus.Completed)
        {
            throw new InvalidOperationException("Completed imports cannot transition to failed.");
        }

        FailureMessage = NormalizeFailureMessage(failureMessage);
        Status = ImportJobStatus.Failed;
        CompletedAt = completedAtUtc ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Clears completion metadata so the ingestion worker can rerun the processor against the stored upload file.
    /// </summary>
    /// <remarks>Relational children (columns, sampled records, issues, artifact rows) should be deleted by the orchestrator beforehand.</remarks>
    public void ResetForReprocessing()
    {
        if (Status != ImportJobStatus.Completed && Status != ImportJobStatus.Failed)
        {
            throw new InvalidOperationException("Only completed or failed imports can be requeued for processing.");
        }

        Status = ImportJobStatus.Pending;
        StartedAt = null;
        CompletedAt = null;
        ProcessorVersion = null;
        RowCount = null;
        IssueCount = null;
        ReportHtmlPath = null;
        NormalizedJsonPath = null;
        SummaryJson = null;
        FailureMessage = null;
    }

    private static string? NormalizeFailureMessage(string? failureMessage)
    {
        if (string.IsNullOrWhiteSpace(failureMessage))
        {
            return null;
        }

        const int maxChars = 16_384;
        var trimmed = failureMessage.Trim();
        return trimmed.Length <= maxChars ? trimmed : trimmed[..maxChars];
    }

    private static string? NormalizeOptionalPath(string? path)
    {
        return string.IsNullOrWhiteSpace(path) ? null : path.Trim();
    }
}
