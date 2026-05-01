namespace NodeScope.Application.Features.Imports.Commands.ReprocessImport;

/// <summary>
/// Outcome surface for manual requeue semantics.
/// </summary>
public enum ReprocessImportJobResult
{
    /// <summary>The import was cleared and returned to the pending queue.</summary>
    Requeued,

    /// <summary>Workspace or import was not found for the caller.</summary>
    NotFound,

    /// <summary>The job is already active (pending or processing).</summary>
    Conflict,
}
