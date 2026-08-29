using BookStore.Domain.Entities;
using BookStore.Domain.ReadModels;
using BookStore.Domain.Specifications;

namespace BookStore.Domain.Interfaces;

/// <summary>
/// Defines supplier persistence operations.
/// </summary>
public interface ISupplierRepository
{
    /// <summary>
    /// Gets a supplier by identifier.
    /// </summary>
    /// <param name="id">The supplier identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The supplier when found; otherwise, <see langword="null"/>.</returns>
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suppliers matching a specification.
    /// </summary>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching suppliers.</returns>
    Task<IReadOnlyCollection<Supplier>> ListAsync(ISpecification<Supplier>? specification = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a supplier.
    /// </summary>
    /// <param name="supplier">The supplier to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a supplier exists with the same company name.
    /// </summary>
    Task<bool> ExistsByCompanyNameAsync(string companyName, Guid? excludedSupplierId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches suppliers using database-side filtering.
    /// </summary>
    Task<IReadOnlyCollection<SupplierListReadModel>> SearchAsync(string? searchTerm, bool? isActive, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts suppliers matching a search filter.
    /// </summary>
    Task<int> CountAsync(string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one supplier summary row.
    /// </summary>
    Task<SupplierListReadModel?> GetSummaryByIdAsync(Guid supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products associated with a supplier using an efficient projection.
    /// </summary>
    Task<IReadOnlyCollection<SupplierProductReadModel>> GetProductsAsync(Guid supplierId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts products associated with a supplier.
    /// </summary>
    Task<int> CountProductsAsync(Guid supplierId, CancellationToken cancellationToken = default);
}
