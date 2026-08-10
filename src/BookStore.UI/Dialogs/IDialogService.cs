namespace BookStore.UI.Dialogs;

/// <summary>
/// Provides reusable asynchronous dialogs for the WPF presentation layer.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a reusable dialog.
    /// </summary>
    /// <param name="request">The dialog request.</param>
    /// <returns>The selected dialog result.</returns>
    Task<DialogResultModel> ShowAsync(DialogRequest request);

    /// <summary>
    /// Shows an information dialog.
    /// </summary>
    Task ShowInformationAsync(string title, string message);

    /// <summary>
    /// Shows a warning dialog.
    /// </summary>
    Task ShowWarningAsync(string title, string message);

    /// <summary>
    /// Shows a success dialog.
    /// </summary>
    Task ShowSuccessAsync(string title, string message);

    /// <summary>
    /// Shows an error dialog.
    /// </summary>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Shows a confirmation dialog.
    /// </summary>
    Task<bool> ShowConfirmationAsync(string title, string message);

    /// <summary>
    /// Shows a question dialog.
    /// </summary>
    Task<bool> ShowQuestionAsync(string title, string message);
}
