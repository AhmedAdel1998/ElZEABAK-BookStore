namespace BookStore.UI.Services;

/// <summary>
/// Default product navigation state.
/// </summary>
public sealed class ProductNavigationState : IProductNavigationState
{
    /// <inheritdoc />
    public Guid? SelectedProductId { get; set; }
}
