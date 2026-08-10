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
        builder.Property(customer => customer.Phone).HasConversion(ValueObjectConverters.PhoneConverter).HasMaxLength(20);
        builder.Property(customer => customer.Email).HasConversion(ValueObjectConverters.EmailConverter).HasMaxLength(254);
        builder.Property(customer => customer.LoyaltyPoints).IsRequired();
        builder.Property(customer => customer.IsActive).IsRequired();
        builder.HasIndex(customer => customer.Phone);
        builder.HasIndex(customer => customer.Email);
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
