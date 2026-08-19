using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BookStore.UI.Converters;

/// <summary>
/// Converts <see langword="true"/> to <see cref="Visibility.Collapsed"/> and anything else to
/// <see cref="Visibility.Visible"/>.
/// </summary>
/// <remarks>
/// Used to show exactly one of a paired <c>PasswordBox</c> and plain <c>TextBox</c>. Without it the
/// masked field stayed visible and hit-testable underneath the revealed one, so it remained in the
/// tab order and a user could type into a field they could not see.
/// </remarks>
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Visibility.Collapsed;
    }
}
