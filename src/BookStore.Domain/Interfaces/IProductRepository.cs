using BookStore.Domain.Entities;
using BookStore.Domain.Specifications;

namespace BookStore.Domain.Interfaces;

/// <summary>
/// Defines product persistence operations.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Gets a product by identifier.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The product when found; otherwise, <see langword="null"/>.</returns>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products matching a specification.
    /// </summary>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching products.</returns>
    Task<IReadOnlyCollection<Product>> ListAsync(ISpecification<Product>? specification = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a product.
    /// </summary>
    /// <param name="product">The product to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a product.
    /// </summary>
    /// <param name="product">The product to remove.</param>
    void Remove(Product product);

    /// <summary>
    /// Searches products with optional filters.
    /// </summary>
    Task<IReadOnlyCollection<Product>> SearchAsync(
        string? searchTerm,
        bool? isActive,
        bool lowStockOnly,
        Guid? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        int? minQuantity,
        int? maxQuantity,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts products with optional filters.
    /// </summary>
    Task<int> CountAsync(
        string? searchTerm = null,
        bool? isActive = null,
        bool lowStockOnly = false,
        Guid? categoryId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int? minQuantity = null,
        int? maxQuantity = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a barcode exists.
    /// </summary>
    Task<bool> ExistsByBarcodeAsync(string barcode, Guid? excludedProductId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether an ISBN exists.
    /// </summary>
    Task<bool> ExistsByIsbnAsync(string isbn, Guid? excludedProductId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether completed sale items reference the product.
    /// </summary>
    Task<bool> HasCompletedSaleReferencesAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by barcode.
    /// </summary>
    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
}
