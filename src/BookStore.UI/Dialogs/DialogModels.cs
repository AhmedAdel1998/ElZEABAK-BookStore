namespace BookStore.UI.Dialogs;

/// <summary>
/// Defines reusable dialog visual categories.
/// </summary>
public enum DialogKind
{
    /// <summary>An informational dialog.</summary>
    Information,
    /// <summary>A confirmation dialog.</summary>
    Confirmation,
    /// <summary>A warning dialog.</summary>
    Warning,
    /// <summary>An error dialog.</summary>
    Error,
    /// <summary>A success dialog.</summary>
    Success,
    /// <summary>A question dialog.</summary>
    Question,
    /// <summary>A progress dialog.</summary>
    Progress,
    /// <summary>A loading dialog.</summary>
    Loading
}

/// <summary>
/// Defines a reusable dialog button.
/// </summary>
/// <param name="Text">The button text.</param>
/// <param name="Result">The result returned when selected.</param>
/// <param name="IsDefault">Whether this button is the default action.</param>
/// <param name="IsCancel">Whether this button is the cancel action.</param>
public sealed record DialogButtonModel(string Text, string Result, bool IsDefault = false, bool IsCancel = false);

/// <summary>
/// Defines a dialog request for MVVM-bound asynchronous dialogs.
/// </summary>
public sealed class DialogRequest
{
    /// <summary>Gets or sets the dialog title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the dialog message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the dialog icon key.</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Gets or sets the dialog kind.</summary>
    public DialogKind Kind { get; set; } = DialogKind.Information;

    /// <summary>Gets or sets reusable button definitions.</summary>
    public IReadOnlyList<DialogButtonModel> Buttons { get; set; } = [new("OK", "OK", true)];
}

/// <summary>
/// Defines the result returned by the reusable dialog framework.
/// </summary>
/// <param name="Result">The selected result key.</param>
public sealed record DialogResultModel(string Result);
