using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Persistence.Configurations;

/// <summary>
/// Configures the product entity.
/// </summary>
public class ProductConfiguration : EntityConfigurationBase<Product>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        // Barcode and ISBN are mapped as owned types rather than through a ValueConverter so that
        // their inner string is a real column to EF. A value-converted property is opaque in a
        // predicate: EF cannot translate member access or string methods on it, which made every
        // barcode lookup, uniqueness check, and product search throw
        // "The LINQ expression ... could not be translated" at runtime. The column names are pinned
        // to the existing ones, so this is a model change only - the schema is unchanged.
        builder.OwnsOne(product => product.Barcode, barcode =>
        {
            barcode.Property(value => value.Value).HasColumnName("Barcode").IsRequired().HasMaxLength(64);
            // Filtered so a soft-deleted product releases its barcode for reuse.
            barcode.HasIndex(value => value.Value).IsUnique().HasDatabaseName("IX_Products_Barcode").HasFilter("\"IsDeleted\" = 0");
        });
        builder.Navigation(product => product.Barcode).IsRequired();

        builder.OwnsOne(product => product.ISBN, isbn =>
        {
            isbn.Property(value => value.Value).HasColumnName("ISBN").HasMaxLength(13);
            isbn.HasIndex(value => value.Value).HasDatabaseName("IX_Products_ISBN");
        });

        builder.Property(product => product.Title).IsRequired().HasMaxLength(250);
        builder.Property(product => product.Subtitle).HasMaxLength(250);
        builder.Property(product => product.Description).HasMaxLength(2000);
        builder.Property(product => product.PurchasePrice).HasPrecision(18, 2).IsRequired();
        builder.Property(product => product.SellingPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(product => product.Quantity).IsRequired();
        builder.Property(product => product.MinimumStock).IsRequired();
        builder.Property(product => product.ImagePath).HasMaxLength(500);
        builder.Property(product => product.ShelfLocation).HasMaxLength(100);
        builder.Property(product => product.Publisher).HasMaxLength(150);
        builder.Property(product => product.Author).HasMaxLength(150);
        builder.Property(product => product.Language).HasMaxLength(50);
        builder.Property(product => product.Edition).HasMaxLength(50);
        builder.Property(product => product.TaxCategory).HasMaxLength(100);
        builder.Property(product => product.PublishDate);
        builder.Property(product => product.IsActive).IsRequired();
        builder.HasIndex(product => product.Title);
        builder.HasIndex(product => new { product.CategoryId, product.IsActive });
        builder.HasIndex(product => new { product.Quantity, product.MinimumStock });
    }
}
