using System.Collections.ObjectModel;
using BookStore.Application.Features.Barcode.Commands.PrintBarcode;
using BookStore.Application.Features.Barcode.DTOs;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarcodeHandlers = BookStore.Application.Features.Barcode.Handlers;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Barcode label print preparation view model.
/// </summary>
public partial class BarcodeLabelPrintViewModel : BaseViewModel
{
    private readonly BarcodeHandlers.PrintBarcodeHandler _printHandler;
    private readonly IAuthorizationService _authorizationService;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private string barcodeValue = string.Empty;
    [ObservableProperty] private string productTitle = string.Empty;
    [ObservableProperty] private int quantity = 1;
    [ObservableProperty] private string template = "Single";

    /// <summary>Initializes a new instance of the <see cref="BarcodeLabelPrintViewModel"/> class.</summary>
    public BarcodeLabelPrintViewModel(BarcodeHandlers.PrintBarcodeHandler printHandler, IAuthorizationService authorizationService, INotificationService notificationService)
    {
        _printHandler = printHandler;
        _authorizationService = authorizationService;
        _notificationService = notificationService;
        Title = "Barcode Label Printing";
    }

    /// <summary>Gets prepared labels.</summary>
    public ObservableCollection<BarcodeLabelDto> Labels { get; } = [];

    /// <summary>Gets whether printing is allowed.</summary>
    public bool CanPrint => _authorizationService.HasPermission(PermissionConstants.BarcodePrint);

    /// <summary>Adds label to print batch.</summary>
    [RelayCommand]
    private void AddLabel()
    {
        Labels.Add(new BarcodeLabelDto { BarcodeValue = BarcodeValue, ProductTitle = ProductTitle, Quantity = Quantity, Template = Template });
    }

    /// <summary>Prepares barcode label print job.</summary>
    [RelayCommand(CanExecute = nameof(CanPrint))]
    private async Task PrintAsync()
    {
        var result = await _printHandler.HandleAsync(new PrintBarcodeRequest(Labels.ToArray()));
        if (!result.Succeeded)
        {
            _notificationService.Show("Barcode", result.Errors.FirstOrDefault()?.Message ?? result.ValidationErrors.FirstOrDefault()?.Message ?? "Unable to print barcode.", NotificationSeverity.Error);
            return;
        }

        _notificationService.Show("Barcode", "Barcode printed.", NotificationSeverity.Success);
    }
}
