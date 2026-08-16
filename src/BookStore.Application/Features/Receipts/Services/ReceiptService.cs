using BookStore.Application.Features.Receipts.Commands;
using BookStore.Application.Features.Receipts.DTOs;
using BookStore.Application.Features.Receipts.Queries;
using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Settings.Services;
using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Receipts.Services;

public sealed class ReceiptService : IReceiptService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReceiptPrinter _receiptPrinter;
    private readonly IReceiptFormatter _receiptFormatter;
    private readonly IReceiptCodeService _receiptCodeService;
    private readonly IPrintQueueService _printQueueService;
    private readonly IValidator<ReceiptModel> _receiptValidator;
    private readonly ISettingsService _settingsService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ReceiptService> _logger;

    public ReceiptService(
        IUnitOfWork unitOfWork,
        IReceiptPrinter receiptPrinter,
        IReceiptFormatter receiptFormatter,
        IReceiptCodeService receiptCodeService,
        IPrintQueueService printQueueService,
        IValidator<ReceiptModel> receiptValidator,
        ISettingsService settingsService,
        ICurrentUserService currentUserService,
        ILogger<ReceiptService> logger)
    {
        _unitOfWork = unitOfWork;
        _receiptPrinter = receiptPrinter;
        _receiptFormatter = receiptFormatter;
        _receiptCodeService = receiptCodeService;
        _printQueueService = printQueueService;
        _receiptValidator = receiptValidator;
        _settingsService = settingsService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<ReceiptModel>> BuildReceiptAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        var sale = await _unitOfWork.Sales.GetCompletedWithDetailsAsync(saleId, cancellationToken);
        return sale is null
            ? Result<ReceiptModel>.Failure("Completed sale was not found.")
            : await BuildReceiptResultAsync(sale, isReprint: false, cancellationToken);
    }

    public async Task<Result<ReceiptModel>> BuildReceiptByInvoiceAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        var sale = await _unitOfWork.Sales.GetCompletedByInvoiceAsync(invoiceNumber.Trim(), cancellationToken);
        return sale is null
            ? Result<ReceiptModel>.Failure("Completed invoice was not found.")
            : await BuildReceiptResultAsync(sale, isReprint: true, cancellationToken);
    }

    public async Task<ReceiptPrintResult> PrintCompletedSaleAsync(Guid saleId, PrintRequestKind kind, string? printerName = null, int copies = 1, CancellationToken cancellationToken = default)
    {
        var receiptResult = await BuildReceiptAsync(saleId, cancellationToken);
        if (!receiptResult.IsSuccess || receiptResult.Value is null)
        {
            return ReceiptPrintResult.Failure(Guid.NewGuid(), receiptResult.Error ?? "Receipt could not be built.", printerName);
        }

        return await PrintReceiptAsync(receiptResult.Value, kind, printerName, copies, cancellationToken);
    }

    public async Task<ReceiptPrintResult> ReprintAsync(ReprintReceiptCommand command, CancellationToken cancellationToken = default)
    {
        var receiptResult = await BuildReceiptByInvoiceAsync(command.InvoiceNumber, cancellationToken);
        if (!receiptResult.IsSuccess || receiptResult.Value is null)
        {
            return ReceiptPrintResult.Failure(Guid.NewGuid(), receiptResult.Error ?? "Receipt could not be built.", command.PrinterName);
        }

        receiptResult.Value.IsReprint = true;
        return await PrintReceiptAsync(receiptResult.Value, PrintRequestKind.Reprint, command.PrinterName, command.Copies, cancellationToken);
    }

    public Task<ReceiptPrintResult> TestPrintAsync(TestPrintCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Test print requested. Printer={PrinterName} User={User}", command.PrinterName, _currentUserService.Username);
        return _receiptPrinter.TestPrintAsync(command.PrinterName, cancellationToken);
    }

    public async Task<Result<ReceiptPreviewDto>> PreviewAsync(GetReceiptPreviewQuery query, CancellationToken cancellationToken = default)
    {
        var receiptResult = await BuildReceiptByInvoiceAsync(query.InvoiceNumber, cancellationToken);
        if (!receiptResult.IsSuccess || receiptResult.Value is null)
        {
            return Result<ReceiptPreviewDto>.Failure(receiptResult.Error ?? "Receipt could not be built.");
        }

        var options = await BuildOptionsAsync(null, 1, cancellationToken);
        return Result<ReceiptPreviewDto>.Success(new ReceiptPreviewDto
        {
            Receipt = receiptResult.Value,
            Content = _receiptFormatter.Format(receiptResult.Value, options)
        });
    }

    private async Task<Result<ReceiptModel>> BuildReceiptResultAsync(Sale sale, bool isReprint, CancellationToken cancellationToken)
    {
        var receipt = await BuildReceiptAsync(sale, isReprint, cancellationToken);
        var validation = await _receiptValidator.ValidateAsync(receipt, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<ReceiptModel>.Failure(validation.Errors[0].ErrorMessage);
        }

        return Result<ReceiptModel>.Success(receipt);
    }

    private async Task<ReceiptModel> BuildReceiptAsync(Sale sale, bool isReprint, CancellationToken cancellationToken)
    {
        var storeSettings = await _settingsService.GetAsync<StoreSettingsDto>(cancellationToken);
        var receiptSettings = await _settingsService.GetAsync<ReceiptSettingsDto>(cancellationToken);
        var subtotal = sale.SaleItems.Sum(item => item.UnitPrice * item.Quantity);
        var lineDiscount = sale.SaleItems.Sum(item => item.Discount);
        var receipt = new ReceiptModel
        {
            SaleId = sale.Id,
            IsReprint = isReprint,
            StoreName = storeSettings.StoreName,
            StoreAddress = storeSettings.Address,
            StorePhone = storeSettings.Phone,
            TaxNumber = storeSettings.TaxNumber,
            LogoPath = storeSettings.LogoPath,
            InvoiceNumber = sale.InvoiceNumber,
            SaleDate = sale.SaleDate,
            Cashier = sale.Cashier?.FullName ?? _currentUserService.FullName ?? _currentUserService.Username ?? "Cashier",
            RegisterName = Environment.MachineName,
            SaleStatus = sale.Status.ToString(),
            CustomerName = sale.Customer?.FullName ?? "Walk-in Customer",
            CustomerPhone = sale.Customer?.Phone?.Value,
            Subtotal = subtotal,
            Discount = lineDiscount + sale.Discount,
            Tax = sale.Tax,
            GrandTotal = sale.Total,
            AmountPaid = sale.PaidAmount,
            Change = sale.ChangeAmount,
            PaymentMethod = sale.PaymentMethod,
            Footer = receiptSettings.FooterText,
            ThankYouMessage = "Thank you for shopping with us."
        };

        receipt.Items = sale.SaleItems.Select(item => new ReceiptItemDto
        {
            Barcode = item.Product?.Barcode.Value ?? string.Empty,
            ProductName = item.Product?.Title ?? item.ProductId.ToString(),
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            Discount = item.Discount,
            Tax = 0,
            LineTotal = item.Total
        }).ToList();
        receipt.ReceiptBarcode = _receiptCodeService.BuildBarcode(receipt);
        receipt.ReceiptQrPayload = receiptSettings.PrintQRCode ? _receiptCodeService.BuildQrPayload(receipt) : null;
        return receipt;
    }

    private async Task<ReceiptPrintResult> PrintReceiptAsync(ReceiptModel receipt, PrintRequestKind kind, string? printerName, int copies, CancellationToken cancellationToken)
    {
        var options = await BuildOptionsAsync(printerName, copies, cancellationToken);
        var job = new ReceiptPrintJob
        {
            RequestId = receipt.PrintRequestId,
            Receipt = receipt,
            Options = options,
            Kind = kind
        };

        if (!_printQueueService.TryStart(job.RequestId))
        {
            _logger.LogWarning("Duplicate receipt print prevented. Invoice={InvoiceNumber} RequestId={PrintRequestId}", receipt.InvoiceNumber, job.RequestId);
            return ReceiptPrintResult.Failure(job.RequestId, "This receipt print request is already in progress or completed.", options.PrinterName, receipt);
        }

        try
        {
            _logger.LogInformation("Print started. Invoice={InvoiceNumber} Printer={PrinterName} RequestId={PrintRequestId} User={User}", receipt.InvoiceNumber, options.PrinterName, job.RequestId, _currentUserService.Username);
            var result = await _receiptPrinter.PrintAsync(job, cancellationToken);
            if (result.Succeeded)
            {
                _printQueueService.MarkCompleted(job.RequestId);
                _logger.LogInformation("Print completed. Invoice={InvoiceNumber} Printer={PrinterName} RequestId={PrintRequestId}", receipt.InvoiceNumber, result.PrinterName, job.RequestId);
            }
            else
            {
                _printQueueService.EnqueueForRetry(job);
                _logger.LogWarning("Print failed. Invoice={InvoiceNumber} Printer={PrinterName} RequestId={PrintRequestId} Error={Error}", receipt.InvoiceNumber, result.PrinterName, job.RequestId, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _printQueueService.EnqueueForRetry(job);
            _logger.LogError(ex, "Print failed. Invoice={InvoiceNumber} Printer={PrinterName} RequestId={PrintRequestId}", receipt.InvoiceNumber, options.PrinterName, job.RequestId);
            return ReceiptPrintResult.Failure(job.RequestId, "Sale completed, but receipt printing failed.", options.PrinterName, receipt);
        }
    }

    private async Task<ReceiptPrintOptions> BuildOptionsAsync(string? printerName, int copies, CancellationToken cancellationToken)
    {
        var printerSettings = await _settingsService.GetAsync<PrinterSettingsDto>(cancellationToken);
        var receiptSettings = await _settingsService.GetAsync<ReceiptSettingsDto>(cancellationToken);
        return new ReceiptPrintOptions
        {
            PrinterName = string.IsNullOrWhiteSpace(printerName) ? printerSettings.PrinterName : printerName,
            PaperWidth = receiptSettings.PaperWidth == 58 ? ReceiptPaperWidth.Mm58 : ReceiptPaperWidth.Mm80,
            Copies = Math.Clamp(copies <= 0 ? receiptSettings.Copies : copies, 1, 5),
            CutPaper = receiptSettings.CutPaper,
            OpenCashDrawer = receiptSettings.OpenCashDrawer,
            PrintLogo = receiptSettings.ShowLogo,
            AutoPrint = true,
            Timeout = TimeSpan.FromMilliseconds(Math.Max(1000, printerSettings.PrintTimeout)),
            Template = new ReceiptTemplateOptions
            {
                PrintStoreInformation = true,
                PrintInvoiceInformation = true,
                PrintCustomerInformation = receiptSettings.ShowCustomer,
                PrintFooter = true,
                PrintQrCode = receiptSettings.PrintQRCode,
                PrintBarcode = receiptSettings.ShowBarcode
            }
        };
    }
}
