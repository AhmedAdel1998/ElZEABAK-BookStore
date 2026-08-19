using System.Windows;

namespace BookStore.UI.Dialogs;

/// <summary>
/// Themed modal dialog used for information, warnings, errors and confirmations.
/// </summary>
public partial class AppDialogWindow : Window
{
    /// <summary>Initializes a new instance of the <see cref="AppDialogWindow"/> class.</summary>
    public AppDialogWindow()
    {
        InitializeComponent();
    }

    /// <summary>Gets whether the user chose the affirmative action.</summary>
    public bool Confirmed { get; private set; }

    /// <summary>
    /// Configures the dialog's content and buttons.
    /// </summary>
    /// <param name="title">The dialog heading.</param>
    /// <param name="message">The dialog body.</param>
    /// <param name="glyph">A Segoe MDL2 glyph for the leading icon.</param>
    /// <param name="glyphBrushKey">Resource key of the brush used for the icon.</param>
    /// <param name="primaryText">Label for the affirmative button.</param>
    /// <param name="secondaryText">Label for the dismissive button, or null to hide it.</param>
    public void Configure(string title, string message, string glyph, string glyphBrushKey, string primaryText, string? secondaryText)
    {
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        IconGlyph.Text = glyph;
        IconGlyph.SetResourceReference(ForegroundProperty, glyphBrushKey);
        PrimaryAction.Content = primaryText;

        if (string.IsNullOrEmpty(secondaryText))
        {
            SecondaryAction.Visibility = Visibility.Collapsed;
        }
        else
        {
            SecondaryAction.Content = secondaryText;
        }
    }

    private void OnPrimaryClicked(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        DialogResult = true;
    }

    private void OnSecondaryClicked(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        DialogResult = false;
    }
}
