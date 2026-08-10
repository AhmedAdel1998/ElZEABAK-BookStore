namespace BookStore.Domain.Enums;

/// <summary>
/// Defines inventory transaction types.
/// </summary>
public enum InventoryTransactionType
{
    /// <summary>
    /// Initial stock entry.
    /// </summary>
    InitialStock = 0,

    /// <summary>
    /// Manual stock adjustment.
    /// </summary>
    ManualAdjustment = 1,

    /// <summary>
    /// Product sale.
    /// </summary>
    Sale = 2,

    /// <summary>
    /// Product return.
    /// </summary>
    Return = 3,

    /// <summary>
    /// Damaged stock.
    /// </summary>
    Damage = 4,

    /// <summary>
    /// Inventory correction.
    /// </summary>
    Correction = 5,

    /// <summary>
    /// Product purchase reserved for future purchase orders.
    /// </summary>
    Purchase = 6,

    /// <summary>
    /// Legacy manual adjustment alias.
    /// </summary>
    Adjustment = ManualAdjustment
}
