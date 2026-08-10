namespace BookStore.Domain.Events;

/// <summary>
/// Raised when product stock changes.
/// </summary>
/// <param name="ProductId">The product identifier.</param>
/// <param name="Quantity">The resulting product quantity.</param>
public sealed record ProductStockChanged(Guid ProductId, int Quantity) : DomainEvent;
