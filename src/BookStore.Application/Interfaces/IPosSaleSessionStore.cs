using BookStore.Application.Features.Sales.DTOs;

namespace BookStore.Application.Interfaces;

/// <summary>
/// Stores active and suspended POS sale sessions.
/// </summary>
public interface IPosSaleSessionStore
{
    /// <summary>Gets the current active sale.</summary>
    Task<SaleSessionDto?> GetCurrentAsync(CancellationToken cancellationToken = default);
    /// <summary>Saves the current active sale.</summary>
    Task SaveCurrentAsync(SaleSessionDto sale, CancellationToken cancellationToken = default);
    /// <summary>Clears the active sale.</summary>
    Task ClearCurrentAsync(CancellationToken cancellationToken = default);
    /// <summary>Saves a suspended sale.</summary>
    Task SuspendAsync(SaleSessionDto sale, CancellationToken cancellationToken = default);
    /// <summary>Gets suspended sales.</summary>
    Task<IReadOnlyCollection<SaleSessionDto>> GetHeldAsync(CancellationToken cancellationToken = default);
    /// <summary>Resumes a suspended sale.</summary>
    Task<SaleSessionDto?> ResumeAsync(Guid saleId, CancellationToken cancellationToken = default);
}
