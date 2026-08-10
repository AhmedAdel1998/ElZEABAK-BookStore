using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Persistence.Configurations;

/// <summary>
/// Configures the inventory transaction entity.
/// </summary>
public class InventoryTransactionConfiguration : EntityConfigurationBase<InventoryTransaction>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("InventoryTransactions");
        builder.Property(transaction => transaction.Quantity).IsRequired();
        builder.Property(transaction => transaction.TransactionType).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(transaction => transaction.QuantityBefore).IsRequired();
        builder.Property(transaction => transaction.QuantityAfter).IsRequired();
        builder.Property(transaction => transaction.Reason).IsRequired().HasMaxLength(250);
        builder.Property(transaction => transaction.Reference).HasMaxLength(100);
        builder.Property(transaction => transaction.UserName).HasMaxLength(150);
        builder.Property(transaction => transaction.Notes).HasMaxLength(1000);
        builder.Property(transaction => transaction.Date).IsRequired();
        builder.HasIndex(transaction => transaction.ProductId);
        builder.HasIndex(transaction => transaction.Date);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(transaction => transaction.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
