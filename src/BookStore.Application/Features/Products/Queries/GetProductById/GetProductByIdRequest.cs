namespace BookStore.Application.Features.Products.Queries.GetProductById;

/// <summary>
/// Requests a product by identifier.
/// </summary>
/// <param name="Id">The product identifier.</param>
public sealed record GetProductByIdRequest(Guid Id);
