using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeScope.Domain.Entities;
using NodeScope.Domain.Enums;

namespace NodeScope.Infrastructure.Data.Configurations;

/// <summary>
/// Maps <see cref="Project"/> to PostgreSQL relational storage including ownership constraints.
/// </summary>
public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(4000);

        builder.Property(x => x.SourceType)
            .HasColumnName("source_type")
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne(x => x.Owner)
            .WithMany(x => x.OwnedProjects)
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.ImportJobs)
            .WithOne(x => x.Project)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.OwnerUserId)
            .HasDatabaseName("ix_projects_owner_user_id");
    }
}
