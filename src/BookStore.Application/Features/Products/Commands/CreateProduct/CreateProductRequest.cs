using BookStore.Application.Features.Products.DTOs;

namespace BookStore.Application.Features.Products.Commands.CreateProduct;

/// <summary>
/// Requests product creation.
/// </summary>
/// <param name="Product">The product editor model.</param>
public sealed record CreateProductRequest(ProductEditorModel Product);
