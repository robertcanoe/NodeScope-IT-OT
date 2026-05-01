using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeScope.Domain.Entities;

namespace NodeScope.Infrastructure.Data.Configurations;

/// <summary>
/// Maps <see cref="ImportJob"/> including lifecycle fields and linkage to derived entities.
/// </summary>
public sealed class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.ToTable("import_jobs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(x => x.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.StoredFilePath)
            .HasColumnName("stored_file_path")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.StartedAt)
            .HasColumnName("started_at");

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(x => x.ProcessorVersion)
            .HasColumnName("processor_version")
            .HasMaxLength(128);

        builder.Property(x => x.RowCount)
            .HasColumnName("row_count");

        builder.Property(x => x.IssueCount)
            .HasColumnName("issue_count");

        builder.Property(x => x.ReportHtmlPath)
            .HasColumnName("report_html_path")
            .HasMaxLength(2048);

        builder.Property(x => x.NormalizedJsonPath)
            .HasColumnName("normalized_json_path")
            .HasMaxLength(2048);

        builder.Property(x => x.SummaryJson)
            .HasColumnName("summary_json");

        builder.HasMany(x => x.DatasetColumns)
            .WithOne(x => x.ImportJob)
            .HasForeignKey(x => x.ImportJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.DatasetRecords)
            .WithOne(x => x.ImportJob)
            .HasForeignKey(x => x.ImportJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ValidationIssues)
            .WithOne(x => x.ImportJob)
            .HasForeignKey(x => x.ImportJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.GeneratedArtifacts)
            .WithOne(x => x.ImportJob)
            .HasForeignKey(x => x.ImportJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ProjectId, x.Status })
            .HasDatabaseName("ix_import_jobs_project_id_status");

        builder.HasIndex(x => x.StartedAt)
            .HasDatabaseName("ix_import_jobs_started_at");
    }
}
