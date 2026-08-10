using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Persistence.Configurations;

/// <summary>
/// Configures the user entity.
/// </summary>
public class UserConfiguration : EntityConfigurationBase<User>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.Property(user => user.Username).IsRequired().HasMaxLength(100);
        builder.Property(user => user.PasswordHash).IsRequired().HasMaxLength(100);
        builder.Property(user => user.FullName).IsRequired().HasMaxLength(200);
        builder.Property(user => user.Email).HasConversion(ValueObjectConverters.EmailConverter).HasMaxLength(254);
        builder.Property(user => user.IsActive).IsRequired();
        builder.Property(user => user.FailedLoginCount).IsRequired();
        builder.Property(user => user.LastFailedLogin);
        builder.Property(user => user.LockoutUntil);
        builder.Ignore(user => user.IsLockedOut);
        builder.HasIndex(user => user.Username).IsUnique();
        builder.HasOne(user => user.Role)
            .WithMany()
            .HasForeignKey(user => user.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
