using NodeScope.Domain.Enums;

namespace NodeScope.Domain.Entities;

/// <summary>
/// Represents an authenticated user who owns projects and triggers imports.
/// </summary>
public sealed class User : EntityBase
{
    private User()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="email">Unique email address (will be normalized).</param>
    /// <param name="passwordHash">Stored password hash (never the plain password).</param>
    /// <param name="displayName">Human-readable display name.</param>
    /// <param name="role">Role used for authorization.</param>
    public User(string email, string passwordHash, string displayName, UserRole role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Id = Guid.NewGuid();
        Email = NormalizeEmail(email);
        PasswordHash = passwordHash;
        DisplayName = displayName.Trim();
        Role = role;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the normalized unique email address.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the password hash persisted for credential verification.
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the user's display name.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the user's role for authorization checks.
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the user was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp of the last successful login, if any.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; private set; }

    /// <summary>
    /// Gets the projects owned by this user (aggregate navigation).
    /// </summary>
    public ICollection<Project> OwnedProjects { get; private set; } = new List<Project>();

    /// <summary>
    /// Records a successful authentication event by updating last login timestamp.
    /// </summary>
    /// <returns>The current instance for chaining in tests.</returns>
    public User RecordSuccessfulLogin(DateTimeOffset? atUtc = null)
    {
        LastLoginAt = atUtc ?? DateTimeOffset.UtcNow;
        return this;
    }

    /// <summary>
    /// Replaces the persisted password hash (e.g., after rotation).
    /// </summary>
    /// <param name="passwordHash">The new hash value.</param>
    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
