namespace BookStore.Application.Interfaces;

/// <summary>
/// Displays informational messages to the user.
/// </summary>
public interface IMessageDialogService
{
    /// <summary>
    /// Shows an informational message.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The message to display.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ShowMessageAsync(string title, string message);
}
