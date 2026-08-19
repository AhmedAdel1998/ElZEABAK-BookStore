using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BookStore.UI.Converters;

/// <summary>
/// Shows an element only when a bound collection count is zero.
/// </summary>
/// <remarks>
/// Used for empty states. The four list views that had one relied on a DataTrigger against a named
/// grid's <c>HasItems</c>; binding the count instead means a view needs no <c>x:Name</c> and the
/// same markup works for every grid.
/// </remarks>
public sealed class EmptyCollectionToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int count && count == 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("EmptyCollectionToVisibilityConverter is one-way.");
}
