namespace BookStore.UI.Services;

/// <summary>
/// Provides shell window state commands.
/// </summary>
public interface IWindowStateService
{
    /// <summary>
    /// Minimizes the main window.
    /// </summary>
    void Minimize();

    /// <summary>
    /// Toggles maximize and restore.
    /// </summary>
    void ToggleMaximize();

    /// <summary>
    /// Closes the application.
    /// </summary>
    void Close();
}
