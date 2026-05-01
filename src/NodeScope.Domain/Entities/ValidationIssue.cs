using NodeScope.Domain.Enums;

namespace NodeScope.Domain.Entities;

/// <summary>
/// Describes a deterministic validation anomaly discovered during analysis.
/// </summary>
public sealed class ValidationIssue : EntityBase
{
    private ValidationIssue()
    {
    }

    /// <summary>
    /// Initializes a persisted validation incident.
    /// </summary>
    /// <param name="importJobId">Associated import identifier.</param>
    /// <param name="severity">Operational severity tier.</param>
    /// <param name="code">Stable machine-readable code for filtering.</param>
    /// <param name="message">Human-readable textual explanation.</param>
    /// <param name="columnName">Affected column label, when applicable.</param>
    /// <param name="rowIndex">Affected canonical row ordinal, when applicable.</param>
    /// <param name="rawValue">Raw cell value excerpt for auditing (keep short).</param>
    /// <param name="createdAtUtc">When the detector emitted the incident.</param>
    public ValidationIssue(
        Guid importJobId,
        ValidationSeverity severity,
        string code,
        string message,
        string? columnName,
        int? rowIndex,
        string? rawValue,
        DateTimeOffset createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Id = Guid.NewGuid();
        ImportJobId = importJobId;
        Severity = severity;
        Code = code.Trim();
        Message = message.Trim();
        ColumnName = string.IsNullOrWhiteSpace(columnName) ? null : columnName.Trim();
        RowIndex = rowIndex;
        RawValue = string.IsNullOrWhiteSpace(rawValue) ? null : rawValue.Trim();
        CreatedAt = createdAtUtc;
    }

    /// <summary>
    /// Gets the associated import identifier.
    /// </summary>
    public Guid ImportJobId { get; private set; }

    /// <summary>
    /// Gets the parent import navigation property.
    /// </summary>
    public ImportJob ImportJob { get; private set; } = null!;

    /// <summary>
    /// Gets the qualitative severity tier.
    /// </summary>
    public ValidationSeverity Severity { get; private set; }

    /// <summary>
    /// Gets the stable coded identifier for dashboards and alerting.
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the human-readable explanatory text.
    /// </summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional column attribution.
    /// </summary>
    public string? ColumnName { get; private set; }

    /// <summary>
    /// Gets the optional row attribution (canonical index pipeline).
    /// </summary>
    public int? RowIndex { get; private set; }

    /// <summary>
    /// Gets optional raw offending value excerpt for QA forensics.
    /// </summary>
    public string? RawValue { get; private set; }

    /// <summary>
    /// Gets emission timestamp recorded in UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }
}
