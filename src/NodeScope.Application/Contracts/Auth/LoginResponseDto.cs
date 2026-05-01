namespace NodeScope.Application.Contracts.Auth;

/// <summary>
/// Successful authentication envelope returned to SPA clients exchanging credentials.
/// </summary>
/// <param name="AccessToken">Compact bearer token.</param>
/// <param name="ExpiresUtc">Absolute JWT expiration instant (UTC).</param>
/// <param name="User">Basic profile metadata for hydrating client session stores.</param>
public sealed record LoginResponseDto(string AccessToken, DateTimeOffset ExpiresUtc, AuthenticatedUserSummaryDto User);
