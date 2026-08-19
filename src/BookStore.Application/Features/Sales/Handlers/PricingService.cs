using BookStore.Application.Features.Sales.DTOs;
using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Settings.Services;
using BookStore.Application.Interfaces;

namespace BookStore.Application.Features.Sales.Handlers;

/// <summary>
/// Calculates POS cart totals using the configured tax rate and tax convention.
/// </summary>
public sealed class PricingService : IPricingService
{
    private readonly ISettingsService _settingsService;

    /// <summary>Initializes a new instance of the <see cref="PricingService"/> class.</summary>
    public PricingService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Rounds a money amount to two places, half away from zero.
    /// </summary>
    /// <remarks>
    /// <see cref="decimal.Round(decimal, int)"/> defaults to banker's rounding, which sends an
    /// exact half-cent to the nearest even value. Retail pricing rounds half up, and the mixture of
    /// the two is what let a receipt's line totals disagree with its invoice total by a cent.
    /// </remarks>
    private static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <inheritdoc />
    public async Task<SaleSummaryDto> RecalculateAsync(SaleSessionDto sale, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sale);
        var taxSettings = await _settingsService.GetAsync<TaxSettingsDto>(cancellationToken);
        var taxRate = taxSettings.Enabled ? Math.Max(taxSettings.DefaultRate, 0m) : 0m;
        var taxIncludedInPrice = taxSettings.Enabled && taxSettings.TaxIncludedInPrice;

        // Pass one: settle each line's gross and its own discount, and total the net.
        var subtotal = 0m;
        var lineDiscountTotal = 0m;
        var netTotal = 0m;
        foreach (var item in sale.Items)
        {
            var gross = item.UnitPrice * item.Quantity;
            item.Discount = Round(Math.Clamp(item.Discount, 0m, gross));
            subtotal += gross;
            lineDiscountTotal += item.Discount;
            netTotal += gross - item.Discount;
        }

        // The invoice discount has to come off the taxable base, so it is shared across the lines
        // before any tax is worked out. Subtracting it from the grand total instead -- which is what
        // this used to do -- charged tax on money the customer never handed over, and made an
        // invoice discount produce a different total from the identical amount entered as a line
        // discount.
        var invoiceDiscount = Round(Math.Clamp(sale.InvoiceDiscount, 0m, Math.Max(netTotal, 0m)));
        sale.InvoiceDiscount = invoiceDiscount;

        var taxTotal = 0m;
        var allocatedDiscount = 0m;
        for (var index = 0; index < sale.Items.Count; index++)
        {
            var item = sale.Items[index];
            var net = (item.UnitPrice * item.Quantity) - item.Discount;

            // The final line absorbs the rounding residue, so the shares always add back up to the
            // invoice discount exactly.
            var share = index == sale.Items.Count - 1
                ? invoiceDiscount - allocatedDiscount
                : netTotal <= 0m ? 0m : Round(invoiceDiscount * (net / netTotal));
            allocatedDiscount += share;

            var taxable = Math.Max(net - share, 0m);
            var lineTax = taxRate <= 0m
                ? 0m
                : taxIncludedInPrice
                    // The shelf price already contains the tax, so extract it rather than adding it.
                    ? taxable - (taxable / (1m + taxRate))
                    : taxable * taxRate;

            item.Tax = Round(lineTax);
            item.LineTotal = Round(taxIncludedInPrice ? taxable : taxable + lineTax);
            taxTotal += item.Tax;
        }

        // Mirrors Sale.CalculateTotal so the figure shown at the till is the figure persisted with
        // the sale: net of line discounts, less the invoice discount, plus tax only when the
        // configured prices exclude it.
        var grandTotal = Math.Max(Round(netTotal - invoiceDiscount + (taxIncludedInPrice ? 0m : taxTotal)), 0m);

        sale.Summary = new SaleSummaryDto
        {
            Subtotal = Round(subtotal),
            LineDiscount = Round(lineDiscountTotal),
            InvoiceDiscount = invoiceDiscount,
            Tax = Round(taxTotal),
            GrandTotal = grandTotal,
            AmountPaid = Round(sale.AmountPaid),
            Change = Round(Math.Max(sale.AmountPaid - grandTotal, 0m)),
            TaxIncludedInPrice = taxIncludedInPrice
        };

        return sale.Summary;
    }
}
