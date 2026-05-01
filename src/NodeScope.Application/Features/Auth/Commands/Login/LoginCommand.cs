using MediatR;
using NodeScope.Application.Contracts.Auth;

namespace NodeScope.Application.Features.Auth.Commands.Login;

/// <summary>
/// Authenticates SPA credentials and returns access token envelopes when hashes match persisted secrets.
/// </summary>
/// <param name="Request">DTO carrying operator supplied credentials.</param>
public sealed record LoginCommand(LoginRequestDto Request) : IRequest<LoginAttemptResult>;
