using BookStore.Application.Features.Reports.DTOs;
using BookStore.Application.Features.Reports.Queries;
using BookStore.Shared.Results;

namespace BookStore.Application.Features.Reports.Services;

public interface IReportQueryService
{
    Task<ReportsDashboardDto> GetDashboardAsync(GetReportsDashboardQuery query, CancellationToken cancellationToken = default);
    Task<SalesSummaryDto> GetSalesSummaryAsync(GetSalesSummaryQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<SalesDetailsRowDto>> GetSalesDetailsAsync(GetSalesDetailsQuery query, CancellationToken cancellationToken = default);
    Task<ProfitReportDto> GetProfitReportAsync(GetProfitReportQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductSalesRowDto>> GetBestSellingProductsAsync(GetBestSellingProductsQuery query, CancellationToken cancellationToken = default);
    Task<ProductSalesRowDto?> GetProductSalesAsync(GetProductSalesQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CategorySalesRowDto>> GetCategorySalesAsync(GetCategorySalesQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<InventoryReportRowDto>> GetInventoryReportAsync(GetInventoryReportQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<InventoryMovementReportRowDto>> GetInventoryMovementsAsync(GetInventoryMovementsQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<LowStockReportRowDto>> GetLowStockAsync(GetLowStockQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CustomerReportRowDto>> GetCustomerReportAsync(GetCustomerReportQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CashierPerformanceRowDto>> GetCashierPerformanceAsync(GetCashierPerformanceQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PaymentMethodReportRowDto>> GetPaymentMethodsAsync(GetPaymentMethodsQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DailySalesRowDto>> GetDailySalesAsync(GetDailySalesQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<HourlySalesRowDto>> GetHourlySalesAsync(GetHourlySalesQuery query, CancellationToken cancellationToken = default);
}
