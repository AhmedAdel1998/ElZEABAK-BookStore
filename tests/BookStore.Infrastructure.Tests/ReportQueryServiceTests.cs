using BookStore.Application.Features.Reports.DTOs;
using BookStore.Application.Features.Reports.Queries;
using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.ValueObjects;
using BookStore.Persistence.Context;
using BookStore.Reporting.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Tests;

public class ReportQueryServiceTests
{
    [Fact]
    public async Task SalesSummary_ExcludesCancelledSales_AndFiltersDateRange()
    {
        await using var fixture = await ReportingFixture.CreateAsync();

        var summary = await fixture.Service.GetSalesSummaryAsync(new GetSalesSummaryQuery(fixture.TodayRange));

        Assert.Equal(214, summary.TotalSales);
        Assert.Equal(2, summary.NumberOfInvoices);
        Assert.Equal(14, summary.TotalTax);
        Assert.Equal(10, summary.TotalDiscounts);
    }

    [Fact]
    public async Task ProfitReport_UsesCurrentPurchasePriceLimitation_WhenSaleItemHasNoHistoricalCost()
    {
        await using var fixture = await ReportingFixture.CreateAsync();

        var profit = await fixture.Service.GetProfitReportAsync(new GetProfitReportQuery(fixture.TodayRange));

        Assert.Equal(210, profit.Revenue);
        Assert.Equal(90, profit.CostOfGoodsSold);
        Assert.Equal(120, profit.GrossProfit);
        Assert.False(profit.UsesHistoricalCost);
        Assert.Contains("not historical purchase cost", profit.AccuracyNote);
    }

    [Fact]
    public async Task InventoryAndLowStockReports_ReturnCurrentValuations()
    {
        await using var fixture = await ReportingFixture.CreateAsync();

        var inventory = await fixture.Service.GetInventoryReportAsync(new GetInventoryReportQuery(PageSize: 10));
        var lowStock = await fixture.Service.GetLowStockAsync(new GetLowStockQuery(PageSize: 10));
        var outOfStock = await fixture.Service.GetLowStockAsync(new GetLowStockQuery(OutOfStockOnly: true, PageSize: 10));

        Assert.Contains(inventory.Items, row => row.Product == "Clean Architecture" && row.InventoryCostValue == 50);
        Assert.Contains(lowStock.Items, row => row.Product == "Domain-Driven Design");
        Assert.Contains(outOfStock.Items, row => row.Product == "Domain-Driven Design");
    }

    [Fact]
    public async Task Dashboard_LoadsDynamicData_WithSqliteValueObjectMappings()
    {
        await using var fixture = await ReportingFixture.CreateAsync();

        var dashboard = await fixture.Service.GetDashboardAsync(new GetReportsDashboardQuery(fixture.TodayRange));

        Assert.Equal(214, dashboard.TodaysSales);
        Assert.Equal(2, dashboard.TodaysTransactions);
        Assert.Equal(120, dashboard.TodaysProfit);
        Assert.Equal("Clean Architecture", dashboard.BestSellingProduct);
        Assert.Equal("Walk-in", dashboard.TopCustomer);
        Assert.Equal("Cashier One", dashboard.TopCashier);
        Assert.Equal(1, dashboard.LowStockProducts);
        Assert.Equal(1, dashboard.OutOfStockProducts);
    }

    [Fact]
    public async Task CashierCustomersPaymentAndHourlyReports_AggregateSeparately()
    {
        await using var fixture = await ReportingFixture.CreateAsync();

        var cashiers = await fixture.Service.GetCashierPerformanceAsync(new GetCashierPerformanceQuery(fixture.TodayRange));
        var customers = await fixture.Service.GetCustomerReportAsync(new GetCustomerReportQuery(fixture.TodayRange));
        var payments = await fixture.Service.GetPaymentMethodsAsync(new GetPaymentMethodsQuery(fixture.TodayRange));
        var hourly = await fixture.Service.GetHourlySalesAsync(new GetHourlySalesQuery(fixture.TodayRange));

        Assert.Contains(cashiers, row => row.Cashier == "Cashier One" && row.TotalSales == 214);
        Assert.Contains(customers, row => row.Customer == "Ahmed Customer" && row.TotalPurchases == 104);
        Assert.Contains(payments, row => row.PaymentMethod == PaymentMethod.Cash && row.TotalAmount == 104);
        Assert.Contains(hourly, row => row.NumberOfSales == 2 && row.Revenue == 214);
    }

    private sealed class ReportingFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private ReportingFixture(SqliteConnection connection, BookStoreDbContext dbContext)
        {
            _connection = connection;
            DbContext = dbContext;
            Service = new BookStoreReportQueryService(dbContext);
            var today = DateTimeOffset.UtcNow.Date;
            TodayRange = new ReportDateRange(new DateTimeOffset(today, TimeSpan.Zero), new DateTimeOffset(today.AddDays(1), TimeSpan.Zero));
        }

        public BookStoreDbContext DbContext { get; }
        public BookStoreReportQueryService Service { get; }
        public ReportDateRange TodayRange { get; }

        public static async Task<ReportingFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<BookStoreDbContext>().UseSqlite(connection).Options;
            var dbContext = new BookStoreDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();
            var fixture = new ReportingFixture(connection, dbContext);
            await fixture.SeedAsync();
            return fixture;
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private async Task SeedAsync()
        {
            var role = new Role("Cashier");
            var user = new User("cashier", "$2a$11$4cIiq8n2V7b7WqtddnMP7uWuBphrvLePAUQV1NQFBnnbaYbWYpJ26", "Cashier One", role.Id);
            var customer = new Customer("Ahmed Customer");
            var category = new Category("Programming");
            var cleanArchitecture = new Product(new BookStore.Domain.ValueObjects.Barcode("BK100"), "Clean Architecture", 25, 50, category.Id);
            cleanArchitecture.SetQuantity(2);
            cleanArchitecture.UpdateInventoryMetadata(5, null, null);
            var ddd = new Product(new BookStore.Domain.ValueObjects.Barcode("BK200"), "Domain-Driven Design", 40, 80, category.Id);
            ddd.SetQuantity(0);
            ddd.UpdateInventoryMetadata(2, null, null);

            DbContext.AddRange(role, user, customer, category, cleanArchitecture, ddd);
            await DbContext.SaveChangesAsync();

            var today = DateTimeOffset.UtcNow.Date.AddHours(10);
            var sale1 = CreateCompletedSale("INV-001", user.Id, customer.Id, PaymentMethod.Cash, today, cleanArchitecture.Id, 2, 50, discount: 10, tax: 14);
            var sale2 = CreateCompletedSale("INV-002", user.Id, null, PaymentMethod.Card, today.AddMinutes(20), ddd.Id, 1, 110, discount: 0, tax: 0);
            var yesterday = CreateCompletedSale("INV-003", user.Id, null, PaymentMethod.MobileWallet, today.AddDays(-1), cleanArchitecture.Id, 1, 500, discount: 0, tax: 0);
            var cancelled = new Sale("INV-004", user.Id, PaymentMethod.Cash);
            cancelled.AddItem(new SaleItem(cleanArchitecture.Id, 1, 999));
            SetSaleDate(cancelled, today);
            cancelled.Cancel();

            DbContext.Sales.AddRange(sale1, sale2, yesterday, cancelled);
            DbContext.InventoryTransactions.Add(new InventoryTransaction(cleanArchitecture.Id, 2, InventoryTransactionType.ManualAdjustment, 0, 2, "Seed", "TEST", user.Id, user.FullName));
            await DbContext.SaveChangesAsync();
        }

        private static Sale CreateCompletedSale(string invoice, Guid userId, Guid? customerId, PaymentMethod paymentMethod, DateTimeOffset date, Guid productId, int quantity, decimal unitPrice, decimal discount, decimal tax)
        {
            var sale = new Sale(invoice, userId, paymentMethod);
            sale.AssignCustomer(customerId);
            sale.AddItem(new SaleItem(productId, quantity, unitPrice));
            sale.UpdateCharges(discount, tax);
            sale.Complete(sale.Total);
            SetSaleDate(sale, date);
            return sale;
        }

        private static void SetSaleDate(Sale sale, DateTimeOffset date)
        {
            typeof(Sale).GetProperty(nameof(Sale.SaleDate))!.SetValue(sale, date);
        }
    }
}
