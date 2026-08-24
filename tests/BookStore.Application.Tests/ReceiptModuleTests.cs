using BookStore.Application.Features.Receipts.Commands;
using BookStore.Application.Features.Receipts.DTOs;
using BookStore.Application.Features.Receipts.Handlers;
using BookStore.Application.Features.Receipts.Queries;
using BookStore.Application.Features.Receipts.Services;
using BookStore.Application.Features.Receipts.Validators;
using BookStore.Application.Interfaces;
using BookStore.Domain.Enums;
using BookStore.Infrastructure.Printing.Services;
using BookStore.Shared.Constants;
using BookStore.Shared.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookStore.Application.Tests;

public sealed class ReceiptModuleTests
{
    [Fact]
    public void Formatter_WrapsLongProductNamesAndMultipleItemsWithoutColumnOverlap()
    {
        var formatter = new ReceiptFormatter();
        var receipt = CreateReceipt();
        receipt.Items =
        [
            new ReceiptItemDto
            {
                Barcode = "100",
                ProductName = "Enterprise Patterns for Offline Point of Sale Systems With Very Long Title",
                Quantity = 2,
                UnitPrice = 150,
                Discount = 25,
                LineTotal = 275
            },
            new ReceiptItemDto
            {
                Barcode = "200",
                ProductName = "C# Pocket Guide",
                Quantity = 1,
                UnitPrice = 125,
                LineTotal = 125
            }
        ];
        receipt.Subtotal = 425;
        receipt.Discount = 25;
        receipt.Tax = 14;
        receipt.GrandTotal = 414;
        receipt.AmountPaid = 500;
        receipt.Change = 86;

        var content = formatter.Format(receipt, new ReceiptPrintOptions { PaperWidth = ReceiptPaperWidth.Mm80 });

        Assert.Contains("TOTAL", content);
        Assert.Contains("Payment: Cash", content);
        Assert.Contains("Discount: 25.00", content);
        Assert.Contains(content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries), line => line.Contains("  2") && line.Contains("275.00"));
        Assert.All(content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries), line => Assert.True(line.Length <= 48, $"Receipt line exceeded 80mm width: {line}"));
    }

    [Fact]
    public async Task PrintQueue_PreventsDuplicateRequestAndAllowsSingleRetry()
    {
        var queue = new PrintQueueService();
        var requestId = Guid.NewGuid();
        var job = new ReceiptPrintJob { RequestId = requestId, Receipt = CreateReceipt() };

        Assert.True(queue.TryStart(requestId));
        Assert.False(queue.TryStart(requestId));

        queue.EnqueueForRetry(job);
        var printer = new FakeReceiptPrinter(shouldSucceed: true);
        var retry = await queue.RetryAsync(requestId, printer);

        Assert.NotNull(retry);
        Assert.True(retry.Succeeded);
        Assert.Equal(1, printer.PrintCalls);
        Assert.Null(await queue.RetryAsync(requestId, printer));
    }

    [Fact]
    public async Task PrintHandler_ReturnsFailureWhenPrinterUnavailableWithoutThrowing()
    {
        var receiptService = new FakeReceiptService
        {
            PrintResult = ReceiptPrintResult.Failure(Guid.NewGuid(), "The printer is unavailable.", "Offline printer", CreateReceipt())
        };
        var handler = new PrintReceiptHandler(
            new FakeAuthorizationService([PermissionConstants.ReceiptPrint]),
            receiptService,
            new PrintReceiptCommandValidator(),
            NullLogger<PrintReceiptHandler>.Instance);

        var result = await handler.HandleAsync(new PrintReceiptCommand(Guid.NewGuid(), "Offline printer"));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Succeeded);
        Assert.Equal("The printer is unavailable.", result.Value.Error);
    }

    [Fact]
    public async Task TestPrint_DoesNotRequireSaleOrInventoryWork()
    {
        var receiptService = new FakeReceiptService();
        var handler = new TestPrintHandler(
            new FakeAuthorizationService([PermissionConstants.ReceiptTestPrint]),
            receiptService,
            new TestPrintCommandValidator());

        var result = await handler.HandleAsync(new TestPrintCommand("Thermal"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Succeeded);
        Assert.Equal(1, receiptService.TestPrintCalls);
        Assert.Equal(0, receiptService.PrintSaleCalls);
        Assert.Equal(0, receiptService.ReprintCalls);
    }

    [Fact]
    public async Task Reprint_RequiresReceiptReprintPermission()
    {
        var handler = new ReprintReceiptHandler(
            new FakeAuthorizationService([]),
            new FakeReceiptService(),
            new ReprintReceiptCommandValidator(),
            NullLogger<ReprintReceiptHandler>.Instance);

        var result = await handler.HandleAsync(new ReprintReceiptCommand("INV-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Current user cannot reprint receipts.", result.Error);
    }

    [Fact]
    public async Task RetryHandler_ReprintsQueuedReceiptWithoutCreatingANewSalePath()
    {
        var queue = new PrintQueueService();
        var requestId = Guid.NewGuid();
        queue.EnqueueForRetry(new ReceiptPrintJob { RequestId = requestId, Receipt = CreateReceipt(), Kind = PrintRequestKind.Automatic });
        var printer = new FakeReceiptPrinter(shouldSucceed: true);
        var handler = new RetryPrintHandler(
            new FakeAuthorizationService([PermissionConstants.ReceiptPrint]),
            queue,
            printer,
            NullLogger<RetryPrintHandler>.Instance);

        var result = await handler.HandleAsync(new RetryPrintCommand(requestId));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Succeeded);
        Assert.Equal(1, printer.PrintCalls);
    }

    private static ReceiptModel CreateReceipt() => new()
    {
        SaleId = Guid.NewGuid(),
        InvoiceNumber = "INV-100",
        SaleDate = DateTimeOffset.UtcNow,
        StoreName = "BOOKSTORE",
        Cashier = "Cashier",
        RegisterName = "REGISTER-1",
        SaleStatus = "Completed",
        CustomerName = "Walk-in Customer",
        PaymentMethod = PaymentMethod.Cash,
        Footer = "Books are returnable by policy.",
        ThankYouMessage = "Thank you for shopping with us."
    };

    private sealed class FakeReceiptService : IReceiptService
    {
        public ReceiptPrintResult? PrintResult { get; init; }
        public int PrintSaleCalls { get; private set; }
        public int ReprintCalls { get; private set; }
        public int TestPrintCalls { get; private set; }

        public Task<Result<ReceiptModel>> BuildReceiptAsync(Guid saleId, CancellationToken cancellationToken = default) => Task.FromResult(Result<ReceiptModel>.Success(CreateReceipt()));

        public Task<Result<ReceiptModel>> BuildReceiptByInvoiceAsync(string invoiceNumber, CancellationToken cancellationToken = default) => Task.FromResult(Result<ReceiptModel>.Success(CreateReceipt()));

        public Task<ReceiptPrintResult> PrintCompletedSaleAsync(Guid saleId, PrintRequestKind kind, string? printerName = null, int copies = 1, CancellationToken cancellationToken = default)
        {
            PrintSaleCalls++;
            return Task.FromResult(PrintResult ?? ReceiptPrintResult.Success(Guid.NewGuid(), printerName, CreateReceipt()));
        }

        public Task<ReceiptPrintResult> ReprintAsync(ReprintReceiptCommand command, CancellationToken cancellationToken = default)
        {
            ReprintCalls++;
            return Task.FromResult(ReceiptPrintResult.Success(Guid.NewGuid(), command.PrinterName, CreateReceipt()));
        }

        public Task<ReceiptPrintResult> TestPrintAsync(TestPrintCommand command, CancellationToken cancellationToken = default)
        {
            TestPrintCalls++;
            return Task.FromResult(ReceiptPrintResult.Success(Guid.NewGuid(), command.PrinterName));
        }

        public Task<Result<ReceiptPreviewDto>> PreviewAsync(GetReceiptPreviewQuery query, CancellationToken cancellationToken = default) => Task.FromResult(Result<ReceiptPreviewDto>.Success(new ReceiptPreviewDto()));
    }

    private sealed class FakeReceiptPrinter(bool shouldSucceed) : IReceiptPrinter
    {
        public int PrintCalls { get; private set; }

        public Task<ReceiptPrintResult> PrintAsync(ReceiptPrintJob job, CancellationToken cancellationToken = default)
        {
            PrintCalls++;
            var result = shouldSucceed
                ? ReceiptPrintResult.Success(job.RequestId, job.Options.PrinterName, job.Receipt)
                : ReceiptPrintResult.Failure(job.RequestId, "Printer unavailable.", job.Options.PrinterName, job.Receipt);
            return Task.FromResult(result);
        }

        public Task<ReceiptPrintResult> TestPrintAsync(string? printerName = null, CancellationToken cancellationToken = default) => Task.FromResult(ReceiptPrintResult.Success(Guid.NewGuid(), printerName));

        public Task<bool> IsPrinterAvailableAsync(string? printerName = null, CancellationToken cancellationToken = default) => Task.FromResult(shouldSucceed);

        public Task<IReadOnlyCollection<PrinterInfoDto>> GetAvailablePrintersAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<PrinterInfoDto>>([]);
    }

    private sealed class FakeAuthorizationService(IReadOnlyCollection<string> permissions) : IAuthorizationService
    {
        public bool HasPermission(string permission) => permissions.Contains(permission);
        public bool HasPermissions(params string[] permissionsToCheck) => permissionsToCheck.All(HasPermission);
        public bool HasRole(string role) => false;
        public bool CanAccess(string requiredPermission) => HasPermission(requiredPermission);
    }
}
