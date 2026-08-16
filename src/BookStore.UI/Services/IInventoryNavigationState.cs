namespace BookStore.UI.Services;

/// <summary>
/// Stores transient inventory filters for view-model-first shell navigation.
/// </summary>
public interface IInventoryNavigationState
{
    /// <summary>Gets or sets whether the inventory list should open with low stock filtering.</summary>
    bool LowStockOnly { get; set; }

    /// <summary>Gets or sets whether the inventory list should open with out-of-stock filtering.</summary>
    bool OutOfStockOnly { get; set; }
}
