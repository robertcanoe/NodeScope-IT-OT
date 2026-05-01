using MediatR;
using NodeScope.Application.Contracts.Common;
using NodeScope.Application.Contracts.Imports;

namespace NodeScope.Application.Features.Imports.Queries.ListValidationIssues;

public sealed record ListValidationIssuesForImportQuery(
    Guid OwnerUserId,
    Guid ImportJobId,
    int Page,
    int PageSize,
    string? Severity,
    string? Search) : IRequest<PagedResultDto<ValidationIssueRowDto>>;
