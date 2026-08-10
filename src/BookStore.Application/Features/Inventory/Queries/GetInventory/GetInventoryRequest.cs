namespace BookStore.Application.Features.Inventory.Queries.GetInventory;

/// <summary>
/// Requests inventory rows.
/// </summary>
public sealed record GetInventoryRequest(string? SearchTerm = null, Guid? CategoryId = null, bool LowStockOnly = false, bool OutOfStockOnly = false, int PageNumber = 1, int PageSize = 25);
