using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeScope.Domain.Entities;

namespace NodeScope.Infrastructure.Data.Configurations;

/// <summary>
/// Persists deterministic validation anomalies from analysis pipelines.
/// </summary>
public sealed class ValidationIssueConfiguration : IEntityTypeConfiguration<ValidationIssue>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ValidationIssue> builder)
    {
        builder.ToTable("validation_issues");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ImportJobId)
            .HasColumnName("import_job_id")
            .IsRequired();

        builder.Property(x => x.Severity)
            .HasColumnName("severity")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Message)
            .HasColumnName("message")
            .HasMaxLength(4096)
            .IsRequired();

        builder.Property(x => x.ColumnName)
            .HasColumnName("column_name")
            .HasMaxLength(512);

        builder.Property(x => x.RowIndex)
            .HasColumnName("row_index");

        builder.Property(x => x.RawValue)
            .HasColumnName("raw_value")
            .HasMaxLength(2048);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(x => new { x.ImportJobId, x.Severity })
            .HasDatabaseName("ix_validation_issues_import_severity");

        builder.HasIndex(x => x.ImportJobId)
            .HasDatabaseName("ix_validation_issues_import_job_id");
    }
}
