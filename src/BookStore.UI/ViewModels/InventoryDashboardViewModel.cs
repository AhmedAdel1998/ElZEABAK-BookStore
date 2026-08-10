using BookStore.Application.Features.Inventory.DTOs;
using BookStore.Application.Features.Inventory.Queries.GetInventoryDashboard;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryHandlers = BookStore.Application.Features.Inventory.Handlers;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Inventory dashboard view model.
/// </summary>
public partial class InventoryDashboardViewModel : BaseViewModel
{
    private readonly InventoryHandlers.GetInventoryDashboardHandler _dashboardHandler;
    private readonly IShellNavigationService _navigationService;
    private readonly IAuthorizationService _authorizationService;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private InventoryDashboardDto dashboard = new();

    /// <summary>Initializes a new instance of the <see cref="InventoryDashboardViewModel"/> class.</summary>
    public InventoryDashboardViewModel(InventoryHandlers.GetInventoryDashboardHandler dashboardHandler, IShellNavigationService navigationService, IAuthorizationService authorizationService, INotificationService notificationService)
    {
        _dashboardHandler = dashboardHandler;
        _navigationService = navigationService;
        _authorizationService = authorizationService;
        _notificationService = notificationService;
        Title = "Inventory Dashboard";
        _ = RefreshAsync();
    }

    /// <summary>Gets whether adjustment is allowed.</summary>
    public bool CanAdjust => _authorizationService.HasPermission(PermissionConstants.InventoryAdjust);

    /// <summary>Refreshes dashboard metrics.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        var result = await _dashboardHandler.HandleAsync(new GetInventoryDashboardRequest());
        IsBusy = false;
        if (result.IsSuccess && result.Value is not null)
        {
            Dashboard = result.Value;
        }
        else
        {
            _notificationService.Show("Inventory", result.Error ?? "Unable to load inventory dashboard.", NotificationSeverity.Error);
        }
    }

    /// <summary>Navigates to inventory list.</summary>
    [RelayCommand]
    private Task ViewInventoryAsync() => _navigationService.NavigateToAsync<InventoryListViewModel>("Inventory > Stock");

    /// <summary>Navigates to stock ledger.</summary>
    [RelayCommand]
    private Task ViewHistoryAsync() => _navigationService.NavigateToAsync<InventoryHistoryViewModel>("Inventory > Stock Ledger");

    /// <summary>Navigates to stock adjustment.</summary>
    [RelayCommand(CanExecute = nameof(CanAdjust))]
    private Task AdjustStockAsync() => _navigationService.NavigateToAsync<InventoryAdjustmentViewModel>("Inventory > Stock Adjustment");
}
