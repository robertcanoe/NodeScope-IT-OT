namespace NodeScope.Application.Abstractions.Processing;

/// <summary>
/// Executes deterministic analysis workflows (Python-backed) for queued import executions.
/// </summary>
public interface IImportAnalysisPipeline
{
    /// <summary>
    /// Attempts to process any single import currently in pending state owned by hosted orchestration semantics.
    /// </summary>
    /// <returns><c>true</c> when a job was leased and processed.</returns>
    Task<bool> TryProcessNextPendingAsync(CancellationToken cancellationToken);
}
