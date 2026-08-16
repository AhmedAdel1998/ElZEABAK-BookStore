using BookStore.Application.Features.Reports.DTOs;
using BookStore.Application.Features.Reports.Queries;
using BookStore.Application.Features.Reports.Services;
using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Persistence.Context;
using BookStore.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Reporting.Services;

public sealed class BookStoreReportQueryService : IReportQueryService
{
    private const string ProfitAccuracyNote = "Sale items store historical selling price but not historical purchase cost. COGS uses current product purchase price until sale-time cost is added to the sales model.";
    private readonly BookStoreDbContext _dbContext;

    public BookStoreReportQueryService(BookStoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReportsDashboardDto> GetDashboardAsync(GetReportsDashboardQuery query, CancellationToken cancellationToken = default)
    {
        var today = ReportDateRange.Today();
        var salesSummary = await GetSalesSummaryAsync(new GetSalesSummaryQuery(today), cancellationToken);
        var profit = await GetProfitReportAsync(new GetProfitReportQuery(today), cancellationToken);
        var inventory = await _dbContext.Products.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                CurrentInventoryValue = group.Sum(product => product.Quantity * product.PurchasePrice),
                LowStockProducts = group.Count(product => product.Quantity > 0 && product.Quantity <= product.MinimumStock),
                OutOfStockProducts = group.Count(product => product.Quantity == 0)
            })
            .SingleOrDefaultAsync(cancellationToken);
        var bestProduct = (await GetBestSellingProductsAsync(new GetBestSellingProductsQuery(query.DateRange, 1), cancellationToken)).FirstOrDefault()?.Product ?? "None";
        var topCustomer = (await GetCustomerReportAsync(new GetCustomerReportQuery(query.DateRange, Top: 1), cancellationToken)).FirstOrDefault()?.Customer ?? "Walk-in";
        var topCashier = (await GetCashierPerformanceAsync(new GetCashierPerformanceQuery(query.DateRange), cancellationToken)).OrderByDescending(row => row.TotalSales).FirstOrDefault()?.Cashier ?? "None";

        return new ReportsDashboardDto
        {
            TodaysSales = salesSummary.TotalSales,
            TodaysTransactions = salesSummary.NumberOfInvoices,
            TodaysProfit = profit.GrossProfit,
            TodaysDiscounts = salesSummary.TotalDiscounts,
            TodaysTax = salesSummary.TotalTax,
            CurrentInventoryValue = Math.Round(inventory?.CurrentInventoryValue ?? 0, 2),
            LowStockProducts = inventory?.LowStockProducts ?? 0,
            OutOfStockProducts = inventory?.OutOfStockProducts ?? 0,
            BestSellingProduct = bestProduct,
            TopCustomer = topCustomer,
            TopCashier = topCashier,
            ProfitAccuracyNote = ProfitAccuracyNote
        };
    }

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(GetSalesSummaryQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await ApplySalesFilters(_dbContext.Sales.AsNoTracking(), query.DateRange, query.CashierId, query.PaymentMethod, query.CustomerId, query.SaleStatus)
            .Where(sale => query.SaleStatus.HasValue || sale.Status != SaleStatus.Cancelled)
            .Select(sale => new
            {
                sale.Total,
                sale.Discount,
                sale.Tax,
                Gross = sale.Total + sale.Discount - sale.Tax
            })
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalSales = group.Sum(row => row.Total),
                NumberOfInvoices = group.Count(),
                AverageInvoiceValue = group.Average(row => row.Total),
                TotalDiscounts = group.Sum(row => row.Discount),
                TotalTax = group.Sum(row => row.Tax),
                GrossRevenue = group.Sum(row => row.Gross),
                NetRevenue = group.Sum(row => row.Total)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return rows is null
            ? new SalesSummaryDto()
            : new SalesSummaryDto
            {
                TotalSales = Math.Round(rows.TotalSales, 2),
                NumberOfInvoices = rows.NumberOfInvoices,
                AverageInvoiceValue = Math.Round(rows.AverageInvoiceValue, 2),
                TotalDiscounts = Math.Round(rows.TotalDiscounts, 2),
                TotalTax = Math.Round(rows.TotalTax, 2),
                GrossRevenue = Math.Round(rows.GrossRevenue, 2),
                NetRevenue = Math.Round(rows.NetRevenue, 2)
            };
    }

    public async Task<PagedResult<SalesDetailsRowDto>> GetSalesDetailsAsync(GetSalesDetailsQuery query, CancellationToken cancellationToken = default)
    {
        var sales = ApplySalesFilters(_dbContext.Sales.AsNoTracking(), query.DateRange, query.CashierId, query.PaymentMethod, query.CustomerId, query.SaleStatus);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim();
            sales = sales.Where(sale => sale.InvoiceNumber.Contains(search) ||
                (sale.Customer != null && sale.Customer.FullName.Contains(search)) ||
                (sale.Cashier != null && sale.Cashier.FullName.Contains(search)));
        }

        var totalCount = await sales.CountAsync(cancellationToken);
        sales = query.SortBy?.ToUpperInvariant() switch
        {
            "INVOICE" => query.SortDescending ? sales.OrderByDescending(sale => sale.InvoiceNumber) : sales.OrderBy(sale => sale.InvoiceNumber),
            "TOTAL" => query.SortDescending ? sales.OrderByDescending(sale => sale.Total) : sales.OrderBy(sale => sale.Total),
            _ => query.SortDescending ? sales.OrderByDescending(sale => sale.SaleDate) : sales.OrderBy(sale => sale.SaleDate)
        };

        var items = await sales
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(sale => new SalesDetailsRowDto
            {
                SaleId = sale.Id,
                InvoiceNumber = sale.InvoiceNumber,
                Date = sale.SaleDate,
                Cashier = sale.Cashier == null ? string.Empty : sale.Cashier.FullName,
                Customer = sale.Customer == null ? "Walk-in" : sale.Customer.FullName,
                Subtotal = sale.Total + sale.Discount - sale.Tax,
                Discount = sale.Discount,
                Tax = sale.Tax,
                Total = sale.Total,
                PaymentMethod = sale.PaymentMethod,
                SaleStatus = sale.Status
            })
            .ToArrayAsync(cancellationToken);

        return new PagedResult<SalesDetailsRowDto> { Items = items, PageNumber = query.PageNumber, PageSize = query.PageSize, TotalCount = totalCount };
    }

    public async Task<ProfitReportDto> GetProfitReportAsync(GetProfitReportQuery query, CancellationToken cancellationToken = default)
    {
        var range = query.DateRange.ToUtcBounds();
        var rows = await SaleItemRows(range)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Revenue = group.Sum(row => row.Item.Total),
                Cost = group.Sum(row => row.Product.PurchasePrice * row.Item.Quantity)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var revenue = Math.Round(rows?.Revenue ?? 0, 2);
        var cost = Math.Round(rows?.Cost ?? 0, 2);
        var profit = revenue - cost;
        return new ProfitReportDto
        {
            Revenue = revenue,
            CostOfGoodsSold = cost,
            GrossProfit = profit,
            GrossMarginPercent = revenue == 0 ? 0 : Math.Round(profit / revenue * 100, 2),
            UsesHistoricalCost = false,
            AccuracyNote = ProfitAccuracyNote
        };
    }

    public async Task<IReadOnlyCollection<ProductSalesRowDto>> GetBestSellingProductsAsync(GetBestSellingProductsQuery query, CancellationToken cancellationToken = default)
    {
        var range = query.DateRange.ToUtcBounds();
        return (await BuildProductSalesRowsAsync(range, cancellationToken))
            .OrderByDescending(row => row.QuantitySold)
            .ThenByDescending(row => row.Revenue)
            .Take(query.Limit)
            .ToArray();
    }

    public async Task<ProductSalesRowDto?> GetProductSalesAsync(GetProductSalesQuery query, CancellationToken cancellationToken = default)
    {
        var range = query.DateRange.ToUtcBounds();
        return (await BuildProductSalesRowsAsync(range, cancellationToken))
            .Where(row => row.ProductId == query.ProductId)
            .FirstOrDefault();
    }

    public async Task<IReadOnlyCollection<CategorySalesRowDto>> GetCategorySalesAsync(GetCategorySalesQuery query, CancellationToken cancellationToken = default)
    {
        var range = query.DateRange.ToUtcBounds();
        var totalRevenue = await SaleItemRows(range).SumAsync(row => row.Item.Total, cancellationToken);
        var rows = await SaleItemRows(range)
            .GroupBy(row => row.Category.Name)
            .Select(group => new CategorySalesRowDto
            {
                Category = group.Key,
                ProductsSold = group.Select(row => row.Product.Id).Distinct().Count(),
                QuantitySold = group.Sum(row => row.Item.Quantity),
                Revenue = group.Sum(row => row.Item.Total),
                Profit = group.Sum(row => row.Item.Total - (row.Product.PurchasePrice * row.Item.Quantity))
            })
            .ToArrayAsync(cancellationToken);

        foreach (var row in rows)
        {
            row.Revenue = Math.Round(row.Revenue, 2);
            row.Profit = Math.Round(row.Profit, 2);
            row.PercentageOfTotalRevenue = totalRevenue == 0 ? 0 : Math.Round(row.Revenue / totalRevenue * 100, 2);
        }

        return rows.OrderByDescending(row => row.Revenue).ToArray();
    }

    public async Task<PagedResult<InventoryReportRowDto>> GetInventoryReportAsync(GetInventoryReportQuery query, CancellationToken cancellationToken = default)
    {
        var products = _dbContext.Products.AsNoTracking().Where(product => product.IsActive);
        if (query.CategoryId.HasValue)
        {
            products = products.Where(product => product.CategoryId == query.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim();
            products = products.Where(product => product.Title.Contains(search) || product.Barcode.Value.Contains(search));
        }

        var totalCount = await products.CountAsync(cancellationToken);
        var rows = await products
            .OrderBy(product => product.Title)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(product => new InventoryReportRowDto
            {
                ProductId = product.Id,
                Product = product.Title,
                Barcode = product.Barcode.Value,
                Category = product.Category == null ? string.Empty : product.Category.Name,
                CurrentQuantity = product.Quantity,
                MinimumStock = product.MinimumStock,
                PurchasePrice = product.PurchasePrice,
                SellingPrice = product.SellingPrice,
                InventoryCostValue = product.Quantity * product.PurchasePrice,
                PotentialSellingValue = product.Quantity * product.SellingPrice,
                InventoryStatus = product.Quantity == 0 ? "Out of Stock" : product.Quantity <= product.MinimumStock ? "Low Stock" : "In Stock"
            })
            .ToArrayAsync(cancellationToken);

        return new PagedResult<InventoryReportRowDto> { Items = rows, PageNumber = query.PageNumber, PageSize = query.PageSize, TotalCount = totalCount };
    }

    public async Task<PagedResult<InventoryMovementReportRowDto>> GetInventoryMovementsAsync(GetInventoryMovementsQuery query, CancellationToken cancellationToken = default)
    {
        var range = query.DateRange.ToUtcBounds();
        var movements = _dbContext.InventoryTransactions.AsNoTracking()
            .Where(transaction => transaction.Date >= range.StartDate && transaction.Date < range.EndDate);
        if (query.ProductId.HasValue)
        {
            movements = movements.Where(transaction => transaction.ProductId == query.ProductId.Value);
        }

        if (query.TransactionType.HasValue)
        {
            movements = movements.Where(transaction => transaction.TransactionType == query.TransactionType.Value);
        }

        if (query.UserId.HasValue)
        {
            movements = movements.Where(transaction => transaction.UserId == query.UserId.Value);
        }

        var totalCount = await movements.CountAsync(cancellationToken);
        var products = _dbContext.Products.AsNoTracking();
        var rows = await movements
            .Join(products, transaction => transaction.ProductId, product => product.Id, (transaction, product) => new InventoryMovementReportRowDto
            {
                Date = transaction.Date,
                Product = product.Title,
                TransactionType = transaction.TransactionType,
                QuantityBefore = transaction.QuantityBefore,
                QuantityChanged = transaction.Quantity,
                QuantityAfter = transaction.QuantityAfter,
                User = transaction.UserName,
                Reference = transaction.Reference,
                Notes = transaction.Notes
            })
            .OrderByDescending(row => row.Date)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<InventoryMovementReportRowDto> { Items = rows, PageNumber = query.PageNumber, PageSize = query.PageSize, TotalCount = totalCount };
    }

    public async Task<PagedResult<LowStockReportRowDto>> GetLowStockAsync(GetLowStockQuery query, CancellationToken cancellationToken = default)
    {
        var products = _dbContext.Products.AsNoTracking()
            .Where(product => product.IsActive && (query.OutOfStockOnly ? product.Quantity == 0 : product.Quantity <= product.MinimumStock));
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim();
            products = products.Where(product => product.Title.Contains(search) || product.Barcode.Value.Contains(search));
        }

        var totalCount = await products.CountAsync(cancellationToken);
        var rows = await products
            .OrderBy(product => product.Quantity)
            .ThenBy(product => product.Title)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(product => new LowStockReportRowDto
            {
                ProductId = product.Id,
                Product = product.Title,
                Barcode = product.Barcode.Value,
                CurrentQuantity = product.Quantity,
                MinimumStock = product.MinimumStock,
                Difference = product.MinimumStock - product.Quantity,
                Category = product.Category == null ? string.Empty : product.Category.Name,
                Supplier = _dbContext.ProductSuppliers.AsNoTracking()
                    .Where(link => link.ProductId == product.Id && link.IsActive)
                    .OrderByDescending(link => link.IsPreferred)
                    .Select(link => link.Supplier == null ? string.Empty : link.Supplier.CompanyName)
                    .FirstOrDefault() ?? string.Empty
            })
            .ToArrayAsync(cancellationToken);

        return new PagedResult<LowStockReportRowDto> { Items = rows, PageNumber = query.PageNumber, PageSize = query.PageSize, TotalCount = totalCount };
    }

    public async Task<IReadOnlyCollection<CustomerReportRowDto>> GetCustomerReportAsync(GetCustomerReportQuery query, CancellationToken cancellationToken = default)
    {
        var sales = ApplySalesFilters(_dbContext.Sales.AsNoTracking(), query.DateRange).Where(sale => sale.Status != SaleStatus.Cancelled);
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim();
            sales = sales.Where(sale => sale.Customer != null && sale.Customer.FullName.Contains(search));
        }

        var rowsQuery = sales
            .GroupBy(sale => new { sale.CustomerId, Customer = sale.Customer == null ? "Walk-in" : sale.Customer.FullName })
            .Select(group => new
            {
                group.Key.CustomerId,
                Customer = group.Key.Customer,
                NumberOfSales = group.Count(),
                TotalPurchases = group.Sum(sale => sale.Total),
                AveragePurchase = group.Average(sale => sale.Total),
                LastPurchaseDate = group.Max(sale => sale.SaleDate)
            });

        var rows = await rowsQuery.ToArrayAsync(cancellationToken);
        IEnumerable<CustomerReportRowDto> result = rows.Select(row => new CustomerReportRowDto
        {
            CustomerId = row.CustomerId,
            Customer = row.Customer,
            NumberOfSales = row.NumberOfSales,
            TotalPurchases = Math.Round(row.TotalPurchases, 2),
            AveragePurchase = Math.Round(row.AveragePurchase, 2),
            LastPurchaseDate = row.LastPurchaseDate
        }).OrderByDescending(row => row.TotalPurchases);

        if (query.Top.HasValue)
        {
            result = result.Take(query.Top.Value);
        }

        return result.ToArray();
    }

    public async Task<IReadOnlyCollection<CashierPerformanceRowDto>> GetCashierPerformanceAsync(GetCashierPerformanceQuery query, CancellationToken cancellationToken = default)
    {
        var sales = ApplySalesFilters(_dbContext.Sales.AsNoTracking(), query.DateRange);
        var rows = await sales
            .GroupBy(sale => new { sale.UserId, Cashier = sale.Cashier == null ? string.Empty : sale.Cashier.FullName })
            .Select(group => new
            {
                group.Key.UserId,
                Cashier = group.Key.Cashier,
                NumberOfTransactions = group.Count(sale => sale.Status != SaleStatus.Cancelled),
                TotalSales = group.Where(sale => sale.Status != SaleStatus.Cancelled).Sum(sale => sale.Total),
                AverageTransaction = group.Where(sale => sale.Status != SaleStatus.Cancelled).Average(sale => (decimal?)sale.Total) ?? 0,
                Discounts = group.Where(sale => sale.Status != SaleStatus.Cancelled).Sum(sale => sale.Discount),
                CancelledSales = group.Count(sale => sale.Status == SaleStatus.Cancelled)
            })
            .ToArrayAsync(cancellationToken);

        return rows.Select(row => new CashierPerformanceRowDto
        {
            CashierId = row.UserId,
            Cashier = row.Cashier,
            NumberOfTransactions = row.NumberOfTransactions,
            TotalSales = Math.Round(row.TotalSales, 2),
            AverageTransaction = Math.Round(row.AverageTransaction, 2),
            Discounts = Math.Round(row.Discounts, 2),
            CancelledSales = row.CancelledSales
        }).OrderByDescending(row => row.TotalSales).ToArray();
    }

    public async Task<IReadOnlyCollection<PaymentMethodReportRowDto>> GetPaymentMethodsAsync(GetPaymentMethodsQuery query, CancellationToken cancellationToken = default)
    {
        var sales = ApplySalesFilters(_dbContext.Sales.AsNoTracking(), query.DateRange).Where(sale => sale.Status != SaleStatus.Cancelled);
        var total = await sales.SumAsync(sale => sale.Total, cancellationToken);
        var rows = await sales
            .GroupBy(sale => sale.PaymentMethod)
            .Select(group => new
            {
                PaymentMethod = group.Key,
                TransactionCount = group.Count(),
                TotalAmount = group.Sum(sale => sale.Total)
            })
            .ToArrayAsync(cancellationToken);
        var result = rows.Select(row => new PaymentMethodReportRowDto
        {
            PaymentMethod = row.PaymentMethod,
            TransactionCount = row.TransactionCount,
            TotalAmount = Math.Round(row.TotalAmount, 2),
            PercentageOfSales = total == 0 ? 0 : Math.Round(row.TotalAmount / total * 100, 2)
        }).OrderByDescending(row => row.TotalAmount).ToArray();

        return result;
    }

    public async Task<IReadOnlyCollection<DailySalesRowDto>> GetDailySalesAsync(GetDailySalesQuery query, CancellationToken cancellationToken = default)
    {
        var range = query.DateRange.ToUtcBounds();
        var sales = _dbContext.Sales.AsNoTracking()
            .Where(sale => sale.SaleDate >= range.StartDate && sale.SaleDate < range.EndDate && sale.Status != SaleStatus.Cancelled);
        var filteredSales = await sales
            .Select(sale => new { sale.SaleDate, sale.Total, sale.Discount, sale.Tax })
            .ToArrayAsync(cancellationToken);
        var saleTotals = filteredSales
            .GroupBy(sale => DateOnly.FromDateTime(sale.SaleDate.UtcDateTime.Date))
            .Select(group => new DailySalesRowDto
            {
                Date = group.Key,
                Transactions = group.Count(),
                Revenue = group.Sum(sale => sale.Total),
                Discount = group.Sum(sale => sale.Discount),
                Tax = group.Sum(sale => sale.Tax)
            })
            .ToArray();
        var profitSource = await SaleItemRows(range)
            .Select(row => new { row.Sale.SaleDate, Profit = row.Item.Total - (row.Product.PurchasePrice * row.Item.Quantity) })
            .ToArrayAsync(cancellationToken);
        var profitRows = profitSource
            .GroupBy(row => DateOnly.FromDateTime(row.SaleDate.UtcDateTime.Date))
            .ToDictionary(group => group.Key, group => group.Sum(row => row.Profit));

        foreach (var row in saleTotals)
        {
            row.Revenue = Math.Round(row.Revenue, 2);
            row.Discount = Math.Round(row.Discount, 2);
            row.Tax = Math.Round(row.Tax, 2);
            row.Profit = Math.Round(profitRows.TryGetValue(row.Date, out var profit) ? profit : 0, 2);
        }

        return saleTotals.OrderBy(row => row.Date).ToArray();
    }

    public async Task<IReadOnlyCollection<HourlySalesRowDto>> GetHourlySalesAsync(GetHourlySalesQuery query, CancellationToken cancellationToken = default)
    {
        var range = query.DateRange.ToUtcBounds();
        var filteredSales = await _dbContext.Sales.AsNoTracking()
            .Where(sale => sale.SaleDate >= range.StartDate && sale.SaleDate < range.EndDate && sale.Status != SaleStatus.Cancelled)
            .Select(sale => new { sale.SaleDate, sale.Total })
            .ToArrayAsync(cancellationToken);
        var rows = filteredSales
            .GroupBy(sale => sale.SaleDate.UtcDateTime.Hour)
            .Select(group => new HourlySalesRowDto
            {
                Hour = group.Key,
                NumberOfSales = group.Count(),
                Revenue = group.Sum(sale => sale.Total)
            })
            .OrderBy(row => row.Hour)
            .ToArray();
        foreach (var row in rows)
        {
            row.Revenue = Math.Round(row.Revenue, 2);
        }

        return rows;
    }

    private async Task<IReadOnlyCollection<ProductSalesRowDto>> BuildProductSalesRowsAsync(ReportDateRange range, CancellationToken cancellationToken)
    {
        var sourceRows = await SaleItemRows(range)
            .Select(row => new
            {
                ProductId = row.Product.Id,
                Product = row.Product.Title,
                Barcode = row.Product.Barcode.Value,
                Category = row.Category.Name,
                row.Item.Quantity,
                row.Item.Total,
                row.Product.PurchasePrice,
                row.Item.UnitPrice,
                SaleId = row.Sale.Id
            })
            .ToArrayAsync(cancellationToken);

        var rows = sourceRows
            .GroupBy(row => new
            {
                row.ProductId,
                row.Product,
                row.Barcode,
                row.Category
            })
            .Select(group => new
            {
                group.Key.ProductId,
                group.Key.Product,
                group.Key.Barcode,
                group.Key.Category,
                QuantitySold = group.Sum(row => row.Quantity),
                Revenue = group.Sum(row => row.Total),
                Cost = group.Sum(row => row.PurchasePrice * row.Quantity),
                AverageSellingPrice = group.Average(row => row.UnitPrice),
                NumberOfTransactions = group.Select(row => row.SaleId).Distinct().Count()
            });

        return rows
            .Select(row =>
            {
                return new ProductSalesRowDto
                {
                    ProductId = row.ProductId,
                    Product = row.Product,
                    Barcode = row.Barcode ?? string.Empty,
                    Category = row.Category,
                    QuantitySold = row.QuantitySold,
                    Revenue = Math.Round(row.Revenue, 2),
                    Cost = Math.Round(row.Cost, 2),
                    Profit = Math.Round(row.Revenue - row.Cost, 2),
                    AverageSellingPrice = Math.Round(row.AverageSellingPrice, 2),
                    NumberOfTransactions = row.NumberOfTransactions
                };
            })
            .ToArray();
    }

    private IQueryable<SaleItemReportRow> SaleItemRows(ReportDateRange range)
    {
        return from item in _dbContext.SaleItems.AsNoTracking()
               join sale in _dbContext.Sales.AsNoTracking() on EF.Property<Guid>(item, "SaleId") equals sale.Id
               join product in _dbContext.Products.AsNoTracking() on item.ProductId equals product.Id
               join category in _dbContext.Categories.AsNoTracking() on product.CategoryId equals category.Id
               where sale.SaleDate >= range.StartDate && sale.SaleDate < range.EndDate && sale.Status != SaleStatus.Cancelled
               select new SaleItemReportRow { Sale = sale, Item = item, Product = product, Category = category };
    }

    private static IQueryable<Sale> ApplySalesFilters(IQueryable<Sale> sales, ReportDateRange dateRange, Guid? cashierId = null, PaymentMethod? paymentMethod = null, Guid? customerId = null, SaleStatus? saleStatus = null)
    {
        var range = dateRange.ToUtcBounds();
        sales = sales.Where(sale => sale.SaleDate >= range.StartDate && sale.SaleDate < range.EndDate);
        if (cashierId.HasValue)
        {
            sales = sales.Where(sale => sale.UserId == cashierId.Value);
        }

        if (paymentMethod.HasValue)
        {
            sales = sales.Where(sale => sale.PaymentMethod == paymentMethod.Value);
        }

        if (customerId.HasValue)
        {
            sales = sales.Where(sale => sale.CustomerId == customerId.Value);
        }

        if (saleStatus.HasValue)
        {
            sales = sales.Where(sale => sale.Status == saleStatus.Value);
        }

        return sales;
    }

    private sealed class SaleItemReportRow
    {
        public Sale Sale { get; init; } = null!;
        public SaleItem Item { get; init; } = null!;
        public Product Product { get; init; } = null!;
        public Category Category { get; init; } = null!;
    }
}
