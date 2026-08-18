using System.Windows;

namespace BookStore.UI.Dialogs;

/// <summary>
/// Displays an unhandled-exception notice with a correlation id and copyable technical detail.
/// </summary>
/// <remarks>
/// Every unhandled exception used to surface as a bare "An unexpected error occurred" message box
/// with nothing to act on: no way to tell support which failure it was, and no detail to attach to
/// a report. This window pairs a friendly message with the same correlation id the exception was
/// logged under, plus a one-click copy of the full exception for a bug report - without expanding
/// the panel, the dialog reads exactly as unobtrusively as the message box it replaces.
/// </remarks>
public partial class CrashDialogWindow : Window
{
    /// <summary>Initializes a new instance of the <see cref="CrashDialogWindow"/> class.</summary>
    /// <param name="model">The crash details to display.</param>
    public CrashDialogWindow(CrashDialogModel model)
    {
        InitializeComponent();
        DataContext = model;
    }

    private void OnCopyDetailsClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not CrashDialogModel model)
        {
            return;
        }

        try
        {
            Clipboard.SetText(model.Details);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // The clipboard can be transiently locked by another process; this is a convenience
            // action, not a critical path, so a failed copy should not raise a second error dialog
            // on top of the one already being shown.
        }
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e) => Close();
}
