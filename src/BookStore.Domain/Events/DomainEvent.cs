namespace BookStore.Domain.Events;

/// <summary>
/// Base class for domain events.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class.
    /// </summary>
    protected DomainEvent()
    {
        OccurredAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; }
}
