using BookStore.Domain.Enums;

namespace BookStore.Application.Features.Inventory.Responses;

/// <summary>
/// Represents a completed inventory movement.
/// </summary>
public sealed class InventoryMovementResponse
{
    /// <summary>Gets or sets the transaction identifier.</summary>
    public Guid TransactionId { get; set; }
    /// <summary>Gets or sets the product identifier.</summary>
    public Guid ProductId { get; set; }
    /// <summary>Gets or sets the product title.</summary>
    public string ProductTitle { get; set; } = string.Empty;
    /// <summary>Gets or sets the transaction type.</summary>
    public InventoryTransactionType TransactionType { get; set; }
    /// <summary>Gets or sets the previous quantity.</summary>
    public int QuantityBefore { get; set; }
    /// <summary>Gets or sets the new quantity.</summary>
    public int QuantityAfter { get; set; }
    /// <summary>Gets or sets the signed quantity change.</summary>
    public int QuantityChange { get; set; }
}
