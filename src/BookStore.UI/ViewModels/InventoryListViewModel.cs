using System.Collections.ObjectModel;
using BookStore.Application.Features.Inventory.DTOs;
using BookStore.Application.Features.Inventory.Queries.GetInventory;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryHandlers = BookStore.Application.Features.Inventory.Handlers;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Inventory list view model.
/// </summary>
public partial class InventoryListViewModel : BaseViewModel
{
    private readonly InventoryHandlers.GetInventoryHandler _inventoryHandler;
    private readonly IShellNavigationService _navigationService;
    private readonly IAuthorizationService _authorizationService;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private string searchTerm = string.Empty;
    [ObservableProperty] private bool lowStockOnly;
    [ObservableProperty] private bool outOfStockOnly;
    [ObservableProperty] private int pageNumber = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalCount;

    /// <summary>Initializes a new instance of the <see cref="InventoryListViewModel"/> class.</summary>
    public InventoryListViewModel(InventoryHandlers.GetInventoryHandler inventoryHandler, IShellNavigationService navigationService, IAuthorizationService authorizationService, INotificationService notificationService)
    {
        _inventoryHandler = inventoryHandler;
        _navigationService = navigationService;
        _authorizationService = authorizationService;
        _notificationService = notificationService;
        Title = "Inventory";
        _ = LoadAsync();
    }

    /// <summary>Gets inventory rows.</summary>
    public ObservableCollection<InventoryItemDto> Items { get; } = [];

    /// <summary>Gets whether adjustment is allowed.</summary>
    public bool CanAdjust => _authorizationService.HasPermission(PermissionConstants.InventoryAdjust);

    partial void OnSearchTermChanged(string value) => _ = SearchAsync();
    partial void OnLowStockOnlyChanged(bool value) => _ = SearchAsync();
    partial void OnOutOfStockOnlyChanged(bool value) => _ = SearchAsync();

    /// <summary>Refreshes inventory rows.</summary>
    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    /// <summary>Searches inventory rows.</summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadAsync();
    }

    /// <summary>Navigates to stock adjustment.</summary>
    [RelayCommand(CanExecute = nameof(CanAdjust))]
    private Task AdjustAsync() => _navigationService.NavigateToAsync<InventoryAdjustmentViewModel>("Inventory > Stock Adjustment");

    /// <summary>Navigates to stock ledger.</summary>
    [RelayCommand]
    private Task HistoryAsync() => _navigationService.NavigateToAsync<InventoryHistoryViewModel>("Inventory > Stock Ledger");

    private async Task LoadAsync()
    {
        IsBusy = true;
        var result = await _inventoryHandler.HandleAsync(new GetInventoryRequest(SearchTerm, null, LowStockOnly, OutOfStockOnly, PageNumber, PageSize));
        Items.Clear();
        if (result.IsSuccess && result.Value is not null)
        {
            foreach (var item in result.Value.Items)
            {
                Items.Add(item);
            }

            TotalCount = result.Value.TotalCount;
        }
        else
        {
            _notificationService.Show("Inventory", result.Error ?? "Unable to load inventory.", NotificationSeverity.Error);
        }

        IsBusy = false;
    }
}
