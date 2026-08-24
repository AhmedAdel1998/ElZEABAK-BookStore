using System.Collections.ObjectModel;
using BookStore.Application.Features.Inventory.DTOs;
using BookStore.Application.Features.Inventory.Queries.GetInventoryDashboard;
using BookStore.Application.Features.Reports.DTOs;
using BookStore.Application.Features.Reports.Handlers;
using BookStore.Application.Features.Reports.Queries;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryHandlers = BookStore.Application.Features.Inventory.Handlers;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Interactive operational dashboard for the main shell.
/// </summary>
public partial class DashboardViewModel : BaseViewModel
{
    private readonly GetReportsDashboardHandler _reportsDashboardHandler;
    private readonly GetSalesSummaryHandler _salesSummaryHandler;
    private readonly InventoryHandlers.GetInventoryDashboardHandler _inventoryDashboardHandler;
    private readonly IShellNavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly ILocalizationService _localizationService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IInventoryNavigationState _inventoryNavigationState;

    [ObservableProperty] private ReportsDashboardDto reports = new();
    [ObservableProperty] private InventoryDashboardDto inventory = new();
    [ObservableProperty] private string subtitle = string.Empty;
    [ObservableProperty] private string lastUpdated = string.Empty;
    [ObservableProperty] private int inventoryHealthPercent;
    [ObservableProperty] private int salesMomentumPercent;
    [ObservableProperty] private string inventoryHealthText = string.Empty;
    [ObservableProperty] private string salesMomentumText = string.Empty;
    [ObservableProperty] private SalesSummaryDto yesterdaySales = new();

    public DashboardViewModel(
        GetReportsDashboardHandler reportsDashboardHandler,
        GetSalesSummaryHandler salesSummaryHandler,
        InventoryHandlers.GetInventoryDashboardHandler inventoryDashboardHandler,
        IShellNavigationService navigationService,
        INotificationService notificationService,
        ILocalizationService localizationService,
        IAuthorizationService authorizationService,
        IInventoryNavigationState inventoryNavigationState)
    {
        _reportsDashboardHandler = reportsDashboardHandler;
        _salesSummaryHandler = salesSummaryHandler;
        _inventoryDashboardHandler = inventoryDashboardHandler;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _localizationService = localizationService;
        _authorizationService = authorizationService;
        _inventoryNavigationState = inventoryNavigationState;
        ApplyLocalizedText();
        _localizationService.CultureChanged += (_, _) => ApplyLocalizedText();
        _ = RefreshAsync();
    }

    public ObservableCollection<DashboardMetric> Metrics { get; } = [];
    public ObservableCollection<DashboardActivity> Activity { get; } = [];
    public ObservableCollection<DashboardQuickAction> QuickActions { get; } = [];

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            var reportResult = await _reportsDashboardHandler.HandleAsync(new GetReportsDashboardQuery(ReportDateRange.Today()));
            var yesterdayResult = await _salesSummaryHandler.HandleAsync(new GetSalesSummaryQuery(ReportDateRange.FromPreset(ReportDateRangePreset.Yesterday)));
            var inventoryResult = await _inventoryDashboardHandler.HandleAsync(new GetInventoryDashboardRequest());

            if (reportResult.IsSuccess && reportResult.Value is not null)
            {
                Reports = reportResult.Value;
            }

            if (inventoryResult.IsSuccess && inventoryResult.Value is not null)
            {
                Inventory = inventoryResult.Value;
            }

            if (yesterdayResult.IsSuccess && yesterdayResult.Value is not null)
            {
                YesterdaySales = yesterdayResult.Value;
            }

            if (!reportResult.IsSuccess || !inventoryResult.IsSuccess || !yesterdayResult.IsSuccess)
            {
                _notificationService.Show(_localizationService.T("Dashboard.Title"), _localizationService.T("Dashboard.LoadFailed"), NotificationSeverity.Warning);
            }

            RebuildDashboard();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand] private Task OpenSalesAsync() => NavigateIfAllowedAsync<POSViewModel>(PermissionConstants.SalesCreate, _localizationService.T("Nav.SalesPOS"));
    [RelayCommand] private Task OpenProductsAsync() => NavigateIfAllowedAsync<ProductListViewModel>(PermissionConstants.ProductView, _localizationService.T("Nav.Products"));
    [RelayCommand] private Task OpenInventoryAsync() => NavigateIfAllowedAsync<InventoryDashboardViewModel>(PermissionConstants.InventoryView, _localizationService.T("Nav.Inventory"));
    [RelayCommand] private Task OpenReportsAsync() => NavigateIfAllowedAsync<ReportsDashboardViewModel>(PermissionConstants.ReportView, _localizationService.T("Nav.Reports"));
    [RelayCommand] private Task OpenBackupAsync() => NavigateIfAllowedAsync<BackupListViewModel>(PermissionConstants.BackupView, _localizationService.T("Nav.Backup"));
    [RelayCommand] private Task OpenSalesSummaryAsync() => NavigateIfAllowedAsync<SalesSummaryViewModel>(PermissionConstants.ReportSales, $"{_localizationService.T("Nav.Reports")} > {_localizationService.T("Dashboard.TodaySales")}");
    [RelayCommand] private Task OpenSalesDetailsAsync() => NavigateIfAllowedAsync<SalesDetailsViewModel>(PermissionConstants.ReportSales, $"{_localizationService.T("Nav.Reports")} > {_localizationService.T("Dashboard.Transactions")}");
    [RelayCommand] private Task OpenProfitAsync() => NavigateIfAllowedAsync<ProfitReportViewModel>(PermissionConstants.ReportProfit, $"{_localizationService.T("Nav.Reports")} > {_localizationService.T("Dashboard.Profit")}");
    [RelayCommand] private Task OpenInventoryValueAsync() => NavigateIfAllowedAsync<InventoryReportViewModel>(PermissionConstants.ReportInventory, $"{_localizationService.T("Nav.Reports")} > {_localizationService.T("Dashboard.InventoryValue")}");
    [RelayCommand] private Task OpenDiscountsAsync() => NavigateIfAllowedAsync<SalesSummaryViewModel>(PermissionConstants.ReportSales, $"{_localizationService.T("Nav.Reports")} > {_localizationService.T("Dashboard.DiscountsToday")}");

    [RelayCommand]
    private Task OpenLowStockAsync()
    {
        _inventoryNavigationState.LowStockOnly = true;
        _inventoryNavigationState.OutOfStockOnly = false;
        return NavigateIfAllowedAsync<InventoryListViewModel>(PermissionConstants.InventoryView, $"{_localizationService.T("Nav.Inventory")} > {_localizationService.T("Dashboard.LowStock")}");
    }

    [RelayCommand]
    private Task OpenOutOfStockAsync()
    {
        _inventoryNavigationState.LowStockOnly = false;
        _inventoryNavigationState.OutOfStockOnly = true;
        return NavigateIfAllowedAsync<InventoryListViewModel>(PermissionConstants.InventoryView, $"{_localizationService.T("Nav.Inventory")} > {_localizationService.T("Dashboard.OutOfStock")}");
    }

    private void ApplyLocalizedText()
    {
        Title = _localizationService.T("Dashboard.Title");
        Subtitle = _localizationService.T("Dashboard.Subtitle");
        BuildQuickActions();
        RebuildDashboard();
    }

    private void RebuildDashboard()
    {
        Metrics.Clear();
        Metrics.Add(new DashboardMetric(_localizationService.T("Dashboard.TodaySales"), Reports.TodaysSales.ToString("N2"), _localizationService.T("Dashboard.TodaySalesHint"), "Sales", "#16833A", OpenSalesSummaryCommand));
        Metrics.Add(new DashboardMetric(_localizationService.T("Dashboard.Transactions"), Reports.TodaysTransactions.ToString("N0"), _localizationService.T("Dashboard.TransactionsHint"), "Transactions", "#0F766E", OpenSalesDetailsCommand));
        Metrics.Add(new DashboardMetric(_localizationService.T("Dashboard.InventoryValue"), Inventory.TotalSellingValue.ToString("N2"), _localizationService.T("Dashboard.InventoryValueHint"), "Inventory", "#7C3AED", OpenInventoryValueCommand));
        Metrics.Add(new DashboardMetric(_localizationService.T("Dashboard.LowStock"), Inventory.LowStockCount.ToString("N0"), _localizationService.T("Dashboard.LowStockHint"), "LowStock", "#B91C1C", OpenLowStockCommand));
        Metrics.Add(new DashboardMetric(_localizationService.T("Dashboard.Profit"), Reports.TodaysProfit.ToString("N2"), _localizationService.T("Dashboard.ProfitTodayHint"), "Profit", "#15803D", OpenProfitCommand));
        Metrics.Add(new DashboardMetric(_localizationService.T("Dashboard.OutOfStock"), Inventory.OutOfStockCount.ToString("N0"), _localizationService.T("Dashboard.OutOfStockHint"), "OutOfStock", "#991B1B", OpenOutOfStockCommand));
        Metrics.Add(new DashboardMetric(_localizationService.T("Dashboard.PurchaseValue"), Inventory.TotalPurchaseValue.ToString("N2"), _localizationService.T("Dashboard.PurchaseValueHint"), "PurchaseValue", "#0369A1", OpenInventoryValueCommand));
        Metrics.Add(new DashboardMetric(_localizationService.T("Dashboard.DiscountsToday"), Reports.TodaysDiscounts.ToString("N2"), _localizationService.T("Dashboard.DiscountsTodayHint"), "Discount", "#2F855A", OpenDiscountsCommand));

        Activity.Clear();
        Activity.Add(new DashboardActivity(_localizationService.T("Dashboard.BestSeller"), EmptyAware(Reports.BestSellingProduct), $"{_localizationService.T("Dashboard.Transactions")}: {Reports.TodaysTransactions:N0}"));
        Activity.Add(new DashboardActivity(_localizationService.T("Dashboard.TopCustomer"), EmptyAware(Reports.TopCustomer), $"{_localizationService.T("Dashboard.TodaySales")}: {Reports.TodaysSales:N2}"));
        Activity.Add(new DashboardActivity(_localizationService.T("Dashboard.TopCashier"), EmptyAware(Reports.TopCashier), $"{_localizationService.T("Dashboard.Profit")}: {Reports.TodaysProfit:N2}"));
        Activity.Add(new DashboardActivity(_localizationService.T("Dashboard.StockStatus"), $"{Inventory.LowStockCount:N0} / {Inventory.OutOfStockCount:N0}", $"{_localizationService.T("Dashboard.TotalProducts")}: {Inventory.TotalProducts:N0}"));
        Activity.Add(new DashboardActivity(_localizationService.T("Dashboard.InventoryValue"), Inventory.TotalSellingValue.ToString("N2"), $"{_localizationService.T("Dashboard.PurchaseValue")}: {Inventory.TotalPurchaseValue:N2}"));
        Activity.Add(new DashboardActivity(_localizationService.T("Dashboard.DiscountsToday"), Reports.TodaysDiscounts.ToString("N2"), $"{_localizationService.T("Dashboard.TodaySales")}: {Reports.TodaysSales:N2}"));

        var stockedProducts = Math.Max(Inventory.TotalProducts - Inventory.OutOfStockCount, 0);
        InventoryHealthPercent = Inventory.TotalProducts == 0 ? 0 : Math.Clamp((int)Math.Round(stockedProducts * 100m / Inventory.TotalProducts), 0, 100);
        SalesMomentumPercent = CalculateSalesMomentumPercent(Reports.TodaysSales, YesterdaySales.TotalSales);
        InventoryHealthText = BidiText.Ltr($"{InventoryHealthPercent:N0}% ({stockedProducts:N0}/{Inventory.TotalProducts:N0})");
        SalesMomentumText = BuildSalesMomentumText(Reports.TodaysSales, YesterdaySales.TotalSales);
        LastUpdated = $"{_localizationService.T("Dashboard.LastUpdated")} {DateTime.Now:HH:mm:ss}";
    }

    private void BuildQuickActions()
    {
        QuickActions.Clear();
        AddQuickAction(PermissionConstants.SalesCreate, _localizationService.T("Dashboard.OpenPOS"), _localizationService.T("Dashboard.OpenPOSHint"), OpenSalesCommand);
        AddQuickAction(PermissionConstants.ProductView, _localizationService.T("Dashboard.Products"), _localizationService.T("Dashboard.ProductsHint"), OpenProductsCommand);
        AddQuickAction(PermissionConstants.InventoryView, _localizationService.T("Dashboard.Inventory"), _localizationService.T("Dashboard.InventoryHint"), OpenInventoryCommand);
        AddQuickAction(PermissionConstants.ReportView, _localizationService.T("Dashboard.Reports"), _localizationService.T("Dashboard.ReportsHint"), OpenReportsCommand);
        AddQuickAction(PermissionConstants.BackupView, _localizationService.T("Dashboard.BackupHealth"), _localizationService.T("Dashboard.BackupHealthHint"), OpenBackupCommand);
    }

    private void AddQuickAction(string permission, string title, string hint, System.Windows.Input.ICommand command)
    {
        if (_authorizationService.HasPermission(permission))
        {
            QuickActions.Add(new DashboardQuickAction(title, hint, command));
        }
    }

    private Task NavigateIfAllowedAsync<TViewModel>(string permission, string breadcrumb)
        where TViewModel : BaseViewModel
    {
        if (!_authorizationService.HasPermission(permission))
        {
            _notificationService.Show(_localizationService.T("Dashboard.Title"), _localizationService.T("Auth.PermissionDenied"), NotificationSeverity.Warning);
            return Task.CompletedTask;
        }

        return _navigationService.NavigateToAsync<TViewModel>(breadcrumb);
    }

    private string EmptyAware(string value)
    {
        return string.IsNullOrWhiteSpace(value) || value is "None" or "Walk-in"
            ? _localizationService.T("Dashboard.NoData")
            : value;
    }

    private static int CalculateSalesMomentumPercent(decimal today, decimal yesterday)
    {
        if (yesterday <= 0)
        {
            return today > 0 ? 100 : 0;
        }

        return Math.Clamp((int)Math.Round(today / yesterday * 100), 0, 100);
    }

    private string BuildSalesMomentumText(decimal today, decimal yesterday)
    {
        if (yesterday <= 0)
        {
            return today > 0
                ? _localizationService.T("Dashboard.NewSalesToday")
                : _localizationService.T("Dashboard.NoSalesCompared");
        }

        var change = Math.Round((today - yesterday) / yesterday * 100, 1);
        return $"{_localizationService.T("Dashboard.VsYesterday")} {BidiText.Ltr($"{change:+0.0;-0.0;0.0}%")}";
    }
}

public sealed record DashboardMetric(string Title, string Value, string Hint, string Icon, string Accent, System.Windows.Input.ICommand Command);
public sealed record DashboardActivity(string Title, string Value, string Detail);
public sealed record DashboardQuickAction(string Title, string Hint, System.Windows.Input.ICommand Command);
