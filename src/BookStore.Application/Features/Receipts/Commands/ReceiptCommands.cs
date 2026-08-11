using BookStore.Application.Features.Receipts.DTOs;

namespace BookStore.Application.Features.Receipts.Commands;

public sealed record PrintReceiptCommand(Guid SaleId, string? PrinterName = null, int Copies = 1);

public sealed record ReprintReceiptCommand(string InvoiceNumber, string? PrinterName = null, int Copies = 1);

public sealed record TestPrintCommand(string? PrinterName = null);

public sealed record RetryPrintCommand(Guid PrintRequestId);
