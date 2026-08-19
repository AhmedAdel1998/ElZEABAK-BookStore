using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Persistence.Configurations;

/// <summary>
/// Configures audit log persistence.
/// </summary>
public sealed class AuditLogEntryConfiguration : EntityConfigurationBase<AuditLogEntry>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLogEntries");
        builder.Property(entry => entry.Area).HasMaxLength(80).IsRequired();
        builder.Property(entry => entry.Action).HasMaxLength(120).IsRequired();
        builder.Property(entry => entry.Outcome).HasMaxLength(40).IsRequired();
        // Stored as UTC ticks, matching Sale.SaleDate and InventoryTransaction.Date. SQLite
        // supports neither comparison nor ORDER BY on DateTimeOffset, so persisting it directly
        // made every audit query fail to translate and left the OccurredAt indexes unusable.
        builder.Property(entry => entry.OccurredAt)
            .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero))
            .IsRequired();
        builder.Property(entry => entry.Username).HasMaxLength(150);
        builder.Property(entry => entry.EntityType).HasMaxLength(120);
        builder.Property(entry => entry.Detail).HasMaxLength(1000);

        builder.HasIndex(entry => entry.OccurredAt);
        builder.HasIndex(entry => new { entry.Area, entry.OccurredAt });
        builder.HasIndex(entry => new { entry.UserId, entry.OccurredAt });
        builder.HasIndex(entry => new { entry.EntityType, entry.EntityId });
    }
}
