using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeScope.Domain.Entities;

namespace NodeScope.Infrastructure.Data.Configurations;

/// <summary>
/// Defines schema metadata for persisted column profiles.
/// </summary>
public sealed class DatasetColumnConfiguration : IEntityTypeConfiguration<DatasetColumn>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DatasetColumn> builder)
    {
        builder.ToTable("dataset_columns");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ImportJobId)
            .HasColumnName("import_job_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.DataTypeDetected)
            .HasColumnName("data_type_detected")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.DistinctCount)
            .HasColumnName("distinct_count")
            .IsRequired();

        builder.Property(x => x.NullCount)
            .HasColumnName("null_count")
            .IsRequired();

        builder.HasIndex(x => new { x.ImportJobId, x.NormalizedName })
            .HasDatabaseName("ix_dataset_columns_import_normalized_name");

        builder.HasIndex(x => x.ImportJobId)
            .HasDatabaseName("ix_dataset_columns_import_job_id");
    }
}
