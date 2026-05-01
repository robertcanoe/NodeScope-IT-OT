using MediatR;
using NodeScope.Application.Contracts.Imports;

namespace NodeScope.Application.Features.Imports.Queries.ListImports;

public sealed record ListImportsForProjectQuery(Guid OwnerUserId, Guid ProjectId) : IRequest<IReadOnlyList<ImportJobSummaryDto>>;
