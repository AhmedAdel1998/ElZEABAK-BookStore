namespace BookStore.Application.Features.Inventory.Commands.DecreaseStock;

/// <summary>
/// Requests a stock decrease.
/// </summary>
public sealed record DecreaseStockRequest(Guid ProductId, int Quantity, string Reason, string? Reference = null, string? Notes = null);
