using MediatR;
using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Authentication;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Application.Contracts.Auth;

namespace NodeScope.Application.Features.Auth.Commands.Login;

/// <summary>
/// Coordinates cryptographic verification plus JWT issuance for interactive sessions.
/// </summary>
/// <remarks>
/// This handler purposely returns <see cref="LoginAttemptResult"/> instead of throwing to avoid unintentional leakage of stack traces for invalid credential attempts at the API tier.
/// </remarks>
/// <param name="dbContext">Relational facade for querying stored users.</param>
/// <param name="passwordHasher">ASP.NET-compatible password hashing adapter.</param>
/// <param name="tokenIssuer">Host supplied JWT issuance implementation.</param>
public sealed class LoginCommandHandler(
    INodeScopeDbContext dbContext,
    IUserPasswordHasher passwordHasher,
    IJwtTokenIssuer tokenIssuer)
    : IRequestHandler<LoginCommand, LoginAttemptResult>
{
    /// <inheritdoc />
    public async Task<LoginAttemptResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Request);

        var normalizedEmail = request.Request.Email.Trim().ToLowerInvariant();

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return LoginAttemptResult.Fail();
        }

        if (!passwordHasher.Verify(user, request.Request.Password, user.PasswordHash))
        {
            return LoginAttemptResult.Fail();
        }

        var token = tokenIssuer.IssueAccessToken(user.Id, user.Email, user.Role, cancellationToken);

        var response = new LoginResponseDto(
            AccessToken: token.Token,
            ExpiresUtc: token.ExpiresUtc,
            User: new AuthenticatedUserSummaryDto(user.Id, user.Email, user.DisplayName, user.Role));

        return LoginAttemptResult.Ok(response);
    }
}
