using BookStore.Domain.Entities;
using BookStore.Domain.ReadModels;
using BookStore.Domain.Specifications;

namespace BookStore.Domain.Interfaces;

/// <summary>
/// Defines customer persistence operations.
/// </summary>
public interface ICustomerRepository
{
    /// <summary>
    /// Gets a customer by identifier.
    /// </summary>
    /// <param name="id">The customer identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The customer when found; otherwise, <see langword="null"/>.</returns>
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customers matching a specification.
    /// </summary>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching customers.</returns>
    Task<IReadOnlyCollection<Customer>> ListAsync(ISpecification<Customer>? specification = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a customer.
    /// </summary>
    /// <param name="customer">The customer to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a phone number already exists.
    /// </summary>
    Task<bool> ExistsByPhoneAsync(string phone, Guid? excludedCustomerId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches customers using database-side filtering and sales summary projection.
    /// </summary>
    Task<IReadOnlyCollection<CustomerListReadModel>> SearchAsync(string? searchTerm, bool? isActive, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts customers matching a search filter.
    /// </summary>
    Task<int> CountAsync(string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one customer summary row.
    /// </summary>
    Task<CustomerListReadModel?> GetSummaryByIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customer sales history using a paged projection.
    /// </summary>
    Task<IReadOnlyCollection<CustomerSaleHistoryReadModel>> GetSalesHistoryAsync(Guid customerId, DateTimeOffset? dateFrom, DateTimeOffset? dateTo, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts customer sales history rows.
    /// </summary>
    Task<int> CountSalesHistoryAsync(Guid customerId, DateTimeOffset? dateFrom = null, DateTimeOffset? dateTo = null, CancellationToken cancellationToken = default);
}
