using NodeScope.Application.Contracts.Auth;

namespace NodeScope.Application.Features.Auth.Commands.Login;

/// <summary>
/// Mediatr-friendly authentication outcome distinguishing success from generic invalid attempts.
/// </summary>
/// <param name="Succeeded"><c>true</c> when the login handler produced a bearer token envelope.</param>
/// <param name="Response">Non-null bearer payload describing the authenticated principal.</param>
public sealed record LoginAttemptResult(bool Succeeded, LoginResponseDto? Response)
{
    /// <summary>
    /// Creates a deterministic failure sentinel without leaking reason strings to API consumers.
    /// </summary>
    /// <returns>Login failure placeholder.</returns>
    public static LoginAttemptResult Fail() => new(false, null);

    /// <summary>
    /// Creates a success sentinel carrying issued tokens.
    /// </summary>
    /// <param name="response">Hydrated SPA friendly login payload.</param>
    /// <returns>Structured success envelope.</returns>
    public static LoginAttemptResult Ok(LoginResponseDto response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new LoginAttemptResult(true, response);
    }
}
