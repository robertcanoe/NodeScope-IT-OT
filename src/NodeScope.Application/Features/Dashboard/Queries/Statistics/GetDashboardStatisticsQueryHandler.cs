using MediatR;
using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Application.Contracts.Dashboard;

namespace NodeScope.Application.Features.Dashboard.Queries.Statistics;

public sealed class GetDashboardStatisticsQueryHandler(INodeScopeDbContext dbContext)
    : IRequestHandler<GetDashboardStatisticsQuery, DashboardStatisticsDto>
{
    public async Task<DashboardStatisticsDto> Handle(GetDashboardStatisticsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var projectIdsQuery = dbContext.Projects.Where(p => p.OwnerUserId == request.OwnerUserId).Select(p => p.Id);

        var projectCount = await dbContext.Projects.CountAsync(p => p.OwnerUserId == request.OwnerUserId, cancellationToken)
            .ConfigureAwait(false);

        var importQueryable = dbContext.ImportJobs.Where(i => projectIdsQuery.Contains(i.ProjectId));

        var importCount = await importQueryable.CountAsync(cancellationToken).ConfigureAwait(false);
        var completed = await importQueryable.CountAsync(i => i.Status == Domain.Enums.ImportJobStatus.Completed, cancellationToken)
            .ConfigureAwait(false);
        var failed = await importQueryable.CountAsync(i => i.Status == Domain.Enums.ImportJobStatus.Failed, cancellationToken)
            .ConfigureAwait(false);
        var processing = await importQueryable.CountAsync(
            i => i.Status == Domain.Enums.ImportJobStatus.Processing,
            cancellationToken).ConfigureAwait(false);

        var recent = await (
                from imp in dbContext.ImportJobs
                join project in dbContext.Projects on imp.ProjectId equals project.Id
                where project.OwnerUserId == request.OwnerUserId
                orderby imp.CompletedAt ?? imp.StartedAt ?? DateTimeOffset.MinValue descending
                select new DashboardRecentImportDto(
                    imp.Id,
                    project.Id,
                    project.Name,
                    imp.OriginalFileName,
                    imp.Status.ToString(),
                    imp.CompletedAt))
            .Take(10)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new DashboardStatisticsDto(projectCount, importCount, completed, failed, processing, recent);
    }
}
