using MediatR;
using NodeScope.Application.Contracts.Common;
using NodeScope.Application.Contracts.Imports;

namespace NodeScope.Application.Features.Imports.Queries.ListDatasetRecords;

public sealed record ListDatasetRecordsForImportQuery(
    Guid OwnerUserId,
    Guid ImportJobId,
    int Page,
    int PageSize,
    string? Search) : IRequest<PagedResultDto<DatasetRecordRowDto>>;
