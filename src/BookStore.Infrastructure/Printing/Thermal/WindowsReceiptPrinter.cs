using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.Versioning;
using BookStore.Application.Features.Receipts.DTOs;
using BookStore.Application.Features.Receipts.Services;
using Microsoft.Extensions.Logging;

namespace BookStore.Infrastructure.Printing.Thermal;

[SupportedOSPlatform("windows6.1")]
public sealed class WindowsReceiptPrinter : IReceiptPrinter
{
    private readonly IPrinterDiscoveryService _printerDiscoveryService;
    private readonly IReceiptFormatter _receiptFormatter;
    private readonly ICashDrawerService _cashDrawerService;
    private readonly ILogger<WindowsReceiptPrinter> _logger;

    public WindowsReceiptPrinter(IPrinterDiscoveryService printerDiscoveryService, IReceiptFormatter receiptFormatter, ICashDrawerService cashDrawerService, ILogger<WindowsReceiptPrinter> logger)
    {
        _printerDiscoveryService = printerDiscoveryService;
        _receiptFormatter = receiptFormatter;
        _cashDrawerService = cashDrawerService;
        _logger = logger;
    }

    public async Task<ReceiptPrintResult> PrintAsync(ReceiptPrintJob job, CancellationToken cancellationToken = default)
    {
        var printerName = await ResolvePrinterNameAsync(job.Options.PrinterName, cancellationToken);
        if (string.IsNullOrWhiteSpace(printerName) || !await IsPrinterAvailableAsync(printerName, cancellationToken))
        {
            _logger.LogWarning("Printer unavailable. Printer={PrinterName} RequestId={PrintRequestId}", printerName, job.RequestId);
            // Describes the printing fault only. Callers compose the surrounding sentence -- the POS
            // notification already reads "Sale completed, but receipt printing failed. {0}", so
            // repeating "Sale completed, but" here printed the same clause twice.
            return ReceiptPrintResult.Failure(job.RequestId, "The printer is unavailable.", printerName, job.Receipt);
        }

        var content = _receiptFormatter.Format(job.Receipt, job.Options);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(job.Options.Timeout);

        try
        {
            for (var copy = 0; copy < job.Options.Copies; copy++)
            {
                await Task.Run(() => PrintText(printerName, content), timeout.Token);
            }

            if (job.Options.OpenCashDrawer)
            {
                try
                {
                    await _cashDrawerService.OpenDrawerAsync(printerName, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cash drawer failure. Printer={PrinterName} RequestId={PrintRequestId}", printerName, job.RequestId);
                }
            }

            return ReceiptPrintResult.Success(job.RequestId, printerName, job.Receipt);
        }
        catch (OperationCanceledException)
        {
            return ReceiptPrintResult.Failure(job.RequestId, "Receipt printing timed out.", printerName, job.Receipt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to print receipt. Printer={PrinterName} RequestId={PrintRequestId}", printerName, job.RequestId);
            return ReceiptPrintResult.Failure(job.RequestId, "Unable to print receipt.", printerName, job.Receipt);
        }
    }

    public async Task<ReceiptPrintResult> TestPrintAsync(string? printerName = null, CancellationToken cancellationToken = default)
    {
        var resolvedPrinter = await ResolvePrinterNameAsync(printerName, cancellationToken);
        var requestId = Guid.NewGuid();
        if (string.IsNullOrWhiteSpace(resolvedPrinter) || !await IsPrinterAvailableAsync(resolvedPrinter, cancellationToken))
        {
            return ReceiptPrintResult.Failure(requestId, "The printer is unavailable.", resolvedPrinter);
        }

        var content = _receiptFormatter.FormatTestPrint(resolvedPrinter, DateTimeOffset.UtcNow, ReceiptPaperWidth.Mm80);
        try
        {
            await Task.Run(() => PrintText(resolvedPrinter, content), cancellationToken);
            _logger.LogInformation("Printer test completed. Printer={PrinterName} RequestId={PrintRequestId}", resolvedPrinter, requestId);
            return ReceiptPrintResult.Success(requestId, resolvedPrinter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Printer test failed. Printer={PrinterName} RequestId={PrintRequestId}", resolvedPrinter, requestId);
            return ReceiptPrintResult.Failure(requestId, "Printer test failed.", resolvedPrinter);
        }
    }

    public Task<bool> IsPrinterAvailableAsync(string? printerName = null, CancellationToken cancellationToken = default)
    {
        return _printerDiscoveryService.IsPrinterAvailableAsync(printerName, cancellationToken);
    }

    public Task<IReadOnlyCollection<PrinterInfoDto>> GetAvailablePrintersAsync(CancellationToken cancellationToken = default)
    {
        return _printerDiscoveryService.GetInstalledPrintersAsync(cancellationToken);
    }

    private async Task<string?> ResolvePrinterNameAsync(string? printerName, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(printerName))
        {
            return printerName;
        }

        return (await _printerDiscoveryService.GetDefaultPrinterAsync(cancellationToken))?.Name;
    }

    private static void PrintText(string printerName, string content)
    {
        using var document = new PrintDocument();
        document.PrinterSettings.PrinterName = printerName;
        document.DocumentName = "BookStore Receipt";
        document.PrintPage += (_, args) =>
        {
            using var font = new Font("Consolas", 9);
            args.Graphics?.DrawString(content, font, Brushes.Black, new RectangleF(0, 0, args.MarginBounds.Width, args.MarginBounds.Height));
        };
        document.Print();
    }
}
