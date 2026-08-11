using System.Globalization;
using System.Text;
using BookStore.Application.Features.Receipts.DTOs;
using BookStore.Application.Features.Receipts.Services;

namespace BookStore.Infrastructure.Printing.Services;

public sealed class ReceiptFormatter : IReceiptFormatter
{
    public string Format(ReceiptModel receipt, ReceiptPrintOptions options)
    {
        var width = GetWidth(options.PaperWidth);
        var builder = new StringBuilder();
        AppendCenter(builder, receipt.StoreName, width);
        AppendIfPresent(builder, receipt.StoreAddress, width);
        AppendIfPresent(builder, receipt.StorePhone, width, "Tel: ");
        AppendIfPresent(builder, receipt.TaxNumber, width, "Tax: ");
        AppendLine(builder, width);
        builder.AppendLine($"Invoice: {receipt.InvoiceNumber}");
        builder.AppendLine($"Date: {receipt.SaleDate.LocalDateTime:yyyy-MM-dd HH:mm}");
        builder.AppendLine($"Cashier: {receipt.Cashier}");
        builder.AppendLine($"Register: {receipt.RegisterName}");
        if (receipt.IsReprint)
        {
            builder.AppendLine("REPRINT");
        }

        if (options.Template.PrintCustomerInformation && !string.IsNullOrWhiteSpace(receipt.CustomerName))
        {
            builder.AppendLine($"Customer: {receipt.CustomerName}");
            AppendIfPresent(builder, receipt.CustomerPhone, width, "Phone: ");
        }

        AppendLine(builder, width);
        builder.AppendLine($"{Fit("Product", width - 16)} {"Qty",3} {"Total",10}");
        AppendLine(builder, width);
        foreach (var item in receipt.Items)
        {
            AppendItem(builder, item, width);
        }

        AppendLine(builder, width);
        AppendMoney(builder, "Subtotal", receipt.Subtotal, width);
        AppendMoney(builder, "Discount", receipt.Discount, width);
        AppendMoney(builder, "Tax", receipt.Tax, width);
        AppendLine(builder, width);
        AppendMoney(builder, "TOTAL", receipt.GrandTotal, width);
        AppendMoney(builder, "Paid", receipt.AmountPaid, width);
        AppendMoney(builder, "Change", receipt.Change, width);
        builder.AppendLine($"Payment: {receipt.PaymentMethod}");
        AppendIfPresent(builder, receipt.ReceiptBarcode, width, "Barcode: ");
        AppendIfPresent(builder, receipt.ReceiptQrPayload, width, "QR: ");
        AppendLine(builder, width);
        AppendIfPresent(builder, receipt.Footer, width);
        AppendCenter(builder, receipt.ThankYouMessage, width);
        return builder.ToString();
    }

    public string FormatTestPrint(string printerName, DateTimeOffset timestamp, ReceiptPaperWidth paperWidth)
    {
        var width = GetWidth(paperWidth);
        var builder = new StringBuilder();
        AppendCenter(builder, "BOOKSTORE", width);
        AppendLine(builder, width);
        AppendCenter(builder, "TEST PRINT", width);
        AppendLine(builder, width);
        builder.AppendLine("Printer:");
        builder.AppendLine(string.IsNullOrWhiteSpace(printerName) ? "Default printer" : printerName);
        builder.AppendLine();
        builder.AppendLine("Date:");
        builder.AppendLine(timestamp.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        builder.AppendLine();
        builder.AppendLine("Status:");
        builder.AppendLine("Printer working correctly");
        AppendLine(builder, width);
        return builder.ToString();
    }

    private static int GetWidth(ReceiptPaperWidth paperWidth) => paperWidth == ReceiptPaperWidth.Mm58 ? 32 : 48;

    private static void AppendItem(StringBuilder builder, ReceiptItemDto item, int width)
    {
        var nameWidth = width - 16;
        var lines = Wrap(item.ProductName, nameWidth).ToArray();
        for (var index = 0; index < lines.Length; index++)
        {
            if (index == 0)
            {
                builder.AppendLine($"{Fit(lines[index], nameWidth)} {item.Quantity,3} {item.LineTotal,10:N2}");
            }
            else
            {
                builder.AppendLine(Fit(lines[index], nameWidth));
            }
        }

        if (item.Discount > 0)
        {
            builder.AppendLine($"  Discount: {item.Discount:N2}");
        }
    }

    private static void AppendMoney(StringBuilder builder, string label, decimal value, int width)
    {
        var amount = value.ToString("N2", CultureInfo.InvariantCulture);
        builder.AppendLine($"{Fit(label, width - amount.Length)}{amount}");
    }

    private static void AppendCenter(StringBuilder builder, string value, int width)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        foreach (var line in Wrap(value.Trim(), width))
        {
            var padding = Math.Max(0, (width - line.Length) / 2);
            builder.AppendLine(new string(' ', padding) + line);
        }
    }

    private static void AppendIfPresent(StringBuilder builder, string? value, int width, string prefix = "")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        foreach (var line in Wrap(prefix + value.Trim(), width))
        {
            builder.AppendLine(line);
        }
    }

    private static void AppendLine(StringBuilder builder, int width) => builder.AppendLine(new string('-', width));

    private static string Fit(string value, int width) => value.Length <= width ? value.PadRight(width) : value[..width];

    private static IEnumerable<string> Wrap(string value, int width)
    {
        var remaining = value.Trim();
        while (remaining.Length > width)
        {
            var split = remaining[..Math.Min(width, remaining.Length)].LastIndexOf(' ');
            if (split <= 0)
            {
                split = width;
            }

            yield return remaining[..split].Trim();
            remaining = remaining[split..].Trim();
        }

        if (remaining.Length > 0)
        {
            yield return remaining;
        }
    }
}
