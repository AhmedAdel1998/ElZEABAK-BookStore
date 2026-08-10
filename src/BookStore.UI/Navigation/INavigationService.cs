using BookStore.UI.ViewModels;

namespace BookStore.UI.Navigation;

/// <summary>
/// Provides view-model-first navigation.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets the currently active view model.
    /// </summary>
    BaseViewModel? CurrentViewModel { get; }

    /// <summary>
    /// Navigates to the requested view model type.
    /// </summary>
    /// <typeparam name="TViewModel">The destination view model type.</typeparam>
    /// <returns>A task that represents the asynchronous navigation operation.</returns>
    Task NavigateToAsync<TViewModel>()
        where TViewModel : BaseViewModel;

    /// <summary>
    /// Navigates to the previous view model when available.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task GoBackAsync();

    /// <summary>
    /// Navigates to the next view model when available.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task GoForwardAsync();

    /// <summary>
    /// Refreshes the current view model.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RefreshAsync();

    /// <summary>
    /// Clears navigation history.
    /// </summary>
    void ClearHistory();
}
