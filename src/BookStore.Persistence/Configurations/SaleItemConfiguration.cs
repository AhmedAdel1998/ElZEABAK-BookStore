using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Persistence.Configurations;

/// <summary>
/// Configures the sale item entity.
/// </summary>
public class SaleItemConfiguration : EntityConfigurationBase<SaleItem>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems");
        builder.Property(item => item.Quantity).IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(item => item.Discount).HasPrecision(18, 2).IsRequired();
        builder.Property(item => item.Total).HasPrecision(18, 2).IsRequired();
        builder.HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex("SaleId");
        builder.HasIndex(item => item.ProductId);
    }
}
