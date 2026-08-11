using System.Drawing.Printing;
using System.Runtime.Versioning;
using BookStore.Application.Features.Receipts.DTOs;
using BookStore.Application.Features.Receipts.Services;

namespace BookStore.Infrastructure.Printing.Services;

[SupportedOSPlatform("windows6.1")]
public sealed class PrinterDiscoveryService : IPrinterDiscoveryService
{
    public Task<IReadOnlyCollection<PrinterInfoDto>> GetInstalledPrintersAsync(CancellationToken cancellationToken = default)
    {
        var defaultPrinter = GetDefaultPrinterName();
        var printers = PrinterSettings.InstalledPrinters
            .Cast<string>()
            .Select(name => new PrinterInfoDto
            {
                Name = name,
                IsDefault = string.Equals(name, defaultPrinter, StringComparison.OrdinalIgnoreCase),
                IsAvailable = true
            })
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<PrinterInfoDto>>(printers);
    }

    public async Task<PrinterInfoDto?> GetDefaultPrinterAsync(CancellationToken cancellationToken = default)
    {
        return (await GetInstalledPrintersAsync(cancellationToken)).FirstOrDefault(printer => printer.IsDefault);
    }

    public async Task<bool> IsPrinterAvailableAsync(string? printerName, CancellationToken cancellationToken = default)
    {
        var printers = await GetInstalledPrintersAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(printerName))
        {
            return printers.Any(printer => printer.IsDefault);
        }

        return printers.Any(printer => string.Equals(printer.Name, printerName, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetDefaultPrinterName()
    {
        using var document = new PrintDocument();
        return document.PrinterSettings.IsDefaultPrinter ? document.PrinterSettings.PrinterName : null;
    }
}
