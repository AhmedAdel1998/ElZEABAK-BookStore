namespace BookStore.Application.Interfaces;

/// <summary>
/// Displays user-friendly error messages.
/// </summary>
public interface IErrorDialogService
{
    /// <summary>
    /// Shows an error message.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The message to display.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ShowErrorAsync(string title, string message);
}
