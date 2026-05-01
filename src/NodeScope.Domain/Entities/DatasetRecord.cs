namespace NodeScope.Domain.Entities;

/// <summary>
/// Stores a single logical row payload for heterogeneous schemas as JSON.
/// </summary>
public sealed class DatasetRecord : EntityBase
{
    private DatasetRecord()
    {
    }

    /// <summary>
    /// Initializes a normalized row snapshot for persistence.
    /// </summary>
    /// <param name="importJobId">Owning import identifier.</param>
    /// <param name="recordIndex">Zero-based index of this row relative to canonical ordering.</param>
    /// <param name="payloadJson">JSON blob containing the flattened record fields.</param>
    public DatasetRecord(Guid importJobId, int recordIndex, string payloadJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);
        ArgumentOutOfRangeException.ThrowIfNegative(recordIndex);

        Id = Guid.NewGuid();
        ImportJobId = importJobId;
        RecordIndex = recordIndex;
        PayloadJson = payloadJson;
    }

    /// <summary>
    /// Gets the owning import identifier.
    /// </summary>
    public Guid ImportJobId { get; private set; }

    /// <summary>
    /// Gets the owning import navigation property.
    /// </summary>
    public ImportJob ImportJob { get; private set; } = null!;

    /// <summary>
    /// Gets the deterministic row ordinal for pagination and lookups.
    /// </summary>
    public int RecordIndex { get; private set; }

    /// <summary>
    /// Gets the JSON-serialized row payload interpreted by PostgreSQL as jsonb storage.
    /// </summary>
    public string PayloadJson { get; private set; } = string.Empty;
}
