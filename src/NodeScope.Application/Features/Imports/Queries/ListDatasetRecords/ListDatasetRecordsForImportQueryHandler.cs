using MediatR;
using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Application.Contracts.Common;
using NodeScope.Application.Contracts.Imports;

namespace NodeScope.Application.Features.Imports.Queries.ListDatasetRecords;

public sealed class ListDatasetRecordsForImportQueryHandler(INodeScopeDbContext dbContext)
    : IRequestHandler<ListDatasetRecordsForImportQuery, PagedResultDto<DatasetRecordRowDto>>
{
    /// <inheritdoc />
    public async Task<PagedResultDto<DatasetRecordRowDto>> Handle(
        ListDatasetRecordsForImportQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var owns = await dbContext.ImportJobs.AsNoTracking()
            .Join(
                dbContext.Projects.Where(p => p.OwnerUserId == request.OwnerUserId),
                j => j.ProjectId,
                p => p.Id,
                (j, _) => j.Id)
            .AnyAsync(importId => importId == request.ImportJobId, cancellationToken)
            .ConfigureAwait(false);

        if (!owns)
        {
            return new PagedResultDto<DatasetRecordRowDto>(Array.Empty<DatasetRecordRowDto>(), 0, request.Page, request.PageSize);
        }

        var query = dbContext.DatasetRecords.AsNoTracking().Where(r => r.ImportJobId == request.ImportJobId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var literal = SanitizeLike(request.Search);
            if (literal.Length > 0)
            {
                var pattern = $"%{literal}%";
                query = query.Where(r => EF.Functions.ILike(r.PayloadJson, pattern));
            }
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var skip = (request.Page - 1) * request.PageSize;

        var rows = await query
            .OrderBy(r => r.RecordIndex)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(r => new DatasetRecordRowDto(r.RecordIndex, r.PayloadJson))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResultDto<DatasetRecordRowDto>(rows, total, request.Page, request.PageSize);
    }

    private static string SanitizeLike(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.Length > 256)
        {
            trimmed = trimmed[..256];
        }

        return trimmed.Replace('%', ' ').Replace('_', ' ').Replace('\\', ' ').Trim();
    }
}
