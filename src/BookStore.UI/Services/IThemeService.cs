namespace BookStore.UI.Services;

/// <summary>
/// Applies application themes.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the current theme name.
    /// </summary>
    string CurrentTheme { get; }

    /// <summary>
    /// Applies the configured theme.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ApplyConfiguredThemeAsync();

    /// <summary>
    /// Toggles between light and dark themes.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ToggleThemeAsync();
}
