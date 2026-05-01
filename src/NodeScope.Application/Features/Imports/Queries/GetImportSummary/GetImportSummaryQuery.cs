using MediatR;
using NodeScope.Application.Contracts.Imports;

namespace NodeScope.Application.Features.Imports.Queries.GetImportSummary;

/// <summary>
/// Returns ownership-validated ingestion metrics with optional enrichment from persisted summary JSON payloads.
/// </summary>
/// <param name="OwnerUserId">Caller subject guarding tenancy boundaries.</param>
/// <param name="ImportId">Target import surrogate key emitted by ingestion flows.</param>
public sealed record GetImportSummaryQuery(Guid OwnerUserId, Guid ImportId) : IRequest<ImportJobSummaryDto?>;
