using MediatR;
using NodeScope.Application.Contracts.Projects;

namespace NodeScope.Application.Features.Projects.Commands.UpdateProject;

public sealed record UpdateProjectCommand(Guid OwnerUserId, Guid ProjectId, UpdateProjectDto Payload) : IRequest<ProjectResponseDto?>;
