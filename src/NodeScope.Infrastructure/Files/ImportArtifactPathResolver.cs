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
    private static readonly StringComparer PathEquality =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

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
                        new CompletedImportArtifacts(
                            job.Id,
                            job.ProjectId,
                            job.ReportHtmlPath,
                            job.NormalizedJsonPath))
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

        if (scoped is null)
        {
            return null;
        }

        var artifactDirectory =
            Path.GetFullPath(fileStorage.GetArtifactsRootPhysicalPath(scoped.ProjectId, scoped.ImportId));

        var fragments = await CollectLogicalPathFragmentsAsync(scoped, kind, cancellationToken).ConfigureAwait(false);

        var candidatePhysicalPaths = OrderedPhysicalCandidates(fragments, artifactDirectory, kind);

        foreach (var physical in candidatePhysicalPaths)
        {
            if (!IsSubPathSafe(artifactDirectory, physical) || !File.Exists(physical))
            {
                continue;
            }

            return kind switch
            {
                ImportArtifactKind.ReportHtml =>
                    new ResolvedImportArtifact(physical, "text/html", $"import-{importJobId:N}-report.html"),
                ImportArtifactKind.NormalizedJson =>
                    new ResolvedImportArtifact(physical, "application/json", $"import-{importJobId:N}-normalized.json"),
                ImportArtifactKind.IssuesCsv =>
                    new ResolvedImportArtifact(physical, "text/csv", $"import-{importJobId:N}-issues.csv"),
                _ => null,
            };
        }

        return null;
    }

    private sealed record CompletedImportArtifacts(Guid ImportId, Guid ProjectId, string? ReportHtmlPath, string? NormalizedJsonPath);

    private async Task<List<string>> CollectLogicalPathFragmentsAsync(
        CompletedImportArtifacts job,
        ImportArtifactKind kind,
        CancellationToken cancellationToken)
    {
        List<string?> raw = [];

        switch (kind)
        {
            case ImportArtifactKind.ReportHtml:
                raw.Add(job.ReportHtmlPath);
                break;
            case ImportArtifactKind.NormalizedJson:
                raw.Add(job.NormalizedJsonPath);
                break;
            case ImportArtifactKind.IssuesCsv:
                raw.Add(DeriveIssuesCsvNextToNormalizedJson(job.NormalizedJsonPath));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }

        var generatedSnapshot = await dbContext.GeneratedArtifacts.AsNoTracking()
            .Where(artifact => artifact.ImportJobId == job.ImportId && artifact.Type == MapKindToGenerated(kind))
            .Select(artifact => artifact.Path)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var path in generatedSnapshot)
        {
            raw.Add(path);
        }

        if (kind == ImportArtifactKind.ReportHtml)
        {
            foreach (var original in raw.ToArray())
            {
                raw.Add(ProbeOppositeReportStem(original));
            }
        }

        return raw.Where(static fragment => !string.IsNullOrWhiteSpace(fragment)).Select(static s => s!.Trim()).Distinct(PathEquality).ToList();
    }

    private static IEnumerable<string> OrderedPhysicalCandidates(
        IEnumerable<string> logicalFragments,
        string artifactDirectory,
        ImportArtifactKind kind)
    {
        var accumulator = new List<string>();

        void AddPhysical(string resolved)
        {
            var canonical = Path.GetFullPath(resolved);
            foreach (var existing in accumulator)
            {
                if (PathEquality.Equals(Path.GetFullPath(existing), canonical))
                {
                    return;
                }
            }

            accumulator.Add(canonical);
        }

        foreach (var fragment in logicalFragments)
        {
            AddPhysical(NormalizeToPhysicalPath(fragment, artifactDirectory));
        }

        foreach (var fallback in DefaultRelativeFileNames(kind))
        {
            AddPhysical(Path.GetFullPath(Path.Combine(artifactDirectory, fallback)));
        }

        return accumulator;
    }

    private static GeneratedArtifactType MapKindToGenerated(ImportArtifactKind kind) =>
        kind switch
        {
            ImportArtifactKind.ReportHtml => GeneratedArtifactType.ReportHtml,
            ImportArtifactKind.NormalizedJson => GeneratedArtifactType.NormalizedJson,
            ImportArtifactKind.IssuesCsv => GeneratedArtifactType.IssuesCsv,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

    private static IEnumerable<string> DefaultRelativeFileNames(ImportArtifactKind kind) =>
        kind switch
        {
            ImportArtifactKind.ReportHtml => ["report.html", "opc-nodes-report.html"],
            ImportArtifactKind.NormalizedJson => ["normalized.json"],
            ImportArtifactKind.IssuesCsv => ["issues.csv"],
            _ => Array.Empty<string>(),
        };

    private static string NormalizeToPhysicalPath(string pathFragment, string artifactDirectory)
    {
        pathFragment = pathFragment.Trim().Replace('\\', Path.DirectorySeparatorChar);
        return Path.IsPathFullyQualified(pathFragment)
            ? Path.GetFullPath(pathFragment)
            : Path.GetFullPath(Path.Combine(artifactDirectory, pathFragment.TrimStart('/')));
    }

    private static string? DeriveIssuesCsvNextToNormalizedJson(string? normalizedJsonPath)
    {
        if (string.IsNullOrWhiteSpace(normalizedJsonPath))
        {
            return null;
        }

        var sanitized = normalizedJsonPath.Replace('\\', Path.DirectorySeparatorChar);
        var directory = Path.GetDirectoryName(sanitized);
        return string.IsNullOrEmpty(directory) ? null : Path.Combine(directory, "issues.csv");
    }

    private static string? ProbeOppositeReportStem(string? logicalPathFragment)
    {
        if (string.IsNullOrWhiteSpace(logicalPathFragment))
        {
            return null;
        }

        var sanitized = logicalPathFragment.Trim().Replace('\\', Path.DirectorySeparatorChar);
        var directory = Path.GetDirectoryName(sanitized);
        var name = Path.GetFileName(sanitized);
        if (string.IsNullOrEmpty(directory) || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        string? alternate = null;
        if (name.Equals("report.html", StringComparison.OrdinalIgnoreCase))
        {
            alternate = Path.Combine(directory, "opc-nodes-report.html");
        }
        else if (name.Equals("opc-nodes-report.html", StringComparison.OrdinalIgnoreCase))
        {
            alternate = Path.Combine(directory, "report.html");
        }

        return alternate != null && File.Exists(alternate)
            ? Path.GetFullPath(alternate)
            : null;
    }

    private static bool IsSubPathSafe(string parentDirectory, string candidateFile)
    {
        var parent = Path.GetFullPath(parentDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var child = Path.GetFullPath(candidateFile);

        var relative = Path.GetRelativePath(parent, child);
        return !relative.StartsWith("..", StringComparison.Ordinal) && relative != "..";
    }
}
