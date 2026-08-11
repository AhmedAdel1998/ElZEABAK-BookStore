using BookStore.Domain.Entities;
using BookStore.Domain.Specifications;

namespace BookStore.Domain.Interfaces;

/// <summary>
/// Defines sale persistence operations.
/// </summary>
public interface ISaleRepository
{
    /// <summary>
    /// Gets a sale by identifier.
    /// </summary>
    /// <param name="id">The sale identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The sale when found; otherwise, <see langword="null"/>.</returns>
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets a completed sale with receipt details.
    /// </summary>
    Task<Sale?> GetCompletedWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets a completed sale with receipt details by invoice number.
    /// </summary>
    Task<Sale?> GetCompletedByInvoiceAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets sales matching a specification.
    /// </summary>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching sales.</returns>
    Task<IReadOnlyCollection<Sale>> ListAsync(ISpecification<Sale>? specification = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Searches completed sales for receipt reprint.
    /// </summary>
    Task<IReadOnlyCollection<Sale>> SearchCompletedAsync(string? invoiceNumber, DateTimeOffset? date, Guid? cashierId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Adds a sale.
    /// </summary>
    /// <param name="sale">The sale to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
