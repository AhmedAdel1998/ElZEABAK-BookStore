namespace BookStore.Application.Features.Customers.DTOs;

/// <summary>
/// Editable customer model.
/// </summary>
public class CustomerEditorModel
{
    /// <summary>Gets or sets customer identifier.</summary>
    public Guid? Id { get; set; }
    /// <summary>Gets or sets full name.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>Gets or sets phone.</summary>
    public string Phone { get; set; } = string.Empty;
    /// <summary>Gets or sets email.</summary>
    public string? Email { get; set; }
    /// <summary>Gets or sets address.</summary>
    public string? Address { get; set; }
    /// <summary>Gets or sets active state.</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Customer data transfer object.
/// </summary>
public class CustomerDto : CustomerEditorModel
{
    /// <summary>Gets or sets required customer identifier.</summary>
    public new Guid Id { get; set; }
    /// <summary>Gets or sets sales count.</summary>
    public int SalesCount { get; set; }
    /// <summary>Gets or sets total purchases.</summary>
    public decimal TotalPurchases { get; set; }
    /// <summary>Gets or sets last purchase date.</summary>
    public DateTimeOffset? LastPurchaseDate { get; set; }
    /// <summary>Gets or sets created date.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Gets or sets updated date.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
    /// <summary>Gets status label.</summary>
    public string Status => IsActive ? "Active" : "Inactive";
}

/// <summary>
/// Customer row in the customer list.
/// </summary>
public sealed class CustomerListItem : CustomerDto
{
}

/// <summary>
/// Customer lookup result optimized for POS selection.
/// </summary>
public sealed class CustomerSelectionItem
{
    /// <summary>Gets or sets customer identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets full name.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>Gets or sets phone.</summary>
    public string Phone { get; set; } = string.Empty;
    /// <summary>Gets display label.</summary>
    public string DisplayName => $"{FullName} ({Phone})";
}

/// <summary>
/// Customer search filter.
/// </summary>
public sealed class CustomerFilter
{
    /// <summary>Gets or sets search term.</summary>
    public string? SearchTerm { get; set; }
    /// <summary>Gets or sets active filter.</summary>
    public bool? IsActive { get; set; }
    /// <summary>Gets or sets page number.</summary>
    public int PageNumber { get; set; } = 1;
    /// <summary>Gets or sets page size.</summary>
    public int PageSize { get; set; } = 25;
}

/// <summary>
/// Customer sales-history row.
/// </summary>
public sealed class CustomerSaleHistoryItem
{
    /// <summary>Gets or sets sale identifier.</summary>
    public Guid SaleId { get; set; }
    /// <summary>Gets or sets invoice number.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets sale date.</summary>
    public DateTimeOffset SaleDate { get; set; }
    /// <summary>Gets or sets total.</summary>
    public decimal Total { get; set; }
    /// <summary>Gets or sets discount.</summary>
    public decimal Discount { get; set; }
    /// <summary>Gets or sets tax.</summary>
    public decimal Tax { get; set; }
    /// <summary>Gets or sets payment method.</summary>
    public string PaymentMethod { get; set; } = string.Empty;
    /// <summary>Gets or sets sale status.</summary>
    public string SaleStatus { get; set; } = string.Empty;
}
