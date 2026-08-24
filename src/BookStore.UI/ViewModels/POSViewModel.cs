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
using BookStore.Application.Features.Sales.Commands.CloseInvoice;
using BookStore.Application.Features.Sales.Commands.CompleteSale;
using BookStore.Application.Features.Sales.Commands.RemoveItem;
using BookStore.Application.Features.Sales.Commands.ResumeSale;
using BookStore.Application.Features.Sales.Commands.SaveInvoiceDraft;
using BookStore.Application.Features.Sales.Commands.ClearCustomer;
using BookStore.Application.Features.Sales.Commands.SelectCustomer;
using BookStore.Application.Features.Sales.Commands.StartSale;
using BookStore.Application.Features.Sales.Commands.SuspendSale;
using BookStore.Application.Features.Sales.Commands.SwitchInvoice;
using BookStore.Application.Features.Sales.Commands.UpdateItemQuantity;
using BookStore.Application.Features.Sales.DTOs;
using BookStore.Application.Features.Sales.Handlers;
using BookStore.Application.Features.Sales.Queries.GetHeldSales;
using BookStore.Application.Features.Sales.Queries.GetOpenInvoices;
using BookStore.Application.Features.Sales.Queries.SearchProduct;
using BookStore.Domain.Enums;
using BookStore.Shared.Constants;
using BookStore.UI.Dialogs;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using System.Media;

namespace BookStore.UI.ViewModels;

/// <summary>
/// View model for the enterprise POS cashier screen.
/// </summary>
/// <remarks>
/// The screen edits one invoice at a time but keeps several open at once. <see cref="OpenInvoices"/>
/// is the strip of open invoices and <see cref="CurrentSale"/> is the one being edited; every command
/// names its target invoice explicitly so a switch that lands mid-flight cannot move an item onto the
/// wrong bill.
/// </remarks>
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
    private readonly SwitchInvoiceHandler _switchInvoiceHandler;
    private readonly CloseInvoiceHandler _closeInvoiceHandler;
    private readonly SaveInvoiceDraftHandler _saveInvoiceDraftHandler;
    private readonly SearchProductHandler _searchProductHandler;
    private readonly GetHeldSalesHandler _getHeldSalesHandler;
    private readonly GetOpenInvoicesHandler _getOpenInvoicesHandler;
    private readonly SelectCustomerForSaleHandler _selectCustomerForSaleHandler;
    private readonly ClearCustomerFromSaleHandler _clearCustomerFromSaleHandler;
    private readonly SearchCustomersHandler _searchCustomersHandler;
    private readonly CreateCustomerHandler _createCustomerHandler;
    private readonly FindProductByBarcodeHandler _findProductByBarcodeHandler;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly ILocalizationService _localizationService;

    /// <summary>
    /// Suppresses the payment property handlers while a different invoice is being loaded into the
    /// screen, so loading invoice B's values does not write them onto invoice A.
    /// </summary>
    private bool _isLoadingInvoice;

    /// <summary>
    /// Suppresses the selection handler while the open invoice strip is being rebuilt, so rebuilding
    /// it does not read as the cashier clicking a tab.
    /// </summary>
    private bool _isSyncingInvoiceStrip;

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

    [ObservableProperty]
    private string cartSummaryText = string.Empty;

    [ObservableProperty]
    private string paymentStatusText = string.Empty;

    [ObservableProperty]
    private string stockAlertText = string.Empty;

    [ObservableProperty]
    private string openInvoicesText = string.Empty;

    [ObservableProperty]
    private SaleSessionDto? selectedOpenInvoice;

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
        SwitchInvoiceHandler switchInvoiceHandler,
        CloseInvoiceHandler closeInvoiceHandler,
        SaveInvoiceDraftHandler saveInvoiceDraftHandler,
        SearchProductHandler searchProductHandler,
        GetHeldSalesHandler getHeldSalesHandler,
        GetOpenInvoicesHandler getOpenInvoicesHandler,
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
        _switchInvoiceHandler = switchInvoiceHandler;
        _closeInvoiceHandler = closeInvoiceHandler;
        _saveInvoiceDraftHandler = saveInvoiceDraftHandler;
        _searchProductHandler = searchProductHandler;
        _getHeldSalesHandler = getHeldSalesHandler;
        _getOpenInvoicesHandler = getOpenInvoicesHandler;
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
        _localizationService.CultureChanged += (_, _) => ApplyLocalizedText();
        _ = ResumeOrStartSaleAsync();
    }

    /// <summary>Gets search results for manual product lookup.</summary>
    public ObservableCollection<PosProductDto> SearchResults { get; } = [];

    /// <summary>Gets the invoices open on this workstation, in the order they were opened.</summary>
    public ObservableCollection<SaleSessionDto> OpenInvoices { get; } = [];

    /// <summary>Gets held sales available to resume.</summary>
    public ObservableCollection<SaleSessionDto> HeldSales { get; } = [];

    /// <summary>Gets POS customer search results.</summary>
    public ObservableCollection<CustomerSelectionItem> CustomerResults { get; } = [];

    /// <summary>Gets supported payment methods.</summary>
    public IReadOnlyCollection<PaymentMethod> PaymentMethods { get; } = Enum.GetValues<PaymentMethod>();

    /// <summary>Gets the identifier of the invoice the screen is editing.</summary>
    public Guid? ActiveSaleId => CurrentSale?.SaleId;

    /// <summary>Gets a value indicating whether more than one invoice is open.</summary>
    public bool HasMultipleInvoices => OpenInvoices.Count > 1;

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

            foreach (var customer in result.Value.Items.Select(item => new CustomerSelectionItem { Id = item.PersistedId, FullName = item.FullName, Phone = item.Phone }))
            {
                CustomerResults.Add(customer);
            }
        });
    }

    /// <summary>Selects a customer for the invoice on screen.</summary>
    [RelayCommand]
    private async Task SelectCustomerAsync()
    {
        if (SelectedCustomer is null || CurrentSale is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _selectCustomerForSaleHandler.HandleAsync(new SelectCustomerForSaleRequest(SelectedCustomer.Id, CurrentSale.SaleId));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            await SetSaleAsync(result.Value);
            _notificationService.Show(_localizationService.T("POS.Title"), _localizationService.T("POS.CustomerSelected"), NotificationSeverity.Information);
        });
    }

    /// <summary>Clears the selected customer and uses walk-in mode.</summary>
    [RelayCommand]
    private async Task ClearCustomerAsync()
    {
        if (CurrentSale is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _clearCustomerFromSaleHandler.HandleAsync(new ClearCustomerFromSaleRequest(CurrentSale.SaleId));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            await SetSaleAsync(result.Value);
        });
    }

    /// <summary>Creates a customer from the POS screen and selects it.</summary>
    [RelayCommand]
    private async Task CreateCustomerFromPosAsync()
    {
        await ExecuteAsync(async () =>
        {
            if (!await EnsureActiveSaleAsync())
            {
                return;
            }

            var create = await _createCustomerHandler.HandleAsync(new CreateCustomerRequest(new CustomerEditorModel { FullName = NewCustomerName, Phone = NewCustomerPhone, IsActive = true }));
            if (!create.IsSuccess || create.Value is null)
            {
                await ShowErrorAsync(create.Error);
                return;
            }

            var select = await _selectCustomerForSaleHandler.HandleAsync(new SelectCustomerForSaleRequest(create.Value.Id, CurrentSale?.SaleId));
            if (!select.IsSuccess || select.Value is null)
            {
                await ShowErrorAsync(select.Error);
                return;
            }

            NewCustomerName = string.Empty;
            NewCustomerPhone = string.Empty;
            await SetSaleAsync(select.Value);
            _notificationService.Show(_localizationService.T("POS.Title"), _localizationService.T("POS.CustomerCreated"), NotificationSeverity.Success);
        });
    }

    /// <summary>
    /// Opens an additional invoice alongside the ones already open and switches the screen to it.
    /// </summary>
    [RelayCommand]
    private async Task StartSaleAsync() => await BeginSaleAsync(forceNew: true);

    /// <summary>
    /// Loads the invoice already in progress, or opens one when there is none. Used when the POS
    /// screen is opened so that navigating away and back does not lose a cart.
    /// </summary>
    private async Task ResumeOrStartSaleAsync() => await BeginSaleAsync(forceNew: false);

    private async Task BeginSaleAsync(bool forceNew)
    {
        await ExecuteAsync(async () =>
        {
            // The payment box of the invoice being left behind is written down first, otherwise the
            // figures a cashier typed vanish the moment another invoice takes the screen.
            if (forceNew)
            {
                await SaveDraftAsync();
            }

            var result = await _startSaleHandler.HandleAsync(new StartSaleRequest(ForceNew: forceNew));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                await RefreshInvoiceListsAsync();
                return;
            }

            await SetSaleAsync(result.Value);
        });
    }

    /// <summary>Switches the screen to another invoice that is already open.</summary>
    [RelayCommand]
    private async Task SwitchInvoiceAsync(SaleSessionDto? invoice)
    {
        if (invoice is null || invoice.SaleId == CurrentSale?.SaleId)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            await SaveDraftAsync();
            var result = await _switchInvoiceHandler.HandleAsync(new SwitchInvoiceRequest(invoice.SaleId));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                await RefreshInvoiceListsAsync();
                return;
            }

            await SetSaleAsync(result.Value);
        });
    }

    /// <summary>Closes an open invoice, asking first when it still holds items.</summary>
    [RelayCommand]
    private async Task CloseInvoiceAsync(SaleSessionDto? invoice)
    {
        var target = invoice ?? CurrentSale;
        if (target is null)
        {
            return;
        }

        if (target.Items.Count > 0)
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                _localizationService.T("POS.CloseInvoiceTitle"),
                string.Format(_localizationService.T("POS.CloseInvoiceConfirm"), target.InvoiceNumber, target.Items.Count));
            if (!confirmed)
            {
                return;
            }
        }

        await ExecuteAsync(async () =>
        {
            var result = await _closeInvoiceHandler.HandleAsync(new CloseInvoiceRequest(target.SaleId));
            if (!result.IsSuccess)
            {
                await ShowErrorAsync(result.Error);
                await RefreshInvoiceListsAsync();
                return;
            }

            // Closing the last invoice must still leave the cashier with something to scan into.
            await ReloadActiveInvoiceAsync();
        });
    }

    /// <summary>Adds the scanned barcode to the invoice on screen.</summary>
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

            var targetSaleId = CurrentSale!.SaleId;
            var lookup = await _findProductByBarcodeHandler.HandleAsync(new FindProductByBarcodeRequest(barcode));
            if (!lookup.IsSuccess || lookup.Value is null)
            {
                LastScanMessage = string.Format(_localizationService.T("POS.ScanNotAdded"), barcode);
                LastScanCategory = string.Empty;
                LastScanDestination = string.Empty;
                SystemSounds.Exclamation.Play();
                await ShowErrorAsync(lookup.Error);
                return;
            }

            var result = await _addItemHandler.HandleAsync(new AddItemRequest(lookup.Value.ProductId, 1, targetSaleId));
            if (!result.IsSuccess || result.Value is null)
            {
                LastScanMessage = string.Format(_localizationService.T("POS.ProductNotAdded"), lookup.Value.Title);
                LastScanCategory = lookup.Value.CategoryName ?? _localizationService.T("POS.NoCategory");
                LastScanDestination = CurrentSale?.InvoiceNumber ?? string.Empty;
                SystemSounds.Exclamation.Play();
                await ShowErrorAsync(result.Error);
                return;
            }

            BarcodeText = string.Empty;
            await SetSaleAsync(result.Value, lookup.Value.ProductId);
            LastScanMessage = string.Format(_localizationService.T("POS.AddedProduct"), lookup.Value.Title);
            LastScanCategory = lookup.Value.CategoryName ?? _localizationService.T("POS.NoCategory");
            LastScanDestination = string.Format(_localizationService.T("POS.AddedToInvoice"), result.Value.InvoiceNumber, result.Value.Items.Count);
            SystemSounds.Asterisk.Play();
            _notificationService.Show(_localizationService.T("POS.ScanTitle"), string.Format(_localizationService.T("POS.AddedToInvoice"), result.Value.InvoiceNumber, result.Value.Items.Count), NotificationSeverity.Success);
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

    /// <summary>Adds the selected search result to the invoice on screen.</summary>
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

            var result = await _addItemHandler.HandleAsync(new AddItemRequest(SelectedProduct.ProductId, Math.Max(ItemQuantity, 1), CurrentSale!.SaleId));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            await SetSaleAsync(result.Value, SelectedProduct.ProductId);
        });
    }

    /// <summary>Increases the selected cart item quantity by one.</summary>
    [RelayCommand]
    private async Task IncreaseQuantityAsync()
    {
        if (SelectedCartItem is null)
        {
            return;
        }

        ItemQuantity = SelectedCartItem.Quantity + 1;
        await UpdateQuantityAsync();
    }

    /// <summary>Decreases the selected cart item quantity by one.</summary>
    [RelayCommand]
    private async Task DecreaseQuantityAsync()
    {
        if (SelectedCartItem is null || SelectedCartItem.Quantity <= 1)
        {
            return;
        }

        ItemQuantity = SelectedCartItem.Quantity - 1;
        await UpdateQuantityAsync();
    }

    /// <summary>Updates the selected cart item quantity.</summary>
    [RelayCommand]
    private async Task UpdateQuantityAsync()
    {
        if (SelectedCartItem is null || CurrentSale is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _updateItemQuantityHandler.HandleAsync(new UpdateItemQuantityRequest(SelectedCartItem.Id, Math.Max(ItemQuantity, 1), CurrentSale.SaleId));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            await SetSaleAsync(result.Value, SelectedCartItem.ProductId);
        });
    }

    /// <summary>Applies a line discount to the selected cart item.</summary>
    [RelayCommand]
    private async Task ApplyLineDiscountAsync()
    {
        if (SelectedCartItem is null || CurrentSale is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _applyLineDiscountHandler.HandleAsync(new ApplyLineDiscountRequest(SelectedCartItem.Id, LineDiscount, CurrentSale.SaleId));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            await SetSaleAsync(result.Value);
        });
    }

    /// <summary>Applies an invoice-level discount.</summary>
    [RelayCommand]
    private async Task ApplyInvoiceDiscountAsync()
    {
        if (CurrentSale is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _applyInvoiceDiscountHandler.HandleAsync(new ApplyInvoiceDiscountRequest(InvoiceDiscount, CurrentSale.SaleId));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            await SetSaleAsync(result.Value);
        });
    }

    /// <summary>Removes the selected cart item.</summary>
    [RelayCommand]
    private async Task RemoveSelectedItemAsync()
    {
        if (SelectedCartItem is null || CurrentSale is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _removeItemHandler.HandleAsync(new RemoveItemRequest(SelectedCartItem.Id, CurrentSale.SaleId));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            await SetSaleAsync(result.Value);
        });
    }

    /// <summary>Suspends the invoice on screen.</summary>
    [RelayCommand]
    private async Task SuspendSaleAsync()
    {
        if (CurrentSale is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            await SaveDraftAsync();
            var result = await _suspendSaleHandler.HandleAsync(new SuspendSaleRequest(CurrentSale.SaleId));
            if (!result.IsSuccess)
            {
                await ShowErrorAsync(result.Error);
                return;
            }

            _notificationService.Show(_localizationService.T("POS.Title"), _localizationService.T("POS.SaleSuspended"), NotificationSeverity.Information);
            await ReloadActiveInvoiceAsync();
        });
    }

    /// <summary>Re-opens a held sale as an additional open invoice.</summary>
    [RelayCommand]
    private async Task ResumeSaleAsync(SaleSessionDto? sale)
    {
        if (sale is null)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            await SaveDraftAsync();
            var result = await _resumeSaleHandler.HandleAsync(new ResumeSaleRequest(sale.SaleId));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                await RefreshInvoiceListsAsync();
                return;
            }

            await SetSaleAsync(result.Value);
        });
    }

    /// <summary>Cancels the invoice on screen.</summary>
    [RelayCommand]
    private async Task CancelSaleAsync()
    {
        if (CurrentSale is null)
        {
            return;
        }

        var targetSaleId = CurrentSale.SaleId;
        var confirmed = await _dialogService.ShowConfirmationAsync(_localizationService.T("POS.CancelTitle"), _localizationService.T("POS.CancelConfirm"));
        if (!confirmed)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _cancelSaleHandler.HandleAsync(new CancelSaleRequest("Cancelled by cashier", targetSaleId));
            if (!result.IsSuccess)
            {
                await ShowErrorAsync(result.Error);
                await RefreshInvoiceListsAsync();
                return;
            }

            await ReloadActiveInvoiceAsync();
        });
    }

    /// <summary>Completes the invoice on screen and prepares the receipt payload.</summary>
    [RelayCommand]
    private async Task CompleteSaleAsync()
    {
        if (CurrentSale is null || CurrentSale.Items.Count == 0)
        {
            await ShowErrorAsync(_localizationService.T("POS.EmptySale"));
            return;
        }

        var targetSaleId = CurrentSale.SaleId;
        var confirmed = await _dialogService.ShowConfirmationAsync(_localizationService.T("POS.CompleteSale"), _localizationService.T("POS.CompleteConfirm"));
        if (!confirmed)
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _completeSaleHandler.HandleAsync(new CompleteSaleRequest(SelectedPaymentMethod, AmountPaid, Math.Clamp(ReceiptCopies, 1, 5), targetSaleId));
            if (!result.IsSuccess || result.Value is null)
            {
                await ShowErrorAsync(result.Error);
                await RefreshInvoiceListsAsync();
                return;
            }

            _notificationService.Show(
                _localizationService.T("POS.Title"),
                result.Value.ReceiptPrintSucceeded
                    ? string.Format(_localizationService.T("POS.ReceiptPrinted"), result.Value.ReceiptCopies, result.Value.InvoiceNumber)
                    : string.Format(_localizationService.T("POS.ReceiptFailed"), result.Value.ReceiptPrintError),
                result.Value.ReceiptPrintSucceeded ? NotificationSeverity.Success : NotificationSeverity.Warning);

            // The paid invoice is gone; the screen falls back to whichever invoice is still open, or
            // to a fresh one when that was the only invoice.
            await ReloadActiveInvoiceAsync();
        });
    }

    /// <summary>Sets the paid amount to the exact sale total.</summary>
    [RelayCommand]
    private void PayExact()
    {
        AmountPaid = CurrentSale?.Summary.GrandTotal ?? 0m;
    }

    /// <summary>Adds a quick cash amount to the paid amount.</summary>
    [RelayCommand]
    private void AddQuickCash(string? amountText)
    {
        // The denomination arrives as a XAML literal, so it must be read with the invariant
        // convention rather than the UI culture.
        if (decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) && amount > 0)
        {
            AmountPaid += amount;
        }
    }

    /// <summary>
    /// Writes the payment fields of the invoice on screen back to the session store, so switching
    /// away and back keeps them.
    /// </summary>
    private async Task SaveDraftAsync()
    {
        if (CurrentSale is null)
        {
            return;
        }

        await _saveInvoiceDraftHandler.HandleAsync(new SaveInvoiceDraftRequest(SelectedPaymentMethod, AmountPaid, Math.Clamp(ReceiptCopies, 1, 5), CurrentSale.SaleId));
    }

    /// <summary>
    /// Reloads whichever invoice the store considers active, opening a fresh one when the last open
    /// invoice has just been completed, cancelled, closed or held.
    /// </summary>
    private async Task ReloadActiveInvoiceAsync()
    {
        var result = await _startSaleHandler.HandleAsync(new StartSaleRequest());
        if (!result.IsSuccess || result.Value is null)
        {
            await ShowErrorAsync(result.Error);
            await RefreshInvoiceListsAsync();
            return;
        }

        await SetSaleAsync(result.Value);
    }

    private async Task RefreshInvoiceListsAsync()
    {
        var open = await _getOpenInvoicesHandler.HandleAsync(new GetOpenInvoicesRequest());
        _isSyncingInvoiceStrip = true;
        try
        {
            OpenInvoices.Clear();
            if (open.IsSuccess && open.Value is not null)
            {
                foreach (var invoice in open.Value.OrderBy(invoice => invoice.CreatedAt))
                {
                    // The strip holds the very object the screen is editing for the active invoice, so
                    // its badge tracks live edits and the tab highlight matches by reference.
                    OpenInvoices.Add(CurrentSale is not null && CurrentSale.SaleId == invoice.SaleId ? CurrentSale : invoice);
                }
            }

            SelectedOpenInvoice = OpenInvoices.FirstOrDefault(invoice => invoice.SaleId == CurrentSale?.SaleId);
        }
        finally
        {
            _isSyncingInvoiceStrip = false;
        }

        var held = await _getHeldSalesHandler.HandleAsync(new GetHeldSalesRequest());
        HeldSales.Clear();
        if (held.IsSuccess && held.Value is not null)
        {
            foreach (var sale in held.Value)
            {
                HeldSales.Add(sale);
            }
        }

        OnPropertyChanged(nameof(HasMultipleInvoices));
        OpenInvoicesText = string.Format(_localizationService.T("POS.OpenInvoicesCount"), OpenInvoices.Count, PosConstants.MaxOpenInvoices);
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

        await SetSaleAsync(result.Value);
        return true;
    }

    private void ApplyLocalizedText()
    {
        Title = _localizationService.T("POS.Cashier");
        if (string.IsNullOrWhiteSpace(LastScanMessage) || LastScanMessage == "Ready to scan." || LastScanMessage == "جاهز لمسح الكود.")
        {
            LastScanMessage = _localizationService.T("POS.ReadyToScan");
        }

        OpenInvoicesText = string.Format(_localizationService.T("POS.OpenInvoicesCount"), OpenInvoices.Count, PosConstants.MaxOpenInvoices);
        UpdateDerivedState();
    }

    private async Task SetSaleAsync(SaleSessionDto sale, Guid? selectedProductId = null)
    {
        // The payment properties belong to the invoice being loaded, not to the one leaving the
        // screen, so their change handlers must not write back while the swap is in progress.
        _isLoadingInvoice = true;
        try
        {
            CurrentSale = sale;
            InvoiceDiscount = sale.InvoiceDiscount;
            AmountPaid = sale.AmountPaid;
            SelectedPaymentMethod = sale.PaymentMethod;
            ReceiptCopies = Math.Clamp(sale.ReceiptCopies, 1, 5);
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
        }
        finally
        {
            _isLoadingInvoice = false;
        }

        OnPropertyChanged(nameof(CurrentSale));
        OnPropertyChanged(nameof(ActiveSaleId));
        await RefreshInvoiceListsAsync();
        UpdateDerivedState();
    }

    partial void OnSelectedOpenInvoiceChanged(SaleSessionDto? value)
    {
        if (_isSyncingInvoiceStrip || value is null || value.SaleId == CurrentSale?.SaleId)
        {
            return;
        }

        _ = SwitchInvoiceCommand.ExecuteAsync(value);
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
        if (_isLoadingInvoice || CurrentSale is null)
        {
            return;
        }

        CurrentSale.AmountPaid = Math.Max(value, 0m);
        CurrentSale.Summary.AmountPaid = decimal.Round(CurrentSale.AmountPaid, 2);
        CurrentSale.Summary.Change = decimal.Round(Math.Max(CurrentSale.AmountPaid - CurrentSale.Summary.GrandTotal, 0m), 2);
        OnPropertyChanged(nameof(CurrentSale));
        UpdateDerivedState();
    }

    partial void OnSelectedPaymentMethodChanged(PaymentMethod value)
    {
        if (_isLoadingInvoice || CurrentSale is null)
        {
            return;
        }

        CurrentSale.PaymentMethod = value;
    }

    partial void OnReceiptCopiesChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, 5);
        if (value != clamped)
        {
            ReceiptCopies = clamped;
            return;
        }

        if (!_isLoadingInvoice && CurrentSale is not null)
        {
            CurrentSale.ReceiptCopies = clamped;
        }
    }

    private void UpdateDerivedState()
    {
        var totalQuantity = CurrentSale?.Items.Sum(item => item.Quantity) ?? 0;
        var lines = CurrentSale?.Items.Count ?? 0;
        var total = CurrentSale?.Summary.GrandTotal ?? 0m;
        var paid = CurrentSale?.AmountPaid ?? 0m;
        var lowStockLines = CurrentSale?.Items.Count(item => item.AvailableQuantity <= 2) ?? 0;

        CartSummaryText = string.Format(_localizationService.T("POS.CartSummary"), lines, totalQuantity, total);
        PaymentStatusText = paid >= total && total > 0
            ? string.Format(_localizationService.T("POS.PaymentReady"), paid - total)
            : string.Format(_localizationService.T("POS.PaymentRemaining"), Math.Max(total - paid, 0m));
        StockAlertText = lowStockLines == 0
            ? _localizationService.T("POS.StockHealthy")
            : string.Format(_localizationService.T("POS.StockWatch"), lowStockLines);
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

    private Task ShowErrorAsync(string? message) => _dialogService.ShowErrorAsync(_localizationService.T("POS.Title"), string.IsNullOrWhiteSpace(message) ? _localizationService.T("POS.OperationFailed") : message);
}
