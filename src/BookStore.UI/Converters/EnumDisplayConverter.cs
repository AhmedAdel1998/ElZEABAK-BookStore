using System.Globalization;
using System.Text;
using System.Windows.Data;
using BookStore.UI.Services;

namespace BookStore.UI.Converters;

/// <summary>
/// Displays an enum value as readable, translated text.
/// </summary>
/// <remarks>
/// An enum bound straight to a ComboBox renders through <see cref="object.ToString"/>, so payment
/// methods and inventory transaction types showed their identifier verbatim -- "MobileWallet" ran
/// together in English and stayed English in Arabic. The tree localizer cannot help: it skips
/// containers generated from a bound collection, because a data row that happened to match a
/// dictionary entry would otherwise be rewritten. Splitting the identifier into words and
/// translating the result fixes both languages at the binding site.
/// </remarks>
public sealed class EnumDisplayConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Enum enumValue)
        {
            return value;
        }

        return LocalizationHelper.Translate(SplitPascalCase(enumValue.ToString()));
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("EnumDisplayConverter is one-way.");

    /// <summary>Turns "MobileWallet" into "Mobile Wallet", leaving single words untouched.</summary>
    private static string SplitPascalCase(string name)
    {
        var builder = new StringBuilder(name.Length + 4);
        for (var index = 0; index < name.Length; index++)
        {
            if (index > 0 && char.IsUpper(name[index]) && !char.IsUpper(name[index - 1]))
            {
                builder.Append(' ');
            }

            builder.Append(name[index]);
        }

        return builder.ToString();
    }
}
