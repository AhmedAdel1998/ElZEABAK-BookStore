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
