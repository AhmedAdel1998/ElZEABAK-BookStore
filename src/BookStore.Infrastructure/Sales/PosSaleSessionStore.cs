using System.Text.Json;
using System.Text.Json.Serialization;
using BookStore.Application.Features.Sales.DTOs;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace BookStore.Infrastructure.Sales;

/// <summary>
/// Stores the open and suspended POS sale sessions on the local workstation.
/// </summary>
public sealed class PosSaleSessionStore : IPosSaleSessionStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ILogger<PosSaleSessionStore> _logger;

    /// <summary>Guards a single file read or write.</summary>
    private readonly SemaphoreSlim _sync = new(1, 1);

    /// <summary>
    /// Guards a whole command's read-modify-write. Separate from <see cref="_sync"/> so that taking it
    /// does not block the individual reads and writes performed while it is held.
    /// </summary>
    private readonly SemaphoreSlim _operationGate = new(1, 1);

    private readonly string _storePath;

    /// <summary>Initializes a new instance of the <see cref="PosSaleSessionStore"/> class.</summary>
    public PosSaleSessionStore(ILogger<PosSaleSessionStore> logger)

        // The install directory is read-only on a real deployment, so the cart file has to live under
        // the resolved writable data root like every other runtime file.
        : this(logger, ApplicationPaths.ResolveDataPath(FolderConstants.Temp))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PosSaleSessionStore"/> class that keeps its cart
    /// file in an explicit folder.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="storeDirectory">The folder the cart file is written to.</param>
    public PosSaleSessionStore(ILogger<PosSaleSessionStore> logger, string storeDirectory)
    {
        _logger = logger;
        Directory.CreateDirectory(storeDirectory);
        _storePath = Path.Combine(storeDirectory, "pos-sales.json");
    }

    /// <inheritdoc />
    public Task<SaleSessionDto?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        ReadAsync(state => state.ActiveSale, cancellationToken);

    /// <inheritdoc />
    public Task<SaleSessionDto?> GetAsync(Guid saleId, CancellationToken cancellationToken = default) =>
        ReadAsync(state => state.OpenSales.FirstOrDefault(sale => sale.SaleId == saleId), cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyCollection<SaleSessionDto>> GetOpenAsync(CancellationToken cancellationToken = default) =>
        ReadAsync(state => (IReadOnlyCollection<SaleSessionDto>)state.OpenSales.ToArray(), cancellationToken);

    /// <inheritdoc />
    public Task SaveCurrentAsync(SaleSessionDto sale, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sale);
        return MutateAsync(
            state =>
            {
                Upsert(state, sale);
                state.ActiveSaleId = sale.SaleId;
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveAsync(SaleSessionDto sale, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sale);
        return MutateAsync(state => Upsert(state, sale), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SaleSessionDto?> SetActiveAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        SaleSessionDto? activated = null;
        await MutateAsync(
            state =>
            {
                activated = state.OpenSales.FirstOrDefault(sale => sale.SaleId == saleId);
                if (activated is not null)
                {
                    state.ActiveSaleId = saleId;
                }
            },
            cancellationToken);

        return activated;
    }

    /// <inheritdoc />
    public Task ClearCurrentAsync(CancellationToken cancellationToken = default) =>
        MutateAsync(state => Close(state, state.ActiveSaleId), cancellationToken);

    /// <inheritdoc />
    public Task CloseAsync(Guid saleId, CancellationToken cancellationToken = default) =>
        MutateAsync(state => Close(state, saleId), cancellationToken);

    /// <inheritdoc />
    public Task SuspendAsync(SaleSessionDto sale, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sale);
        return MutateAsync(
            state =>
            {
                Close(state, sale.SaleId);
                state.HeldSales.RemoveAll(existing => existing.SaleId == sale.SaleId);
                state.HeldSales.Add(sale);
                _logger.LogInformation("POS sale suspended. Invoice={InvoiceNumber}", sale.InvoiceNumber);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<SaleSessionDto>> GetHeldAsync(CancellationToken cancellationToken = default) =>
        ReadAsync(state => (IReadOnlyCollection<SaleSessionDto>)state.HeldSales.ToArray(), cancellationToken);

    /// <inheritdoc />
    public async Task<SaleSessionDto?> ResumeAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        SaleSessionDto? resumed = null;
        await MutateAsync(
            state =>
            {
                resumed = state.HeldSales.FirstOrDefault(existing => existing.SaleId == saleId);
                if (resumed is null)
                {
                    return;
                }

                state.HeldSales.RemoveAll(existing => existing.SaleId == saleId);
                resumed.IsSuspended = false;
                Upsert(state, resumed);
                state.ActiveSaleId = saleId;
                _logger.LogInformation("POS sale resumed. Invoice={InvoiceNumber}", resumed.InvoiceNumber);
            },
            cancellationToken);

        return resumed;
    }

    /// <inheritdoc />
    public Task<int> GetReservedQuantityAsync(Guid productId, Guid? excludingSaleId, CancellationToken cancellationToken = default) =>
        ReadAsync(
            state => state.OpenSales
                .Where(sale => excludingSaleId is null || sale.SaleId != excludingSaleId.Value)
                .SelectMany(sale => sale.Items)
                .Where(item => item.ProductId == productId)
                .Sum(item => item.Quantity),
            cancellationToken);

    /// <inheritdoc />
    public async Task<IAsyncDisposable> LockAsync(CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken);
        return new OperationLock(_operationGate);
    }

    private static void Upsert(PosSaleSessionStoreState state, SaleSessionDto sale)
    {
        var index = state.OpenSales.FindIndex(existing => existing.SaleId == sale.SaleId);
        if (index >= 0)
        {
            state.OpenSales[index] = sale;
            return;
        }

        state.OpenSales.Add(sale);
    }

    private static void Close(PosSaleSessionStoreState state, Guid? saleId)
    {
        if (saleId is null)
        {
            return;
        }

        state.OpenSales.RemoveAll(sale => sale.SaleId == saleId.Value);
        if (state.ActiveSaleId == saleId.Value)
        {
            // The most recently created remaining invoice takes over, so the cashier is never left
            // looking at a screen with no invoice while other carts are still open.
            state.ActiveSaleId = state.OpenSales.Count == 0
                ? null
                : state.OpenSales[^1].SaleId;
        }
    }

    private async Task<T> ReadAsync<T>(Func<PosSaleSessionStoreState, T> projection, CancellationToken cancellationToken)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            return projection(await LoadStateAsync(cancellationToken));
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task MutateAsync(Action<PosSaleSessionStoreState> mutation, CancellationToken cancellationToken)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadStateAsync(cancellationToken);
            mutation(state);
            await SaveStateAsync(state, cancellationToken);
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task<PosSaleSessionStoreState> LoadStateAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storePath))
        {
            return new PosSaleSessionStoreState();
        }

        PosSaleSessionStoreState state;
        try
        {
            await using var stream = File.OpenRead(_storePath);
            state = await JsonSerializer.DeserializeAsync<PosSaleSessionStoreState>(stream, SerializerOptions, cancellationToken)
                ?? new PosSaleSessionStoreState();
        }
        catch (JsonException ex)
        {
            // A cart file truncated by a power cut used to throw on every later POS operation, which
            // left the till unusable until somebody deleted the file by hand.
            _logger.LogError(ex, "POS session file was unreadable and has been quarantined. Path={Path}", _storePath);
            Quarantine();
            return new PosSaleSessionStoreState();
        }

        Normalize(state);
        return state;
    }

    private static void Normalize(PosSaleSessionStoreState state)
    {
        // Files written before invoices could be open side by side carry a single CurrentSale.
        if (state.CurrentSale is not null)
        {
            Upsert(state, state.CurrentSale);
            state.ActiveSaleId ??= state.CurrentSale.SaleId;
            state.CurrentSale = null;
        }

        state.OpenSales.RemoveAll(sale => sale is null);
        state.HeldSales.RemoveAll(sale => sale is null);

        // A held sale must never also be open, and an invoice must never appear twice.
        var seen = new HashSet<Guid>();
        state.OpenSales.RemoveAll(sale => !seen.Add(sale.SaleId));
        state.HeldSales.RemoveAll(sale => seen.Contains(sale.SaleId) || !seen.Add(sale.SaleId));

        foreach (var sale in state.OpenSales)
        {
            sale.IsSuspended = false;
        }

        foreach (var sale in state.HeldSales)
        {
            sale.IsSuspended = true;
        }

        if (state.ActiveSaleId is not null && state.OpenSales.All(sale => sale.SaleId != state.ActiveSaleId.Value))
        {
            state.ActiveSaleId = null;
        }

        state.ActiveSaleId ??= state.OpenSales.Count == 0 ? null : state.OpenSales[^1].SaleId;
    }

    private void Quarantine()
    {
        try
        {
            File.Move(_storePath, _storePath + ".corrupt", overwrite: true);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unreadable POS session file could not be moved aside. Path={Path}", _storePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unreadable POS session file could not be moved aside. Path={Path}", _storePath);
        }
    }

    private async Task SaveStateAsync(PosSaleSessionStoreState state, CancellationToken cancellationToken)
    {
        // Written to a sibling file and swapped in, so an interrupted write cannot leave a half
        // serialized cart behind. Writing in place is what made the file corruptible.
        var temporaryPath = _storePath + ".tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, state, SerializerOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(temporaryPath, _storePath, overwrite: true);
    }

    /// <summary>Releases the session lock once, however many times it is disposed.</summary>
    private sealed class OperationLock(SemaphoreSlim gate) : IAsyncDisposable
    {
        private int _released;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                gate.Release();
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class PosSaleSessionStoreState
    {
        /// <summary>Gets or sets the single active sale written by earlier versions of the store.</summary>
        public SaleSessionDto? CurrentSale { get; set; }

        public Guid? ActiveSaleId { get; set; }

        public List<SaleSessionDto> OpenSales { get; set; } = [];

        public List<SaleSessionDto> HeldSales { get; set; } = [];

        [JsonIgnore]
        public SaleSessionDto? ActiveSale => ActiveSaleId is null
            ? null
            : OpenSales.FirstOrDefault(sale => sale.SaleId == ActiveSaleId.Value);
    }
}
