namespace NodeScope.Domain.Enums;

/// <summary>
/// Application-level role for authorization and ownership rules.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Standard user with access to owned projects.
    /// </summary>
    User = 0,

    /// <summary>
    /// Administrator with elevated privileges.
    /// </summary>
    Admin = 1,
}
