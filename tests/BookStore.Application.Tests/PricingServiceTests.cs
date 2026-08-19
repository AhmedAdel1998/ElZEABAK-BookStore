using BookStore.Application.Features.Sales.DTOs;
using BookStore.Application.Features.Sales.Handlers;
using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Settings.Services;
using BookStore.Shared.Results;
using Xunit;

namespace BookStore.Application.Tests;

/// <summary>
/// Covers the POS money maths: the tax convention, where the invoice discount lands relative to
/// tax, and money rounding.
/// </summary>
public class PricingServiceTests
{
    private static PricingService Pricing(bool taxEnabled = true, decimal rate = 0.14m, bool taxIncluded = false) =>
        new(new StubSettingsService(new TaxSettingsDto { Enabled = taxEnabled, DefaultRate = rate, TaxIncludedInPrice = taxIncluded }));

    private static SaleSessionDto Cart(params (decimal UnitPrice, int Quantity, decimal Discount)[] lines)
    {
        var sale = new SaleSessionDto();
        foreach (var (unitPrice, quantity, discount) in lines)
        {
            sale.Items.Add(new SaleCartItemDto { UnitPrice = unitPrice, Quantity = quantity, Discount = discount });
        }

        return sale;
    }

    [Fact]
    public async Task TaxExclusivePricingAddsTaxOnTop()
    {
        var sale = Cart((100m, 1, 0m));

        var summary = await Pricing().RecalculateAsync(sale);

        Assert.Equal(100m, summary.Subtotal);
        Assert.Equal(14m, summary.Tax);
        Assert.Equal(114m, summary.GrandTotal);
        Assert.False(summary.TaxIncludedInPrice);
    }

    [Fact]
    public async Task TaxInclusivePricingExtractsTaxInsteadOfAddingIt()
    {
        var sale = Cart((114m, 1, 0m));

        var summary = await Pricing(taxIncluded: true).RecalculateAsync(sale);

        // The shelf price already contains the tax, so the customer pays exactly the shelf price
        // and the tax is reported as the portion contained within it.
        Assert.Equal(114m, summary.GrandTotal);
        Assert.Equal(14m, summary.Tax);
        Assert.True(summary.TaxIncludedInPrice);
    }

    [Fact]
    public async Task TaxInclusivePricingDoesNotOverchargeTheShelfPrice()
    {
        var sale = Cart((100m, 1, 0m));

        var summary = await Pricing(taxIncluded: true).RecalculateAsync(sale);

        // Before the fix this produced 114.00 -- a 14% overcharge on every tax-inclusive sale.
        Assert.Equal(100m, summary.GrandTotal);
    }

    [Fact]
    public async Task InvoiceDiscountAndLineDiscountProduceTheSameTotal()
    {
        var withInvoiceDiscount = Cart((1000m, 1, 0m));
        withInvoiceDiscount.InvoiceDiscount = 200m;
        var invoiceSummary = await Pricing().RecalculateAsync(withInvoiceDiscount);

        var withLineDiscount = Cart((1000m, 1, 200m));
        var lineSummary = await Pricing().RecalculateAsync(withLineDiscount);

        // The same 200 off the same sale must cost the customer the same amount and report the same
        // tax. Applying the invoice discount after tax used to give 940.00/140.00 here against
        // 912.00/112.00 for the line discount.
        Assert.Equal(912m, invoiceSummary.GrandTotal);
        Assert.Equal(112m, invoiceSummary.Tax);
        Assert.Equal(lineSummary.GrandTotal, invoiceSummary.GrandTotal);
        Assert.Equal(lineSummary.Tax, invoiceSummary.Tax);
    }

    [Fact]
    public async Task InvoiceDiscountIsSharedAcrossLinesAndSumsExactly()
    {
        var sale = Cart((300m, 1, 0m), (700m, 1, 0m));
        sale.InvoiceDiscount = 100m;

        var summary = await Pricing().RecalculateAsync(sale);

        // 100 shared 30/70, then taxed: (270 + 630) * 1.14
        Assert.Equal(900m * 1.14m, summary.GrandTotal);
        Assert.Equal(100m, summary.InvoiceDiscount);
        Assert.Equal(summary.Tax, sale.Items.Sum(item => item.Tax));
    }

    [Fact]
    public async Task InvoiceDiscountResidueLandsOnTheLastLineSoSharesReconcile()
    {
        // Three equal lines and a discount that does not divide evenly by three.
        var sale = Cart((10m, 1, 0m), (10m, 1, 0m), (10m, 1, 0m));
        sale.InvoiceDiscount = 10m;

        var summary = await Pricing(taxEnabled: false).RecalculateAsync(sale);

        Assert.Equal(20m, summary.GrandTotal);
        Assert.Equal(20m, sale.Items.Sum(item => item.LineTotal));
    }

    [Fact]
    public async Task InvoiceDiscountIsClampedToTheNetOfLineDiscounts()
    {
        var sale = Cart((100m, 1, 40m));
        sale.InvoiceDiscount = 500m;

        var summary = await Pricing(taxEnabled: false).RecalculateAsync(sale);

        Assert.Equal(60m, summary.InvoiceDiscount);
        Assert.Equal(0m, summary.GrandTotal);
    }

    [Fact]
    public async Task DisabledTaxChargesNothingExtra()
    {
        var sale = Cart((250m, 2, 0m));

        var summary = await Pricing(taxEnabled: false).RecalculateAsync(sale);

        Assert.Equal(0m, summary.Tax);
        Assert.Equal(500m, summary.GrandTotal);
    }

    [Fact]
    public async Task MoneyRoundsHalfAwayFromZeroNotToEven()
    {
        // 0.125 * 1 with tax off rounds to 0.13 under half-up; banker's rounding gives 0.12.
        var sale = Cart((0.125m, 1, 0m));

        var summary = await Pricing(taxEnabled: false).RecalculateAsync(sale);

        Assert.Equal(0.13m, summary.GrandTotal);
    }

    [Fact]
    public async Task ChangeIsNeverNegativeAndPaymentIsReported()
    {
        var sale = Cart((100m, 1, 0m));
        sale.AmountPaid = 50m;

        var summary = await Pricing(taxEnabled: false).RecalculateAsync(sale);

        Assert.Equal(50m, summary.AmountPaid);
        Assert.Equal(0m, summary.Change);

        sale.AmountPaid = 130m;
        summary = await Pricing(taxEnabled: false).RecalculateAsync(sale);
        Assert.Equal(30m, summary.Change);
    }

    [Fact]
    public async Task LineDiscountIsClampedToTheLineGross()
    {
        var sale = Cart((50m, 2, 500m));

        var summary = await Pricing(taxEnabled: false).RecalculateAsync(sale);

        Assert.Equal(100m, summary.LineDiscount);
        Assert.Equal(0m, summary.GrandTotal);
    }

    [Fact]
    public async Task EmptyCartTotalsZero()
    {
        var summary = await Pricing().RecalculateAsync(new SaleSessionDto());

        Assert.Equal(0m, summary.Subtotal);
        Assert.Equal(0m, summary.Tax);
        Assert.Equal(0m, summary.GrandTotal);
    }

    private sealed class StubSettingsService(TaxSettingsDto tax) : ISettingsService
    {
        public Task<T> GetAsync<T>(CancellationToken cancellationToken = default)
            where T : class, new()
        {
            object value = typeof(T) == typeof(TaxSettingsDto) ? tax : new T();
            return Task.FromResult((T)value);
        }

        public Task<Result> SetAsync<T>(T settings, CancellationToken cancellationToken = default)
            where T : class, new() => Task.FromResult(Result.Success());

        public Task<IReadOnlyList<SettingEntryDto>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SettingEntryDto>>([]);
        public Task<Result> ResetAsync(CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());
        public Task<Result> ResetCategoryAsync(SettingsCategory category, CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
