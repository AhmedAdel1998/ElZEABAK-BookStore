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
        builder.Property(supplier => supplier.ContactName).HasMaxLength(150);

        // Owned types rather than ValueConverter - see the identical note in
        // CustomerConfiguration for why a value-converted Phone/Email is a dormant query trap.
        builder.OwnsOne(supplier => supplier.Phone, phone =>
        {
            phone.Property(value => value.Value).HasColumnName("Phone").HasMaxLength(20);
            phone.HasIndex(value => value.Value).HasDatabaseName("IX_Suppliers_Phone");
        });
        builder.OwnsOne(supplier => supplier.Email, email =>
        {
            email.Property(value => value.Value).HasColumnName("Email").HasMaxLength(254);
            email.HasIndex(value => value.Value).HasDatabaseName("IX_Suppliers_Email");
        });

        builder.Property(supplier => supplier.Notes).HasMaxLength(2000);
        builder.Property(supplier => supplier.IsActive).IsRequired();
        builder.HasIndex(supplier => supplier.CompanyName);
        builder.HasIndex(supplier => supplier.ContactName);
        builder.HasIndex(supplier => supplier.IsActive);
        builder.HasIndex(supplier => supplier.IsDeleted);
        builder.OwnsOne(supplier => supplier.Address, address =>
        {
            address.Property(value => value.Line1).HasMaxLength(250).HasColumnName("AddressLine1");
            address.Property(value => value.Line2).HasMaxLength(250).HasColumnName("AddressLine2");
            address.Property(value => value.City).HasMaxLength(100).HasColumnName("City");
            address.Property(value => value.Country).HasMaxLength(100).HasColumnName("Country");
        });
    }
}
