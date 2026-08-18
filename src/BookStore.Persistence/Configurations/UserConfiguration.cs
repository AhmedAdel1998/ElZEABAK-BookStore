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

        // Owned type rather than ValueConverter - see the identical note in
        // CustomerConfiguration for why a value-converted Email is a dormant query trap.
        builder.OwnsOne(user => user.Email, email =>
        {
            email.Property(value => value.Value).HasColumnName("Email").HasMaxLength(254);
        });

        builder.Property(user => user.IsActive).IsRequired();
        builder.Property(user => user.FailedLoginCount).IsRequired();

        // Same ticks conversion as BaseEntity.CreatedAt/UpdatedAt: unconverted, these are
        // DateTimeOffset? columns SQLite cannot compare or sort, which would break the first
        // lockout-expiry query written against them (for example "find users whose lockout has
        // elapsed").
        builder.Property(user => user.LastFailedLogin)
            .HasConversion(
                value => value.HasValue ? value.Value.UtcTicks : (long?)null,
                value => value.HasValue ? new DateTimeOffset(value.Value, TimeSpan.Zero) : null);
        builder.Property(user => user.LockoutUntil)
            .HasConversion(
                value => value.HasValue ? value.Value.UtcTicks : (long?)null,
                value => value.HasValue ? new DateTimeOffset(value.Value, TimeSpan.Zero) : null);
        builder.Ignore(user => user.IsLockedOut);
        builder.HasIndex(user => user.Username).IsUnique();
        builder.HasOne(user => user.Role)
            .WithMany()
            .HasForeignKey(user => user.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
