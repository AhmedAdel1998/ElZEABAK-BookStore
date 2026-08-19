using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Persistence.Configurations;

/// <summary>
/// Configures the category entity.
/// </summary>
public class CategoryConfiguration : EntityConfigurationBase<Category>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.Property(category => category.Name).IsRequired().HasMaxLength(100);
        builder.Property(category => category.Description).HasMaxLength(500);
        builder.Property(category => category.IsActive).IsRequired();
        // Filtered so a soft-deleted category releases its name for reuse.
        builder.HasIndex(category => category.Name).IsUnique().HasFilter("\"IsDeleted\" = 0");
        builder.Navigation(category => category.Products).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(category => category.Products)
            .WithOne(product => product.Category)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
