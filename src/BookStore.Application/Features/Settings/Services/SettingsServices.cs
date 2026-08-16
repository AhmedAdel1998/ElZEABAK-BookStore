using System.Collections.Concurrent;
using System.Text.Json;
using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Interfaces;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Constants;
using BookStore.Shared.Models;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookStore.Application.Features.Settings.Services;

public sealed record SettingsChangedEvent(SettingsCategory Category, Type SettingsType, string UpdatedBy, DateTimeOffset Timestamp);

public interface ISettingsChangedNotifier
{
    event EventHandler<SettingsChangedEvent>? SettingsChanged;
    void Notify(SettingsChangedEvent change);
}

public interface ISettingsCache
{
    bool TryGet<T>(out T? value)
        where T : class, new();

    void Set<T>(T value)
        where T : class, new();

    void Invalidate<T>()
        where T : class, new();

    void Clear();
}

public interface ISettingsService
{
    Task<T> GetAsync<T>(CancellationToken cancellationToken = default)
        where T : class, new();

    Task<Result> SetAsync<T>(T settings, CancellationToken cancellationToken = default)
        where T : class, new();

    Task<IReadOnlyList<SettingEntryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result> ResetAsync(CancellationToken cancellationToken = default);
    Task<Result> ResetCategoryAsync(SettingsCategory category, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task ReloadAsync(CancellationToken cancellationToken = default);
}

public sealed class SettingsChangedNotifier : ISettingsChangedNotifier
{
    public event EventHandler<SettingsChangedEvent>? SettingsChanged;

    public void Notify(SettingsChangedEvent change)
    {
        SettingsChanged?.Invoke(this, change);
    }
}

public sealed class SettingsCache : ISettingsCache
{
    private static readonly HashSet<Type> CacheableTypes =
    [
        typeof(StoreSettingsDto),
        typeof(POSSettingsDto),
        typeof(ReceiptSettingsDto),
        typeof(TaxSettingsDto),
        typeof(CurrencySettingsDto),
        typeof(BarcodeSettingsDto)
    ];

    private readonly ConcurrentDictionary<Type, object> _items = new();

    public bool TryGet<T>(out T? value)
        where T : class, new()
    {
        if (CacheableTypes.Contains(typeof(T)) && _items.TryGetValue(typeof(T), out var cached) && cached is T typed)
        {
            value = typed;
            return true;
        }

        value = null;
        return false;
    }

    public void Set<T>(T value)
        where T : class, new()
    {
        if (CacheableTypes.Contains(typeof(T)))
        {
            _items[typeof(T)] = value;
        }
    }

    public void Invalidate<T>()
        where T : class, new()
    {
        _items.TryRemove(typeof(T), out _);
    }

    public void Clear()
    {
        _items.Clear();
    }
}

public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };
    private readonly ISettingsStore _store;
    private readonly ISettingsCache _cache;
    private readonly ISettingsChangedNotifier _notifier;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOptions<ApplicationSettings> _defaults;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(
        ISettingsStore store,
        ISettingsCache cache,
        ISettingsChangedNotifier notifier,
        IServiceProvider serviceProvider,
        ICurrentUserService currentUserService,
        IOptions<ApplicationSettings> defaults,
        ILogger<SettingsService> logger)
    {
        _store = store;
        _cache = cache;
        _notifier = notifier;
        _serviceProvider = serviceProvider;
        _currentUserService = currentUserService;
        _defaults = defaults;
        _logger = logger;
    }

    public async Task<T> GetAsync<T>(CancellationToken cancellationToken = default)
        where T : class, new()
    {
        if (_cache.TryGet<T>(out var cached) && cached is not null)
        {
            return cached;
        }

        var definition = SettingsRegistry.GetByType<T>();
        var record = await _store.GetByKeyAsync(definition.Key, cancellationToken);
        var value = record is null ? (T)definition.CreateDefault(_defaults.Value) : Deserialize<T>(record);
        _cache.Set(value);
        return value;
    }

    public async Task<Result> SetAsync<T>(T settings, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(settings);
        var definition = SettingsRegistry.GetByType<T>();
        var validator = _serviceProvider.GetService<IValidator<T>>();
        if (validator is not null)
        {
            var validation = await validator.ValidateAsync(settings, cancellationToken);
            if (!validation.IsValid)
            {
                return Result.Failure(validation.Errors[0].ErrorMessage);
            }
        }

        await _store.BeginTransactionAsync(cancellationToken);
        try
        {
            var oldRecord = await _store.GetByKeyAsync(definition.Key, cancellationToken);
            var updatedBy = _currentUserService.Username ?? "System";
            var value = JsonSerializer.Serialize(settings, JsonOptions);
            var record = new PersistedSettingRecord(definition.Key, value, definition.Category.ToString(), typeof(T).FullName ?? typeof(T).Name, definition.Description, definition.IsEncrypted, definition.IsSystemSetting, DateTimeOffset.UtcNow, updatedBy);
            await _store.UpsertAsync(record, cancellationToken);
            await _store.CommitAsync(cancellationToken);

            _cache.Invalidate<T>();
            _cache.Set(settings);
            _logger.LogInformation("Settings changed. Category={Category} Key={Key} User={User} OldValuePresent={OldValuePresent}", definition.Category, definition.Key, updatedBy, oldRecord is not null);
            _notifier.Notify(new SettingsChangedEvent(definition.Category, typeof(T), updatedBy, DateTimeOffset.UtcNow));
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _store.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Settings update failed. Category={Category} Key={Key}", definition.Category, definition.Key);
            return Result.Failure("Settings could not be saved.");
        }
    }

    public async Task<IReadOnlyList<SettingEntryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var records = await _store.GetAllAsync(cancellationToken);
        return records
            .Select(record => new SettingEntryDto(record.Key, Enum.Parse<SettingsCategory>(record.Category), record.DataType, record.Description, record.IsEncrypted, record.IsSystemSetting, record.UpdatedAt, record.UpdatedBy))
            .OrderBy(item => item.Category)
            .ToArray();
    }

    public async Task<Result> ResetAsync(CancellationToken cancellationToken = default)
    {
        foreach (var category in SettingsRegistry.Definitions.Select(definition => definition.Category))
        {
            if (category is SettingsCategory.Security or SettingsCategory.Backup)
            {
                continue;
            }

            var result = await ResetCategoryAsync(category, cancellationToken);
            if (!result.IsSuccess)
            {
                return result;
            }
        }

        return Result.Success();
    }

    public async Task<Result> ResetCategoryAsync(SettingsCategory category, CancellationToken cancellationToken = default)
    {
        var definition = SettingsRegistry.GetByCategory(category);
        await _store.BeginTransactionAsync(cancellationToken);
        try
        {
            await _store.DeleteAsync(definition.Key, cancellationToken);
            await _store.CommitAsync(cancellationToken);
            _cache.Clear();
            _logger.LogInformation("Settings reset. Category={Category} User={User}", category, _currentUserService.Username ?? "System");
            _notifier.Notify(new SettingsChangedEvent(category, definition.SettingsType, _currentUserService.Username ?? "System", DateTimeOffset.UtcNow));
            return Result.Success();
        }
        catch (Exception ex)
        {
            await _store.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Settings reset failed. Category={Category}", category);
            return Result.Failure("Settings could not be reset.");
        }
    }

    public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        _cache.Clear();
        return Task.CompletedTask;
    }

    private static T Deserialize<T>(PersistedSettingRecord record)
        where T : class, new()
    {
        if (record.DataType != (typeof(T).FullName ?? typeof(T).Name))
        {
            throw new InvalidOperationException($"Setting '{record.Key}' has invalid data type '{record.DataType}'.");
        }

        return JsonSerializer.Deserialize<T>(record.Value, JsonOptions) ?? new T();
    }
}

internal sealed record SettingsDefinition(string Key, SettingsCategory Category, Type SettingsType, string Description, bool IsEncrypted, bool IsSystemSetting, Func<ApplicationSettings, object> CreateDefault);

internal static class SettingsRegistry
{
    public static IReadOnlyList<SettingsDefinition> Definitions { get; } =
    [
        new("Settings.Store", SettingsCategory.Store, typeof(StoreSettingsDto), "Store identity and receipt header settings", false, false, app => new StoreSettingsDto { StoreName = app.Store.Name, Address = app.Store.Address, Phone = app.Store.Phone, TaxNumber = app.Store.TaxNumber, LogoPath = app.Store.LogoPath }),
        new("Settings.POS", SettingsCategory.POS, typeof(POSSettingsDto), "POS behavior settings", false, false, app => new POSSettingsDto { AutoPrintReceipt = app.Printer.AutoPrint }),
        new("Settings.Receipt", SettingsCategory.Receipt, typeof(ReceiptSettingsDto), "Receipt content and paper settings", false, false, app => new ReceiptSettingsDto { PaperWidth = app.Printer.PaperWidthMm, Copies = app.Printer.Copies, ShowLogo = app.Printer.PrintLogo, ShowCustomer = app.Printer.PrintCustomerInformation, FooterText = app.Printer.ReceiptFooter, PrintQRCode = app.Printer.PrintQrCode, CutPaper = app.Printer.CutPaper, OpenCashDrawer = app.Printer.OpenCashDrawer }),
        new("Settings.Printer", SettingsCategory.Printer, typeof(PrinterSettingsDto), "Printer selection and connection settings", false, false, app => new PrinterSettingsDto { PrinterName = app.Printer.DefaultPrinterName, PrintTimeout = app.Printer.PrintTimeoutMilliseconds }),
        new("Settings.Tax", SettingsCategory.Tax, typeof(TaxSettingsDto), "Tax calculation settings", false, false, app => new TaxSettingsDto { Enabled = app.Store.TaxRate > 0, DefaultRate = app.Store.TaxRate }),
        new("Settings.Currency", SettingsCategory.Currency, typeof(CurrencySettingsDto), "Currency formatting settings", false, false, app => new CurrencySettingsDto { CurrencyCode = app.Store.Currency, CurrencySymbol = app.Store.Currency }),
        new("Settings.Barcode", SettingsCategory.Barcode, typeof(BarcodeSettingsDto), "Barcode generation and scanner settings", false, false, app => new BarcodeSettingsDto { DefaultFormat = app.Barcode.DefaultType, Prefix = app.Barcode.Prefix, StartingNumber = app.Barcode.StartingNumber, ScanTimeout = app.Barcode.ScanTimeoutMilliseconds, AutoGenerate = app.Barcode.AutomaticGenerationEnabled }),
        new("Settings.Inventory", SettingsCategory.Inventory, typeof(InventorySettingsDto), "Inventory policy settings", false, false, app => new InventorySettingsDto()),
        new("Settings.Backup", SettingsCategory.Backup, typeof(BackupSettingsDto), "Backup schedule and retention settings", false, false, app => new BackupSettingsDto { Enabled = app.Backup.Enabled, Frequency = app.Backup.Frequency, BackupLocation = app.Backup.Folder, RetentionCount = app.Backup.RetentionCount, ValidateAfterBackup = app.Backup.ValidateAfterBackup, CreatePreRestoreBackup = app.Backup.CreatePreRestoreBackup }),
        new("Settings.Security", SettingsCategory.Security, typeof(SecuritySettingsDto), "Authentication and session safety settings", true, true, app => new SecuritySettingsDto { SessionTimeout = app.Authentication.SessionTimeoutMinutes, MaxLoginAttempts = app.Authentication.MaxFailedLoginAttempts, LockoutDuration = app.Authentication.LockoutMinutes }),
        new("Settings.Appearance", SettingsCategory.Appearance, typeof(AppearanceSettingsDto), "Theme and language settings", false, false, app => new AppearanceSettingsDto { Theme = app.UserInterface.Theme, Language = app.UserInterface.Language }),
        new("Settings.Application", SettingsCategory.Application, typeof(ApplicationSettingsDto), "Read-only application information", false, true, app => new ApplicationSettingsDto())
    ];

    public static SettingsDefinition GetByType<T>() => Definitions.Single(definition => definition.SettingsType == typeof(T));

    public static SettingsDefinition GetByCategory(SettingsCategory category) => Definitions.Single(definition => definition.Category == category);
}

public static class SettingsPermissionMap
{
    public static string GetPermission(SettingsCategory category) => category switch
    {
        SettingsCategory.Store => PermissionConstants.SettingsStore,
        SettingsCategory.POS => PermissionConstants.SettingsPOS,
        SettingsCategory.Receipt => PermissionConstants.SettingsReceipt,
        SettingsCategory.Printer => PermissionConstants.SettingsPrinter,
        SettingsCategory.Tax => PermissionConstants.SettingsTax,
        SettingsCategory.Currency => PermissionConstants.SettingsCurrency,
        SettingsCategory.Barcode => PermissionConstants.SettingsBarcode,
        SettingsCategory.Inventory => PermissionConstants.SettingsInventory,
        SettingsCategory.Backup => PermissionConstants.SettingsBackup,
        SettingsCategory.Security => PermissionConstants.SettingsSecurity,
        SettingsCategory.Appearance => PermissionConstants.SettingsAppearance,
        _ => PermissionConstants.SettingsView
    };
}
