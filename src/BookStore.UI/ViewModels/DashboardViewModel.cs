using System.Collections.ObjectModel;
using BookStore.Application.Features.Inventory.DTOs;
using BookStore.Application.Features.Inventory.Queries.GetInventoryDashboard;
using BookStore.Application.Features.Reports.DTOs;
using BookStore.Application.Features.Reports.Handlers;
using BookStore.Application.Features.Reports.Queries;
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
        ILocalizationService localizationService)
    {
        _reportsDashboardHandler = reportsDashboardHandler;
        _salesSummaryHandler = salesSummaryHandler;
        _inventoryDashboardHandler = inventoryDashboardHandler;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _localizationService = localizationService;
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

    [RelayCommand] private Task OpenSalesAsync() => _navigationService.NavigateToAsync<POSViewModel>(_localizationService.T("Nav.SalesPOS"));
    [RelayCommand] private Task OpenProductsAsync() => _navigationService.NavigateToAsync<ProductListViewModel>(_localizationService.T("Nav.Products"));
    [RelayCommand] private Task OpenInventoryAsync() => _navigationService.NavigateToAsync<InventoryDashboardViewModel>(_localizationService.T("Nav.Inventory"));
    [RelayCommand] private Task OpenReportsAsync() => _navigationService.NavigateToAsync<ReportsDashboardViewModel>(_localizationService.T("Nav.Reports"));

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
        Metrics.Add(new DashboardMetric(_localizationService.T("Dashboard.TodaySales"), Reports.TodaysSales.ToString("N2"), _localizationService.T("Dashboard.TodaySalesHint"), "S", "#D99A00"));
        Metrics.Add(new DashboardMetric(_localizationService.T("Dashboard.Transactions"), Reports.TodaysTransactions.ToString("N0"), _localizationService.T("Dashboard.TransactionsHint"), "T", "#0F766E"));
        Metrics.Add(new DashboardMetric(_localizationService.T("Dashboard.InventoryValue"), Inventory.TotalSellingValue.ToString("N2"), _localizationService.T("Dashboard.InventoryValueHint"), "I", "#7C3AED"));
        Metrics.Add(new DashboardMetric(_localizationService.T("Dashboard.LowStock"), Inventory.LowStockCount.ToString("N0"), _localizationService.T("Dashboard.LowStockHint"), "L", "#B91C1C"));

        Activity.Clear();
        Activity.Add(new DashboardActivity(_localizationService.T("Dashboard.BestSeller"), EmptyAware(Reports.BestSellingProduct), $"{_localizationService.T("Dashboard.Transactions")}: {Reports.TodaysTransactions:N0}"));
        Activity.Add(new DashboardActivity(_localizationService.T("Dashboard.TopCustomer"), EmptyAware(Reports.TopCustomer), $"{_localizationService.T("Dashboard.TodaySales")}: {Reports.TodaysSales:N2}"));
        Activity.Add(new DashboardActivity(_localizationService.T("Dashboard.TopCashier"), EmptyAware(Reports.TopCashier), $"{_localizationService.T("Dashboard.Profit")}: {Reports.TodaysProfit:N2}"));
        Activity.Add(new DashboardActivity(_localizationService.T("Dashboard.StockStatus"), $"{Inventory.LowStockCount:N0} / {Inventory.OutOfStockCount:N0}", $"{_localizationService.T("Dashboard.TotalProducts")}: {Inventory.TotalProducts:N0}"));

        var stockedProducts = Math.Max(Inventory.TotalProducts - Inventory.OutOfStockCount, 0);
        InventoryHealthPercent = Inventory.TotalProducts == 0 ? 0 : Math.Clamp((int)Math.Round(stockedProducts * 100m / Inventory.TotalProducts), 0, 100);
        SalesMomentumPercent = CalculateSalesMomentumPercent(Reports.TodaysSales, YesterdaySales.TotalSales);
        InventoryHealthText = $"{InventoryHealthPercent:N0}% ({stockedProducts:N0}/{Inventory.TotalProducts:N0})";
        SalesMomentumText = BuildSalesMomentumText(Reports.TodaysSales, YesterdaySales.TotalSales);
        LastUpdated = $"{_localizationService.T("Dashboard.LastUpdated")} {DateTime.Now:HH:mm:ss}";
    }

    private void BuildQuickActions()
    {
        QuickActions.Clear();
        QuickActions.Add(new DashboardQuickAction(_localizationService.T("Dashboard.OpenPOS"), _localizationService.T("Dashboard.OpenPOSHint"), OpenSalesCommand));
        QuickActions.Add(new DashboardQuickAction(_localizationService.T("Dashboard.Products"), _localizationService.T("Dashboard.ProductsHint"), OpenProductsCommand));
        QuickActions.Add(new DashboardQuickAction(_localizationService.T("Dashboard.Inventory"), _localizationService.T("Dashboard.InventoryHint"), OpenInventoryCommand));
        QuickActions.Add(new DashboardQuickAction(_localizationService.T("Dashboard.Reports"), _localizationService.T("Dashboard.ReportsHint"), OpenReportsCommand));
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
        return $"{_localizationService.T("Dashboard.VsYesterday")} {change:+0.0;-0.0;0.0}%";
    }
}

public sealed record DashboardMetric(string Title, string Value, string Hint, string Icon, string Accent);
public sealed record DashboardActivity(string Title, string Value, string Detail);
public sealed record DashboardQuickAction(string Title, string Hint, System.Windows.Input.ICommand Command);
