using BookStore.Application.Features.Receipts.DTOs;

namespace BookStore.Application.Features.Sales.Responses;

/// <summary>
/// Represents completed sale response.
/// </summary>
public sealed class CompleteSaleResponse
{
    /// <summary>Gets or sets persisted sale identifier.</summary>
    public Guid SaleId { get; set; }
    /// <summary>Gets or sets invoice number.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets receipt model.</summary>
    public ReceiptModel Receipt { get; set; } = new();

    /// <summary>Gets or sets whether receipt printing succeeded.</summary>
    public bool ReceiptPrintSucceeded { get; set; }

    /// <summary>Gets or sets receipt print failure message.</summary>
    public string? ReceiptPrintError { get; set; }

    /// <summary>Gets or sets print request identifier.</summary>
    public Guid? PrintRequestId { get; set; }
}
