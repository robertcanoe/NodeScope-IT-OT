using MediatR;
using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Application.Contracts.Common;
using NodeScope.Application.Contracts.Imports;
using NodeScope.Domain.Enums;

namespace NodeScope.Application.Features.Imports.Queries.ListValidationIssues;

public sealed class ListValidationIssuesForImportQueryHandler(INodeScopeDbContext dbContext)
    : IRequestHandler<ListValidationIssuesForImportQuery, PagedResultDto<ValidationIssueRowDto>>
{
    /// <inheritdoc />
    public async Task<PagedResultDto<ValidationIssueRowDto>> Handle(
        ListValidationIssuesForImportQuery request,
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
            return new PagedResultDto<ValidationIssueRowDto>(Array.Empty<ValidationIssueRowDto>(), 0, request.Page, request.PageSize);
        }

        var query = dbContext.ValidationIssues.AsNoTracking().Where(i => i.ImportJobId == request.ImportJobId);

        if (!string.IsNullOrWhiteSpace(request.Severity)
            && Enum.TryParse<ValidationSeverity>(request.Severity, ignoreCase: true, out var severity))
        {
            query = query.Where(i => i.Severity == severity);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var literal = SanitizeLike(request.Search);
            if (literal.Length > 0)
            {
                var pattern = $"%{literal}%";
                query = query.Where(
                    i =>
                        EF.Functions.ILike(i.Message, pattern)
                        || EF.Functions.ILike(i.Code, pattern)
                        || (i.ColumnName != null && EF.Functions.ILike(i.ColumnName, pattern)));
            }
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var skip = (request.Page - 1) * request.PageSize;

        var rows = await query
            .OrderBy(i => i.Code)
            .ThenBy(i => i.RowIndex)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(
                i => new ValidationIssueRowDto(
                    i.Id,
                    i.Severity.ToString(),
                    i.Code,
                    i.Message,
                    i.ColumnName,
                    i.RowIndex,
                    i.RawValue,
                    i.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResultDto<ValidationIssueRowDto>(rows, total, request.Page, request.PageSize);
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
