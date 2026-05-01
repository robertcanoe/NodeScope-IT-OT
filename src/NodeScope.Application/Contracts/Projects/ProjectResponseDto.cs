using NodeScope.Domain.Enums;

namespace NodeScope.Application.Contracts.Projects;

/// <summary>
/// Response projection returned after provisioning a persisted <see cref="Domain.Entities.Project"/> aggregate.
/// </summary>
/// <param name="Id">Project identifier assigned by relational storage.</param>
/// <param name="OwnerUserId">Owning subject responsible for tenancy boundaries.</param>
/// <param name="Name">Project display name persisted verbatim after trimming.</param>
/// <param name="Description">Optional narrative.</param>
/// <param name="SourceType">Enumerated ingestion profile mirrored from domain enums.</param>
/// <param name="CreatedAt">Creation UTC timestamp.</param>
/// <param name="UpdatedAt">Last mutation UTC timestamp.</param>
public sealed record ProjectResponseDto(
    Guid Id,
    Guid OwnerUserId,
    string Name,
    string? Description,
    ProjectSourceType SourceType,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
