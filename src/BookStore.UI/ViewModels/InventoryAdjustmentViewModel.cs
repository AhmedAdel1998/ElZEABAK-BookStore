using System.Collections.ObjectModel;
using BookStore.Application.Features.Inventory.Commands.AdjustStock;
using BookStore.Application.Features.Inventory.DTOs;
using BookStore.Application.Features.Inventory.Queries.GetInventory;
using BookStore.Domain.Enums;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryHandlers = BookStore.Application.Features.Inventory.Handlers;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Inventory adjustment view model.
/// </summary>
public partial class InventoryAdjustmentViewModel : BaseViewModel
{
    private readonly InventoryHandlers.AdjustStockHandler _adjustHandler;
    private readonly InventoryHandlers.GetInventoryHandler _inventoryHandler;
    private readonly IShellNavigationService _navigationService;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private InventoryItemDto? selectedProduct;
    [ObservableProperty] private InventoryTransactionType adjustmentType = InventoryTransactionType.ManualAdjustment;
    [ObservableProperty] private int targetQuantity;
    [ObservableProperty] private string reason = string.Empty;
    [ObservableProperty] private string? notes;
    [ObservableProperty] private string validationMessage = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="InventoryAdjustmentViewModel"/> class.</summary>
    public InventoryAdjustmentViewModel(InventoryHandlers.AdjustStockHandler adjustHandler, InventoryHandlers.GetInventoryHandler inventoryHandler, IShellNavigationService navigationService, INotificationService notificationService)
    {
        _adjustHandler = adjustHandler;
        _inventoryHandler = inventoryHandler;
        _navigationService = navigationService;
        _notificationService = notificationService;
        Title = "Stock Adjustment";
        TransactionTypes = [InventoryTransactionType.InitialStock, InventoryTransactionType.ManualAdjustment, InventoryTransactionType.Correction, InventoryTransactionType.Damage, InventoryTransactionType.Return];
        _ = LoadProductsAsync();
    }

    /// <summary>Gets products available for adjustment.</summary>
    public ObservableCollection<InventoryItemDto> Products { get; } = [];

    /// <summary>Gets selectable transaction types.</summary>
    public ObservableCollection<InventoryTransactionType> TransactionTypes { get; }

    /// <summary>Gets the current quantity before adjustment.</summary>
    public int QuantityBefore => SelectedProduct?.CurrentQuantity ?? 0;

    /// <summary>Gets the quantity after adjustment.</summary>
    public int QuantityAfter => TargetQuantity;

    partial void OnSelectedProductChanged(InventoryItemDto? value)
    {
        TargetQuantity = value?.CurrentQuantity ?? 0;
        OnPropertyChanged(nameof(QuantityBefore));
        OnPropertyChanged(nameof(QuantityAfter));
    }

    partial void OnTargetQuantityChanged(int value) => OnPropertyChanged(nameof(QuantityAfter));

    /// <summary>Saves stock adjustment.</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedProduct is null)
        {
            ValidationMessage = "Product is required.";
            return;
        }

        IsBusy = true;
        ValidationMessage = string.Empty;
        var result = await _adjustHandler.HandleAsync(new AdjustStockRequest(SelectedProduct.ProductId, TargetQuantity, AdjustmentType, Reason, null, Notes));
        IsBusy = false;
        if (!result.IsSuccess)
        {
            ValidationMessage = result.Error ?? "Unable to save adjustment.";
            _notificationService.Show("Inventory", ValidationMessage, NotificationSeverity.Error);
            return;
        }

        _notificationService.Show("Inventory", "Adjustment saved.", NotificationSeverity.Success);
        if (result.Value?.QuantityAfter == 0)
        {
            _notificationService.Show("Inventory", "Out of stock warning.", NotificationSeverity.Warning);
        }
        else if (SelectedProduct.MinimumStock >= result.Value?.QuantityAfter)
        {
            _notificationService.Show("Inventory", "Low stock warning.", NotificationSeverity.Warning);
        }

        await _navigationService.NavigateToAsync<InventoryHistoryViewModel>("Inventory > Stock Ledger");
    }

    /// <summary>Cancels adjustment.</summary>
    [RelayCommand]
    private Task CancelAsync() => _navigationService.GoBackAsync();

    private async Task LoadProductsAsync()
    {
        var result = await _inventoryHandler.HandleAsync(new GetInventoryRequest(PageSize: 200));
        if (result.IsSuccess && result.Value is not null)
        {
            foreach (var product in result.Value.Items)
            {
                Products.Add(product);
            }
        }
    }
}
