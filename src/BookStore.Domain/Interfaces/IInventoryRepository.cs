using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Specifications;

namespace BookStore.Domain.Interfaces;

/// <summary>
/// Defines inventory transaction persistence operations.
/// </summary>
public interface IInventoryRepository
{
    /// <summary>
    /// Gets an inventory transaction by identifier.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The transaction when found; otherwise, <see langword="null"/>.</returns>
    Task<InventoryTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inventory transactions matching a specification.
    /// </summary>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching transactions.</returns>
    Task<IReadOnlyCollection<InventoryTransaction>> ListAsync(ISpecification<InventoryTransaction>? specification = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an inventory transaction.
    /// </summary>
    /// <param name="transaction">The transaction to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(InventoryTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches inventory transaction history.
    /// </summary>
    Task<IReadOnlyCollection<InventoryTransaction>> SearchHistoryAsync(
        Guid? productId,
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        InventoryTransactionType? transactionType,
        Guid? userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts inventory transaction history rows.
    /// </summary>
    Task<int> CountHistoryAsync(
        Guid? productId = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        InventoryTransactionType? transactionType = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}
