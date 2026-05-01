using MediatR;
using NodeScope.Application.Contracts.Auth;

namespace NodeScope.Application.Features.Auth.Queries.CurrentUser;

/// <summary>
/// Resolves authoritative profile metadata persisted for callers represented by deterministic JWT subjects.
/// </summary>
public sealed record CurrentUserQuery(Guid UserId) : IRequest<AuthenticatedUserSummaryDto?>;
