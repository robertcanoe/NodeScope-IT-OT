using MediatR;
using NodeScope.Application.Contracts.Imports;

namespace NodeScope.Application.Features.Imports.Queries.CompareImports;

public sealed record CompareImportsQuery(Guid OwnerUserId, Guid ProjectId, Guid LeftImportId, Guid RightImportId)
    : IRequest<CompareImportsResponseDto?>;
