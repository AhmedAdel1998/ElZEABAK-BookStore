using System.Collections.ObjectModel;
using System.IO;
using BookStore.Application.Features.Reports.DTOs;
using BookStore.Application.Features.Reports.Handlers;
using BookStore.Application.Features.Reports.Queries;
using BookStore.Application.Features.Reports.Services;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.Shared.Results;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace BookStore.UI.ViewModels;

public abstract partial class ReportPageViewModel : BaseViewModel
{
    private readonly INotificationService _notificationService;
    private readonly IReportExporter _reportExporter;
    private readonly IAuthorizationService _authorizationService;

    [ObservableProperty] private ReportDateRangePreset selectedPreset = ReportDateRangePreset.Today;
    [ObservableProperty] private DateTime? startDate = DateTime.Today;
    [ObservableProperty] private DateTime? endDate = DateTime.Today;
    [ObservableProperty] private string emptyMessage = "No rows found for the selected filters.";

    protected ReportPageViewModel(string title, INotificationService notificationService, IReportExporter reportExporter, IAuthorizationService authorizationService)
    {
        Title = title;
        _notificationService = notificationService;
        _reportExporter = reportExporter;
        _authorizationService = authorizationService;
    }

    public ObservableCollection<object> Rows { get; } = [];

    /// <summary>Gets whether the current user may export reports.</summary>
    public bool CanExport => _authorizationService.HasPermission(PermissionConstants.ReportExport);

    /// <summary>Exports the currently loaded rows as CSV.</summary>
    [RelayCommand(CanExecute = nameof(CanExport))]
    private Task ExportCsvAsync() => ExportAsync(ReportExportFormat.Csv);

    /// <summary>Exports the currently loaded rows as an Excel workbook.</summary>
    [RelayCommand(CanExecute = nameof(CanExport))]
    private Task ExportExcelAsync() => ExportAsync(ReportExportFormat.Excel);

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

    private async Task ExportAsync(ReportExportFormat format)
    {
        if (Rows.Count == 0)
        {
            _notificationService.Show(Title, "There are no report rows to export.", NotificationSeverity.Information);
            return;
        }
        var extension = format == ReportExportFormat.Csv ? ".csv" : ".xlsx";
        var dialog = new SaveFileDialog
        {
            Title = $"Export {Title}",
            Filter = format == ReportExportFormat.Csv ? "CSV files (*.csv)|*.csv" : "Excel workbooks (*.xlsx)|*.xlsx",
            DefaultExt = extension,
            AddExtension = true,
            FileName = $"{Title.Replace(' ', '-')}-{DateTime.Now:yyyyMMdd-HHmmss}{extension}",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var result = await _reportExporter.ExportAsync(new ReportExportRequest<object>(Title, Rows.ToArray(), format));
            var fullPath = Path.GetFullPath(dialog.FileName);
            var temporaryPath = $"{fullPath}.{Guid.NewGuid():N}.tmp";
            try
            {
                await File.WriteAllBytesAsync(temporaryPath, result.Content);
                File.Move(temporaryPath, fullPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            _notificationService.Show(Title, $"Exported {Rows.Count} report rows.", NotificationSeverity.Success);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _notificationService.Show(Title, $"Unable to export report: {ex.Message}", NotificationSeverity.Error);
        }
    }
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

public sealed class SalesSummaryViewModel(GetSalesSummaryHandler handler, INotificationService notificationService, IReportExporter exporter, IAuthorizationService authorizationService) : ReportPageViewModel("Sales Summary", notificationService, exporter, authorizationService)
{
    protected override async Task<IReadOnlyCollection<object>> LoadRowsAsync()
    {
        var result = await handler.HandleAsync(new GetSalesSummaryQuery(GetDateRange()));
        return result.IsSuccess && result.Value is not null ? [result.Value] : [];
    }
}

public sealed class SalesDetailsViewModel(GetSalesDetailsHandler handler, INotificationService notificationService, IReportExporter exporter, IAuthorizationService authorizationService) : ReportPageViewModel("Sales Details", notificationService, exporter, authorizationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromPagedResultAsync(handler.HandleAsync(new GetSalesDetailsQuery(GetDateRange())));
}

public sealed class ProfitReportViewModel(GetProfitReportHandler handler, INotificationService notificationService, IReportExporter exporter, IAuthorizationService authorizationService) : ReportPageViewModel("Profit Report", notificationService, exporter, authorizationService)
{
    protected override async Task<IReadOnlyCollection<object>> LoadRowsAsync()
    {
        var result = await handler.HandleAsync(new GetProfitReportQuery(GetDateRange()));
        return result.IsSuccess && result.Value is not null ? [result.Value] : [];
    }
}

public sealed class BestSellingProductsViewModel(GetBestSellingProductsHandler handler, INotificationService notificationService, IReportExporter exporter, IAuthorizationService authorizationService) : ReportPageViewModel("Best-Selling Products", notificationService, exporter, authorizationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromResultAsync(handler.HandleAsync(new GetBestSellingProductsQuery(GetDateRange(), 10)));
}

public sealed class InventoryReportViewModel(GetInventoryReportHandler handler, INotificationService notificationService, IReportExporter exporter, IAuthorizationService authorizationService) : ReportPageViewModel("Inventory Report", notificationService, exporter, authorizationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromPagedResultAsync(handler.HandleAsync(new GetInventoryReportQuery()));
}

public sealed class InventoryMovementViewModel(GetInventoryMovementsHandler handler, INotificationService notificationService, IReportExporter exporter, IAuthorizationService authorizationService) : ReportPageViewModel("Inventory Movements", notificationService, exporter, authorizationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromPagedResultAsync(handler.HandleAsync(new GetInventoryMovementsQuery(GetDateRange())));
}

public sealed class LowStockReportViewModel(GetLowStockHandler handler, INotificationService notificationService, IReportExporter exporter, IAuthorizationService authorizationService) : ReportPageViewModel("Low Stock", notificationService, exporter, authorizationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromPagedResultAsync(handler.HandleAsync(new GetLowStockQuery()));
}

public sealed class CustomerReportViewModel(GetCustomerReportHandler handler, INotificationService notificationService, IReportExporter exporter, IAuthorizationService authorizationService) : ReportPageViewModel("Customer Report", notificationService, exporter, authorizationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromResultAsync(handler.HandleAsync(new GetCustomerReportQuery(GetDateRange())));
}

public sealed class CashierPerformanceViewModel(GetCashierPerformanceHandler handler, INotificationService notificationService, IReportExporter exporter, IAuthorizationService authorizationService) : ReportPageViewModel("Cashier Performance", notificationService, exporter, authorizationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromResultAsync(handler.HandleAsync(new GetCashierPerformanceQuery(GetDateRange())));
}

public sealed class PaymentMethodsViewModel(GetPaymentMethodsHandler handler, INotificationService notificationService, IReportExporter exporter, IAuthorizationService authorizationService) : ReportPageViewModel("Payment Methods", notificationService, exporter, authorizationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromResultAsync(handler.HandleAsync(new GetPaymentMethodsQuery(GetDateRange())));
}

public sealed class DailySalesViewModel(GetDailySalesHandler handler, INotificationService notificationService, IReportExporter exporter, IAuthorizationService authorizationService) : ReportPageViewModel("Daily Sales", notificationService, exporter, authorizationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromResultAsync(handler.HandleAsync(new GetDailySalesQuery(GetDateRange())));
}

public sealed class HourlySalesViewModel(GetHourlySalesHandler handler, INotificationService notificationService, IReportExporter exporter, IAuthorizationService authorizationService) : ReportPageViewModel("Hourly Sales", notificationService, exporter, authorizationService)
{
    protected override Task<IReadOnlyCollection<object>> LoadRowsAsync() => FromResultAsync(handler.HandleAsync(new GetHourlySalesQuery(GetDateRange())));
}
