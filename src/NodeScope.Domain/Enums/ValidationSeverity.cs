namespace NodeScope.Domain.Enums;

/// <summary>
/// Severity for a structured validation finding on imported technical data.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational observation.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Potential problem that should be reviewed.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Blocking or critical issue for the dataset.
    /// </summary>
    Error = 2,
}
