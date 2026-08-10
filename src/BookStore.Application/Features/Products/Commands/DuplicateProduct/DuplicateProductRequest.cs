namespace BookStore.Application.Features.Products.Commands.DuplicateProduct;

/// <summary>
/// Requests duplication of an existing product with a new barcode.
/// </summary>
/// <param name="Id">The source product identifier.</param>
/// <param name="NewBarcode">The new product barcode.</param>
public sealed record DuplicateProductRequest(Guid Id, string NewBarcode);
