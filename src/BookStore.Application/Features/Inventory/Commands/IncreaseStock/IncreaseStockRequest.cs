namespace BookStore.Application.Features.Inventory.Commands.IncreaseStock;

/// <summary>
/// Requests a stock increase.
/// </summary>
public sealed record IncreaseStockRequest(Guid ProductId, int Quantity, string Reason, string? Reference = null, string? Notes = null);
