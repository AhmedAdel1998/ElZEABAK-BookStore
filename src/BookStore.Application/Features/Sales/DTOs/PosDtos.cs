using BookStore.Domain.Enums;

namespace BookStore.Application.Features.Sales.DTOs;

/// <summary>
/// Represents a POS product search result.
/// </summary>
public sealed class PosProductDto
{
    /// <summary>Gets or sets product identifier.</summary>
    public Guid ProductId { get; set; }
    /// <summary>Gets or sets barcode.</summary>
    public string Barcode { get; set; } = string.Empty;
    /// <summary>Gets or sets ISBN.</summary>
    public string? ISBN { get; set; }
    /// <summary>Gets or sets title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets category name.</summary>
    public string? CategoryName { get; set; }
    /// <summary>Gets or sets author.</summary>
    public string? Author { get; set; }
    /// <summary>Gets or sets unit price.</summary>
    public decimal UnitPrice { get; set; }
    /// <summary>Gets or sets available quantity.</summary>
    public int AvailableQuantity { get; set; }
    /// <summary>Gets or sets active state.</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Represents a POS cart item.
/// </summary>
public sealed class SaleCartItemDto
{
    /// <summary>Gets or sets cart item identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Gets or sets product identifier.</summary>
    public Guid ProductId { get; set; }
    /// <summary>Gets or sets barcode.</summary>
    public string Barcode { get; set; } = string.Empty;
    /// <summary>Gets or sets title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets category name.</summary>
    public string? CategoryName { get; set; }
    /// <summary>Gets or sets quantity.</summary>
    public int Quantity { get; set; }
    /// <summary>Gets or sets available quantity.</summary>
    public int AvailableQuantity { get; set; }
    /// <summary>Gets or sets unit price.</summary>
    public decimal UnitPrice { get; set; }
    /// <summary>Gets or sets line discount.</summary>
    public decimal Discount { get; set; }
    /// <summary>Gets or sets line tax.</summary>
    public decimal Tax { get; set; }
    /// <summary>Gets or sets line total.</summary>
    public decimal LineTotal { get; set; }
}

/// <summary>
/// Represents a POS sale session.
/// </summary>
public sealed class SaleSessionDto
{
    /// <summary>Gets or sets sale session identifier.</summary>
    public Guid SaleId { get; set; } = Guid.NewGuid();
    /// <summary>Gets or sets invoice number.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets cashier user identifier.</summary>
    public Guid CashierId { get; set; }
    /// <summary>Gets or sets cashier display name.</summary>
    public string CashierName { get; set; } = string.Empty;
    /// <summary>Gets or sets selected customer identifier.</summary>
    public Guid? CustomerId { get; set; }
    /// <summary>Gets or sets customer display name.</summary>
    public string CustomerName { get; set; } = "Walk-in Customer";
    /// <summary>Gets or sets selected customer phone.</summary>
    public string? CustomerPhone { get; set; }
    /// <summary>Gets or sets created timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Gets or sets suspended flag.</summary>
    public bool IsSuspended { get; set; }
    /// <summary>Gets or sets cart items.</summary>
    public List<SaleCartItemDto> Items { get; set; } = [];
    /// <summary>Gets or sets invoice discount.</summary>
    public decimal InvoiceDiscount { get; set; }
    /// <summary>Gets or sets payment method.</summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    /// <summary>Gets or sets amount paid.</summary>
    public decimal AmountPaid { get; set; }
    /// <summary>Gets or sets how many receipt copies this invoice prints at checkout.</summary>
    public int ReceiptCopies { get; set; } = 1;
    /// <summary>Gets or sets the last time this invoice was edited.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Gets or sets summary.</summary>
    public SaleSummaryDto Summary { get; set; } = new();
}

/// <summary>
/// Represents sale summary totals.
/// </summary>
public sealed class SaleSummaryDto
{
    /// <summary>Gets or sets subtotal.</summary>
    public decimal Subtotal { get; set; }
    /// <summary>Gets or sets line discounts.</summary>
    public decimal LineDiscount { get; set; }
    /// <summary>Gets or sets invoice discount.</summary>
    public decimal InvoiceDiscount { get; set; }
    /// <summary>Gets or sets tax.</summary>
    public decimal Tax { get; set; }
    /// <summary>Gets or sets grand total.</summary>
    public decimal GrandTotal { get; set; }
    /// <summary>Gets or sets amount paid.</summary>
    public decimal AmountPaid { get; set; }
    /// <summary>Gets or sets change.</summary>
    public decimal Change { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Tax"/> is already contained in the line
    /// prices rather than added on top of them.
    /// </summary>
    public bool TaxIncludedInPrice { get; set; }
}

/// <summary>
/// Represents receipt data prepared after checkout.
/// </summary>
public sealed class ReceiptModel
{
    /// <summary>Gets or sets invoice number.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets completed timestamp.</summary>
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Gets or sets cashier name.</summary>
    public string CashierName { get; set; } = string.Empty;
    /// <summary>Gets or sets summary.</summary>
    public SaleSummaryDto Summary { get; set; } = new();
}

/// <summary>
/// Describes one change the POS had to make to an open invoice because live stock no longer
/// supported the quantity the cart was holding.
/// </summary>
public sealed class PosCartAdjustmentDto
{
    /// <summary>Gets or sets the product identifier.</summary>
    public Guid ProductId { get; set; }
    /// <summary>Gets or sets the product title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the quantity the cart held before the adjustment.</summary>
    public int PreviousQuantity { get; set; }
    /// <summary>Gets or sets the quantity the cart holds after the adjustment. Zero means removed.</summary>
    public int NewQuantity { get; set; }
    /// <summary>Gets or sets the reason for the adjustment.</summary>
    public PosCartAdjustmentReason Reason { get; set; }
}

/// <summary>
/// Identifies why an open invoice line had to be adjusted.
/// </summary>
public enum PosCartAdjustmentReason
{
    /// <summary>Remaining stock was lower than the quantity held, so the line was reduced.</summary>
    QuantityReduced = 0,

    /// <summary>No stock remained, so the line was dropped.</summary>
    LineRemoved = 1,

    /// <summary>The product was deleted or deactivated, so the line was dropped.</summary>
    ProductUnavailable = 2
}
