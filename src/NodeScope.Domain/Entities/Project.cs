using NodeScope.Domain.Enums;

namespace NodeScope.Domain.Entities;

/// <summary>
/// Workspace container grouping imports and datasets for one technical analysis context.
/// </summary>
public sealed class Project : EntityBase
{
    private Project()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Project"/> class.
    /// </summary>
    /// <param name="ownerUserId">Identifier of the owning user.</param>
    /// <param name="name">Project name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="sourceType">Primary source kind for the project.</param>
    public Project(Guid ownerUserId, string name, string? description, ProjectSourceType sourceType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = Guid.NewGuid();
        OwnerUserId = ownerUserId;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SourceType = sourceType;
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Gets the foreign key to the owning <see cref="User"/>.
    /// </summary>
    public Guid OwnerUserId { get; private set; }

    /// <summary>
    /// Gets the owning user navigation.
    /// </summary>
    public User Owner { get; private set; } = null!;

    /// <summary>
    /// Gets the display name of the project.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets an optional longer description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the declared primary source type for imports in this project.
    /// </summary>
    public ProjectSourceType SourceType { get; private set; }

    /// <summary>
    /// Gets when the project was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets when the project was last updated (UTC).
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Gets import jobs associated with this project.
    /// </summary>
    public ICollection<ImportJob> ImportJobs { get; private set; } = new List<ImportJob>();

    /// <summary>
    /// Updates mutable project metadata and bumps <see cref="UpdatedAt"/>.
    /// </summary>
    /// <param name="name">New name.</param>
    /// <param name="description">New description, or null to clear.</param>
    /// <param name="sourceType">Optional new source classification.</param>
    public void UpdateProfile(string name, string? description, ProjectSourceType? sourceType = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (sourceType.HasValue)
        {
            SourceType = sourceType.Value;
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
