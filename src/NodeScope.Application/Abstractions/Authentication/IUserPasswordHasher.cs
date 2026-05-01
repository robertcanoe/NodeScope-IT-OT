using NodeScope.Domain.Entities;

namespace NodeScope.Application.Abstractions.Authentication;

/// <summary>
/// Domain-friendly password cryptography operations implemented by ASP.NET-compatible infrastructure.
/// </summary>
public interface IUserPasswordHasher
{
    /// <summary>
    /// Creates a cryptographic hash representation for long-term persistence.
    /// </summary>
    /// <param name="user">The user entity owning the hashed secret.</param>
    /// <param name="plaintext">The raw password provided by an operator.</param>
    /// <returns>The encoded credential hash.</returns>
    string Hash(User user, string plaintext);

    /// <summary>
    /// Verifies the supplied plaintext against the persisted hash snapshot.
    /// </summary>
    /// <param name="user">The user owning the cryptographic metadata.</param>
    /// <param name="plaintext">The plaintext candidate password.</param>
    /// <param name="storedHash">Previously persisted hashed secret.</param>
    /// <returns><c>true</c> when the candidate matches acceptable verification outcomes.</returns>
    bool Verify(User user, string plaintext, string storedHash);
}
