using MediatR;
using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Application.Abstractions.Files;
using NodeScope.Application.Contracts.Imports;
using NodeScope.Domain.Entities;
using NodeScope.Domain.Enums;

namespace NodeScope.Application.Features.Imports.Commands.CreateImport;

/// <summary>
/// Persists import aggregates and uploads raw payloads before handing off to hosted python orchestration services.
/// </summary>
public sealed class CreateImportJobCommandHandler(INodeScopeDbContext dbContext, IImportFileStorage fileStorage)
    : IRequestHandler<CreateImportJobCommand, ImportJobQueuedDto?>
{
    /// <inheritdoc />
    public async Task<ImportJobQueuedDto?> Handle(CreateImportJobCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.FileStream);

        var project = await dbContext.Projects
            .SingleOrDefaultAsync(
                p => p.Id == request.ProjectId && p.OwnerUserId == request.OwnerUserId,
                cancellationToken)
            .ConfigureAwait(false);

        if (project is null)
        {
            return null;
        }

        var importId = Guid.NewGuid();
        var safeName = SanitizeFileName(request.OriginalFileName);
        var relativePath = fileStorage.BuildStoredRelativePath(project.Id, importId, safeName);

        var aggregate = new ImportJob(project.Id, request.OriginalFileName, relativePath, importId);
        dbContext.ImportJobs.Add(aggregate);

        fileStorage.EnsureDirectoriesForImport(project.Id, importId);
        await fileStorage
            .WriteUploadedFileAsync(project.Id, importId, safeName, request.FileStream, cancellationToken)
            .ConfigureAwait(false);

        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ImportJobQueuedDto(aggregate.Id, ImportJobStatus.Pending.ToString());
    }

    private static string SanitizeFileName(string original)
    {
        var file = Path.GetFileName(original);
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            file = file.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(file) ? "upload.bin" : file.Trim();
    }
}
