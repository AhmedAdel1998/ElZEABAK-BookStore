using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Persistence.Configurations;

/// <summary>
/// Configures product-supplier associations.
/// </summary>
public class ProductSupplierConfiguration : EntityConfigurationBase<ProductSupplier>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<ProductSupplier> builder)
    {
        builder.ToTable("ProductSuppliers");
        builder.Property(productSupplier => productSupplier.SupplierSku).HasMaxLength(100);
        builder.Property(productSupplier => productSupplier.IsPreferred).IsRequired();
        builder.Property(productSupplier => productSupplier.IsActive).IsRequired();
        builder.HasIndex(productSupplier => productSupplier.ProductId);
        builder.HasIndex(productSupplier => productSupplier.SupplierId);
        builder.HasIndex(productSupplier => new { productSupplier.ProductId, productSupplier.SupplierId }).IsUnique();
        builder.HasOne(productSupplier => productSupplier.Product)
            .WithMany()
            .HasForeignKey(productSupplier => productSupplier.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(productSupplier => productSupplier.Supplier)
            .WithMany(supplier => supplier.Products)
            .HasForeignKey(productSupplier => productSupplier.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
