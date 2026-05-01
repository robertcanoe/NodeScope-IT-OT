using NodeScope.Domain.Enums;

namespace NodeScope.Application.Contracts.Auth;

/// <summary>
/// Minimal identity snapshot surfaced after successful JWT issuance.
/// </summary>
/// <param name="Id">User identifier.</param>
/// <param name="Email">Normalized email persisted in relational storage.</param>
/// <param name="DisplayName">Presentation friendly display name.</param>
/// <param name="Role">Authorization role enumerated in domain vocabulary.</param>
public sealed record AuthenticatedUserSummaryDto(Guid Id, string Email, string DisplayName, UserRole Role);
