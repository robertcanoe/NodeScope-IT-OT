namespace NodeScope.Domain.Entities;

/// <summary>
/// Describes a detected column profile for a normalized import execution.
/// </summary>
public sealed class DatasetColumn : EntityBase
{
    private DatasetColumn()
    {
    }

    /// <summary>
    /// Initializes a new <see cref="DatasetColumn"/>.
    /// </summary>
    /// <param name="importJobId">Parent import identifier.</param>
    /// <param name="name">Original column/header name.</param>
    /// <param name="normalizedName">Normalized machine-friendly name.</param>
    /// <param name="dataTypeDetected">Heuristic datatype label as detected by profiling.</param>
    /// <param name="distinctCount">Count of distinct non-null samples used for profiling.</param>
    /// <param name="nullCount">Count of null/absent samples in the profile window.</param>
    public DatasetColumn(
        Guid importJobId,
        string name,
        string normalizedName,
        string dataTypeDetected,
        int distinctCount,
        int nullCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataTypeDetected);
        ArgumentOutOfRangeException.ThrowIfNegative(distinctCount);
        ArgumentOutOfRangeException.ThrowIfNegative(nullCount);

        Id = Guid.NewGuid();
        ImportJobId = importJobId;
        Name = name.Trim();
        NormalizedName = normalizedName.Trim();
        DataTypeDetected = dataTypeDetected.Trim();
        DistinctCount = distinctCount;
        NullCount = nullCount;
    }

    /// <summary>
    /// Gets the parent import identifier.
    /// </summary>
    public Guid ImportJobId { get; private set; }

    /// <summary>
    /// Gets the parent import navigation property.
    /// </summary>
    public ImportJob ImportJob { get; private set; } = null!;

    /// <summary>
    /// Gets the detected column/header name prior to normalization.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the normalized canonical name used for downstream checks.
    /// </summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the heuristic datatype classification (human-readable tag).
    /// </summary>
    public string DataTypeDetected { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the count of distinct values observed while profiling this column.
    /// </summary>
    public int DistinctCount { get; private set; }

    /// <summary>
    /// Gets the count of null or missing values counted while profiling this column.
    /// </summary>
    public int NullCount { get; private set; }
}
