using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeScope.Domain.Entities;

namespace NodeScope.Infrastructure.Data.Configurations;

/// <summary>
/// Maps hybrid JSON payloads for heterogeneous record shapes.
/// </summary>
public sealed class DatasetRecordConfiguration : IEntityTypeConfiguration<DatasetRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DatasetRecord> builder)
    {
        builder.ToTable("dataset_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ImportJobId)
            .HasColumnName("import_job_id")
            .IsRequired();

        builder.Property(x => x.RecordIndex)
            .HasColumnName("record_index")
            .IsRequired();

        builder.Property(x => x.PayloadJson)
            .HasColumnName("payload_json")
            .IsRequired();

        builder.HasIndex(x => new { x.ImportJobId, x.RecordIndex })
            .IsUnique()
            .HasDatabaseName("uq_dataset_records_import_row_index");

        builder.HasIndex(x => x.ImportJobId)
            .HasDatabaseName("ix_dataset_records_import_job_id");
    }
}
