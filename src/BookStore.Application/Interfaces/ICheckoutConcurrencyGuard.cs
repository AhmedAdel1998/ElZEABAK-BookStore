namespace BookStore.Application.Interfaces;

/// <summary>
/// Prevents overlapping checkout operations on the local workstation.
/// </summary>
public interface ICheckoutConcurrencyGuard
{
    /// <summary>Attempts to enter the checkout critical section.</summary>
    Task<bool> TryEnterAsync(CancellationToken cancellationToken = default);

    /// <summary>Leaves the checkout critical section.</summary>
    void Exit();
}
