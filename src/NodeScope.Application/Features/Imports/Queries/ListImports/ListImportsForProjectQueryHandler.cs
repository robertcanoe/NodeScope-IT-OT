using MediatR;
using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Application.Contracts.Imports;
using NodeScope.Domain.Enums;

namespace NodeScope.Application.Features.Imports.Queries.ListImports;

public sealed class ListImportsForProjectQueryHandler(INodeScopeDbContext dbContext)
    : IRequestHandler<ListImportsForProjectQuery, IReadOnlyList<ImportJobSummaryDto>>
{
    public async Task<IReadOnlyList<ImportJobSummaryDto>> Handle(ListImportsForProjectQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ownedProject = await dbContext.Projects.AsNoTracking().AnyAsync(
                p => p.Id == request.ProjectId && p.OwnerUserId == request.OwnerUserId,
                cancellationToken)
            .ConfigureAwait(false);

        if (!ownedProject)
        {
            return Array.Empty<ImportJobSummaryDto>();
        }

        return await dbContext.ImportJobs.AsNoTracking()
            .Where(j => j.ProjectId == request.ProjectId)
            .OrderByDescending(j => j.StartedAt ?? j.CompletedAt)
            .Select(
                j => new ImportJobSummaryDto(
                    j.Id,
                    j.ProjectId,
                    j.OriginalFileName,
                    j.Status,
                    j.RowCount,
                    j.IssueCount,
                    null,
                    null,
                    j.CompletedAt,
                    j.FailureMessage))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
