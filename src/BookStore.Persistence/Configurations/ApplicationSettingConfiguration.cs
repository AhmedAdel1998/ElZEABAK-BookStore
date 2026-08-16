using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Persistence.Configurations;

/// <summary>
/// Configures persisted application settings.
/// </summary>
public sealed class ApplicationSettingConfiguration : EntityConfigurationBase<ApplicationSetting>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<ApplicationSetting> builder)
    {
        builder.ToTable("ApplicationSettings");
        builder.Property(setting => setting.Key).HasMaxLength(120).IsRequired();
        builder.Property(setting => setting.Category).HasMaxLength(60).IsRequired();
        builder.Property(setting => setting.DataType).HasMaxLength(260).IsRequired();
        builder.Property(setting => setting.Description).HasMaxLength(500);
        builder.Property(setting => setting.Value).IsRequired();
        builder.Property(setting => setting.UpdatedBy).HasMaxLength(100);
        builder.HasIndex(setting => setting.Key).IsUnique();
        builder.HasIndex(setting => setting.Category);
    }
}
