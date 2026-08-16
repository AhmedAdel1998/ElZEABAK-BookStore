using BookStore.Application.Features.Settings.Commands;
using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Settings.Queries;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SettingsHandlers = BookStore.Application.Features.Settings.Handlers;

namespace BookStore.UI.ViewModels;

/// <summary>
/// View model for centralized store and application settings.
/// </summary>
public partial class SettingsViewModel : BaseViewModel
{
    private readonly SettingsHandlers.SettingsQueryHandler _queryHandler;
    private readonly SettingsHandlers.SettingsCommandHandler _commandHandler;
    private readonly INotificationService _notificationService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty] private StoreSettingsDto store = new();
    [ObservableProperty] private POSSettingsDto pos = new();
    [ObservableProperty] private ReceiptSettingsDto receipt = new();
    [ObservableProperty] private PrinterSettingsDto printer = new();
    [ObservableProperty] private TaxSettingsDto tax = new();
    [ObservableProperty] private CurrencySettingsDto currency = new();
    [ObservableProperty] private BarcodeSettingsDto barcode = new();
    [ObservableProperty] private InventorySettingsDto inventory = new();
    [ObservableProperty] private BackupSettingsDto backup = new();
    [ObservableProperty] private SecuritySettingsDto security = new();
    [ObservableProperty] private AppearanceSettingsDto appearance = new();
    [ObservableProperty] private ApplicationSettingsDto application = new();
    [ObservableProperty] private bool hasUnsavedChanges;

    public SettingsViewModel(SettingsHandlers.SettingsQueryHandler queryHandler, SettingsHandlers.SettingsCommandHandler commandHandler, INotificationService notificationService, ILocalizationService localizationService)
    {
        _queryHandler = queryHandler;
        _commandHandler = commandHandler;
        _notificationService = notificationService;
        _localizationService = localizationService;
        Title = _localizationService.T("Settings.Title");
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _queryHandler.Handle(new GetSettingsQuery());
            if (result.IsSuccess && result.Value is not null)
            {
                Store = result.Value.Store;
                Pos = result.Value.POS;
                Receipt = result.Value.Receipt;
                Printer = result.Value.Printer;
                Tax = result.Value.Tax;
                Currency = result.Value.Currency;
                Barcode = result.Value.Barcode;
                Inventory = result.Value.Inventory;
                Backup = result.Value.Backup;
                Security = result.Value.Security;
                Appearance = result.Value.Appearance;
                Application = result.Value.Application;
                HasUnsavedChanges = false;
            }
            else
            {
                _notificationService.Show("Settings", result.Error ?? "Settings could not be loaded.", NotificationSeverity.Warning);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand] private Task SaveStoreAsync() => SaveAsync(new UpdateStoreSettingsCommand(Store));
    [RelayCommand] private Task SavePOSAsync() => SaveAsync(new UpdatePOSSettingsCommand(Pos));
    [RelayCommand] private Task SaveReceiptAsync() => SaveAsync(new UpdateReceiptSettingsCommand(Receipt));
    [RelayCommand] private Task SavePrinterAsync() => SaveAsync(new UpdatePrinterSettingsCommand(Printer));
    [RelayCommand] private Task SaveTaxAsync() => SaveAsync(new UpdateTaxSettingsCommand(Tax));
    [RelayCommand] private Task SaveCurrencyAsync() => SaveAsync(new UpdateCurrencySettingsCommand(Currency));
    [RelayCommand] private Task SaveBarcodeAsync() => SaveAsync(new UpdateBarcodeSettingsCommand(Barcode));
    [RelayCommand] private Task SaveInventoryAsync() => SaveAsync(new UpdateInventorySettingsCommand(Inventory));
    [RelayCommand] private Task SaveBackupAsync() => SaveAsync(new UpdateBackupSettingsCommand(Backup));
    [RelayCommand] private Task SaveSecurityAsync() => SaveAsync(new UpdateSecuritySettingsCommand(Security));
    [RelayCommand] private Task SaveAppearanceAsync() => SaveAsync(new UpdateAppearanceSettingsCommand(Appearance));

    [RelayCommand]
    private async Task ResetCategoryAsync(SettingsCategory category)
    {
        var result = await _commandHandler.Handle(new ResetSettingsCommand(category));
            _notificationService.Show(_localizationService.T("Settings.Title"), result.IsSuccess ? "Settings category reset." : result.Error ?? "Settings could not be reset.", result.IsSuccess ? NotificationSeverity.Success : NotificationSeverity.Warning);
        if (result.IsSuccess)
        {
            await LoadAsync();
        }
    }

    [RelayCommand]
    private void MarkDirty()
    {
        HasUnsavedChanges = true;
    }

    private async Task SaveAsync<TCommand>(TCommand command)
    {
        IsBusy = true;
        try
        {
            var result = command switch
            {
                UpdateStoreSettingsCommand typed => await _commandHandler.Handle(typed),
                UpdatePOSSettingsCommand typed => await _commandHandler.Handle(typed),
                UpdateReceiptSettingsCommand typed => await _commandHandler.Handle(typed),
                UpdatePrinterSettingsCommand typed => await _commandHandler.Handle(typed),
                UpdateTaxSettingsCommand typed => await _commandHandler.Handle(typed),
                UpdateCurrencySettingsCommand typed => await _commandHandler.Handle(typed),
                UpdateBarcodeSettingsCommand typed => await _commandHandler.Handle(typed),
                UpdateInventorySettingsCommand typed => await _commandHandler.Handle(typed),
                UpdateBackupSettingsCommand typed => await _commandHandler.Handle(typed),
                UpdateSecuritySettingsCommand typed => await _commandHandler.Handle(typed),
                UpdateAppearanceSettingsCommand typed => await _commandHandler.Handle(typed),
                _ => throw new InvalidOperationException("Unsupported settings command.")
            };

            _notificationService.Show(_localizationService.T("Settings.Title"), result.IsSuccess ? _localizationService.T("Settings.Saved") : result.Error ?? _localizationService.T("Settings.NotSaved"), result.IsSuccess ? NotificationSeverity.Success : NotificationSeverity.Warning);
            HasUnsavedChanges = !result.IsSuccess;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
