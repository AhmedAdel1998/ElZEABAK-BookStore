using BookStore.Application.Features.Sales.DTOs;
using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Settings.Services;
using BookStore.Application.Interfaces;

namespace BookStore.Application.Features.Sales.Handlers;

/// <summary>
/// Calculates POS cart totals using the configured tax rate.
/// </summary>
public sealed class PricingService : IPricingService
{
    private readonly ISettingsService _settingsService;

    /// <summary>Initializes a new instance of the <see cref="PricingService"/> class.</summary>
    public PricingService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <inheritdoc />
    public SaleSummaryDto Recalculate(SaleSessionDto sale)
    {
        ArgumentNullException.ThrowIfNull(sale);
        var taxSettings = _settingsService.GetAsync<TaxSettingsDto>().GetAwaiter().GetResult();
        var taxRate = taxSettings.Enabled ? Math.Max(taxSettings.DefaultRate, 0m) : 0m;

        var subtotal = 0m;
        var lineDiscount = 0m;
        var tax = 0m;

        foreach (var item in sale.Items)
        {
            var gross = item.UnitPrice * item.Quantity;
            var discount = Math.Clamp(item.Discount, 0m, gross);
            var taxableAmount = Math.Max(gross - discount, 0m);
            var lineTax = taxableAmount * taxRate;

            item.Discount = decimal.Round(discount, 2);
            item.Tax = decimal.Round(lineTax, 2);
            item.LineTotal = decimal.Round(taxableAmount + lineTax, 2);

            subtotal += gross;
            lineDiscount += item.Discount;
            tax += item.Tax;
        }

        sale.InvoiceDiscount = decimal.Round(Math.Clamp(sale.InvoiceDiscount, 0m, Math.Max(subtotal - lineDiscount, 0m)), 2);
        var grandTotal = Math.Max(subtotal - lineDiscount - sale.InvoiceDiscount + tax, 0m);
        sale.Summary = new SaleSummaryDto
        {
            Subtotal = decimal.Round(subtotal, 2),
            LineDiscount = decimal.Round(lineDiscount, 2),
            InvoiceDiscount = sale.InvoiceDiscount,
            Tax = decimal.Round(tax, 2),
            GrandTotal = decimal.Round(grandTotal, 2),
            AmountPaid = decimal.Round(sale.AmountPaid, 2),
            Change = decimal.Round(Math.Max(sale.AmountPaid - grandTotal, 0m), 2)
        };

        return sale.Summary;
    }
}
