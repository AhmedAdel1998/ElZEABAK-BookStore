namespace BookStore.Domain.Events;

/// <summary>
/// Raised when a sale is completed.
/// </summary>
/// <param name="SaleId">The sale identifier.</param>
public sealed record SaleCompleted(Guid SaleId) : DomainEvent;
