using NodeScope.Domain.Enums;

namespace NodeScope.Application.Contracts.Projects;

/// <summary>
/// Mutable projection for workspace renaming and scope adjustments.
/// </summary>
/// <param name="Name">Non-empty persisted label derived from SPA forms.</param>
/// <param name="Description">Optional narrative clarification.</param>
/// <param name="SourceType">Optional source-type override when operators recategorise datasets.</param>
public sealed record UpdateProjectDto(string Name, string? Description, ProjectSourceType? SourceType);
