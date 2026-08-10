using BookStore.UI.ViewModels;

namespace BookStore.UI.Navigation;

/// <summary>
/// Provides navigation inside the authenticated shell content region.
/// </summary>
public interface IShellNavigationService : INavigationService
{
    /// <summary>
    /// Gets the current breadcrumb text.
    /// </summary>
    string Breadcrumb { get; }

    /// <summary>
    /// Navigates with explicit breadcrumb metadata.
    /// </summary>
    /// <typeparam name="TViewModel">The destination view model type.</typeparam>
    /// <param name="breadcrumb">The breadcrumb path.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NavigateToAsync<TViewModel>(string breadcrumb)
        where TViewModel : BaseViewModel;
}
