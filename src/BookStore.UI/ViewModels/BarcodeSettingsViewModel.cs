using BookStore.Application.Features.Barcode.DTOs;
using BookStore.Application.Features.Barcode.Queries.GetBarcodeSettings;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarcodeHandlers = BookStore.Application.Features.Barcode.Handlers;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Barcode settings view model.
/// </summary>
public partial class BarcodeSettingsViewModel : BaseViewModel
{
    private readonly BarcodeHandlers.GetBarcodeSettingsHandler _settingsHandler;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private BarcodeSettingsDto settings = new();

    /// <summary>Initializes a new instance of the <see cref="BarcodeSettingsViewModel"/> class.</summary>
    public BarcodeSettingsViewModel(BarcodeHandlers.GetBarcodeSettingsHandler settingsHandler, INotificationService notificationService)
    {
        _settingsHandler = settingsHandler;
        _notificationService = notificationService;
        Title = "Barcode Settings";
        _ = LoadAsync();
    }

    /// <summary>Reloads settings.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        var result = await _settingsHandler.HandleAsync(new GetBarcodeSettingsRequest());
        if (result.IsSuccess && result.Value is not null)
        {
            Settings = result.Value;
        }
    }

    /// <summary>Shows settings persistence placeholder.</summary>
    [RelayCommand]
    private void Save()
    {
        _notificationService.Show("Barcode", "Barcode settings are loaded from appsettings and prepared for future persistence.", NotificationSeverity.Information);
    }
}
