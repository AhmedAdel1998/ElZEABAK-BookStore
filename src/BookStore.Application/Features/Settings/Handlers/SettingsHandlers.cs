using BookStore.Application.Features.Settings.Commands;
using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Settings.Queries;
using BookStore.Application.Features.Settings.Services;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.Shared.Results;

namespace BookStore.Application.Features.Settings.Handlers;

public sealed class SettingsQueryHandler
{
    private readonly ISettingsService _settingsService;
    private readonly IAuthorizationService _authorizationService;

    public SettingsQueryHandler(ISettingsService settingsService, IAuthorizationService authorizationService)
    {
        _settingsService = settingsService;
        _authorizationService = authorizationService;
    }

    public async Task<Result<SettingsSnapshotDto>> Handle(GetSettingsQuery query, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.SettingsView))
        {
            return Result<SettingsSnapshotDto>.Failure("You do not have permission to view settings.");
        }

        return Result<SettingsSnapshotDto>.Success(new SettingsSnapshotDto
        {
            Store = await _settingsService.GetAsync<StoreSettingsDto>(cancellationToken),
            POS = await _settingsService.GetAsync<POSSettingsDto>(cancellationToken),
            Receipt = await _settingsService.GetAsync<ReceiptSettingsDto>(cancellationToken),
            Printer = await _settingsService.GetAsync<PrinterSettingsDto>(cancellationToken),
            Tax = await _settingsService.GetAsync<TaxSettingsDto>(cancellationToken),
            Currency = await _settingsService.GetAsync<CurrencySettingsDto>(cancellationToken),
            Barcode = await _settingsService.GetAsync<BarcodeSettingsDto>(cancellationToken),
            Inventory = await _settingsService.GetAsync<InventorySettingsDto>(cancellationToken),
            Backup = await _settingsService.GetAsync<BackupSettingsDto>(cancellationToken),
            Security = await _settingsService.GetAsync<SecuritySettingsDto>(cancellationToken),
            Appearance = await _settingsService.GetAsync<AppearanceSettingsDto>(cancellationToken),
            Application = await _settingsService.GetAsync<ApplicationSettingsDto>(cancellationToken)
        });
    }

    public Task<Result<StoreSettingsDto>> Handle(GetStoreSettingsQuery query, CancellationToken cancellationToken = default) => GetAllowedAsync<StoreSettingsDto>(SettingsCategory.Store, cancellationToken);
    public Task<Result<POSSettingsDto>> Handle(GetPOSSettingsQuery query, CancellationToken cancellationToken = default) => GetAllowedAsync<POSSettingsDto>(SettingsCategory.POS, cancellationToken);
    public Task<Result<ReceiptSettingsDto>> Handle(GetReceiptSettingsQuery query, CancellationToken cancellationToken = default) => GetAllowedAsync<ReceiptSettingsDto>(SettingsCategory.Receipt, cancellationToken);
    public Task<Result<PrinterSettingsDto>> Handle(GetPrinterSettingsQuery query, CancellationToken cancellationToken = default) => GetAllowedAsync<PrinterSettingsDto>(SettingsCategory.Printer, cancellationToken);
    public Task<Result<TaxSettingsDto>> Handle(GetTaxSettingsQuery query, CancellationToken cancellationToken = default) => GetAllowedAsync<TaxSettingsDto>(SettingsCategory.Tax, cancellationToken);
    public Task<Result<CurrencySettingsDto>> Handle(GetCurrencySettingsQuery query, CancellationToken cancellationToken = default) => GetAllowedAsync<CurrencySettingsDto>(SettingsCategory.Currency, cancellationToken);
    public Task<Result<BarcodeSettingsDto>> Handle(GetBarcodeSettingsQuery query, CancellationToken cancellationToken = default) => GetAllowedAsync<BarcodeSettingsDto>(SettingsCategory.Barcode, cancellationToken);
    public Task<Result<InventorySettingsDto>> Handle(GetInventorySettingsQuery query, CancellationToken cancellationToken = default) => GetAllowedAsync<InventorySettingsDto>(SettingsCategory.Inventory, cancellationToken);
    public Task<Result<BackupSettingsDto>> Handle(GetBackupSettingsQuery query, CancellationToken cancellationToken = default) => GetAllowedAsync<BackupSettingsDto>(SettingsCategory.Backup, cancellationToken);
    public Task<Result<SecuritySettingsDto>> Handle(GetSecuritySettingsQuery query, CancellationToken cancellationToken = default) => GetAllowedAsync<SecuritySettingsDto>(SettingsCategory.Security, cancellationToken);
    public Task<Result<AppearanceSettingsDto>> Handle(GetAppearanceSettingsQuery query, CancellationToken cancellationToken = default) => GetAllowedAsync<AppearanceSettingsDto>(SettingsCategory.Appearance, cancellationToken);

    private async Task<Result<T>> GetAllowedAsync<T>(SettingsCategory category, CancellationToken cancellationToken)
        where T : class, new()
    {
        if (!_authorizationService.HasPermission(PermissionConstants.SettingsView) || !_authorizationService.HasPermission(SettingsPermissionMap.GetPermission(category)))
        {
            return Result<T>.Failure("You do not have permission to view this settings category.");
        }

        return Result<T>.Success(await _settingsService.GetAsync<T>(cancellationToken));
    }
}

public sealed class SettingsCommandHandler
{
    private readonly ISettingsService _settingsService;
    private readonly IAuthorizationService _authorizationService;

    public SettingsCommandHandler(ISettingsService settingsService, IAuthorizationService authorizationService)
    {
        _settingsService = settingsService;
        _authorizationService = authorizationService;
    }

    public Task<Result> Handle(UpdateStoreSettingsCommand command, CancellationToken cancellationToken = default) => SetAllowedAsync(SettingsCategory.Store, command.Settings, cancellationToken);
    public Task<Result> Handle(UpdatePOSSettingsCommand command, CancellationToken cancellationToken = default) => SetAllowedAsync(SettingsCategory.POS, command.Settings, cancellationToken);
    public Task<Result> Handle(UpdateReceiptSettingsCommand command, CancellationToken cancellationToken = default) => SetAllowedAsync(SettingsCategory.Receipt, command.Settings, cancellationToken);
    public Task<Result> Handle(UpdatePrinterSettingsCommand command, CancellationToken cancellationToken = default) => SetAllowedAsync(SettingsCategory.Printer, command.Settings, cancellationToken);
    public Task<Result> Handle(UpdateTaxSettingsCommand command, CancellationToken cancellationToken = default) => SetAllowedAsync(SettingsCategory.Tax, command.Settings, cancellationToken);
    public Task<Result> Handle(UpdateCurrencySettingsCommand command, CancellationToken cancellationToken = default) => SetAllowedAsync(SettingsCategory.Currency, command.Settings, cancellationToken);
    public Task<Result> Handle(UpdateBarcodeSettingsCommand command, CancellationToken cancellationToken = default) => SetAllowedAsync(SettingsCategory.Barcode, command.Settings, cancellationToken);
    public Task<Result> Handle(UpdateInventorySettingsCommand command, CancellationToken cancellationToken = default) => SetAllowedAsync(SettingsCategory.Inventory, command.Settings, cancellationToken);
    public Task<Result> Handle(UpdateBackupSettingsCommand command, CancellationToken cancellationToken = default) => SetAllowedAsync(SettingsCategory.Backup, command.Settings, cancellationToken);
    public Task<Result> Handle(UpdateSecuritySettingsCommand command, CancellationToken cancellationToken = default) => SetAllowedAsync(SettingsCategory.Security, command.Settings, cancellationToken);
    public Task<Result> Handle(UpdateAppearanceSettingsCommand command, CancellationToken cancellationToken = default) => SetAllowedAsync(SettingsCategory.Appearance, command.Settings, cancellationToken);

    public Task<Result> Handle(ResetSettingsCommand command, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.SettingsView))
        {
            return Task.FromResult(Result.Failure("You do not have permission to reset settings."));
        }

        if (command.Category is null)
        {
            return _settingsService.ResetAsync(cancellationToken);
        }

        if (!_authorizationService.HasPermission(SettingsPermissionMap.GetPermission(command.Category.Value)))
        {
            return Task.FromResult(Result.Failure("You do not have permission to reset this settings category."));
        }

        return _settingsService.ResetCategoryAsync(command.Category.Value, cancellationToken);
    }

    private Task<Result> SetAllowedAsync<T>(SettingsCategory category, T settings, CancellationToken cancellationToken)
        where T : class, new()
    {
        return !_authorizationService.HasPermission(SettingsPermissionMap.GetPermission(category))
            ? Task.FromResult(Result.Failure("You do not have permission to modify this settings category."))
            : _settingsService.SetAsync(settings, cancellationToken);
    }
}
