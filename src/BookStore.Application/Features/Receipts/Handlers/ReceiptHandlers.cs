using BookStore.Application.Features.Receipts.Commands;
using BookStore.Application.Features.Receipts.DTOs;
using BookStore.Application.Features.Receipts.Queries;
using BookStore.Application.Features.Receipts.Services;
using BookStore.Application.Interfaces;
using BookStore.Domain.Interfaces;
using BookStore.Shared.Constants;
using BookStore.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Receipts.Handlers;

public sealed class PrintReceiptHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IReceiptService _receiptService;
    private readonly IValidator<PrintReceiptCommand> _validator;
    private readonly ILogger<PrintReceiptHandler> _logger;

    public PrintReceiptHandler(IAuthorizationService authorizationService, IReceiptService receiptService, IValidator<PrintReceiptCommand> validator, ILogger<PrintReceiptHandler> logger)
    {
        _authorizationService = authorizationService;
        _receiptService = receiptService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<ReceiptPrintResult>> HandleAsync(PrintReceiptCommand command, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.ReceiptPrint))
        {
            _logger.LogWarning("Unauthorized receipt print request. SaleId={SaleId}", command.SaleId);
            return Result<ReceiptPrintResult>.Failure("Current user cannot print receipts.");
        }

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<ReceiptPrintResult>.Failure(validation.Errors[0].ErrorMessage);
        }

        return Result<ReceiptPrintResult>.Success(await _receiptService.PrintCompletedSaleAsync(command.SaleId, PrintRequestKind.Manual, command.PrinterName, command.Copies, cancellationToken));
    }
}

public sealed class ReprintReceiptHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IReceiptService _receiptService;
    private readonly IValidator<ReprintReceiptCommand> _validator;
    private readonly ILogger<ReprintReceiptHandler> _logger;

    public ReprintReceiptHandler(IAuthorizationService authorizationService, IReceiptService receiptService, IValidator<ReprintReceiptCommand> validator, ILogger<ReprintReceiptHandler> logger)
    {
        _authorizationService = authorizationService;
        _receiptService = receiptService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<ReceiptPrintResult>> HandleAsync(ReprintReceiptCommand command, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.ReceiptReprint))
        {
            _logger.LogWarning("Unauthorized receipt reprint request. Invoice={InvoiceNumber}", command.InvoiceNumber);
            return Result<ReceiptPrintResult>.Failure("Current user cannot reprint receipts.");
        }

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<ReceiptPrintResult>.Failure(validation.Errors[0].ErrorMessage);
        }

        return Result<ReceiptPrintResult>.Success(await _receiptService.ReprintAsync(command, cancellationToken));
    }
}

public sealed class TestPrintHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IReceiptService _receiptService;
    private readonly IValidator<TestPrintCommand> _validator;

    public TestPrintHandler(IAuthorizationService authorizationService, IReceiptService receiptService, IValidator<TestPrintCommand> validator)
    {
        _authorizationService = authorizationService;
        _receiptService = receiptService;
        _validator = validator;
    }

    public async Task<Result<ReceiptPrintResult>> HandleAsync(TestPrintCommand command, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.ReceiptTestPrint))
        {
            return Result<ReceiptPrintResult>.Failure("Current user cannot test printers.");
        }

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        return validation.IsValid
            ? Result<ReceiptPrintResult>.Success(await _receiptService.TestPrintAsync(command, cancellationToken))
            : Result<ReceiptPrintResult>.Failure(validation.Errors[0].ErrorMessage);
    }
}

public sealed class RetryPrintHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IPrintQueueService _printQueueService;
    private readonly IReceiptPrinter _receiptPrinter;
    private readonly ILogger<RetryPrintHandler> _logger;

    public RetryPrintHandler(IAuthorizationService authorizationService, IPrintQueueService printQueueService, IReceiptPrinter receiptPrinter, ILogger<RetryPrintHandler> logger)
    {
        _authorizationService = authorizationService;
        _printQueueService = printQueueService;
        _receiptPrinter = receiptPrinter;
        _logger = logger;
    }

    public async Task<Result<ReceiptPrintResult>> HandleAsync(RetryPrintCommand command, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.ReceiptPrint))
        {
            return Result<ReceiptPrintResult>.Failure("Current user cannot retry receipt printing.");
        }

        var result = await _printQueueService.RetryAsync(command.PrintRequestId, _receiptPrinter, cancellationToken);
        if (result is null)
        {
            _logger.LogWarning("Receipt retry request was not found. RequestId={PrintRequestId}", command.PrintRequestId);
            return Result<ReceiptPrintResult>.Failure("Receipt print request is not available for retry.");
        }

        _logger.LogInformation("Receipt retry completed. RequestId={PrintRequestId} Succeeded={Succeeded}", command.PrintRequestId, result.Succeeded);
        return Result<ReceiptPrintResult>.Success(result);
    }
}

public sealed class GetReceiptPreviewHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IReceiptService _receiptService;
    private readonly IValidator<GetReceiptPreviewQuery> _validator;

    public GetReceiptPreviewHandler(IAuthorizationService authorizationService, IReceiptService receiptService, IValidator<GetReceiptPreviewQuery> validator)
    {
        _authorizationService = authorizationService;
        _receiptService = receiptService;
        _validator = validator;
    }

    public async Task<Result<ReceiptPreviewDto>> HandleAsync(GetReceiptPreviewQuery query, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.ReceiptPrint) && !_authorizationService.HasPermission(PermissionConstants.ReceiptReprint))
        {
            return Result<ReceiptPreviewDto>.Failure("Current user cannot preview receipts.");
        }

        var validation = await _validator.ValidateAsync(query, cancellationToken);
        return validation.IsValid
            ? await _receiptService.PreviewAsync(query, cancellationToken)
            : Result<ReceiptPreviewDto>.Failure(validation.Errors[0].ErrorMessage);
    }
}

public sealed class GetAvailablePrintersHandler
{
    private readonly IPrinterDiscoveryService _printerDiscoveryService;

    public GetAvailablePrintersHandler(IPrinterDiscoveryService printerDiscoveryService)
    {
        _printerDiscoveryService = printerDiscoveryService;
    }

    public async Task<Result<IReadOnlyCollection<PrinterInfoDto>>> HandleAsync(GetAvailablePrintersQuery query, CancellationToken cancellationToken = default)
    {
        return Result<IReadOnlyCollection<PrinterInfoDto>>.Success(await _printerDiscoveryService.GetInstalledPrintersAsync(cancellationToken));
    }
}

public sealed class SearchReceiptSalesHandler
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<SearchReceiptSalesQuery> _validator;

    public SearchReceiptSalesHandler(IAuthorizationService authorizationService, IUnitOfWork unitOfWork, IValidator<SearchReceiptSalesQuery> validator)
    {
        _authorizationService = authorizationService;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<IReadOnlyCollection<ReceiptSaleSearchRowDto>>> HandleAsync(SearchReceiptSalesQuery query, CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(PermissionConstants.ReceiptReprint))
        {
            return Result<IReadOnlyCollection<ReceiptSaleSearchRowDto>>.Failure("Current user cannot search receipts for reprint.");
        }

        var validation = await _validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<IReadOnlyCollection<ReceiptSaleSearchRowDto>>.Failure(validation.Errors[0].ErrorMessage);
        }

        var sales = await _unitOfWork.Sales.SearchCompletedAsync(query.InvoiceNumber, query.Date, query.CashierId, query.PageNumber, query.PageSize, cancellationToken);
        return Result<IReadOnlyCollection<ReceiptSaleSearchRowDto>>.Success(sales.Select(sale => new ReceiptSaleSearchRowDto
        {
            SaleId = sale.Id,
            InvoiceNumber = sale.InvoiceNumber,
            SaleDate = sale.SaleDate,
            Cashier = sale.Cashier?.FullName ?? string.Empty,
            Total = sale.Total
        }).ToArray());
    }
}
