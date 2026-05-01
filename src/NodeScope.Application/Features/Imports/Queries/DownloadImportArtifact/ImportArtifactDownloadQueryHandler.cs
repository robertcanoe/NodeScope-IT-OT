using MediatR;
using NodeScope.Application.Abstractions.Files;

namespace NodeScope.Application.Features.Imports.Queries.DownloadImportArtifact;

public sealed class ImportArtifactDownloadQueryHandler(IImportArtifactPathResolver pathResolver)
    : IRequestHandler<ImportArtifactDownloadQuery, ResolvedImportArtifact?>
{
    /// <inheritdoc />
    public Task<ResolvedImportArtifact?> Handle(ImportArtifactDownloadQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return pathResolver.ResolveAsync(request.OwnerUserId, request.ImportJobId, request.Kind, cancellationToken);
    }
}
