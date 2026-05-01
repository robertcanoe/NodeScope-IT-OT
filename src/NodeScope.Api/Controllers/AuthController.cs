using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodeScope.Api.Extensions;
using NodeScope.Application.Contracts.Auth;
using NodeScope.Application.Features.Auth.Commands.Login;
using NodeScope.Application.Features.Auth.Queries.CurrentUser;

namespace NodeScope.Api.Controllers;

/// <summary>
/// Exposes operator authentication primitives for SPA clients exchanging credentials against NodeScope backends.
/// </summary>
/// <param name="mediator">MediatR entry point bridging HTTP contracts to CQRS pipelines.</param>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Returns the persisted profile for the authenticated subject.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthenticatedUserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticatedUserSummaryDto>> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var profile = await mediator.Send(new CurrentUserQuery(userId), cancellationToken).ConfigureAwait(false);
        return profile is null ? Unauthorized() : Ok(profile);
    }

    /// <summary>
    /// Validates password materials and returns signed JWT envelopes when hashing outcomes succeed.
    /// </summary>
    /// <param name="request">JSON payload originating from SPA login dialogs.</param>
    /// <param name="cancellationToken">HTTP cancellation token bridging Kestrel to MediatR.</param>
    /// <returns>Normalized authentication payload serialized as JSON plus HTTP 401 sentinel on invalid attempts.</returns>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> LoginAsync(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var outcome = await mediator.Send(new LoginCommand(request), cancellationToken).ConfigureAwait(false);
        return outcome.Succeeded && outcome.Response is not null ? Ok(outcome.Response) : Unauthorized();
    }
}
