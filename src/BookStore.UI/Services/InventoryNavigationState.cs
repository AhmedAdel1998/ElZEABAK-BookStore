namespace BookStore.UI.Services;

/// <summary>
/// Default inventory navigation state.
/// </summary>
public sealed class InventoryNavigationState : IInventoryNavigationState
{
    /// <inheritdoc />
    public bool LowStockOnly { get; set; }

    /// <inheritdoc />
    public bool OutOfStockOnly { get; set; }
}
