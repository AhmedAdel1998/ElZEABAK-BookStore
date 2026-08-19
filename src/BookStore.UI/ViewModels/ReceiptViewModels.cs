using System.Collections.ObjectModel;
using BookStore.Application.Features.Receipts.Commands;
using BookStore.Application.Features.Receipts.DTOs;
using BookStore.Application.Features.Receipts.Handlers;
using BookStore.Application.Features.Receipts.Queries;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

public partial class ReceiptPreviewViewModel : BaseViewModel
{
    private readonly GetReceiptPreviewHandler _previewHandler;
    private readonly PrintReceiptHandler _printHandler;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private string invoiceNumber = string.Empty;
    [ObservableProperty] private string previewContent = string.Empty;
    [ObservableProperty] private double zoom = 1;
    [ObservableProperty] private ReceiptModel? receipt;

    public ReceiptPreviewViewModel(GetReceiptPreviewHandler previewHandler, PrintReceiptHandler printHandler, INotificationService notificationService)
    {
        _previewHandler = previewHandler;
        _printHandler = printHandler;
        _notificationService = notificationService;
        Title = "Receipt Preview";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        var result = await _previewHandler.HandleAsync(new GetReceiptPreviewQuery(InvoiceNumber));
        IsBusy = false;
        if (result.IsSuccess && result.Value is not null)
        {
            Receipt = result.Value.Receipt;
            PreviewContent = result.Value.Content;
        }
        else
        {
            _notificationService.Show("Receipt", result.Error ?? "Unable to load receipt preview.", NotificationSeverity.Error);
        }
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (Receipt is null)
        {
            return;
        }

        var result = await _printHandler.HandleAsync(new PrintReceiptCommand(Receipt.SaleId));
        _notificationService.Show("Receipt", result.Value?.Succeeded == true ? "Receipt printed successfully." : result.Value?.Error ?? result.Error ?? "Unable to print receipt.", result.Value?.Succeeded == true ? NotificationSeverity.Success : NotificationSeverity.Error);
    }
}

public partial class ReprintReceiptViewModel : BaseViewModel
{
    private readonly SearchReceiptSalesHandler _searchHandler;
    private readonly ReprintReceiptHandler _reprintHandler;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private string invoiceNumber = string.Empty;
    [ObservableProperty] private DateTime? saleDate;
    [ObservableProperty] private ReceiptSaleSearchRowDto? selectedSale;

    public ReprintReceiptViewModel(SearchReceiptSalesHandler searchHandler, ReprintReceiptHandler reprintHandler, INotificationService notificationService)
    {
        _searchHandler = searchHandler;
        _reprintHandler = reprintHandler;
        _notificationService = notificationService;
        Title = "Reprint Receipt";
    }

    public ObservableCollection<ReceiptSaleSearchRowDto> Results { get; } = [];

    [RelayCommand]
    private async Task SearchAsync()
    {
        IsBusy = true;
        // The start of the chosen local day, carrying its real offset so the repository can build
        // an exact 24-hour window without guessing a time zone.
        var date = SaleDate.HasValue
            ? new DateTimeOffset(SaleDate.Value.Date, TimeZoneInfo.Local.GetUtcOffset(SaleDate.Value.Date))
            : (DateTimeOffset?)null;
        var result = await _searchHandler.HandleAsync(new SearchReceiptSalesQuery(InvoiceNumber, date));
        IsBusy = false;
        Results.Clear();
        if (!result.IsSuccess || result.Value is null)
        {
            _notificationService.Show("Receipt", result.Error ?? "Unable to search receipts.", NotificationSeverity.Error);
            return;
        }

        foreach (var row in result.Value)
        {
            Results.Add(row);
        }
    }

    [RelayCommand]
    private async Task ReprintAsync()
    {
        var invoice = SelectedSale?.InvoiceNumber ?? InvoiceNumber;
        var result = await _reprintHandler.HandleAsync(new ReprintReceiptCommand(invoice));
        _notificationService.Show("Receipt", result.Value?.Succeeded == true ? "Receipt reprinted successfully." : result.Value?.Error ?? result.Error ?? "Unable to reprint receipt.", result.Value?.Succeeded == true ? NotificationSeverity.Success : NotificationSeverity.Error);
    }
}

public partial class PrinterTestViewModel : BaseViewModel
{
    private readonly GetAvailablePrintersHandler _printersHandler;
    private readonly TestPrintHandler _testPrintHandler;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private PrinterInfoDto? selectedPrinter;

    public PrinterTestViewModel(GetAvailablePrintersHandler printersHandler, TestPrintHandler testPrintHandler, INotificationService notificationService)
    {
        _printersHandler = printersHandler;
        _testPrintHandler = testPrintHandler;
        _notificationService = notificationService;
        Title = "Printer Test";
        _ = LoadPrintersAsync();
    }

    public ObservableCollection<PrinterInfoDto> Printers { get; } = [];

    [RelayCommand]
    private async Task LoadPrintersAsync()
    {
        var result = await _printersHandler.HandleAsync(new GetAvailablePrintersQuery());
        Printers.Clear();
        if (result.IsSuccess && result.Value is not null)
        {
            foreach (var printer in result.Value)
            {
                Printers.Add(printer);
            }

            SelectedPrinter = Printers.FirstOrDefault(printer => printer.IsDefault) ?? Printers.FirstOrDefault();
        }
    }

    [RelayCommand]
    private async Task TestPrintAsync()
    {
        var result = await _testPrintHandler.HandleAsync(new TestPrintCommand(SelectedPrinter?.Name));
        _notificationService.Show("Printer", result.Value?.Succeeded == true ? "Printer test completed successfully." : result.Value?.Error ?? result.Error ?? "Printer test failed.", result.Value?.Succeeded == true ? NotificationSeverity.Success : NotificationSeverity.Error);
    }
}
