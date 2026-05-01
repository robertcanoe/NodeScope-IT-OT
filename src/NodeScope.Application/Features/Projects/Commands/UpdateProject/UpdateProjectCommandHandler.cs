using MediatR;
using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Application.Contracts.Projects;

namespace NodeScope.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandHandler(INodeScopeDbContext dbContext)
    : IRequestHandler<UpdateProjectCommand, ProjectResponseDto?>
{
    public async Task<ProjectResponseDto?> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Payload);

        var aggregate = await dbContext.Projects
            .SingleOrDefaultAsync(
                p => p.Id == request.ProjectId && p.OwnerUserId == request.OwnerUserId,
                cancellationToken)
            .ConfigureAwait(false);

        if (aggregate is null)
        {
            return null;
        }

        aggregate.UpdateProfile(request.Payload.Name, request.Payload.Description, request.Payload.SourceType);
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ProjectResponseDto(
            aggregate.Id,
            aggregate.OwnerUserId,
            aggregate.Name,
            aggregate.Description,
            aggregate.SourceType,
            aggregate.CreatedAt,
            aggregate.UpdatedAt);
    }
}
