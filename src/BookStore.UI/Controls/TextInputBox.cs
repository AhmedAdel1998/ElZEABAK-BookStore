using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BookStore.UI.Controls;

/// <summary>
/// Provides common text input behavior for reusable enterprise fields.
/// </summary>
public class TextInputBox : TextBox
{
    /// <summary>
    /// Identifies the <see cref="Placeholder"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(TextInputBox), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Identifies the <see cref="Icon"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(TextInputBox), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Identifies the <see cref="ErrorMessage"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(TextInputBox), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets placeholder text shown by the input template.
    /// </summary>
    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>
    /// Gets or sets an icon shown by the input template.
    /// </summary>
    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the validation error message shown by the input template.
    /// </summary>
    public string ErrorMessage
    {
        get => (string)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }
}

/// <summary>
/// Text input optimized for searching.
/// </summary>
public class SearchBox : TextInputBox
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchBox"/> class.
    /// </summary>
    public SearchBox()
    {
        Icon = "Search";
        Placeholder = "Search";
    }
}

/// <summary>
/// Numeric text input that accepts digits only.
/// </summary>
public class NumericTextBox : TextInputBox
{
    private static readonly Regex NumericPattern = new("^[0-9]+$", RegexOptions.Compiled);

    /// <inheritdoc />
    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        e.Handled = !NumericPattern.IsMatch(e.Text);
        base.OnPreviewTextInput(e);
    }
}

/// <summary>
/// Currency text input that accepts localized decimal values.
/// </summary>
public class CurrencyTextBox : TextInputBox
{
    /// <inheritdoc />
    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        var candidate = Text.Remove(SelectionStart, SelectionLength).Insert(SelectionStart, e.Text);
        e.Handled = !IsAcceptable(candidate);
        base.OnPreviewTextInput(e);
    }

    /// <summary>
    /// Accepts a partially typed amount under either the current culture or the invariant
    /// convention. Numeric keypads emit "." regardless of the UI language, and Arabic cultures
    /// declare U+066B as their decimal separator, so a culture-only check rejects the decimal
    /// point outright and makes fractional prices impossible to enter.
    /// </summary>
    private static bool IsAcceptable(string candidate)
    {
        if (string.IsNullOrEmpty(candidate))
        {
            return true;
        }

        // A lone or trailing separator is a valid intermediate state while typing.
        var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        foreach (var terminator in new[] { ".", separator })
        {
            if (candidate == terminator || candidate == "-" || candidate == "-" + terminator)
            {
                return true;
            }

            if (candidate.EndsWith(terminator, StringComparison.Ordinal))
            {
                candidate = candidate[..^terminator.Length];
                break;
            }
        }

        return decimal.TryParse(candidate, NumberStyles.Number, CultureInfo.CurrentCulture, out _)
            || decimal.TryParse(candidate, NumberStyles.Number, CultureInfo.InvariantCulture, out _);
    }
}

/// <summary>
/// Barcode text input.
/// </summary>
public class BarcodeTextBox : TextInputBox
{
}

/// <summary>
/// ISBN text input.
/// </summary>
public class ISBNTextBox : TextInputBox
{
}

/// <summary>
/// Email text input.
/// </summary>
public class EmailTextBox : TextInputBox
{
}

/// <summary>
/// Phone number text input.
/// </summary>
public class PhoneTextBox : TextInputBox
{
}
/// <summary>
/// Theme-aware date picker placeholder for module forms.
/// </summary>
public class AppDatePicker : DatePicker
{
}

/// <summary>
/// Theme-aware combo box placeholder for module forms.
/// </summary>
public class AppComboBox : ComboBox
{
}
