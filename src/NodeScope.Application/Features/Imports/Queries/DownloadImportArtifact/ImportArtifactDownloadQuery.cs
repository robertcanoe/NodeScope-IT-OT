using MediatR;
using NodeScope.Application.Abstractions.Files;

namespace NodeScope.Application.Features.Imports.Queries.DownloadImportArtifact;

public sealed record ImportArtifactDownloadQuery(
    Guid OwnerUserId,
    Guid ImportJobId,
    ImportArtifactKind Kind) : IRequest<ResolvedImportArtifact?>;
