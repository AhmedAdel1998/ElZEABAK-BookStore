using System.Diagnostics;
using BookStore.Application.Features.Reports.DTOs;
using BookStore.Application.Features.Reports.Queries;
using BookStore.Application.Features.Reports.Services;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Reports.Handlers;

public abstract class ReportQueryHandlerBase<TQuery, TResult>
{
    private readonly IValidator<TQuery> _validator;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger _logger;
    private readonly string _permission;
    private readonly string _reportName;

    protected ReportQueryHandlerBase(IValidator<TQuery> validator, IAuthorizationService authorizationService, ILogger logger, string permission, string reportName)
    {
        _validator = validator;
        _authorizationService = authorizationService;
        _logger = logger;
        _permission = permission;
        _reportName = reportName;
    }

    public async Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(_permission))
        {
            _logger.LogWarning("Unauthorized report access: {ReportName}, Permission: {Permission}", _reportName, _permission);
            return Result<TResult>.Failure("You are not authorized to access this report.");
        }

        var validation = await _validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<TResult>.Failure(validation.Errors[0].ErrorMessage);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("Report opened: {ReportName}", _reportName);
            var result = await ExecuteAsync(query, cancellationToken);
            stopwatch.Stop();
            _logger.LogInformation("Report generated: {ReportName} in {ElapsedMilliseconds} ms", _reportName, stopwatch.ElapsedMilliseconds);
            return Result<TResult>.Success(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Report generation cancelled: {ReportName}", _reportName);
            return Result<TResult>.Failure("Report generation was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Report generation failed: {ReportName}", _reportName);
            return Result<TResult>.Failure("Unable to generate the report. Please try again.");
        }
    }

    protected abstract Task<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken);
}

public sealed class GetReportsDashboardHandler : ReportQueryHandlerBase<GetReportsDashboardQuery, ReportsDashboardDto>
{
    private readonly IReportQueryService _reportQueryService;
    public GetReportsDashboardHandler(IReportQueryService reportQueryService, IValidator<GetReportsDashboardQuery> validator, IAuthorizationService authorizationService, ILogger<GetReportsDashboardHandler> logger)
        : base(validator, authorizationService, logger, PermissionConstants.ReportView, "Reports Dashboard") => _reportQueryService = reportQueryService;
    protected override Task<ReportsDashboardDto> ExecuteAsync(GetReportsDashboardQuery query, CancellationToken cancellationToken) => _reportQueryService.GetDashboardAsync(query, cancellationToken);
}

public sealed class GetSalesSummaryHandler : ReportQueryHandlerBase<GetSalesSummaryQuery, SalesSummaryDto>
{
    private readonly IReportQueryService _reportQueryService;
    public GetSalesSummaryHandler(IReportQueryService reportQueryService, IValidator<GetSalesSummaryQuery> validator, IAuthorizationService authorizationService, ILogger<GetSalesSummaryHandler> logger)
        : base(validator, authorizationService, logger, PermissionConstants.ReportSales, "Sales Summary") => _reportQueryService = reportQueryService;
    protected override Task<SalesSummaryDto> ExecuteAsync(GetSalesSummaryQuery query, CancellationToken cancellationToken) => _reportQueryService.GetSalesSummaryAsync(query, cancellationToken);
}

public sealed class GetSalesDetailsHandler : ReportQueryHandlerBase<GetSalesDetailsQuery, PagedResult<SalesDetailsRowDto>>
{
    private readonly IReportQueryService _reportQueryService;
    public GetSalesDetailsHandler(IReportQueryService reportQueryService, IValidator<GetSalesDetailsQuery> validator, IAuthorizationService authorizationService, ILogger<GetSalesDetailsHandler> logger)
        : base(validator, authorizationService, logger, PermissionConstants.ReportSales, "Sales Details") => _reportQueryService = reportQueryService;
    protected override Task<PagedResult<SalesDetailsRowDto>> ExecuteAsync(GetSalesDetailsQuery query, CancellationToken cancellationToken) => _reportQueryService.GetSalesDetailsAsync(query, cancellationToken);
}

public sealed class GetProfitReportHandler : ReportQueryHandlerBase<GetProfitReportQuery, ProfitReportDto>
{
    private readonly IReportQueryService _reportQueryService;
    public GetProfitReportHandler(IReportQueryService reportQueryService, IValidator<GetProfitReportQuery> validator, IAuthorizationService authorizationService, ILogger<GetProfitReportHandler> logger)
        : base(validator, authorizationService, logger, PermissionConstants.ReportProfit, "Profit Report") => _reportQueryService = reportQueryService;
    protected override Task<ProfitReportDto> ExecuteAsync(GetProfitReportQuery query, CancellationToken cancellationToken) => _reportQueryService.GetProfitReportAsync(query, cancellationToken);
}

public sealed class GetBestSellingProductsHandler : ReportQueryHandlerBase<GetBestSellingProductsQuery, IReadOnlyCollection<ProductSalesRowDto>>
{
    private readonly IReportQueryService _reportQueryService;
    public GetBestSellingProductsHandler(IReportQueryService reportQueryService, IValidator<GetBestSellingProductsQuery> validator, IAuthorizationService authorizationService, ILogger<GetBestSellingProductsHandler> logger)
        : base(validator, authorizationService, logger, PermissionConstants.ReportSales, "Best-Selling Products") => _reportQueryService = reportQueryService;
    protected override Task<IReadOnlyCollection<ProductSalesRowDto>> ExecuteAsync(GetBestSellingProductsQuery query, CancellationToken cancellationToken) => _reportQueryService.GetBestSellingProductsAsync(query, cancellationToken);
}

public sealed class GetProductSalesHandler : ReportQueryHandlerBase<GetProductSalesQuery, ProductSalesRowDto?>
{
    private readonly IReportQueryService _reportQueryService;
    public GetProductSalesHandler(IReportQueryService reportQueryService, IValidator<GetProductSalesQuery> validator, IAuthorizationService authorizationService, ILogger<GetProductSalesHandler> logger)
        : base(validator, authorizationService, logger, PermissionConstants.ReportSales, "Product Sales") => _reportQueryService = reportQueryService;
    protected override Task<ProductSalesRowDto?> ExecuteAsync(GetProductSalesQuery query, CancellationToken cancellationToken) => _reportQueryService.GetProductSalesAsync(query, cancellationToken);
}

public sealed class GetCategorySalesHandler : ReportQueryHandlerBase<GetCategorySalesQuery, IReadOnlyCollection<CategorySalesRowDto>>
{
    private readonly IReportQueryService _reportQueryService;
    public GetCategorySalesHandler(IReportQueryService reportQueryService, IValidator<GetCategorySalesQuery> validator, IAuthorizationService authorizationService, ILogger<GetCategorySalesHandler> logger)
        : base(validator, authorizationService, logger, PermissionConstants.ReportSales, "Category Sales") => _reportQueryService = reportQueryService;
    protected override Task<IReadOnlyCollection<CategorySalesRowDto>> ExecuteAsync(GetCategorySalesQuery query, CancellationToken cancellationToken) => _reportQueryService.GetCategorySalesAsync(query, cancellationToken);
}

public sealed class GetInventoryReportHandler : ReportQueryHandlerBase<GetInventoryReportQuery, PagedResult<InventoryReportRowDto>>
{
    private readonly IReportQueryService _reportQueryService;
    public GetInventoryReportHandler(IReportQueryService reportQueryService, IValidator<GetInventoryReportQuery> validator, IAuthorizationService authorizationService, ILogger<GetInventoryReportHandler> logger)
        : base(validator, authorizationService, logger, PermissionConstants.ReportInventory, "Inventory Report") => _reportQueryService = reportQueryService;
    protected override Task<PagedResult<InventoryReportRowDto>> ExecuteAsync(GetInventoryReportQuery query, CancellationToken cancellationToken) => _reportQueryService.GetInventoryReportAsync(query, cancellationToken);
}

public sealed class GetInventoryMovementsHandler : ReportQueryHandlerBase<GetInventoryMovementsQuery, PagedResult<InventoryMovementReportRowDto>>
{
    private readonly IReportQueryService _reportQueryService;
    public GetInventoryMovementsHandler(IReportQueryService reportQueryService, IValidator<GetInventoryMovementsQuery> validator, IAuthorizationService authorizationService, ILogger<GetInventoryMovementsHandler> logger)
        : base(validator, authorizationService, logger, PermissionConstants.ReportInventory, "Inventory Movement Report") => _reportQueryService = reportQueryService;
    protected override Task<PagedResult<InventoryMovementReportRowDto>> ExecuteAsync(GetInventoryMovementsQuery query, CancellationToken cancellationToken) => _reportQueryService.GetInventoryMovementsAsync(query, cancellationToken);
}

public sealed class GetLowStockHandler : ReportQueryHandlerBase<GetLowStockQuery, PagedResult<LowStockReportRowDto>>
{
    private readonly IReportQueryService _reportQueryService;
    public GetLowStockHandler(IReportQueryService reportQueryService, IValidator<GetLowStockQuery> validator, IAuthorizationService authorizationService, ILogger<GetLowStockHandler> logger)
        : base(validator, authorizationService, logger, PermissionConstants.ReportInventory, "Low Stock Report") => _reportQueryService = reportQueryService;
    protected override Task<PagedResult<LowStockReportRowDto>> ExecuteAsync(GetLowStockQuery query, CancellationToken cancellationToken) => _reportQueryService.GetLowStockAsync(query, cancellationToken);
}

public sealed class GetCustomerReportHandler : ReportQueryHandlerBase<GetCustomerReportQuery, IReadOnlyCollection<CustomerReportRowDto>>
{
    private readonly IReportQueryService _reportQueryService;
    public GetCustomerReportHandler(IReportQueryService reportQueryService, IValidator<GetCustomerReportQuery> validator, IAuthorizationService authorizationService, ILogger<GetCustomerReportHandler> logger)
        : base(validator, authorizationService, logger, PermissionConstants.ReportCustomers, "Customer Report") => _reportQueryService = reportQueryService;
    protected override Task<IReadOnlyCollection<CustomerReportRowDto>> ExecuteAsync(GetCustomerReportQuery query, CancellationToken cancellationToken) => _reportQueryService.GetCustomerReportAsync(query, cancellationToken);
}

public sealed class GetCashierPerformanceHandler : ReportQueryHandlerBase<GetCashierPerformanceQuery, IReadOnlyCollection<CashierPerformanceRowDto>>
{
    private readonly IReportQueryService _reportQueryService;
    public GetCashierPerformanceHandler(IReportQueryService reportQueryService, IValidator<GetCashierPerformanceQuery> validator, IAuthorizationService authorizationService, ILogger<GetCashierPerformanceHandler> logger)
        : base(validator, authorizationService, logger, PermissionConstants.ReportCashiers, "Cashier Performance") => _reportQueryService = reportQueryService;
    protected override Task<IReadOnlyCollection<CashierPerformanceRowDto>> ExecuteAsync(GetCashierPerformanceQuery query, CancellationToken cancellationToken) => _reportQueryService.GetCashierPerformanceAsync(query, cancellationToken);
}

public sealed class GetPaymentMethodsHandler : ReportQueryHandlerBase<GetPaymentMethodsQuery, IReadOnlyCollection<PaymentMethodReportRowDto>>
{
    private readonly IReportQueryService _reportQueryService;
    public GetPaymentMethodsHandler(IReportQueryService reportQueryService, IValidator<GetPaymentMethodsQuery> validator, IAuthorizationService authorizationService, ILogger<GetPaymentMethodsHandler> logger)
        : base(validator, authorizationService, logger, PermissionConstants.ReportSales, "Payment Methods") => _reportQueryService = reportQueryService;
    protected override Task<IReadOnlyCollection<PaymentMethodReportRowDto>> ExecuteAsync(GetPaymentMethodsQuery query, CancellationToken cancellationToken) => _reportQueryService.GetPaymentMethodsAsync(query, cancellationToken);
}

public sealed class GetDailySalesHandler : ReportQueryHandlerBase<GetDailySalesQuery, IReadOnlyCollection<DailySalesRowDto>>
{
    private readonly IReportQueryService _reportQueryService;
    public GetDailySalesHandler(IReportQueryService reportQueryService, IValidator<GetDailySalesQuery> validator, IAuthorizationService authorizationService, ILogger<GetDailySalesHandler> logger)
        : base(validator, authorizationService, logger, PermissionConstants.ReportSales, "Daily Sales") => _reportQueryService = reportQueryService;
    protected override Task<IReadOnlyCollection<DailySalesRowDto>> ExecuteAsync(GetDailySalesQuery query, CancellationToken cancellationToken) => _reportQueryService.GetDailySalesAsync(query, cancellationToken);
}

public sealed class GetHourlySalesHandler : ReportQueryHandlerBase<GetHourlySalesQuery, IReadOnlyCollection<HourlySalesRowDto>>
{
    private readonly IReportQueryService _reportQueryService;
    public GetHourlySalesHandler(IReportQueryService reportQueryService, IValidator<GetHourlySalesQuery> validator, IAuthorizationService authorizationService, ILogger<GetHourlySalesHandler> logger)
        : base(validator, authorizationService, logger, PermissionConstants.ReportSales, "Hourly Sales") => _reportQueryService = reportQueryService;
    protected override Task<IReadOnlyCollection<HourlySalesRowDto>> ExecuteAsync(GetHourlySalesQuery query, CancellationToken cancellationToken) => _reportQueryService.GetHourlySalesAsync(query, cancellationToken);
}
