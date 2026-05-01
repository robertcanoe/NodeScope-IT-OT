using NodeScope.Domain.Enums;

namespace NodeScope.Application.Contracts.Projects;

/// <summary>
/// Client-authored payload capturing metadata for a freshly created workspace project.
/// </summary>
public sealed class CreateProjectDto
{
    /// <summary>
    /// Gets or sets the human readable project designation.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets optional narrative describing ingestion intent.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the predominant technical ingest profile for telemetry alignment.
    /// </summary>
    public ProjectSourceType SourceType { get; init; }
}
