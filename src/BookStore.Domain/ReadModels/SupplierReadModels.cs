namespace BookStore.Domain.ReadModels;

/// <summary>
/// Represents a supplier row with product statistics.
/// </summary>
public sealed class SupplierListReadModel
{
    /// <summary>Gets or sets supplier identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets company name.</summary>
    public string CompanyName { get; set; } = string.Empty;
    /// <summary>Gets or sets contact name.</summary>
    public string? ContactName { get; set; }
    /// <summary>Gets or sets phone.</summary>
    public string Phone { get; set; } = string.Empty;
    /// <summary>Gets or sets email.</summary>
    public string? Email { get; set; }
    /// <summary>Gets or sets address.</summary>
    public string? Address { get; set; }
    /// <summary>Gets or sets notes.</summary>
    public string? Notes { get; set; }
    /// <summary>Gets or sets product count.</summary>
    public int ProductCount { get; set; }
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
/// Represents a product associated with a supplier.
/// </summary>
public sealed class SupplierProductReadModel
{
    /// <summary>Gets or sets product identifier.</summary>
    public Guid ProductId { get; set; }
    /// <summary>Gets or sets barcode.</summary>
    public string Barcode { get; set; } = string.Empty;
    /// <summary>Gets or sets ISBN.</summary>
    public string? ISBN { get; set; }
    /// <summary>Gets or sets product title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets author.</summary>
    public string? Author { get; set; }
    /// <summary>Gets or sets category.</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>Gets or sets purchase price.</summary>
    public decimal PurchasePrice { get; set; }
    /// <summary>Gets or sets selling price.</summary>
    public decimal SellingPrice { get; set; }
    /// <summary>Gets or sets current stock.</summary>
    public int CurrentStock { get; set; }
    /// <summary>Gets or sets active state.</summary>
    public bool IsActive { get; set; }
}
