namespace NodeScope.Domain.Enums;

/// <summary>
/// Lifecycle state for a single file import / analysis execution.
/// </summary>
public enum ImportJobStatus
{
    /// <summary>
    /// Import registered and waiting to start processing.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Analysis is actively running (e.g., Python processor).
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Completed successfully with artifacts available.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Failed due to validation, I/O, or pipeline error.
    /// </summary>
    Failed = 3,
}
