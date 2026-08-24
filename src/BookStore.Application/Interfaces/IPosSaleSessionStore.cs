using BookStore.Application.Features.Sales.DTOs;

namespace BookStore.Application.Interfaces;

/// <summary>
/// Stores the POS sale sessions of a workstation.
/// </summary>
/// <remarks>
/// A workstation keeps several invoices open at once. Every open invoice is a full cart with its own
/// lines, customer, discount and payment draft; exactly one of them is the active invoice that the
/// cashier screen is currently editing. Suspended ("held") sales are a separate, closed set that is
/// only re-opened on request.
/// </remarks>
public interface IPosSaleSessionStore
{
    /// <summary>Gets the active open invoice, or <see langword="null"/> when none is open.</summary>
    Task<SaleSessionDto?> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets one open invoice by identifier, or <see langword="null"/> when it is not open.</summary>
    Task<SaleSessionDto?> GetAsync(Guid saleId, CancellationToken cancellationToken = default);

    /// <summary>Gets every open invoice, oldest first.</summary>
    Task<IReadOnlyCollection<SaleSessionDto>> GetOpenAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds or updates an open invoice and makes it the active one.</summary>
    Task SaveCurrentAsync(SaleSessionDto sale, CancellationToken cancellationToken = default);

    /// <summary>Adds or updates an open invoice without changing which one is active.</summary>
    Task SaveAsync(SaleSessionDto sale, CancellationToken cancellationToken = default);

    /// <summary>Makes an already open invoice the active one, returning it when it exists.</summary>
    Task<SaleSessionDto?> SetActiveAsync(Guid saleId, CancellationToken cancellationToken = default);

    /// <summary>Closes the active invoice and activates the most recent remaining one.</summary>
    Task ClearCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>Closes one open invoice, activating another one when the closed invoice was active.</summary>
    Task CloseAsync(Guid saleId, CancellationToken cancellationToken = default);

    /// <summary>Moves an invoice out of the open set and into the held set.</summary>
    Task SuspendAsync(SaleSessionDto sale, CancellationToken cancellationToken = default);

    /// <summary>Gets held sales.</summary>
    Task<IReadOnlyCollection<SaleSessionDto>> GetHeldAsync(CancellationToken cancellationToken = default);

    /// <summary>Moves a held sale back into the open set and makes it active.</summary>
    Task<SaleSessionDto?> ResumeAsync(Guid saleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets how many units of a product are already committed by open invoices other than
    /// <paramref name="excludingSaleId"/>. Used to stop two open invoices from selling the same unit.
    /// </summary>
    Task<int> GetReservedQuantityAsync(Guid productId, Guid? excludingSaleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Takes the workstation's POS session lock, released by disposing the result.
    /// </summary>
    /// <remarks>
    /// Every command that reads an invoice, changes it and writes it back must hold this for the whole
    /// sequence. Without it two commands can each load the same invoice, add a line, and save over one
    /// another -- losing a scan, which undercharges -- and a command in flight during checkout can
    /// write a completed invoice back into the open set.
    /// </remarks>
    Task<IAsyncDisposable> LockAsync(CancellationToken cancellationToken = default);
}
