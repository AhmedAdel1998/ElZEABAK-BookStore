using BookStore.Application.Features.Products.DTOs;

namespace BookStore.Application.Features.Products.Queries.SearchProducts;

/// <summary>
/// Requests filtered products.
/// </summary>
/// <param name="Filter">The product filter.</param>
public sealed record SearchProductsRequest(ProductFilter Filter);
