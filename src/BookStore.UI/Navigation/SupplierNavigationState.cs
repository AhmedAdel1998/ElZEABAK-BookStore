namespace BookStore.UI.Navigation;

/// <summary>
/// Stores selected supplier navigation state.
/// </summary>
public interface ISupplierNavigationState
{
    /// <summary>Gets or sets selected supplier identifier.</summary>
    Guid? SelectedSupplierId { get; set; }
}

/// <summary>
/// Default selected supplier navigation state.
/// </summary>
public sealed class SupplierNavigationState : ISupplierNavigationState
{
    /// <inheritdoc />
    public Guid? SelectedSupplierId { get; set; }
}
