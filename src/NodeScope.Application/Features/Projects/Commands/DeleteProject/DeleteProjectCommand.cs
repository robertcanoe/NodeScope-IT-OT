using MediatR;

namespace NodeScope.Application.Features.Projects.Commands.DeleteProject;

public sealed record DeleteProjectCommand(Guid OwnerUserId, Guid ProjectId) : IRequest<bool>;
