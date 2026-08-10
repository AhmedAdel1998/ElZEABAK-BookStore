using BookStore.Application.Features.Sales.DTOs;

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
}
