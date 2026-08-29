using System.Collections.ObjectModel;
using BookStore.Application.Features.Barcode.DTOs;
using BookStore.Application.Features.Receipts.DTOs;
using BookStore.Application.Features.Receipts.Services;
using BookStore.Application.Features.Settings.Commands;
using BookStore.Application.Features.Settings.Queries;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SettingsBarcodeDto = BookStore.Application.Features.Settings.DTOs.BarcodeSettingsDto;
using SettingsHandlers = BookStore.Application.Features.Settings.Handlers;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Barcode settings view model.
/// </summary>
public partial class BarcodeSettingsViewModel : BaseViewModel
{
    private readonly SettingsHandlers.SettingsQueryHandler _queryHandler;
    private readonly SettingsHandlers.SettingsCommandHandler _commandHandler;
    private readonly IPrinterDiscoveryService _printerDiscoveryService;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private BarcodeSettingsDto settings = new();

    /// <summary>Gets selectable barcode formats.</summary>
    public IReadOnlyList<BarcodeFormat> BarcodeFormats { get; } = Enum.GetValues<BarcodeFormat>();

    /// <summary>Gets installed printers.</summary>
    public ObservableCollection<PrinterInfoDto> Printers { get; } = [];

    /// <summary>Initializes a new instance of the <see cref="BarcodeSettingsViewModel"/> class.</summary>
    public BarcodeSettingsViewModel(SettingsHandlers.SettingsQueryHandler queryHandler, SettingsHandlers.SettingsCommandHandler commandHandler, IPrinterDiscoveryService printerDiscoveryService, INotificationService notificationService)
    {
        _queryHandler = queryHandler;
        _commandHandler = commandHandler;
        _printerDiscoveryService = printerDiscoveryService;
        _notificationService = notificationService;
        Title = "Barcode Settings";
        _ = LoadAsync();
    }

    /// <summary>Reloads settings.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            Printers.Clear();
            try
            {
                foreach (var printer in await _printerDiscoveryService.GetInstalledPrintersAsync()) Printers.Add(printer);
            }
            catch (Exception ex)
            {
                _notificationService.Show("Barcode", $"Installed printers could not be read: {ex.Message}", NotificationSeverity.Warning);
            }

            var result = await _queryHandler.Handle(new GetBarcodeSettingsQuery());
            if (result.IsSuccess && result.Value is not null)
            {
                var value = result.Value;
                Settings = new BarcodeSettingsDto
                {
                    DefaultFormat = Enum.TryParse<BarcodeFormat>(value.DefaultFormat, true, out var format) ? format : BarcodeFormat.Code128,
                    Prefix = value.Prefix,
                    StartingNumber = value.StartingNumber,
                    Length = value.Length,
                    LabelWidthMm = value.LabelWidthMm,
                    LabelHeightMm = value.LabelHeightMm,
                    PrinterName = value.PrinterName,
                    ScanTimeoutMilliseconds = value.ScanTimeout,
                    AutomaticGenerationEnabled = value.AutoGenerate,
                    ManualGenerationEnabled = value.ManualGenerationEnabled
                };
                return;
            }

            _notificationService.Show("Barcode", result.Error ?? "Unable to load barcode settings.", NotificationSeverity.Error);
        }
        catch (Exception ex)
        {
            _notificationService.Show("Barcode", $"Unable to load barcode settings: {ex.Message}", NotificationSeverity.Error);
        }
    }

    /// <summary>Validates and persists barcode settings.</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var value = new SettingsBarcodeDto
            {
                DefaultFormat = Settings.DefaultFormat.ToString(),
                Prefix = Settings.Prefix,
                StartingNumber = Settings.StartingNumber,
                Length = Settings.Length,
                LabelWidthMm = Settings.LabelWidthMm,
                LabelHeightMm = Settings.LabelHeightMm,
                PrinterName = Settings.PrinterName,
                ScanTimeout = Settings.ScanTimeoutMilliseconds,
                AutoGenerate = Settings.AutomaticGenerationEnabled,
                ManualGenerationEnabled = Settings.ManualGenerationEnabled
            };
            var result = await _commandHandler.Handle(new UpdateBarcodeSettingsCommand(value));
            _notificationService.Show("Barcode", result.IsSuccess ? "Barcode settings saved." : result.Error ?? "Unable to save barcode settings.", result.IsSuccess ? NotificationSeverity.Success : NotificationSeverity.Error);
        }
        catch (Exception ex)
        {
            _notificationService.Show("Barcode", $"Unable to save barcode settings: {ex.Message}", NotificationSeverity.Error);
        }
    }
}
