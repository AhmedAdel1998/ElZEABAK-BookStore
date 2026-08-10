namespace BookStore.UI.Services;

/// <summary>
/// Stores transient product navigation state for view-model-first shell navigation.
/// </summary>
public interface IProductNavigationState
{
    /// <summary>Gets or sets the selected product identifier.</summary>
    Guid? SelectedProductId { get; set; }
}
