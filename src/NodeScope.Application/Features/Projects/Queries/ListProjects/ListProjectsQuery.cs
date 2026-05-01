using MediatR;
using NodeScope.Application.Contracts.Projects;

namespace NodeScope.Application.Features.Projects.Queries.ListProjects;

/// <summary>
/// Lists ingestion workspaces anchored to deterministic JWT tenancy subjects sorted by freshest mutation timestamps.
/// </summary>
/// <param name="OwnerUserId">Caller subject identifier derived from Bearer claims orchestration pipelines.</param>
public sealed record ListProjectsQuery(Guid OwnerUserId) : IRequest<IReadOnlyList<ProjectResponseDto>>;
