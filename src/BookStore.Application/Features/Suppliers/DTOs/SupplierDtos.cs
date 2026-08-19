namespace BookStore.Application.Features.Suppliers.DTOs;

/// <summary>
/// Editable supplier model.
/// </summary>
public class SupplierEditorModel
{
    /// <summary>Gets or sets supplier identifier.</summary>
    public Guid? Id { get; set; }
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
    /// <summary>Gets or sets active state.</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Supplier data transfer object.
/// </summary>
public class SupplierDto : SupplierEditorModel
{
    /// <summary>
    /// Gets the identifier of this persisted supplier. Query results always carry one; a missing
    /// value means a DTO was built by hand and never saved.
    /// </summary>
    public Guid PersistedId => Id ?? throw new InvalidOperationException("Supplier identifier is missing from a persisted record.");

    /// <summary>Gets or sets associated product count.</summary>
    public int ProductCount { get; set; }
    /// <summary>Gets or sets created date.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Gets or sets updated date.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
    /// <summary>Gets status label.</summary>
    public string Status => IsActive ? "Active" : "Inactive";
}

/// <summary>
/// Supplier row in the supplier list.
/// </summary>
public sealed class SupplierListItem : SupplierDto
{
}

/// <summary>
/// Supplier search filter.
/// </summary>
public sealed class SupplierFilter
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
/// Product row associated with a supplier.
/// </summary>
public sealed class SupplierProductItem
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
    /// <summary>Gets status label.</summary>
    public string Status => IsActive ? "Active" : "Inactive";
}
