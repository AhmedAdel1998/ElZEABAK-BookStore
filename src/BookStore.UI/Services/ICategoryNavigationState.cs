namespace BookStore.UI.Services;

/// <summary>
/// Stores transient category navigation state for view-model-first shell navigation.
/// </summary>
public interface ICategoryNavigationState
{
    /// <summary>
    /// Gets or sets the selected category identifier.
    /// </summary>
    Guid? SelectedCategoryId { get; set; }
}
