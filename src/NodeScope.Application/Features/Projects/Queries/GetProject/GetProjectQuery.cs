using MediatR;
using NodeScope.Application.Contracts.Projects;

namespace NodeScope.Application.Features.Projects.Queries.GetProject;

public sealed record GetProjectQuery(Guid OwnerUserId, Guid ProjectId) : IRequest<ProjectResponseDto?>;
