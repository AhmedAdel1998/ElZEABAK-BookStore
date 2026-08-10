namespace BookStore.Domain.Events;

/// <summary>
/// Represents a domain event raised by an aggregate.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the date and time when the event occurred.
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
