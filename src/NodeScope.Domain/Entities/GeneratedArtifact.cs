using NodeScope.Domain.Enums;

namespace NodeScope.Domain.Entities;

/// <summary>
/// Represents a persisted file produced alongside an analysis run (report, extracts, snapshots).
/// </summary>
public sealed class GeneratedArtifact : EntityBase
{
    private GeneratedArtifact()
    {
    }

    /// <summary>
    /// Initializes a persisted artifact tracker row.
    /// </summary>
    /// <param name="importJobId">Owning import.</param>
    /// <param name="type">Logical artifact classification.</param>
    /// <param name="path">Relative or absolute filesystem path.</param>
    /// <param name="mimeType">MIME type describing the artifact bytes.</param>
    /// <param name="sizeBytes">Size on disk.</param>
    /// <param name="createdAtUtc">Materialization UTC timestamp.</param>
    public GeneratedArtifact(
        Guid importJobId,
        GeneratedArtifactType type,
        string path,
        string mimeType,
        long sizeBytes,
        DateTimeOffset createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);
        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);

        Id = Guid.NewGuid();
        ImportJobId = importJobId;
        Type = type;
        Path = path.Trim();
        MimeType = mimeType.Trim();
        SizeBytes = sizeBytes;
        CreatedAt = createdAtUtc;
    }

    /// <summary>
    /// Gets the owning import identifier.
    /// </summary>
    public Guid ImportJobId { get; private set; }

    /// <summary>
    /// Gets the parent import navigation property.
    /// </summary>
    public ImportJob ImportJob { get; private set; } = null!;

    /// <summary>
    /// Gets the enumerated artifact discriminator.
    /// </summary>
    public GeneratedArtifactType Type { get; private set; }

    /// <summary>
    /// Gets the persisted path for download or embedding.
    /// </summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the MIME classifier for browsers and gateways.
    /// </summary>
    public string MimeType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the persisted size measured in bytes.
    /// </summary>
    public long SizeBytes { get; private set; }

    /// <summary>
    /// Gets when generation completed.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }
}
