using System.Globalization;
using System.Windows.Data;
using BookStore.UI.Icons;

namespace BookStore.UI.Converters;

/// <summary>
/// Converts a semantic icon name into its Segoe MDL2 Assets glyph for display in a control template.
/// </summary>
public sealed class IconGlyphConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        IconGlyphs.Resolve(value as string);

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("IconGlyphConverter is one-way.");
}
