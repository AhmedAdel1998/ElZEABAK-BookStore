using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Persistence.Configurations;

/// <summary>
/// Configures the supplier entity.
/// </summary>
public class SupplierConfiguration : EntityConfigurationBase<Supplier>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.Property(supplier => supplier.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(supplier => supplier.ContactName).HasMaxLength(200);
        builder.Property(supplier => supplier.Phone).HasConversion(ValueObjectConverters.PhoneConverter).HasMaxLength(20);
        builder.Property(supplier => supplier.Email).HasConversion(ValueObjectConverters.EmailConverter).HasMaxLength(254);
        builder.Property(supplier => supplier.Notes).HasMaxLength(1000);
        builder.Property(supplier => supplier.IsActive).IsRequired();
        builder.HasIndex(supplier => supplier.CompanyName);
        builder.OwnsOne(supplier => supplier.Address, address =>
        {
            address.Property(value => value.Line1).HasMaxLength(250).HasColumnName("AddressLine1");
            address.Property(value => value.Line2).HasMaxLength(250).HasColumnName("AddressLine2");
            address.Property(value => value.City).HasMaxLength(100).HasColumnName("City");
            address.Property(value => value.Country).HasMaxLength(100).HasColumnName("Country");
        });
    }
}
