using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeScope.Domain.Entities;

namespace NodeScope.Infrastructure.Data.Configurations;

/// <summary>
/// Maps audit telemetry entries referencing optional aggregate scopes.
/// </summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .HasColumnName("user_id");

        builder.Property(x => x.ProjectId)
            .HasColumnName("project_id");

        builder.Property(x => x.Action)
            .HasColumnName("action")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.TargetType)
            .HasColumnName("target_type")
            .HasMaxLength(256);

        builder.Property(x => x.TargetId)
            .HasColumnName("target_id");

        builder.Property(x => x.MetadataJson)
            .HasColumnName("metadata_json");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("ix_audit_logs_created_at");

        builder.HasIndex(x => new { x.UserId, x.CreatedAt })
            .HasDatabaseName("ix_audit_logs_user_created_at");
    }
}
