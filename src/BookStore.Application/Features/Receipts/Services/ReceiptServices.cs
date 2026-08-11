using BookStore.Application.Features.Receipts.Commands;
using BookStore.Application.Features.Receipts.DTOs;
using BookStore.Application.Features.Receipts.Queries;
using BookStore.Shared.Results;

namespace BookStore.Application.Features.Receipts.Services;

public interface IReceiptService
{
    Task<Result<ReceiptModel>> BuildReceiptAsync(Guid saleId, CancellationToken cancellationToken = default);
    Task<Result<ReceiptModel>> BuildReceiptByInvoiceAsync(string invoiceNumber, CancellationToken cancellationToken = default);
    Task<ReceiptPrintResult> PrintCompletedSaleAsync(Guid saleId, PrintRequestKind kind, string? printerName = null, int copies = 1, CancellationToken cancellationToken = default);
    Task<ReceiptPrintResult> ReprintAsync(ReprintReceiptCommand command, CancellationToken cancellationToken = default);
    Task<ReceiptPrintResult> TestPrintAsync(TestPrintCommand command, CancellationToken cancellationToken = default);
    Task<Result<ReceiptPreviewDto>> PreviewAsync(GetReceiptPreviewQuery query, CancellationToken cancellationToken = default);
}

public interface IReceiptPrinter
{
    Task<ReceiptPrintResult> PrintAsync(ReceiptPrintJob job, CancellationToken cancellationToken = default);
    Task<ReceiptPrintResult> TestPrintAsync(string? printerName = null, CancellationToken cancellationToken = default);
    Task<bool> IsPrinterAvailableAsync(string? printerName = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PrinterInfoDto>> GetAvailablePrintersAsync(CancellationToken cancellationToken = default);
}

public interface IPrinterDiscoveryService
{
    Task<IReadOnlyCollection<PrinterInfoDto>> GetInstalledPrintersAsync(CancellationToken cancellationToken = default);
    Task<PrinterInfoDto?> GetDefaultPrinterAsync(CancellationToken cancellationToken = default);
    Task<bool> IsPrinterAvailableAsync(string? printerName, CancellationToken cancellationToken = default);
}

public interface IReceiptFormatter
{
    string Format(ReceiptModel receipt, ReceiptPrintOptions options);
    string FormatTestPrint(string printerName, DateTimeOffset timestamp, ReceiptPaperWidth paperWidth);
}

public interface IPrintQueueService
{
    bool TryStart(Guid printRequestId);
    void MarkCompleted(Guid printRequestId);
    void EnqueueForRetry(ReceiptPrintJob job);
    Task<ReceiptPrintResult?> RetryAsync(Guid printRequestId, IReceiptPrinter printer, CancellationToken cancellationToken = default);
}

public interface ICashDrawerService
{
    Task OpenDrawerAsync(string? printerName = null, CancellationToken cancellationToken = default);
}

public interface IReceiptCodeService
{
    string BuildBarcode(ReceiptModel receipt);
    string? BuildQrPayload(ReceiptModel receipt);
}
