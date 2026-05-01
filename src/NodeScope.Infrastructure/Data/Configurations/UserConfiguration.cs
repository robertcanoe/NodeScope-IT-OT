using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodeScope.Domain.Entities;
using NodeScope.Domain.Enums;

namespace NodeScope.Infrastructure.Data.Configurations;

/// <summary>
/// Maps the <see cref="User"/> aggregate root to PostgreSQL relational storage.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email_unique");
    }
}
