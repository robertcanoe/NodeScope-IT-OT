using NodeScope.Domain.Enums;

namespace NodeScope.Application.Abstractions.Authentication;

/// <summary>
/// Host-side contract for emitting signed JWT access tokens consumed by SPA clients.
/// </summary>
public interface IJwtTokenIssuer
{
    /// <summary>
    /// Builds a bearer token representing the authenticated subject claims.
    /// </summary>
    /// <param name="userId">Primary subject identifier.</param>
    /// <param name="email">Normalized email leveraged for auditing and identity correlation.</param>
    /// <param name="role">Domain role surfaced as JWT role claims.</param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    /// <returns>Serialized token envelope with expiration metadata.</returns>
    JwtAccessTokenIssueResult IssueAccessToken(
        Guid userId,
        string email,
        UserRole role,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents issuance metadata for SPA-friendly bearer authentication.
/// </summary>
/// <param name="Token">Base64-encoded compact JWT serialization.</param>
/// <param name="ExpiresUtc">Absolute expiry instant in UTC.</param>
public sealed record JwtAccessTokenIssueResult(string Token, DateTimeOffset ExpiresUtc);
