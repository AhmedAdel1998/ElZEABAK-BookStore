using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Persistence.Configurations;

/// <summary>
/// Configures the permission entity.
/// </summary>
public class PermissionConfiguration : EntityConfigurationBase<Permission>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.Property(permission => permission.Name).IsRequired().HasMaxLength(150);
        builder.Property(permission => permission.Description).HasMaxLength(500);
        builder.HasIndex(permission => permission.Name).IsUnique();
    }
}
