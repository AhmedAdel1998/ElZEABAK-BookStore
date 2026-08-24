using BookStore.Application.Features.Sales.DTOs;
using BookStore.Application.Interfaces;

namespace BookStore.Application.Tests;

/// <summary>
/// In-memory session store used by the POS tests. Mirrors the open/active/held bookkeeping of the
/// real store so the tests exercise the same rules the workstation does.
/// </summary>
public sealed class InMemoryPosSaleSessionStore : IPosSaleSessionStore
{
    private readonly List<SaleSessionDto> _open = [];
    private readonly List<SaleSessionDto> _held = [];
    private Guid? _activeSaleId;

    /// <inheritdoc />
    public Task<SaleSessionDto?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_activeSaleId is null ? null : _open.FirstOrDefault(sale => sale.SaleId == _activeSaleId.Value));

    /// <inheritdoc />
    public Task<SaleSessionDto?> GetAsync(Guid saleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_open.FirstOrDefault(sale => sale.SaleId == saleId));

    /// <inheritdoc />
    public Task<IReadOnlyCollection<SaleSessionDto>> GetOpenAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<SaleSessionDto>>(_open.ToArray());

    /// <inheritdoc />
    public Task SaveCurrentAsync(SaleSessionDto sale, CancellationToken cancellationToken = default)
    {
        Upsert(sale);
        _activeSaleId = sale.SaleId;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveAsync(SaleSessionDto sale, CancellationToken cancellationToken = default)
    {
        Upsert(sale);
        _activeSaleId ??= sale.SaleId;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<SaleSessionDto?> SetActiveAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        var sale = _open.FirstOrDefault(existing => existing.SaleId == saleId);
        if (sale is not null)
        {
            _activeSaleId = saleId;
        }

        return Task.FromResult(sale);
    }

    /// <inheritdoc />
    public Task ClearCurrentAsync(CancellationToken cancellationToken = default)
    {
        Close(_activeSaleId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CloseAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        Close(saleId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SuspendAsync(SaleSessionDto sale, CancellationToken cancellationToken = default)
    {
        Close(sale.SaleId);
        _held.RemoveAll(existing => existing.SaleId == sale.SaleId);
        _held.Add(sale);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<SaleSessionDto>> GetHeldAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<SaleSessionDto>>(_held.ToArray());

    /// <inheritdoc />
    public Task<SaleSessionDto?> ResumeAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        var sale = _held.FirstOrDefault(existing => existing.SaleId == saleId);
        if (sale is null)
        {
            return Task.FromResult<SaleSessionDto?>(null);
        }

        _held.Remove(sale);
        sale.IsSuspended = false;
        Upsert(sale);
        _activeSaleId = saleId;
        return Task.FromResult<SaleSessionDto?>(sale);
    }

    /// <inheritdoc />
    public Task<int> GetReservedQuantityAsync(Guid productId, Guid? excludingSaleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_open
            .Where(sale => excludingSaleId is null || sale.SaleId != excludingSaleId.Value)
            .SelectMany(sale => sale.Items)
            .Where(item => item.ProductId == productId)
            .Sum(item => item.Quantity));

    /// <inheritdoc />
    public Task<IAsyncDisposable> LockAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IAsyncDisposable>(new NoOpLock());

    private sealed class NoOpLock : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private void Upsert(SaleSessionDto sale)
    {
        var index = _open.FindIndex(existing => existing.SaleId == sale.SaleId);
        if (index >= 0)
        {
            _open[index] = sale;
            return;
        }

        _open.Add(sale);
    }

    private void Close(Guid? saleId)
    {
        if (saleId is null)
        {
            return;
        }

        _open.RemoveAll(sale => sale.SaleId == saleId.Value);
        if (_activeSaleId == saleId.Value)
        {
            _activeSaleId = _open.Count == 0 ? null : _open[^1].SaleId;
        }
    }
}
