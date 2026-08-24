using BookStore.Application.Features.Sales.DTOs;

namespace BookStore.Application.Interfaces;

/// <summary>
/// Answers stock questions for POS carts, accounting for the units the other open invoices are
/// already holding.
/// </summary>
public interface IPosCartStockService
{
    /// <summary>
    /// Gets how many units of a product an invoice may still take, after subtracting the units the
    /// other open invoices are holding. Returns zero when the product is missing or inactive.
    /// </summary>
    /// <param name="productId">The product to check.</param>
    /// <param name="saleId">The invoice asking for stock. Its own lines are not counted against it.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<int> GetAvailableAsync(Guid productId, Guid saleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Brings an open invoice back in line with live stock: refreshes each line's remaining stock,
    /// reduces or drops lines that no longer fit, and reports what it changed.
    /// </summary>
    /// <param name="sale">The invoice to reconcile. Mutated in place.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IReadOnlyCollection<PosCartAdjustmentDto>> SynchronizeAsync(SaleSessionDto sale, CancellationToken cancellationToken = default);
}
