namespace NodeScope.Domain.Entities;

/// <summary>
/// Base type for persisted domain entities that share a surrogate <see cref="Guid"/> identifier.
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    public Guid Id { get; protected set; }
}
