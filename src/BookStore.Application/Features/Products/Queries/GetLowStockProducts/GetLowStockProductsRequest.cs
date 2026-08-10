namespace BookStore.Application.Features.Products.Queries.GetLowStockProducts;

/// <summary>
/// Requests low-stock products.
/// </summary>
/// <param name="PageNumber">The page number.</param>
/// <param name="PageSize">The page size.</param>
public sealed record GetLowStockProductsRequest(int PageNumber = 1, int PageSize = 25);
