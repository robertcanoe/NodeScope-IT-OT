using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeScope.Domain.Entities;

namespace NodeScope.Infrastructure.Data.Configurations;

/// <summary>
/// Tracks generated assets such as reports and CSV derivatives.
/// </summary>
public sealed class GeneratedArtifactConfiguration : IEntityTypeConfiguration<GeneratedArtifact>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GeneratedArtifact> builder)
    {
        builder.ToTable("generated_artifacts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ImportJobId)
            .HasColumnName("import_job_id")
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(48)
            .IsRequired();

        builder.Property(x => x.Path)
            .HasColumnName("path")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.MimeType)
            .HasColumnName("mime_type")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.SizeBytes)
            .HasColumnName("size_bytes")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(x => new { x.ImportJobId, x.Type })
            .HasDatabaseName("ix_generated_artifacts_import_type");

        builder.HasIndex(x => x.ImportJobId)
            .HasDatabaseName("ix_generated_artifacts_import_job_id");
    }
}
