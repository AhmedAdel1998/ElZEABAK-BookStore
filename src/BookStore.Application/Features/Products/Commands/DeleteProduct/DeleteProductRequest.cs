namespace BookStore.Application.Features.Products.Commands.DeleteProduct;

/// <summary>
/// Requests soft deletion of a product.
/// </summary>
/// <param name="Id">The product identifier.</param>
public sealed record DeleteProductRequest(Guid Id);
