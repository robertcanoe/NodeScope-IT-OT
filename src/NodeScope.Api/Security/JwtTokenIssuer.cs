using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NodeScope.Api.Configuration;
using NodeScope.Application.Abstractions.Authentication;
using NodeScope.Domain.Enums;

namespace NodeScope.Api.Security;

/// <summary>
/// Host-supplied JWT factory implementing the application abstraction for issuing bearer tokens consumed by SPA clients.
/// </summary>
/// <param name="jwtOptions">Validated strongly typed configuration binder.</param>
public sealed class JwtTokenIssuer(IOptions<JwtSettings> jwtOptions) : IJwtTokenIssuer
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    /// <inheritdoc />
    public JwtAccessTokenIssueResult IssueAccessToken(
        Guid userId,
        string email,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var settings = jwtOptions.Value;
        settings.EnsureConfigured();

        var keyBytes = Encoding.UTF8.GetBytes(settings.SigningKey);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var utcNowOffset = DateTimeOffset.UtcNow;
        var utcExpiresOffset = utcNowOffset.AddMinutes(settings.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString("D")),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.NameIdentifier, userId.ToString("D")),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: utcNowOffset.UtcDateTime,
            expires: utcExpiresOffset.UtcDateTime,
            signingCredentials: credentials);

        return new JwtAccessTokenIssueResult(
            Token: _tokenHandler.WriteToken(token),
            ExpiresUtc: utcExpiresOffset);
    }
}
