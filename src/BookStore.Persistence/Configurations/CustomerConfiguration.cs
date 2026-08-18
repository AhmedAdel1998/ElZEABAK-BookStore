using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Persistence.Configurations;

/// <summary>
/// Configures the customer entity.
/// </summary>
public class CustomerConfiguration : EntityConfigurationBase<Customer>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.Property(customer => customer.FullName).IsRequired().HasMaxLength(150);

        // Owned types rather than ValueConverter, matching Product.Barcode/ISBN and the existing
        // Address mapping just below: a value-converted property is opaque inside a predicate, so
        // the first `Where(c => c.Phone.Value.Contains(term))` written against these would throw
        // "could not be translated" - the exact failure that broke POS barcode scanning. Column
        // names are pinned to the existing ones, so this is a model change only.
        builder.OwnsOne(customer => customer.Phone, phone =>
        {
            phone.Property(value => value.Value).HasColumnName("Phone").HasMaxLength(20);
            phone.HasIndex(value => value.Value).HasDatabaseName("IX_Customers_Phone");
        });
        builder.OwnsOne(customer => customer.Email, email =>
        {
            email.Property(value => value.Value).HasColumnName("Email").HasMaxLength(254);
            email.HasIndex(value => value.Value).HasDatabaseName("IX_Customers_Email");
        });

        builder.Property(customer => customer.LoyaltyPoints).IsRequired();
        builder.Property(customer => customer.IsActive).IsRequired();
        builder.HasIndex(customer => customer.FullName);
        builder.HasIndex(customer => customer.IsDeleted);
        builder.HasIndex(customer => customer.IsActive);
        builder.OwnsOne(customer => customer.Address, address =>
        {
            address.Property(value => value.Line1).HasMaxLength(250).HasColumnName("AddressLine1");
            address.Property(value => value.Line2).HasMaxLength(250).HasColumnName("AddressLine2");
            address.Property(value => value.City).HasMaxLength(100).HasColumnName("City");
            address.Property(value => value.Country).HasMaxLength(100).HasColumnName("Country");
        });
    }
}
