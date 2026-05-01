using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NodeScope.Api.Extensions;

/// <summary>
/// Helps controllers extract deterministic subject identifiers from validated JWT Bearer claims maps.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Attempts to parse the JWT <c>sub</c>/<c>NameIdentifier</c> claim payload into <see cref="Guid"/>.
    /// </summary>
    /// <param name="principal">The authenticated caller principal injected by ASP.NET Core.</param>
    /// <param name="userId">Outputs the parsed subject Guid when decoding succeeds.</param>
    /// <returns><c>true</c> when the claim resolves to <see cref="Guid.TryParse(Guid, out Guid)"/> success.</returns>
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        ArgumentNullException.ThrowIfNull(principal);

        userId = Guid.Empty;
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out userId);
    }

    /// <summary>
    /// Extracts mandatory subject identifiers or throws deterministic <see cref="UnauthorizedAccessException"/> instances.
    /// </summary>
    /// <param name="principal">The authenticated caller.</param>
    /// <returns>The parsed subject Guid.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when subject claims are missing.</exception>
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        if (!principal.TryGetUserId(out var userId) || userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Bearer token lacks a deterministic subject identifier claim.");
        }

        return userId;
    }
}
