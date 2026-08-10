using BookStore.Domain.Enums;

namespace BookStore.Domain.ReadModels;

/// <summary>
/// Represents a customer row with sales statistics.
/// </summary>
public sealed class CustomerListReadModel
{
    /// <summary>Gets or sets customer identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets full name.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>Gets or sets phone.</summary>
    public string Phone { get; set; } = string.Empty;
    /// <summary>Gets or sets email.</summary>
    public string? Email { get; set; }
    /// <summary>Gets or sets address.</summary>
    public string? Address { get; set; }
    /// <summary>Gets or sets sales count.</summary>
    public int SalesCount { get; set; }
    /// <summary>Gets or sets total purchases.</summary>
    public decimal TotalPurchases { get; set; }
    /// <summary>Gets or sets last purchase date.</summary>
    public DateTimeOffset? LastPurchaseDate { get; set; }
    /// <summary>Gets or sets active state.</summary>
    public bool IsActive { get; set; }
    /// <summary>Gets or sets deleted state.</summary>
    public bool IsDeleted { get; set; }
    /// <summary>Gets or sets created date.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Gets or sets updated date.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Represents a customer sale history row.
/// </summary>
public sealed class CustomerSaleHistoryReadModel
{
    /// <summary>Gets or sets sale identifier.</summary>
    public Guid SaleId { get; set; }
    /// <summary>Gets or sets invoice number.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets sale date.</summary>
    public DateTimeOffset SaleDate { get; set; }
    /// <summary>Gets or sets sale total.</summary>
    public decimal Total { get; set; }
    /// <summary>Gets or sets discount.</summary>
    public decimal Discount { get; set; }
    /// <summary>Gets or sets tax.</summary>
    public decimal Tax { get; set; }
    /// <summary>Gets or sets payment method.</summary>
    public PaymentMethod PaymentMethod { get; set; }
    /// <summary>Gets or sets sale status.</summary>
    public SaleStatus SaleStatus { get; set; }
}
