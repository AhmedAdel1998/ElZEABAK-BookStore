using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Persistence.Configurations;

/// <summary>
/// Configures the sale entity.
/// </summary>
public class SaleConfiguration : EntityConfigurationBase<Sale>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");
        builder.Property(sale => sale.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.Property(sale => sale.SaleDate).IsRequired();
        builder.Property(sale => sale.PaymentMethod).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(sale => sale.Discount).HasPrecision(18, 2).IsRequired();
        builder.Property(sale => sale.Tax).HasPrecision(18, 2).IsRequired();
        builder.Property(sale => sale.Total).HasPrecision(18, 2).IsRequired();
        builder.Property(sale => sale.PaidAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(sale => sale.ChangeAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(sale => sale.Status).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(sale => sale.InvoiceNumber).IsUnique();
        builder.Navigation(sale => sale.SaleItems).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(sale => sale.SaleItems)
            .WithOne()
            .HasForeignKey("SaleId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(sale => sale.Customer)
            .WithMany()
            .HasForeignKey(sale => sale.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(sale => sale.Cashier)
            .WithMany()
            .HasForeignKey(sale => sale.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
