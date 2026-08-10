namespace BookStore.Application.Features.Products.Queries.GetProducts;

/// <summary>
/// Requests paged products without additional filtering.
/// </summary>
/// <param name="PageNumber">The page number.</param>
/// <param name="PageSize">The page size.</param>
public sealed record GetProductsRequest(int PageNumber = 1, int PageSize = 25);
