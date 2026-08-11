namespace BookStore.Application.Features.Receipts.Queries;

public sealed record GetReceiptPreviewQuery(string InvoiceNumber);

public sealed record SearchReceiptSalesQuery(string? InvoiceNumber = null, DateTimeOffset? Date = null, Guid? CashierId = null, int PageNumber = 1, int PageSize = 50);

public sealed record GetAvailablePrintersQuery();
