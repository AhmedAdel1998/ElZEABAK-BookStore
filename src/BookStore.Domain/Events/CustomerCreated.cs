namespace BookStore.Domain.Events;

/// <summary>
/// Raised when a customer is created.
/// </summary>
/// <param name="CustomerId">The customer identifier.</param>
public sealed record CustomerCreated(Guid CustomerId) : DomainEvent;
