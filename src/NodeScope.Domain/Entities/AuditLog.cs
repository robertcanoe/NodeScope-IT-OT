namespace NodeScope.Domain.Entities;

/// <summary>
/// Captures non-repudiation events for security-sensitive operations.
/// </summary>
public sealed class AuditLog : EntityBase
{
    private AuditLog()
    {
    }

    /// <summary>
    /// Initializes an audit record.
    /// </summary>
    /// <param name="userId">Acting user, when known.</param>
    /// <param name="projectId">Related project scope, when applicable.</param>
    /// <param name="action">Verb-style action key (e.g., &quot;Project.Created&quot;).</param>
    /// <param name="targetType">Domain type name for the affected aggregate or entity.</param>
    /// <param name="targetId">Identifier of the affected domain object, when strongly typed.</param>
    /// <param name="metadataJson">Additional structured details stored as JSON.</param>
    /// <param name="createdAtUtc">Event time in UTC.</param>
    public AuditLog(
        Guid? userId,
        Guid? projectId,
        string action,
        string? targetType,
        Guid? targetId,
        string? metadataJson,
        DateTimeOffset createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        Id = Guid.NewGuid();
        UserId = userId;
        ProjectId = projectId;
        Action = action.Trim();
        TargetType = string.IsNullOrWhiteSpace(targetType) ? null : targetType.Trim();
        TargetId = targetId;
        MetadataJson = metadataJson;
        CreatedAt = createdAtUtc;
    }

    /// <summary>
    /// Gets the acting user identifier, if the action is attributable.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Gets the optional related user navigation.
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Gets the associated project scope identifier, if any.
    /// </summary>
    public Guid? ProjectId { get; private set; }

    /// <summary>
    /// Gets the optional related project navigation.
    /// </summary>
    public Project? Project { get; private set; }

    /// <summary>
    /// Gets the stable action key recorded for analytics and investigations.
    /// </summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the logical type name of the affected target.
    /// </summary>
    public string? TargetType { get; private set; }

    /// <summary>
    /// Gets the optional strongly typed target identifier.
    /// </summary>
    public Guid? TargetId { get; private set; }

    /// <summary>
    /// Gets optional JSON metadata for extended forensics.
    /// </summary>
    public string? MetadataJson { get; private set; }

    /// <summary>
    /// Gets when the audit record was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }
}
