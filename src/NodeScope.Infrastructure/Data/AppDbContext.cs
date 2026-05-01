using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Domain.Entities;

namespace NodeScope.Infrastructure.Data;

/// <summary>
/// PostgreSQL-backed EF Core persistence layer for NodeScope aggregates.
/// </summary>
/// <remarks>
/// Registrations (<c>AddDbContext</c>) belong in composition roots (Api/Worker), not here.
/// </remarks>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), INodeScopeDbContext
{
    /// <summary>
    /// Gets the persisted users set.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets workspace projects belonging to owners.
    /// </summary>
    public DbSet<Project> Projects => Set<Project>();

    /// <summary>
    /// Gets import/analysis executions grouped under projects.
    /// </summary>
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();

    /// <summary>
    /// Gets column statistical profiles emitted by analysis.
    /// </summary>
    public DbSet<DatasetColumn> DatasetColumns => Set<DatasetColumn>();

    /// <summary>
    /// Gets hybrid JSON payloads for heterogeneous row shapes.
    /// </summary>
    public DbSet<DatasetRecord> DatasetRecords => Set<DatasetRecord>();

    /// <summary>
    /// Gets structured validation anomalies for UI surfacing.
    /// </summary>
    public DbSet<ValidationIssue> ValidationIssues => Set<ValidationIssue>();

    /// <summary>
    /// Gets supplementary generated assets such as CSV extracts.
    /// </summary>
    public DbSet<GeneratedArtifact> GeneratedArtifacts => Set<GeneratedArtifact>();

    /// <summary>
    /// Gets forensic audit tuples for privileged operations.
    /// </summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
