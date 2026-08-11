using System.Collections.ObjectModel;
using BookStore.Application.Features.Reports.DTOs;
using BookStore.Application.Features.Reports.Handlers;
using BookStore.Application.Features.Reports.Queries;
using BookStore.Shared.Results;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

public abstract partial class ReportPageViewModel : BaseViewModel
{
    private readonly INotificationService _notificationService;

    [ObservableProperty] private ReportDateRangePreset selectedPreset = ReportDateRangePreset.Today;
    [ObservableProperty] private DateTime? startDate = DateTime.Today;
    [ObservableProperty] private DateTime? endDate = DateTime.Today;
    [ObservableProperty] private string emptyMessage = "No rows found for the selected filters.";

    protected ReportPageViewModel(string title, INotificationService notificationService)
    {
        Title = title;
        _notificationService = notificationService;
    }

    public ObservableCollection<object> Rows { get; } = [];

    [RelayCommand]
    protected async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            Rows.Clear();
            foreach (var row in await LoadRowsAsync())
            {
                Rows.Add(row);
            }
        }
        catch (Exception)
        {
            _notificationService.Show(Title, "Unable to load report.", NotificationSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected ReportDateRange GetDateRange()
    {
        var start = StartDate ?? DateTime.Today;
        var end = EndDate ?? start;
        return new ReportDateRange(new DateTimeOffset(start.Date), new DateTimeOffset(end.Date.AddDays(1)));
    }

    protected async Task<IReadOnlyCollection<object>> FromResultAsync<T>(Task<Result<IReadOnlyCollection<T>>> resultTask)
    {
        var result = await resultTask;
        if (!result.IsSuccess || result.Value is null)
        {
            _notificationService.Show(Title, result.Error ?? "Unable to load report.", NotificationSeverity.Error);
            return [];
        }

        return result.Value.Cast<object>().ToArray();
    }

    protected async Task<IReadOnlyCollection<object>> FromPagedResultAsync<T>(Task<Result<PagedResult<T>>> resultTask)
    {
        var result = await resultTask;
        if (!result.IsSuccess || result.Value is null)
        {
            _notificationService.Show(Title, result.Error ?? "Unable to load report.", NotificationSeverity.Error);
            return [];
        }

        return result.Value.Items.Cast<object>().ToArray();
    }

    protected abstract Task<IReadOnlyCollection<object>> LoadRowsAsync();
}

public partial class ReportsDashboardViewModel : BaseViewModel
{
    private readonly GetReportsDashboardHandler _dashboardHandler;
    private readonly IShellNavigationService _navigationService;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private ReportDateRangePreset selectedPreset = ReportDateRangePreset.Today;
    [ObservableProperty] private DateTime? startDate = DateTime.Today;
    [ObservableProperty] private DateTime? endDate = DateTime.Today;
    [ObservableProperty] private ReportsDashboardDto dashboard = new();

    public ReportsDashboardViewModel(GetReportsDashboardHandler dashboardHandler, IShellNavigationService navigationService, INotificationService notificationService)
    {
        _dashboardHandler = dashboardHandler;
        _navigationService = navigationService;
        _notificationService = notificationService;
        Title = "Reports Dashboard";
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        var start = StartDate ?? DateTime.Today;
        var end = EndDate ?? start;
        var result = await _dashboardHandler.HandleAsync(new GetReportsDashboardQuery(new ReportDateRange(new DateTimeOffset(start.Date), new DateTimeOffset(end.Date.AddDays(1)))));
        IsBusy = false;
        if (result.IsSuccess && result.Value is not null)
        {
            Dashboard = result.Value;
        }
        else
        {
            _notificationService.Show("Reports", result.Error ?? "Unable to load reports dashboard.", NotificationSeverity.Error);
        }
    }

    [RelayCommand] private Task OpenSalesSummaryAsync() => _navigationService.NavigateToAsync<SalesSummaryViewModel>("Reports > Sales Summary");
    [RelayCommand] private Task OpenSalesDetailsAsync() => _navigationService.NavigateToAsync<SalesDetailsViewModel>("Reports > Sales Details");
    [RelayCommand] private Task OpenProfitAsync() => _navigationService.NavigateToAsync<ProfitReportViewModel>("Reports > Profit");
    [RelayCommand] private Task OpenBestProductsAsync() => _navigationService.NavigateToAsync<BestSellingProductsViewModel>("Reports > Best-Selling Products");
    [RelayCommand] private Task OpenInventoryAsync() => _navigationService.NavigateToAsync<InventoryReportViewModel>("Reports > Inventory");
    [RelayCommand] private Task OpenMovementsAsync() => _navigationService.NavigateToAsync<InventoryMovementViewModel>("Reports > Inventory Movements");
    [RelayCommand] private Task OpenLowStockAsync() => _navigationService.NavigateToAsync<LowStockReportViewModel>("Reports > Low Stock");
    [RelayCommand] private Task OpenCustomersAsync() => _navigationService.NavigateToAsync<CustomerReportViewModel>("Reports > Customers");
    [RelayCommand] private Task OpenCashiersAsync() => _navigationService.NavigateToAsync<CashierPerformanceViewModel>("Reports > Cashiers");
    [RelayCommand] private Task OpenPaymentsAsync() => _navigationService.NavigateToAsync<PaymentMethodsViewModel>("Reports > Payment Methods");
    [RelayCommand] private Task OpenDailyAsync() => _navigationService.NavigateToAsync<DailySalesViewModel>("Reports > Daily Sales");
    [RelayCommand] private Task OpenHourlyAsync() => _navigationService.NavigateToAsync<HourlySalesViewModel>("Reports > Hourly Sales");
}

public sealed class SalesSummaryViewModel(GetSalesSummaryHandler handler, INotificationService notificationService) : ReportPageViewModel("Sales Summary", notificationService)
{
    protected override async Task<IReadOnlyCollection<object>> LoadRowsAsync()
    {
        var result = await handler.HandleAsync(new GetSalesSummaryQuery(GetDateRange()));
        return result.IsSuccess && result.Value is not null ? [result.Value] : [];
    }
}

public sealed class SalesDetailsViewModel(GetSalesDetailsHandler handler, INotificationService notificationService) : ReportPageViewModel("Sales Details", notificationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromPagedResultAsync(handler.HandleAsync(new GetSalesDetailsQuery(GetDateRange())));
}

public sealed class ProfitReportViewModel(GetProfitReportHandler handler, INotificationService notificationService) : ReportPageViewModel("Profit Report", notificationService)
{
    protected override async Task<IReadOnlyCollection<object>> LoadRowsAsync()
    {
        var result = await handler.HandleAsync(new GetProfitReportQuery(GetDateRange()));
        return result.IsSuccess && result.Value is not null ? [result.Value] : [];
    }
}

public sealed class BestSellingProductsViewModel(GetBestSellingProductsHandler handler, INotificationService notificationService) : ReportPageViewModel("Best-Selling Products", notificationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromResultAsync(handler.HandleAsync(new GetBestSellingProductsQuery(GetDateRange(), 10)));
}

public sealed class InventoryReportViewModel(GetInventoryReportHandler handler, INotificationService notificationService) : ReportPageViewModel("Inventory Report", notificationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromPagedResultAsync(handler.HandleAsync(new GetInventoryReportQuery()));
}

public sealed class InventoryMovementViewModel(GetInventoryMovementsHandler handler, INotificationService notificationService) : ReportPageViewModel("Inventory Movements", notificationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromPagedResultAsync(handler.HandleAsync(new GetInventoryMovementsQuery(GetDateRange())));
}

public sealed class LowStockReportViewModel(GetLowStockHandler handler, INotificationService notificationService) : ReportPageViewModel("Low Stock", notificationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromPagedResultAsync(handler.HandleAsync(new GetLowStockQuery()));
}

public sealed class CustomerReportViewModel(GetCustomerReportHandler handler, INotificationService notificationService) : ReportPageViewModel("Customer Report", notificationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromResultAsync(handler.HandleAsync(new GetCustomerReportQuery(GetDateRange())));
}

public sealed class CashierPerformanceViewModel(GetCashierPerformanceHandler handler, INotificationService notificationService) : ReportPageViewModel("Cashier Performance", notificationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromResultAsync(handler.HandleAsync(new GetCashierPerformanceQuery(GetDateRange())));
}

public sealed class PaymentMethodsViewModel(GetPaymentMethodsHandler handler, INotificationService notificationService) : ReportPageViewModel("Payment Methods", notificationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromResultAsync(handler.HandleAsync(new GetPaymentMethodsQuery(GetDateRange())));
}

public sealed class DailySalesViewModel(GetDailySalesHandler handler, INotificationService notificationService) : ReportPageViewModel("Daily Sales", notificationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromResultAsync(handler.HandleAsync(new GetDailySalesQuery(GetDateRange())));
}

public sealed class HourlySalesViewModel(GetHourlySalesHandler handler, INotificationService notificationService) : ReportPageViewModel("Hourly Sales", notificationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromResultAsync(handler.HandleAsync(new GetHourlySalesQuery(GetDateRange())));
}
