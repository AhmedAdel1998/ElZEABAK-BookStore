using BookStore.Application.Interfaces;

namespace BookStore.Application.Features.Sales.Services;

/// <summary>
/// Process-local checkout guard used to reject accidental double checkout.
/// </summary>
public sealed class CheckoutConcurrencyGuard : ICheckoutConcurrencyGuard
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <inheritdoc />
    public async Task<bool> TryEnterAsync(CancellationToken cancellationToken = default)
    {
        return await _semaphore.WaitAsync(0, cancellationToken);
    }

    /// <inheritdoc />
    public void Exit()
    {
        _semaphore.Release();
    }
}
