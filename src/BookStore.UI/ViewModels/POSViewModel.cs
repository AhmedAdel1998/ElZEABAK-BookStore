using System.Collections.ObjectModel;
using BookStore.Application.Features.Barcode.Handlers;
using BookStore.Application.Features.Barcode.Queries.FindProductByBarcode;
using BookStore.Application.Features.Customers.Commands.CreateCustomer;
using BookStore.Application.Features.Customers.DTOs;
using BookStore.Application.Features.Customers.Handlers;
using BookStore.Application.Features.Customers.Queries.SearchCustomers;
using BookStore.Application.Features.Sales.Commands.AddItem;
using BookStore.Application.Features.Sales.Commands.ApplyInvoiceDiscount;
using BookStore.Application.Features.Sales.Commands.ApplyLineDiscount;
using BookStore.Application.Features.Sales.Commands.CancelSale;
using BookStore.Application.Features.Sales.Commands.CompleteSale;
using BookStore.Application.Features.Sales.Commands.RemoveItem;
using BookStore.Application.Features.Sales.Commands.ResumeSale;
using BookStore.Application.Features.Sales.Commands.ClearCustomer;
using BookStore.Application.Features.Sales.Commands.SelectCustomer;
using BookStore.Application.Features.Sales.Commands.StartSale;
using BookStore.Application.Features.Sales.Commands.SuspendSale;
using BookStore.Application.Features.Sales.Commands.UpdateItemQuantity;
using BookStore.Application.Features.Sales.DTOs;
using BookStore.Application.Features.Sales.Handlers;
using BookStore.Application.Features.Sales.Queries.GetHeldSales;
using BookStore.Application.Features.Sales.Queries.SearchProduct;
using BookStore.Domain.Enums;
using BookStore.UI.Dialogs;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// View model for the enterprise POS cashier screen.
/// </summary>
public partial class POSViewModel : BaseViewModel
{
    private readonly StartSaleHandler _startSaleHandler;
    private readonly AddItemHandler _addItemHandler;
    private readonly UpdateItemQuantityHandler _updateItemQuantityHandler;
    private readonly RemoveItemHandler _removeItemHandler;
    private readonly ApplyLineDiscountHandler _applyLineDiscountHandler;
    private readonly ApplyInvoiceDiscountHandler _applyInvoiceDiscountHandler;
    private readonly CancelSaleHandler _cancelSaleHandler;
    private readonly SuspendSaleHandler _suspendSaleHandler;
    private readonly ResumeSaleHandler _resumeSaleHandler;
    private readonly CompleteSaleHandler _completeSaleHandler;
    private readonly SearchProductHandler _searchProductHandler;
    private readonly GetHeldSalesHandler _getHeldSalesHandler;
    private readonly SelectCustomerForSaleHandler _selectCustomerForSaleHandler;
    private readonly ClearCustomerFromSaleHandler _clearCustomerFromSaleHandler;
    private readonly SearchCustomersHandler _searchCustomersHandler;
    private readonly CreateCustomerHandler _createCustomerHandler;
    private readonly FindProductByBarcodeHandler _findProductByBarcodeHandler;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private SaleSessionDto? currentSale;

    [ObservableProperty]
    private string barcodeText = string.Empty;

    [ObservableProperty]
    private bool autoAddScans = true;

    [ObservableProperty]
    private string lastScanMessage = string.Empty;

    [ObservableProperty]
    private string lastScanCategory = string.Empty;

    [ObservableProperty]
    private string lastScanDestination = string.Empty;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private PosProductDto? selectedProduct;

    [ObservableProperty]
    private SaleCartItemDto? selectedCartItem;

    [ObservableProperty]
    private int itemQuantity = 1;

    [ObservableProperty]
    private decimal lineDiscount;

    [ObservableProperty]
    private decimal invoiceDiscount;

    [ObservableProperty]
    private PaymentMethod selectedPaymentMethod = PaymentMethod.Cash;

    [ObservableProperty]
    private decimal amountPaid;

    [ObservableProperty]
    private int receiptCopies = 1;

    [ObservableProperty]
    private string customerSearchText = string.Empty;

    [ObservableProperty]
    private CustomerSelectionItem? selectedCustomer;

    [ObservableProperty]
    private string newCustomerName = string.Empty;

    [ObservableProperty]
    private string newCustomerPhone = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="POSViewModel"/> class.</summary>
    public POSViewModel(
        StartSaleHandler startSaleHandler,
        AddItemHandler addItemHandler,
        UpdateItemQuantityHandler updateItemQuantityHandler,
        RemoveItemHandler removeItemHandler,
        ApplyLineDiscountHandler applyLineDiscountHandler,
        ApplyInvoiceDiscountHandler applyInvoiceDiscountHandler,
        CancelSaleHandler cancelSaleHandler,
        SuspendSaleHandler suspendSaleHandler,
        ResumeSaleHandler resumeSaleHandler,
        CompleteSaleHandler completeSaleHandler,
        SearchProductHandler searchProductHandler,
        GetHeldSalesHandler getHeldSalesHandler,
        SelectCustomerForSaleHandler selectCustomerForSaleHandler,
        ClearCustomerFromSaleHandler clearCustomerFromSaleHandler,
        SearchCustomersHandler searchCustomersHandler,
        CreateCustomerHandler createCustomerHandler,
        FindProductByBarcodeHandler findProductByBarcodeHandler,
        IDialogService dialogService,
        INotificationService notificationService,
        ILocalizationService localizationService)
    {
        _startSaleHandler = startSaleHandler;
        _addItemHandler = addItemHandler;
        _updateItemQuantityHandler = updateItemQuantityHandler;
        _removeItemHandler = removeItemHandler;
        _applyLineDiscountHandler = applyLineDiscountHandler;
        _applyInvoiceDiscountHandler = applyInvoiceDiscountHandler;
        _cancelSaleHandler = cancelSaleHandler;
        _suspendSaleHandler = suspendSaleHandler;
        _resumeSaleHandler = resumeSaleHandler;
        _completeSaleHandler = completeSaleHandler;
        _searchProductHandler = searchProductHandler;
        _getHeldSalesHandler = getHeldSalesHandler;
        _selectCustomerForSaleHandler = selectCustomerForSaleHandler;
        _clearCustomerFromSaleHandler = clearCustomerFromSaleHandler;
        _searchCustomersHandler = searchCustomersHandler;
        _createCustomerHandler = createCustomerHandler;
        _findProductByBarcodeHandler = findProductByBarcodeHandler;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _localizationService = localizationService;
        Title = _localizationService.T("POS.Cashier");
        LastScanMessage = _localizationService.T("POS.ReadyToScan");
        _ = StartSaleAsync();
    }

    /// <summary>Gets search results for manual product lookup.</summary>
    public ObservableCollection<PosProductDto> SearchResults { get; } = [];

    /// <summary>Gets held sales available to resume.</summary>
    public ObservableCollection<SaleSessionDto> HeldSales { get; } = [];

    /// <summary>Gets POS customer search results.</summary>
    public ObservableCollection<CustomerSelectionItem> CustomerResults { get; } = [];

    /// <summary>Gets supported payment methods.</summary>
    public IReadOnlyCollection<PaymentMethod> PaymentMethods { get; } = Enum.GetValues<PaymentMethod>();

    /// <summary>Searches customers for POS selection.</summary>
    [RelayCommand]
    private async Task SearchCustomersAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomerSearchText))
        {
            CustomerResults.Clear();
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _searchCustomersHandler.HandleAsync(new SearchCustomersRequest(new CustomerFilter { SearchTerm = CustomerSearchText, IsActive = true, PageSize = 10 }));
            CustomerResults.Clear();
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            foreach (var customer in result.Value.Items.Select(item => new CustomerSelectionItem { Id = item.Id, FullName = item.FullName, Phone = item.Phone }))
            {
                CustomerResults.Add(customer);
            }
        });
    }

    /// <summary>Selects a customer for the active sale.</summary>
    [RelayCommand]
    private async Task SelectCustomerAsync()
    {
        if (SelectedCustomer is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _selectCustomerForSaleHandler.HandleAsync(new SelectCustomerForSaleRequest(SelectedCustomer.Id));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            SetSale(result.Value);
            _notificationService.Show("POS", "Customer selected.", NotificationSeverity.Information);
        });
    }

    /// <summary>Clears the selected customer and uses walk-in mode.</summary>
    [RelayCommand]
    private async Task ClearCustomerAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _clearCustomerFromSaleHandler.HandleAsync(new ClearCustomerFromSaleRequest());
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            SetSale(result.Value);
        });
    }

    /// <summary>Creates a customer from the POS screen and selects it.</summary>
    [RelayCommand]
    private async Task CreateCustomerFromPosAsync()
    {
        await ExecuteAsync(async () =>
        {
            var create = await _createCustomerHandler.HandleAsync(new CreateCustomerRequest(new CustomerEditorModel { FullName = NewCustomerName, Phone = NewCustomerPhone, IsActive = true }));
            if (!create.IsSuccess || create.Value is null)
            {
                await ShowErrorAsync(create.Error);
                return;
            }

            var select = await _selectCustomerForSaleHandler.HandleAsync(new SelectCustomerForSaleRequest(create.Value.Id));
            if (!select.IsSuccess || select.Value is null)
            {
                await ShowErrorAsync(select.Error);
                return;
            }

            NewCustomerName = string.Empty;
            NewCustomerPhone = string.Empty;
            SetSale(select.Value);
            _notificationService.Show("POS", "Customer created and selected.", NotificationSeverity.Success);
        });
    }

    /// <summary>Starts a new cashier sale.</summary>
    [RelayCommand]
    private async Task StartSaleAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _startSaleHandler.HandleAsync(new StartSaleRequest());
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            SetSale(result.Value);
            await RefreshHeldSalesAsync();
        });
    }

    /// <summary>Adds the scanned barcode to the cart.</summary>
    [RelayCommand]
    private async Task AddBarcodeAsync()
    {
        var barcode = BarcodeText.Trim();
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            if (!await EnsureActiveSaleAsync())
            {
                return;
            }

            var lookup = await _findProductByBarcodeHandler.HandleAsync(new FindProductByBarcodeRequest(barcode));
            if (!lookup.IsSuccess || lookup.Value is null)
            {
                LastScanMessage = $"Barcode {barcode} was not added.";
                LastScanCategory = string.Empty;
                LastScanDestination = string.Empty;
                await ShowErrorAsync(lookup.Error);
                return;
            }

            var result = await _addItemHandler.HandleAsync(new AddItemRequest(lookup.Value.ProductId, 1));
            if (!result.IsSuccess || result.Value is null)
            {
                LastScanMessage = $"{lookup.Value.Title} was not added.";
                LastScanCategory = lookup.Value.CategoryName ?? "No category";
                LastScanDestination = CurrentSale?.InvoiceNumber ?? string.Empty;
                await ShowErrorAsync(result.Error);
                return;
            }

            BarcodeText = string.Empty;
            SetSale(result.Value, lookup.Value.ProductId);
            LastScanMessage = $"Added {lookup.Value.Title}.";
            LastScanCategory = lookup.Value.CategoryName ?? "No category";
            LastScanDestination = $"Added to invoice {result.Value.InvoiceNumber}; cart lines: {result.Value.Items.Count}.";
            _notificationService.Show("Scan", $"{lookup.Value.Title} added to {result.Value.InvoiceNumber}.", NotificationSeverity.Success);
        });
    }

    /// <summary>Adds the scanned barcode when scanner auto-add is enabled.</summary>
    [RelayCommand]
    private async Task AddScannedBarcodeAsync()
    {
        if (AutoAddScans)
        {
            await AddBarcodeAsync();
        }
    }

    /// <summary>Searches active products for manual selection.</summary>
    [RelayCommand]
    private async Task SearchProductsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SearchResults.Clear();
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _searchProductHandler.HandleAsync(new SearchProductRequest(SearchText));
            SearchResults.Clear();
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            foreach (var item in result.Value.Items)
            {
                SearchResults.Add(item);
            }
        });
    }

    /// <summary>Adds the selected search result to the cart.</summary>
    [RelayCommand]
    private async Task AddSelectedProductAsync()
    {
        if (SelectedProduct is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            if (!await EnsureActiveSaleAsync())
            {
                return;
            }

            var result = await _addItemHandler.HandleAsync(new AddItemRequest(SelectedProduct.ProductId, Math.Max(ItemQuantity, 1)));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            SetSale(result.Value, SelectedProduct.ProductId);
        });
    }

    /// <summary>Updates the selected cart item quantity.</summary>
    [RelayCommand]
    private async Task UpdateQuantityAsync()
    {
        if (SelectedCartItem is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _updateItemQuantityHandler.HandleAsync(new UpdateItemQuantityRequest(SelectedCartItem.Id, Math.Max(ItemQuantity, 1)));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            SetSale(result.Value, SelectedCartItem.ProductId);
        });
    }

    /// <summary>Applies a line discount to the selected cart item.</summary>
    [RelayCommand]
    private async Task ApplyLineDiscountAsync()
    {
        if (SelectedCartItem is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _applyLineDiscountHandler.HandleAsync(new ApplyLineDiscountRequest(SelectedCartItem.Id, LineDiscount));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            SetSale(result.Value);
        });
    }

    /// <summary>Applies an invoice-level discount.</summary>
    [RelayCommand]
    private async Task ApplyInvoiceDiscountAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _applyInvoiceDiscountHandler.HandleAsync(new ApplyInvoiceDiscountRequest(InvoiceDiscount));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            SetSale(result.Value);
        });
    }

    /// <summary>Removes the selected cart item.</summary>
    [RelayCommand]
    private async Task RemoveSelectedItemAsync()
    {
        if (SelectedCartItem is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _removeItemHandler.HandleAsync(new RemoveItemRequest(SelectedCartItem.Id));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            SetSale(result.Value);
        });
    }

    /// <summary>Suspends the active sale.</summary>
    [RelayCommand]
    private async Task SuspendSaleAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _suspendSaleHandler.HandleAsync(new SuspendSaleRequest());
            if (!result.IsSuccess)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            _notificationService.Show("POS", "Sale suspended.", NotificationSeverity.Information);
            await StartSaleAsync();
        });
    }

    /// <summary>Resumes a held sale.</summary>
    [RelayCommand]
    private async Task ResumeSaleAsync(SaleSessionDto? sale)
    {
        if (sale is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _resumeSaleHandler.HandleAsync(new ResumeSaleRequest(sale.SaleId));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            SetSale(result.Value);
            await RefreshHeldSalesAsync();
        });
    }

    /// <summary>Cancels the active sale.</summary>
    [RelayCommand]
    private async Task CancelSaleAsync()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync("Cancel sale", "Cancel the active sale?");
        if (!confirmed)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _cancelSaleHandler.HandleAsync(new CancelSaleRequest("Cancelled by cashier"));
            if (!result.IsSuccess)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            await StartSaleAsync();
        });
    }

    /// <summary>Completes the sale and prepares the receipt payload.</summary>
    [RelayCommand]
    private async Task CompleteSaleAsync()
    {
        if (CurrentSale is null || CurrentSale.Items.Count == 0)
        {
            await ShowErrorAsync("A sale must contain at least one item.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync("Complete sale", "Complete payment and prepare receipt?");
        if (!confirmed)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _completeSaleHandler.HandleAsync(new CompleteSaleRequest(SelectedPaymentMethod, AmountPaid, Math.Clamp(ReceiptCopies, 1, 5)));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            _notificationService.Show(
                "POS",
                result.Value.ReceiptPrintSucceeded
                    ? $"{result.Value.ReceiptCopies} receipt copy/copies printed successfully for {result.Value.InvoiceNumber}."
                    : $"Sale completed, but receipt printing failed. {result.Value.ReceiptPrintError}",
                result.Value.ReceiptPrintSucceeded ? NotificationSeverity.Success : NotificationSeverity.Warning);
            await StartSaleAsync();
        });
    }

    private async Task RefreshHeldSalesAsync()
    {
        var result = await _getHeldSalesHandler.HandleAsync(new GetHeldSalesRequest());
        HeldSales.Clear();
        if (result.IsSuccess && result.Value is not null)
        {
            foreach (var sale in result.Value)
            {
                HeldSales.Add(sale);
            }
        }
    }

    private async Task<bool> EnsureActiveSaleAsync()
    {
        if (CurrentSale is not null)
        {
            return true;
        }

        var result = await _startSaleHandler.HandleAsync(new StartSaleRequest());
        if (!result.IsSuccess || result.Value is null)
        {
            await ShowErrorAsync(result.Error);
            return false;
        }

        SetSale(result.Value);
        await RefreshHeldSalesAsync();
        return true;
    }

    private void SetSale(SaleSessionDto sale, Guid? selectedProductId = null)
    {
        CurrentSale = sale;
        InvoiceDiscount = sale.InvoiceDiscount;
        AmountPaid = sale.AmountPaid;
        SelectedPaymentMethod = sale.PaymentMethod;
        ReceiptCopies = Math.Clamp(ReceiptCopies, 1, 5);
        SelectedCartItem = selectedProductId.HasValue
            ? sale.Items.FirstOrDefault(item => item.ProductId == selectedProductId.Value) ?? sale.Items.FirstOrDefault()
            : sale.Items.FirstOrDefault();
        if (SelectedCartItem is not null)
        {
            ItemQuantity = SelectedCartItem.Quantity;
            LineDiscount = SelectedCartItem.Discount;
        }
        else
        {
            ItemQuantity = 1;
            LineDiscount = 0;
        }

        OnPropertyChanged(nameof(CurrentSale));
    }

    partial void OnSelectedCartItemChanged(SaleCartItemDto? value)
    {
        if (value is null)
        {
            return;
        }

        ItemQuantity = value.Quantity;
        LineDiscount = value.Discount;
    }

    partial void OnAmountPaidChanged(decimal value)
    {
        if (CurrentSale is null)
        {
            return;
        }

        CurrentSale.AmountPaid = Math.Max(value, 0m);
        CurrentSale.Summary.AmountPaid = decimal.Round(CurrentSale.AmountPaid, 2);
        CurrentSale.Summary.Change = decimal.Round(Math.Max(CurrentSale.AmountPaid - CurrentSale.Summary.GrandTotal, 0m), 2);
        OnPropertyChanged(nameof(CurrentSale));
    }

    partial void OnReceiptCopiesChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, 5);
        if (value != clamped)
        {
            ReceiptCopies = clamped;
        }
    }

    private async Task ExecuteAsync(Func<Task> action)
    {
        IsBusy = true;
        try
        {
            await action();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task ShowErrorAsync(string? message) => _dialogService.ShowErrorAsync("POS", string.IsNullOrWhiteSpace(message) ? "The POS operation could not be completed." : message);
}
