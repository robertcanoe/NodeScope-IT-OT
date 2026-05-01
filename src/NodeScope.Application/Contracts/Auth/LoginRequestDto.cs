namespace NodeScope.Application.Contracts.Auth;

/// <summary>
/// Request transport for interactive login attempts coming from SPA clients.
/// </summary>
public sealed class LoginRequestDto
{
    /// <summary>
    /// Gets or sets the user email address credential.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the plaintext password entered by an operator during login.
    /// </summary>
    public string Password { get; init; } = string.Empty;
}
