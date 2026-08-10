using BookStore.Domain.Enums;

namespace BookStore.Application.Features.Inventory.DTOs;

/// <summary>
/// Represents an inventory product row.
/// </summary>
public sealed class InventoryItemDto
{
    /// <summary>Gets or sets the product identifier.</summary>
    public Guid ProductId { get; set; }
    /// <summary>Gets or sets the product image path.</summary>
    public string? ImagePath { get; set; }
    /// <summary>Gets or sets the barcode.</summary>
    public string Barcode { get; set; } = string.Empty;
    /// <summary>Gets or sets the ISBN.</summary>
    public string? ISBN { get; set; }
    /// <summary>Gets or sets the title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the category name.</summary>
    public string? CategoryName { get; set; }
    /// <summary>Gets or sets the current quantity.</summary>
    public int CurrentQuantity { get; set; }
    /// <summary>Gets or sets the minimum stock.</summary>
    public int MinimumStock { get; set; }
    /// <summary>Gets or sets the purchase price.</summary>
    public decimal PurchasePrice { get; set; }
    /// <summary>Gets or sets the selling price.</summary>
    public decimal SellingPrice { get; set; }
    /// <summary>Gets or sets the last updated date.</summary>
    public DateTimeOffset? LastUpdated { get; set; }
    /// <summary>Gets the inventory status.</summary>
    public string InventoryStatus => CurrentQuantity == 0 ? "Out of Stock" : CurrentQuantity <= MinimumStock ? "Low Stock" : "In Stock";
}

/// <summary>
/// Represents a stock ledger row.
/// </summary>
public sealed class InventoryTransactionDto
{
    /// <summary>Gets or sets the transaction identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the product identifier.</summary>
    public Guid ProductId { get; set; }
    /// <summary>Gets or sets the product title.</summary>
    public string ProductTitle { get; set; } = string.Empty;
    /// <summary>Gets or sets the transaction type.</summary>
    public InventoryTransactionType TransactionType { get; set; }
    /// <summary>Gets or sets the signed quantity change.</summary>
    public int QuantityChange { get; set; }
    /// <summary>Gets or sets the quantity before the change.</summary>
    public int QuantityBefore { get; set; }
    /// <summary>Gets or sets the quantity after the change.</summary>
    public int QuantityAfter { get; set; }
    /// <summary>Gets or sets the reference.</summary>
    public string? Reference { get; set; }
    /// <summary>Gets or sets the user display name.</summary>
    public string? User { get; set; }
    /// <summary>Gets or sets the reason.</summary>
    public string Reason { get; set; } = string.Empty;
    /// <summary>Gets or sets notes.</summary>
    public string? Notes { get; set; }
    /// <summary>Gets or sets the transaction date.</summary>
    public DateTimeOffset Date { get; set; }
}

/// <summary>
/// Represents inventory dashboard metrics.
/// </summary>
public sealed class InventoryDashboardDto
{
    /// <summary>Gets or sets total product count.</summary>
    public int TotalProducts { get; set; }
    /// <summary>Gets or sets total stock quantity.</summary>
    public int TotalStock { get; set; }
    /// <summary>Gets or sets low stock product count.</summary>
    public int LowStockCount { get; set; }
    /// <summary>Gets or sets out-of-stock product count.</summary>
    public int OutOfStockCount { get; set; }
    /// <summary>Gets or sets inventory value.</summary>
    public decimal InventoryValue { get; set; }
    /// <summary>Gets or sets total purchase value.</summary>
    public decimal TotalPurchaseValue { get; set; }
    /// <summary>Gets or sets total selling value.</summary>
    public decimal TotalSellingValue { get; set; }
}
