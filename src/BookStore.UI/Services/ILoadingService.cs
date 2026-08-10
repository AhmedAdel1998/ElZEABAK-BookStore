namespace BookStore.UI.Services;

/// <summary>
/// Controls shell loading overlay state.
/// </summary>
public interface ILoadingService
{
    /// <summary>
    /// Occurs when loading state changes.
    /// </summary>
    event EventHandler? StateChanged;

    /// <summary>
    /// Gets a value indicating whether loading is active.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Gets the loading text.
    /// </summary>
    string LoadingText { get; }

    /// <summary>
    /// Shows the loading overlay.
    /// </summary>
    /// <param name="text">The loading text.</param>
    void Show(string text);

    /// <summary>
    /// Hides the loading overlay.
    /// </summary>
    void Hide();
}
