namespace NodeScope.Api.Configuration;

/// <summary>
/// Describes signing material and issuance metadata for SPA-friendly JWT Bearer authentication.
/// </summary>
/// <remarks>
/// Never commit production signing keys. Prefer environment variables plus secret managers for non-local tiers.
/// </remarks>
public sealed class JwtSettings
{
    /// <summary>
    /// Binding section name tied to appsettings payloads.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Gets the logical issuer surfaced inside validated tokens (<c>iss</c>).
    /// </summary>
    [System.ComponentModel.DataAnnotations.Required]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Gets the intended audience surfaced inside validated tokens (<c>aud</c>).
    /// </summary>
    [System.ComponentModel.DataAnnotations.Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Gets the symmetric key material used both for issuance and Bearer validation (<c>HMACSHA256</c>).
    /// </summary>
    /// <remarks>Minimum entropy requirements are asserted through <see cref="ValidateSigningKeyEntropy"/>.</remarks>
    [System.ComponentModel.DataAnnotations.Required]
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets the absolute TTL for access tokens in minutes—short lived per NodeScope threat model assumptions.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Range(1, 1440)]
    public int AccessTokenLifetimeMinutes { get; init; } = 60;

    /// <summary>
    /// Performs lightweight validation for development-time misconfiguration guarding.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the signing key violates minimum cryptographic strength assumptions.</exception>
    public void ValidateSigningKeyEntropy()
    {
        if (string.IsNullOrWhiteSpace(SigningKey) || SigningKey.Trim().Length < 32)
        {
            throw new InvalidOperationException("Jwt SigningKey must be at least 32 characters to satisfy symmetric key entropy requirements.");
        }
    }

    /// <summary>
    /// Validates that mandatory issuer metadata is hydrated from configuration bindings.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when required settings are absent.</exception>
    public void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("Jwt Issuer binding is missing.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Jwt Audience binding is missing.");
        }

        ValidateSigningKeyEntropy();

        if (AccessTokenLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt AccessTokenLifetimeMinutes must be a positive duration.");
        }
    }
}
