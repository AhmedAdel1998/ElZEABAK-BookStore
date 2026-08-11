using BookStore.Application.Features.Reports.DTOs;
using BookStore.Application.Features.Reports.Handlers;
using BookStore.Application.Features.Reports.Queries;
using BookStore.Application.Features.Reports.Services;
using BookStore.Application.Features.Reports.Validators;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.Shared.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookStore.Application.Tests;

public class ReportModuleTests
{
    [Fact]
    public async Task DateRangeValidator_RejectsStartAfterEnd()
    {
        var validator = new GetSalesSummaryQueryValidator();

        var result = await validator.ValidateAsync(new GetSalesSummaryQuery(new ReportDateRange(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-1))));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ProfitReport_RequiresProfitPermission()
    {
        var handler = new GetProfitReportHandler(new FakeReportQueryService(), new GetProfitReportQueryValidator(), new FakeAuthorizationService([]), NullLogger<GetProfitReportHandler>.Instance);

        var result = await handler.HandleAsync(new GetProfitReportQuery(ReportDateRange.Today()));

        Assert.False(result.IsSuccess);
        Assert.Equal("You are not authorized to access this report.", result.Error);
    }

    [Fact]
    public async Task SalesSummary_UsesReportSalesPermission()
    {
        var handler = new GetSalesSummaryHandler(new FakeReportQueryService(), new GetSalesSummaryQueryValidator(), new FakeAuthorizationService([PermissionConstants.ReportSales]), NullLogger<GetSalesSummaryHandler>.Instance);

        var result = await handler.HandleAsync(new GetSalesSummaryQuery(ReportDateRange.Today()));

        Assert.True(result.IsSuccess);
        Assert.Equal(125, result.Value!.TotalSales);
    }

    private sealed class FakeAuthorizationService(IReadOnlyCollection<string> permissions) : IAuthorizationService
    {
        public bool HasPermission(string permission) => permissions.Contains(permission);
        public bool HasPermissions(params string[] permissionsToCheck) => permissionsToCheck.All(HasPermission);
        public bool HasRole(string role) => false;
        public bool CanAccess(string requiredPermission) => HasPermission(requiredPermission);
    }

    private sealed class FakeReportQueryService : IReportQueryService
    {
        public Task<ReportsDashboardDto> GetDashboardAsync(GetReportsDashboardQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new ReportsDashboardDto());
        public Task<SalesSummaryDto> GetSalesSummaryAsync(GetSalesSummaryQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new SalesSummaryDto { TotalSales = 125, NumberOfInvoices = 1 });
        public Task<PagedResult<SalesDetailsRowDto>> GetSalesDetailsAsync(GetSalesDetailsQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new PagedResult<SalesDetailsRowDto>());
        public Task<ProfitReportDto> GetProfitReportAsync(GetProfitReportQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new ProfitReportDto());
        public Task<IReadOnlyCollection<ProductSalesRowDto>> GetBestSellingProductsAsync(GetBestSellingProductsQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<ProductSalesRowDto>>([]);
        public Task<ProductSalesRowDto?> GetProductSalesAsync(GetProductSalesQuery query, CancellationToken cancellationToken = default) => Task.FromResult<ProductSalesRowDto?>(null);
        public Task<IReadOnlyCollection<CategorySalesRowDto>> GetCategorySalesAsync(GetCategorySalesQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<CategorySalesRowDto>>([]);
        public Task<PagedResult<InventoryReportRowDto>> GetInventoryReportAsync(GetInventoryReportQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new PagedResult<InventoryReportRowDto>());
        public Task<PagedResult<InventoryMovementReportRowDto>> GetInventoryMovementsAsync(GetInventoryMovementsQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new PagedResult<InventoryMovementReportRowDto>());
        public Task<PagedResult<LowStockReportRowDto>> GetLowStockAsync(GetLowStockQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new PagedResult<LowStockReportRowDto>());
        public Task<IReadOnlyCollection<CustomerReportRowDto>> GetCustomerReportAsync(GetCustomerReportQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<CustomerReportRowDto>>([]);
        public Task<IReadOnlyCollection<CashierPerformanceRowDto>> GetCashierPerformanceAsync(GetCashierPerformanceQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<CashierPerformanceRowDto>>([]);
        public Task<IReadOnlyCollection<PaymentMethodReportRowDto>> GetPaymentMethodsAsync(GetPaymentMethodsQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<PaymentMethodReportRowDto>>([]);
        public Task<IReadOnlyCollection<DailySalesRowDto>> GetDailySalesAsync(GetDailySalesQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<DailySalesRowDto>>([]);
        public Task<IReadOnlyCollection<HourlySalesRowDto>> GetHourlySalesAsync(GetHourlySalesQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<HourlySalesRowDto>>([]);
    }
}
