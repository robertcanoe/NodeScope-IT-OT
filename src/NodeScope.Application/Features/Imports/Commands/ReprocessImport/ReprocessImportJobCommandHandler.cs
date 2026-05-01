using MediatR;
using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Application.Abstractions.Files;
using NodeScope.Domain.Enums;

namespace NodeScope.Application.Features.Imports.Commands.ReprocessImport;

public sealed class ReprocessImportJobCommandHandler(INodeScopeDbContext dbContext, IImportFileStorage fileStorage)
    : IRequestHandler<ReprocessImportJobCommand, ReprocessImportJobResult>
{
    /// <inheritdoc />
    public async Task<ReprocessImportJobResult> Handle(ReprocessImportJobCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var job = await dbContext.ImportJobs
            .Include(j => j.Project)
            .Where(
                j =>
                    j.Id == request.ImportJobId
                    && j.ProjectId == request.ProjectId
                    && j.Project.OwnerUserId == request.OwnerUserId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (job is null)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return ReprocessImportJobResult.NotFound;
        }

        if (job.Status is ImportJobStatus.Pending or ImportJobStatus.Processing)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return ReprocessImportJobResult.Conflict;
        }

        var jobId = job.Id;
        var projectId = job.ProjectId;

        _ =
            await dbContext.DatasetRecords
                .Where(r => r.ImportJobId == jobId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        _ =
            await dbContext.DatasetColumns
                .Where(c => c.ImportJobId == jobId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        _ =
            await dbContext.ValidationIssues
                .Where(i => i.ImportJobId == jobId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        _ =
            await dbContext.GeneratedArtifacts
                .Where(a => a.ImportJobId == jobId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

        await fileStorage.ClearArtifactsDirectoryAsync(projectId, jobId, cancellationToken).ConfigureAwait(false);

        job.ResetForReprocessing();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return ReprocessImportJobResult.Requeued;
    }
}
