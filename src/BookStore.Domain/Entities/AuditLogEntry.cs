using BookStore.Domain.Common;
using BookStore.Domain.Exceptions;

namespace BookStore.Domain.Entities;

/// <summary>
/// Represents a persisted production audit event.
/// </summary>
public sealed class AuditLogEntry : BaseEntity
{
    private AuditLogEntry()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogEntry"/> class.
    /// </summary>
    public AuditLogEntry(string area, string action, string outcome, Guid? userId, string? username, string? entityType = null, Guid? entityId = null, string? detail = null)
    {
        SetArea(area);
        SetAction(action);
        SetOutcome(outcome);
        UserId = userId;
        Username = NormalizeOptional(username, 150);
        EntityType = NormalizeOptional(entityType, 120);
        EntityId = entityId;
        Detail = NormalizeOptional(detail, 1000);
        OccurredAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets the audited feature area.</summary>
    public string Area { get; private set; } = string.Empty;

    /// <summary>Gets the audited action.</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Gets the action outcome.</summary>
    public string Outcome { get; private set; } = string.Empty;

    /// <summary>Gets when the event occurred.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Gets the user identifier when available.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>Gets the username when available.</summary>
    public string? Username { get; private set; }

    /// <summary>Gets the affected entity type.</summary>
    public string? EntityType { get; private set; }

    /// <summary>Gets the affected entity identifier.</summary>
    public Guid? EntityId { get; private set; }

    /// <summary>Gets a short human-readable detail.</summary>
    public string? Detail { get; private set; }

    private void SetArea(string area)
    {
        Area = NormalizeRequired(area, nameof(area), 80);
    }

    private void SetAction(string action)
    {
        Action = NormalizeRequired(action, nameof(action), 120);
    }

    private void SetOutcome(string outcome)
    {
        Outcome = NormalizeRequired(outcome, nameof(outcome), 40);
    }

    private static string NormalizeRequired(string value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException($"{name} is required.");
        }

        value = value.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
