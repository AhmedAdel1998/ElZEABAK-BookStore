namespace BookStore.UI.Services;

/// <summary>
/// Default transient category navigation state.
/// </summary>
public sealed class CategoryNavigationState : ICategoryNavigationState
{
    /// <inheritdoc />
    public Guid? SelectedCategoryId { get; set; }
}
