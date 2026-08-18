using BookStore.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Persistence.Configurations;

/// <summary>
/// Provides shared entity configuration for domain entities.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public abstract class EntityConfigurationBase<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(entity => entity.Id);

        // Stored as UTC ticks, matching Sale.SaleDate, InventoryTransaction.Date, and
        // AuditLogEntry.OccurredAt. SQLite supports neither comparison nor ORDER BY on
        // DateTimeOffset, so persisting it directly is a dormant trap shared by every entity: the
        // first Where(x => x.CreatedAt >= ...) or OrderBy(x => x.UpdatedAt) written against any of
        // them throws "could not be translated" - exactly the failure that broke the audit trail.
        // No caller does that today, but nothing stops the next one from trying.
        builder.Property(entity => entity.CreatedAt)
            .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero))
            .IsRequired();
        builder.Property(entity => entity.UpdatedAt)
            .HasConversion(
                value => value.HasValue ? value.Value.UtcTicks : (long?)null,
                value => value.HasValue ? new DateTimeOffset(value.Value, TimeSpan.Zero) : null);
        builder.Property(entity => entity.IsDeleted).IsRequired();
        builder.Ignore(entity => entity.DomainEvents);
        ConfigureEntity(builder);
    }

    /// <summary>
    /// Configures entity-specific mapping.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}
