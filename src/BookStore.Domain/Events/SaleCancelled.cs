namespace BookStore.Domain.Events;

/// <summary>
/// Raised when a sale is cancelled.
/// </summary>
/// <param name="SaleId">The sale identifier.</param>
public sealed record SaleCancelled(Guid SaleId) : DomainEvent;
