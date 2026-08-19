using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BookStore.UI.Converters;

/// <summary>
/// Shows an element only when the bound width reaches the threshold passed as the converter
/// parameter, and collapses it below that.
/// </summary>
/// <remarks>
/// WPF has no adaptive layout triggers, so decorative side panels used to be clipped on narrow
/// windows instead of stepping out of the way. Collapsing them keeps the primary content -- the
/// sign-in form -- correctly sized at 1366x768 and at the window minimum.
/// </remarks>
public sealed class MinWidthToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double actualWidth)
        {
            return Visibility.Visible;
        }

        var threshold = parameter is string text && double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0d;

        return actualWidth >= threshold ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("MinWidthToVisibilityConverter is one-way.");
}
