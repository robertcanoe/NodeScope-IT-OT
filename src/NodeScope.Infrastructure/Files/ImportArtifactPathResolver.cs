using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Files;
using NodeScope.Domain.Enums;
using NodeScope.Infrastructure.Data;

namespace NodeScope.Infrastructure.Files;

/// <summary>
/// Resolves completed-import artifacts beneath the tenancy-scoped artifact directory.
/// </summary>
public sealed class ImportArtifactPathResolver(AppDbContext dbContext, IImportFileStorage fileStorage)
    : IImportArtifactPathResolver
{
    /// <inheritdoc />
    public async Task<ResolvedImportArtifact?> ResolveAsync(
        Guid ownerUserId,
        Guid importJobId,
        ImportArtifactKind kind,
        CancellationToken cancellationToken)
    {
        var scoped =
            await dbContext.ImportJobs.AsNoTracking()
                .Join(
                    dbContext.Projects.Where(project => project.OwnerUserId == ownerUserId),
                    job => job.ProjectId,
                    project => project.Id,
                    (job, _) => job)
                .Where(job => job.Id == importJobId && job.Status == ImportJobStatus.Completed)
                .Select(
                    job =>
                        new
                        {
                            job.Id,
                            job.ProjectId,
                            job.ReportHtmlPath,
                            job.NormalizedJsonPath,
                        })
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

        if (scoped is null)
        {
            return null;
        }

        var artifactDirectory = Path.GetFullPath(fileStorage.GetArtifactsRootPhysicalPath(scoped.ProjectId, scoped.Id));

        string? logicalPath = kind switch
        {
            ImportArtifactKind.ReportHtml => scoped.ReportHtmlPath,
            ImportArtifactKind.NormalizedJson => scoped.NormalizedJsonPath,
            ImportArtifactKind.IssuesCsv => await ResolveIssuesCsvPathAsync(importJobId, scoped.NormalizedJsonPath, cancellationToken)
                .ConfigureAwait(false),
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(logicalPath))
        {
            return null;
        }

        var physical = Path.GetFullPath(logicalPath.Replace('/', Path.DirectorySeparatorChar));

        if (!IsSubPathSafe(artifactDirectory, physical))
        {
            return null;
        }

        if (!File.Exists(physical))
        {
            return null;
        }

        return kind switch
        {
            ImportArtifactKind.ReportHtml => new ResolvedImportArtifact(physical, "text/html", $"import-{importJobId:N}-report.html"),
            ImportArtifactKind.NormalizedJson => new ResolvedImportArtifact(physical, "application/json", $"import-{importJobId:N}-normalized.json"),
            ImportArtifactKind.IssuesCsv => new ResolvedImportArtifact(physical, "text/csv", $"import-{importJobId:N}-issues.csv"),
            _ => null,
        };
    }

    private async Task<string?> ResolveIssuesCsvPathAsync(
        Guid importJobId,
        string? normalizedJsonPath,
        CancellationToken cancellationToken)
    {
        var tracked =
            await dbContext.GeneratedArtifacts.AsNoTracking()
                .Where(artifact => artifact.ImportJobId == importJobId && artifact.Type == GeneratedArtifactType.IssuesCsv)
                .Select(artifact => artifact.Path)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(tracked))
        {
            return tracked;
        }

        if (string.IsNullOrWhiteSpace(normalizedJsonPath))
        {
            return null;
        }

        var directory = Path.GetDirectoryName(normalizedJsonPath.Replace('/', Path.DirectorySeparatorChar));
        return string.IsNullOrEmpty(directory) ? null : Path.Combine(directory, "issues.csv");
    }

    private static bool IsSubPathSafe(string parentDirectory, string candidateFile)
    {
        var parent = Path.GetFullPath(parentDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var child = Path.GetFullPath(candidateFile);

        var relative = Path.GetRelativePath(parent, child);
        return !relative.StartsWith("..", StringComparison.Ordinal) && relative != "..";
    }
}
