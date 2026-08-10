using System.Text.Json;
using BookStore.Application.Features.Sales.DTOs;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace BookStore.Infrastructure.Sales;

/// <summary>
/// Stores the active and suspended POS sale sessions on the local workstation.
/// </summary>
public sealed class PosSaleSessionStore : IPosSaleSessionStore
{
    private readonly ILogger<PosSaleSessionStore> _logger;
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly string _storePath;
    private SaleSessionDto? _currentSale;

    /// <summary>Initializes a new instance of the <see cref="PosSaleSessionStore"/> class.</summary>
    public PosSaleSessionStore(ILogger<PosSaleSessionStore> logger)
    {
        _logger = logger;
        var tempFolder = Path.Combine(AppContext.BaseDirectory, FolderConstants.Temp);
        Directory.CreateDirectory(tempFolder);
        _storePath = Path.Combine(tempFolder, "pos-sales.json");
    }

    /// <inheritdoc />
    public async Task<SaleSessionDto?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            await LoadAsync(cancellationToken);
            return _currentSale;
        }
        finally
        {
            _sync.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveCurrentAsync(SaleSessionDto sale, CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            await LoadAsync(cancellationToken);
            _currentSale = sale;
            await SaveAsync(cancellationToken);
        }
        finally
        {
            _sync.Release();
        }
    }

    /// <inheritdoc />
    public async Task ClearCurrentAsync(CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            await LoadAsync(cancellationToken);
            _currentSale = null;
            await SaveAsync(cancellationToken);
        }
        finally
        {
            _sync.Release();
        }
    }

    /// <inheritdoc />
    public async Task SuspendAsync(SaleSessionDto sale, CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadStateAsync(cancellationToken);
            state.HeldSales.RemoveAll(existing => existing.SaleId == sale.SaleId);
            state.HeldSales.Add(sale);
            state.CurrentSale = null;
            _currentSale = null;
            await SaveStateAsync(state, cancellationToken);
            _logger.LogInformation("POS sale suspended. Invoice={InvoiceNumber}", sale.InvoiceNumber);
        }
        finally
        {
            _sync.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<SaleSessionDto>> GetHeldAsync(CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadStateAsync(cancellationToken);
            return state.HeldSales.ToArray();
        }
        finally
        {
            _sync.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SaleSessionDto?> ResumeAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadStateAsync(cancellationToken);
            var sale = state.HeldSales.FirstOrDefault(existing => existing.SaleId == saleId);
            if (sale is null)
            {
                return null;
            }

            state.HeldSales.RemoveAll(existing => existing.SaleId == saleId);
            state.CurrentSale = sale;
            _currentSale = sale;
            await SaveStateAsync(state, cancellationToken);
            _logger.LogInformation("POS sale resumed. Invoice={InvoiceNumber}", sale.InvoiceNumber);
            return sale;
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var state = await LoadStateAsync(cancellationToken);
        _currentSale = state.CurrentSale;
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var state = await LoadStateAsync(cancellationToken);
        state.CurrentSale = _currentSale;
        await SaveStateAsync(state, cancellationToken);
    }

    private async Task<PosSaleSessionStoreState> LoadStateAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            return new PosSaleSessionStoreState();
        }

        await using var stream = File.OpenRead(_storePath);
        return await JsonSerializer.DeserializeAsync<PosSaleSessionStoreState>(stream, cancellationToken: cancellationToken) ?? new PosSaleSessionStoreState();
    }

    private async Task SaveStateAsync(PosSaleSessionStoreState state, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_storePath);
        await JsonSerializer.SerializeAsync(stream, state, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
    }

    private sealed class PosSaleSessionStoreState
    {
        public SaleSessionDto? CurrentSale { get; set; }

        public List<SaleSessionDto> HeldSales { get; set; } = [];
    }
}
