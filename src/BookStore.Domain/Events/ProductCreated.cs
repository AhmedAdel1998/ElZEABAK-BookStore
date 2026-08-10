namespace BookStore.Domain.Events;

/// <summary>
/// Raised when a product is created.
/// </summary>
/// <param name="ProductId">The product identifier.</param>
public sealed record ProductCreated(Guid ProductId) : DomainEvent;
