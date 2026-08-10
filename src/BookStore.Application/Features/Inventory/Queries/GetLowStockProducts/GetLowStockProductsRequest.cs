namespace BookStore.Application.Features.Inventory.Queries.GetLowStockProducts;

/// <summary>
/// Requests low stock inventory rows.
/// </summary>
public sealed record GetLowStockProductsRequest(int PageNumber = 1, int PageSize = 25);
