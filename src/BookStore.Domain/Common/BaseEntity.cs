using BookStore.Domain.Events;

namespace BookStore.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// </summary>
public abstract class BaseEntity : IEntity, IAuditable, ISoftDelete
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntity"/> class.
    /// </summary>
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public Guid Id { get; protected set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; protected set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; protected set; }

    /// <inheritdoc />
    public bool IsDeleted { get; protected set; }

    /// <summary>
    /// Gets the domain events raised by this entity.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Marks the entity as updated.
    /// </summary>
    public void MarkUpdated()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the entity as deleted.
    /// </summary>
    public void MarkDeleted()
    {
        IsDeleted = true;
        MarkUpdated();
    }

    /// <summary>
    /// Adds a domain event to the entity.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events from the entity.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
