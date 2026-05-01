using Microsoft.EntityFrameworkCore;
using NodeScope.Domain.Entities;

namespace NodeScope.Application.Abstractions.Data;

/// <summary>
/// Application-facing persistence abstraction so handlers stay infrastructure-agnostic.
/// </summary>
public interface INodeScopeDbContext
{
    /// <summary>
    /// Gets persisted application users used for authentication and ownership.
    /// </summary>
    DbSet<User> Users { get; }

    /// <summary>
    /// Gets analysis workspace projects keyed by owning users.
    /// </summary>
    DbSet<Project> Projects { get; }

    /// <summary>
    /// Gets ingestion and analysis executions.
    /// </summary>
    DbSet<ImportJob> ImportJobs { get; }

    /// <summary>
    /// Gets column summaries attached to imports.
    /// </summary>
    DbSet<DatasetColumn> DatasetColumns { get; }

    /// <summary>
    /// Gets hybrid-row payloads keyed by imports.
    /// </summary>
    DbSet<DatasetRecord> DatasetRecords { get; }

    /// <summary>
    /// Gets structured anomalies raised by validation rules.
    /// </summary>
    DbSet<ValidationIssue> ValidationIssues { get; }

    /// <summary>
    /// Gets supplementary generated assets (reports, extracts).
    /// </summary>
    DbSet<GeneratedArtifact> GeneratedArtifacts { get; }

    /// <summary>
    /// Gets forensic audit tuples.
    /// </summary>
    DbSet<AuditLog> AuditLogs { get; }

    /// <summary>
    /// Persists pending domain changes asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cooperative cancellation signal.</param>
    /// <returns>The number of state entries persisted.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
