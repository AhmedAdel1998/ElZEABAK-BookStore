using BookStore.Application.Features.Reports.DTOs;
using BookStore.Domain.Enums;

namespace BookStore.Application.Features.Reports.Queries;

public sealed record GetReportsDashboardQuery(ReportDateRange DateRange);

public sealed record GetSalesSummaryQuery(
    ReportDateRange DateRange,
    Guid? CashierId = null,
    PaymentMethod? PaymentMethod = null,
    Guid? CustomerId = null,
    SaleStatus? SaleStatus = null);

public sealed record GetSalesDetailsQuery(
    ReportDateRange DateRange,
    Guid? CashierId = null,
    PaymentMethod? PaymentMethod = null,
    Guid? CustomerId = null,
    SaleStatus? SaleStatus = null,
    string? SearchTerm = null,
    string? SortBy = null,
    bool SortDescending = true,
    int PageNumber = 1,
    int PageSize = 50);

public sealed record GetProfitReportQuery(ReportDateRange DateRange);

public sealed record GetBestSellingProductsQuery(ReportDateRange DateRange, int Limit = 10);

public sealed record GetProductSalesQuery(ReportDateRange DateRange, Guid ProductId);

public sealed record GetCategorySalesQuery(ReportDateRange DateRange);

public sealed record GetInventoryReportQuery(string? SearchTerm = null, Guid? CategoryId = null, int PageNumber = 1, int PageSize = 100);

public sealed record GetInventoryMovementsQuery(
    ReportDateRange DateRange,
    Guid? ProductId = null,
    InventoryTransactionType? TransactionType = null,
    Guid? UserId = null,
    int PageNumber = 1,
    int PageSize = 100);

public sealed record GetLowStockQuery(bool OutOfStockOnly = false, string? SearchTerm = null, int PageNumber = 1, int PageSize = 100);

public sealed record GetCustomerReportQuery(ReportDateRange DateRange, string? SearchTerm = null, int? Top = null);

public sealed record GetCashierPerformanceQuery(ReportDateRange DateRange);

public sealed record GetPaymentMethodsQuery(ReportDateRange DateRange);

public sealed record GetDailySalesQuery(ReportDateRange DateRange);

public sealed record GetHourlySalesQuery(ReportDateRange DateRange);
