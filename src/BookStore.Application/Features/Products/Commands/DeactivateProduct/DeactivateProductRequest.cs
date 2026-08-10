namespace BookStore.Application.Features.Products.Commands.DeactivateProduct;

/// <summary>
/// Requests product deactivation.
/// </summary>
/// <param name="Id">The product identifier.</param>
public sealed record DeactivateProductRequest(Guid Id);
