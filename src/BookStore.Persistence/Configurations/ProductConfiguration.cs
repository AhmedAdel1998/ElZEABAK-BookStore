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
        builder.Property(product => product.Barcode).HasConversion(ValueObjectConverters.BarcodeConverter).IsRequired().HasMaxLength(64);
        builder.Property(product => product.ISBN).HasConversion(ValueObjectConverters.IsbnConverter).HasMaxLength(13);
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
        builder.HasIndex(product => product.Barcode).IsUnique();
        builder.HasIndex(product => product.ISBN);
        builder.HasIndex(product => product.Title);
        builder.HasIndex(product => new { product.CategoryId, product.IsActive });
        builder.HasIndex(product => new { product.Quantity, product.MinimumStock });
    }
}
