namespace BookStore.UI.ViewModels;

/// <summary>
/// Base view model for future module placeholder pages.
/// </summary>
public abstract class ModulePlaceholderViewModel : BaseViewModel
{
    /// <summary>
    /// Gets the module description.
    /// </summary>
    public string Description { get; protected init; } = "This module is ready for future implementation.";
}
