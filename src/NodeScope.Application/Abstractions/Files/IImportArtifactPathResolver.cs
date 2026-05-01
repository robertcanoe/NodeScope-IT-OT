namespace NodeScope.Application.Abstractions.Files;

/// <summary>
/// Downloadable outputs produced for a completed import job.
/// </summary>
public enum ImportArtifactKind
{
    /// <summary>HTML report.</summary>
    ReportHtml = 0,

    /// <summary>Normalized rows as JSON.</summary>
    NormalizedJson = 1,

    /// <summary>Structured issues as CSV.</summary>
    IssuesCsv = 2,
}

/// <summary>
/// Resolved file on disk with MIME type and suggested download name for API file responses.
/// </summary>
public sealed record ResolvedImportArtifact(string AbsolutePhysicalPath, string ContentType, string DownloadFileName);

/// <summary>
/// Locates import artifacts for authorized owners without path traversal outside the configured storage root.
/// </summary>
public interface IImportArtifactPathResolver
{
    Task<ResolvedImportArtifact?> ResolveAsync(
        Guid ownerUserId,
        Guid importJobId,
        ImportArtifactKind kind,
        CancellationToken cancellationToken);
}
