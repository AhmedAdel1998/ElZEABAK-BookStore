using BookStore.Domain.Entities;
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
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets suppliers matching a specification.
    /// </summary>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching suppliers.</returns>
    Task<IReadOnlyCollection<Supplier>> ListAsync(ISpecification<Supplier>? specification = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Adds a supplier.
    /// </summary>
    /// <param name="supplier">The supplier to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
