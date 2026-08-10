namespace BookStore.Application.Features.Products.Commands.ActivateProduct;

/// <summary>
/// Requests product activation.
/// </summary>
/// <param name="Id">The product identifier.</param>
public sealed record ActivateProductRequest(Guid Id);
