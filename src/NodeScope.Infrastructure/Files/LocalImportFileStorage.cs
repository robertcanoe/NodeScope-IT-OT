using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NodeScope.Application.Abstractions.Files;
using NodeScope.Application.Configuration;

namespace NodeScope.Infrastructure.Files;

/// <summary>
/// File-system backed implementation aligning with deterministic <see cref="IImportFileStorage"/> contracts for local/on-prem workloads.
/// </summary>
/// <remarks>
/// For object storage swaps (S3/Blob), replace this adapter while retaining domain-relative path conventions.
/// </remarks>
/// <param name="environment">Hosting environment supplying composition-root paths.</param>
/// <param name="optionsMonitor">Configured storage root + ingestion ceilings.</param>
public sealed class LocalImportFileStorage(IHostEnvironment environment, IOptionsMonitor<ProcessingSettings> optionsMonitor)
    : IImportFileStorage
{
    private string ResolvePhysicalRoot()
    {
        var relativeOrAbsolute = optionsMonitor.CurrentValue.StorageRoot.Trim();
        return Path.IsPathFullyQualified(relativeOrAbsolute)
            ? relativeOrAbsolute
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, relativeOrAbsolute));
    }

    /// <inheritdoc />
    public string BuildStoredRelativePath(Guid projectId, Guid importJobId, string sanitizedOriginalFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sanitizedOriginalFileName);

        var segments = new[] { "uploads", projectId.ToString("D"), importJobId.ToString("D"), sanitizedOriginalFileName };
        return string.Join("/", segments);
    }

    /// <inheritdoc />
    public void EnsureDirectoriesForImport(Guid projectId, Guid importJobId)
    {
        Directory.CreateDirectory(GetUploadDirectory(projectId, importJobId));
        Directory.CreateDirectory(GetArtifactsPhysicalPath(projectId, importJobId));
    }

    /// <inheritdoc />
    public async Task WriteUploadedFileAsync(
        Guid projectId,
        Guid importJobId,
        string sanitizedOriginalFileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        EnsureDirectoriesForImport(projectId, importJobId);

        var directory = GetUploadDirectory(projectId, importJobId);
        var physicalPath = Path.Combine(directory, sanitizedOriginalFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

        await using var target = new FileStream(
            physicalPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await content.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string GetArtifactsDirectory(Guid projectId, Guid importJobId)
    {
        var physical = GetArtifactsPhysicalPath(projectId, importJobId);
        Directory.CreateDirectory(physical);
        return physical;
    }

    private string GetArtifactsPhysicalPath(Guid projectId, Guid importJobId) =>
        Path.Combine(ResolvePhysicalRoot(), "artifacts", projectId.ToString("D"), importJobId.ToString("D"));

    /// <inheritdoc />
    public string GetArtifactsRootPhysicalPath(Guid projectId, Guid importJobId) => GetArtifactsPhysicalPath(projectId, importJobId);

    /// <inheritdoc />
    public Task ClearArtifactsDirectoryAsync(Guid projectId, Guid importJobId, CancellationToken cancellationToken)
    {
        var root = GetArtifactsPhysicalPath(projectId, importJobId);
        if (!Directory.Exists(root))
        {
            return Task.CompletedTask;
        }

        return Task.Run(
            () =>
            {
                foreach (var entry in Directory.EnumerateFileSystemEntries(root))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (File.Exists(entry))
                    {
                        File.Delete(entry);
                    }
                    else if (Directory.Exists(entry))
                    {
                        Directory.Delete(entry, recursive: true);
                    }
                }
            },
            cancellationToken);
    }

    private string GetUploadDirectory(Guid projectId, Guid importJobId) =>
        Path.Combine(ResolvePhysicalRoot(), "uploads", projectId.ToString("D"), importJobId.ToString("D"));
}
