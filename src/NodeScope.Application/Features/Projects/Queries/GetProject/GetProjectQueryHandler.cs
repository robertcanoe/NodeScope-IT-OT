using MediatR;
using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Application.Contracts.Projects;

namespace NodeScope.Application.Features.Projects.Queries.GetProject;

public sealed class GetProjectQueryHandler(INodeScopeDbContext dbContext)
    : IRequestHandler<GetProjectQuery, ProjectResponseDto?>
{
    public async Task<ProjectResponseDto?> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await dbContext.Projects.AsNoTracking()
            .Where(p => p.Id == request.ProjectId && p.OwnerUserId == request.OwnerUserId)
            .Select(p => new ProjectResponseDto(
                p.Id,
                p.OwnerUserId,
                p.Name,
                p.Description,
                p.SourceType,
                p.CreatedAt,
                p.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
