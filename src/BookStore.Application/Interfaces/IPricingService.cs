using BookStore.Application.Features.Sales.DTOs;

namespace BookStore.Application.Interfaces;

/// <summary>
/// Calculates POS cart totals, discounts, tax, and change.
/// </summary>
public interface IPricingService
{
    /// <summary>Recalculates totals for a sale session.</summary>
    Task<SaleSummaryDto> RecalculateAsync(SaleSessionDto sale, CancellationToken cancellationToken = default);
}
