using BookStore.Domain.Enums;

namespace BookStore.Application.Features.Reports.DTOs;

public enum ReportDateRangePreset
{
    Today,
    Yesterday,
    ThisWeek,
    ThisMonth,
    PreviousMonth,
    ThisYear,
    Custom
}

public sealed record ReportDateRange(DateTimeOffset StartDate, DateTimeOffset EndDate)
{
    public static ReportDateRange Today(TimeProvider? timeProvider = null)
    {
        var now = (timeProvider ?? TimeProvider.System).GetLocalNow();
        var start = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
        return new ReportDateRange(start, start.AddDays(1));
    }

    public static ReportDateRange FromPreset(ReportDateRangePreset preset, TimeProvider? timeProvider = null)
    {
        var now = (timeProvider ?? TimeProvider.System).GetLocalNow();
        var today = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
        return preset switch
        {
            ReportDateRangePreset.Today => new ReportDateRange(today, today.AddDays(1)),
            ReportDateRangePreset.Yesterday => new ReportDateRange(today.AddDays(-1), today),
            ReportDateRangePreset.ThisWeek => new ReportDateRange(today.AddDays(-(int)today.DayOfWeek), today.AddDays(1)),
            ReportDateRangePreset.ThisMonth => new ReportDateRange(new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, today.Offset), today.AddDays(1)),
            ReportDateRangePreset.PreviousMonth => PreviousMonth(today),
            ReportDateRangePreset.ThisYear => new ReportDateRange(new DateTimeOffset(today.Year, 1, 1, 0, 0, 0, today.Offset), today.AddDays(1)),
            _ => new ReportDateRange(today, today.AddDays(1))
        };
    }

    public ReportDateRange ToUtcBounds() => new(StartDate.ToUniversalTime(), EndDate.ToUniversalTime());

    private static ReportDateRange PreviousMonth(DateTimeOffset today)
    {
        var thisMonth = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, today.Offset);
        var previousMonth = thisMonth.AddMonths(-1);
        return new ReportDateRange(previousMonth, thisMonth);
    }
}

public sealed class ReportsDashboardDto
{
    public decimal TodaysSales { get; set; }
    public int TodaysTransactions { get; set; }
    public decimal TodaysProfit { get; set; }
    public decimal TodaysDiscounts { get; set; }
    public decimal TodaysTax { get; set; }
    public decimal CurrentInventoryValue { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public string BestSellingProduct { get; set; } = "None";
    public string TopCustomer { get; set; } = "Walk-in";
    public string TopCashier { get; set; } = "None";
    public string ProfitAccuracyNote { get; set; } = string.Empty;
}

public sealed class SalesSummaryDto
{
    public decimal TotalSales { get; set; }
    public int NumberOfInvoices { get; set; }
    public decimal AverageInvoiceValue { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal TotalTax { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal NetRevenue { get; set; }
}

public sealed class SalesDetailsRowDto
{
    public Guid SaleId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public string Cashier { get; set; } = string.Empty;
    public string Customer { get; set; } = "Walk-in";
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public SaleStatus SaleStatus { get; set; }
}

public sealed class ProfitReportDto
{
    public decimal Revenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossMarginPercent { get; set; }
    public bool UsesHistoricalCost { get; set; }
    public string AccuracyNote { get; set; } = string.Empty;
}

public sealed class ProductSalesRowDto
{
    public Guid ProductId { get; set; }
    public string Product { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public decimal AverageSellingPrice { get; set; }
    public int NumberOfTransactions { get; set; }
}

public sealed class CategorySalesRowDto
{
    public string Category { get; set; } = string.Empty;
    public int ProductsSold { get; set; }
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal Profit { get; set; }
    public decimal PercentageOfTotalRevenue { get; set; }
}

public sealed class InventoryReportRowDto
{
    public Guid ProductId { get; set; }
    public string Product { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentQuantity { get; set; }
    public int MinimumStock { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal InventoryCostValue { get; set; }
    public decimal PotentialSellingValue { get; set; }
    public string InventoryStatus { get; set; } = string.Empty;
}

public sealed class InventoryMovementReportRowDto
{
    public DateTimeOffset Date { get; set; }
    public string Product { get; set; } = string.Empty;
    public InventoryTransactionType TransactionType { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityChanged { get; set; }
    public int QuantityAfter { get; set; }
    public string? User { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public sealed class LowStockReportRowDto
{
    public Guid ProductId { get; set; }
    public string Product { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int CurrentQuantity { get; set; }
    public int MinimumStock { get; set; }
    public int Difference { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
}

public sealed class CustomerReportRowDto
{
    public Guid? CustomerId { get; set; }
    public string Customer { get; set; } = "Walk-in";
    public int NumberOfSales { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal AveragePurchase { get; set; }
    public DateTimeOffset? LastPurchaseDate { get; set; }
}

public sealed class CashierPerformanceRowDto
{
    public Guid CashierId { get; set; }
    public string Cashier { get; set; } = string.Empty;
    public int NumberOfTransactions { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AverageTransaction { get; set; }
    public decimal Discounts { get; set; }
    public int CancelledSales { get; set; }
}

public sealed class PaymentMethodReportRowDto
{
    public PaymentMethod PaymentMethod { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PercentageOfSales { get; set; }
}

public sealed class DailySalesRowDto
{
    public DateOnly Date { get; set; }
    public int Transactions { get; set; }
    public decimal Revenue { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Profit { get; set; }
}

public sealed class HourlySalesRowDto
{
    public int Hour { get; set; }
    public int NumberOfSales { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class ChartPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}
