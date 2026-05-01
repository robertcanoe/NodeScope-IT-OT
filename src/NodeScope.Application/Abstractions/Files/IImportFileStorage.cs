namespace NodeScope.Application.Abstractions.Files;

/// <summary>
/// Persists raw uploaded technical files under tenancy-safe paths computed from project and import identifiers.
/// </summary>
public interface IImportFileStorage
{
    /// <summary>
    /// Builds the relative logical path (<c>uploads/{projectId}/{importId}/...</c>) reserved for this upload.
    /// </summary>
    string BuildStoredRelativePath(Guid projectId, Guid importJobId, string sanitizedOriginalFileName);

    /// <summary>
    /// Materializes backing storage locations for an import artifact directory (<c>uploads/</c>, <c>artifacts/</c>).
    /// </summary>
    void EnsureDirectoriesForImport(Guid projectId, Guid importJobId);

    /// <summary>
    /// Writes stream contents atomically beneath the uploads tree.
    /// </summary>
    Task WriteUploadedFileAsync(Guid projectId, Guid importJobId, string sanitizedOriginalFileName, Stream content, CancellationToken cancellationToken);

    /// <summary>
    /// Returns an absolute filesystem path rooted at configured storage roots.
    /// </summary>
    string GetArtifactsDirectory(Guid projectId, Guid importJobId);

    /// <summary>
    /// Absolute path to the artifact folder for the import (no directory creation).
    /// </summary>
    string GetArtifactsRootPhysicalPath(Guid projectId, Guid importJobId);
}
