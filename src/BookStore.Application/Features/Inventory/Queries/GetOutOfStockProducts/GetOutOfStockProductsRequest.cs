namespace BookStore.Application.Features.Inventory.Queries.GetOutOfStockProducts;

/// <summary>
/// Requests out-of-stock inventory rows.
/// </summary>
public sealed record GetOutOfStockProductsRequest(int PageNumber = 1, int PageSize = 25);
