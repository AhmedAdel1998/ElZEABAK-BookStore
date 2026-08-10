using BookStore.Application.Features.Sales.DTOs;

namespace BookStore.Application.Interfaces;

/// <summary>
/// Prepares receipt models after checkout without printing.
/// </summary>
public interface IReceiptPreparationService
{
    /// <summary>Prepares receipt data for later printing.</summary>
    Task PrepareAsync(ReceiptModel receipt, CancellationToken cancellationToken = default);
}
