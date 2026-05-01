using MediatR;
using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Application.Contracts.Projects;

namespace NodeScope.Application.Features.Projects.Queries.ListProjects;

public sealed class ListProjectsQueryHandler(INodeScopeDbContext dbContext)
    : IRequestHandler<ListProjectsQuery, IReadOnlyList<ProjectResponseDto>>
{
    public async Task<IReadOnlyList<ProjectResponseDto>> Handle(ListProjectsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await dbContext.Projects.AsNoTracking()
            .Where(p => p.OwnerUserId == request.OwnerUserId)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new ProjectResponseDto(
                p.Id,
                p.OwnerUserId,
                p.Name,
                p.Description,
                p.SourceType,
                p.CreatedAt,
                p.UpdatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
