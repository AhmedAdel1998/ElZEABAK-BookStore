using BookStore.Application.Features.Barcode.Commands.GenerateBarcode;
using BookStore.Application.Features.Barcode.Commands.ValidateBarcode;
using BookStore.Application.Features.Barcode.DTOs;
using BookStore.Application.Features.Barcode.Queries.FindProductByBarcode;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarcodeHandlers = BookStore.Application.Features.Barcode.Handlers;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Barcode preview and scanner test view model.
/// </summary>
public partial class BarcodePreviewViewModel : BaseViewModel
{
    private readonly BarcodeHandlers.GenerateBarcodeHandler _generateHandler;
    private readonly BarcodeHandlers.ValidateBarcodeHandler _validateHandler;
    private readonly BarcodeHandlers.FindProductByBarcodeHandler _findHandler;
    private readonly IAuthorizationService _authorizationService;
    private readonly INotificationService _notificationService;
    private readonly IShellNavigationService _navigationService;

    [ObservableProperty] private string barcodeValue = string.Empty;
    [ObservableProperty] private BarcodeFormat format = BarcodeFormat.Code128;
    [ObservableProperty] private string productTitle = "Barcode Preview";
    [ObservableProperty] private string validationMessage = string.Empty;
    [ObservableProperty] private decimal sellingPrice;
    [ObservableProperty] private int quantity;

    /// <summary>Initializes a new instance of the <see cref="BarcodePreviewViewModel"/> class.</summary>
    public BarcodePreviewViewModel(BarcodeHandlers.GenerateBarcodeHandler generateHandler, BarcodeHandlers.ValidateBarcodeHandler validateHandler, BarcodeHandlers.FindProductByBarcodeHandler findHandler, IAuthorizationService authorizationService, INotificationService notificationService, IShellNavigationService navigationService)
    {
        _generateHandler = generateHandler;
        _validateHandler = validateHandler;
        _findHandler = findHandler;
        _authorizationService = authorizationService;
        _notificationService = notificationService;
        _navigationService = navigationService;
        Title = "Barcode Preview";
        Formats = [BarcodeFormat.Code128, BarcodeFormat.Code39, BarcodeFormat.Ean13, BarcodeFormat.Ean8];
    }

    /// <summary>Gets supported barcode formats.</summary>
    public System.Collections.ObjectModel.ObservableCollection<BarcodeFormat> Formats { get; }

    /// <summary>Gets whether barcode generation is allowed.</summary>
    public bool CanGenerate => _authorizationService.HasPermission(PermissionConstants.BarcodeGenerate);

    /// <summary>Gets whether barcode printing is allowed.</summary>
    public bool CanPrint => _authorizationService.HasPermission(PermissionConstants.BarcodePrint);

    /// <summary>Gets whether barcode settings are allowed.</summary>
    public bool CanOpenSettings => _authorizationService.HasPermission(PermissionConstants.BarcodeSettings);

    /// <summary>Generates a barcode.</summary>
    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync()
    {
        var result = await _generateHandler.HandleAsync(new GenerateBarcodeRequest(Format));
        if (!result.IsSuccess || result.Value is null)
        {
            _notificationService.Show("Barcode", result.Error ?? "Unable to generate barcode.", NotificationSeverity.Error);
            return;
        }

        BarcodeValue = result.Value.Value;
        _notificationService.Show("Barcode", "Barcode generated successfully.", NotificationSeverity.Success);
    }

    /// <summary>Validates barcode.</summary>
    [RelayCommand]
    private async Task ValidateAsync()
    {
        var result = await _validateHandler.HandleAsync(new ValidateBarcodeRequest(BarcodeValue, Format));
        ValidationMessage = result.IsSuccess && result.Value is not null ? result.Value.Message : result.Error ?? "Invalid barcode.";
        _notificationService.Show("Barcode", ValidationMessage, result.Value?.IsValid == true ? NotificationSeverity.Success : NotificationSeverity.Warning);
    }

    /// <summary>Finds product by barcode.</summary>
    [RelayCommand]
    private async Task FindProductAsync()
    {
        var result = await _findHandler.HandleAsync(new FindProductByBarcodeRequest(BarcodeValue));
        if (!result.IsSuccess || result.Value is null)
        {
            ProductTitle = "Product not found";
            SellingPrice = 0;
            Quantity = 0;
            _notificationService.Show("Barcode", result.Error ?? "Product was not found.", NotificationSeverity.Warning);
            return;
        }

        ProductTitle = result.Value.Title;
        SellingPrice = result.Value.SellingPrice;
        Quantity = result.Value.Quantity;
        _notificationService.Show("Barcode", "Product found.", NotificationSeverity.Success);
    }

    /// <summary>Navigates to barcode label print preparation.</summary>
    [RelayCommand(CanExecute = nameof(CanPrint))]
    private Task OpenPrintAsync() => _navigationService.NavigateToAsync<BarcodeLabelPrintViewModel>("Barcode > Print Labels");

    /// <summary>Navigates to barcode settings.</summary>
    [RelayCommand(CanExecute = nameof(CanOpenSettings))]
    private Task OpenSettingsAsync() => _navigationService.NavigateToAsync<BarcodeSettingsViewModel>("Barcode > Settings");
}
