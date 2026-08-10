using BookStore.Application.Features.Products.DTOs;

namespace BookStore.Application.Features.Products.Commands.UpdateProduct;

/// <summary>
/// Requests product update.
/// </summary>
/// <param name="Product">The product editor model.</param>
public sealed record UpdateProductRequest(ProductEditorModel Product);
