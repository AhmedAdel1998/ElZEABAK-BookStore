namespace BookStore.Application.Interfaces;

/// <summary>
/// Displays confirmation prompts to the user.
/// </summary>
public interface IConfirmationDialogService
{
    /// <summary>
    /// Shows a confirmation prompt.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The message to display.</param>
    /// <returns><see langword="true"/> when the user confirms; otherwise, <see langword="false"/>.</returns>
    Task<bool> ConfirmAsync(string title, string message);
}
