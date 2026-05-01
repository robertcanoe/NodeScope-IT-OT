using Microsoft.AspNetCore.Identity;
using NodeScope.Application.Abstractions.Authentication;
using NodeScope.Domain.Entities;

namespace NodeScope.Infrastructure.Identity;

/// <summary>
/// Bridges Identity password primitives with the application's domain hashing contract while keeping cryptography consistent with ASP.NET guidance.
/// </summary>
/// <remarks>
/// The implementation intentionally stays singleton-safe because underlying <see cref="PasswordHasher{TUser}"/> is thread-safe stateless cryptography.
/// </remarks>
public sealed class AspNetCompatibleUserPasswordHasher : IUserPasswordHasher
{
    /// <inheritdoc />
    public string Hash(User user, string plaintext)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);

        return InnerHasher.HashPassword(user, plaintext);
    }

    /// <inheritdoc />
    public bool Verify(User user, string plaintext, string storedHash)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        ArgumentException.ThrowIfNullOrWhiteSpace(storedHash);

        var outcome = InnerHasher.VerifyHashedPassword(user, storedHash, plaintext);
        return outcome is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    private static readonly PasswordHasher<User> InnerHasher = new();
}
