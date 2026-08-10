namespace BookStore.Domain.Interfaces;

/// <summary>
/// Coordinates persistence changes across repositories.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Gets the product repository.
    /// </summary>
    IProductRepository Products { get; }

    /// <summary>
    /// Gets the category repository.
    /// </summary>
    ICategoryRepository Categories { get; }

    /// <summary>
    /// Gets the customer repository.
    /// </summary>
    ICustomerRepository Customers { get; }

    /// <summary>
    /// Gets the supplier repository.
    /// </summary>
    ISupplierRepository Suppliers { get; }

    /// <summary>
    /// Gets the user repository.
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Gets the role repository.
    /// </summary>
    IRoleRepository Roles { get; }

    /// <summary>
    /// Gets the sale repository.
    /// </summary>
    ISaleRepository Sales { get; }

    /// <summary>
    /// Gets the inventory repository.
    /// </summary>
    IInventoryRepository Inventory { get; }

    /// <summary>
    /// Begins a database transaction.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists pending changes.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The number of changed records.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
