using System.Globalization;
using System.Windows.Data;
using BookStore.UI.Services;

namespace BookStore.UI.Converters;

/// <summary>
/// Translates an authored English literal supplied by a view model into the current language.
/// </summary>
/// <remarks>
/// The tree localizer never writes a property that carries a binding, because doing so would freeze
/// live data. That left every English literal a view model assigned to a bound text property showing
/// verbatim -- empty-state messages, inline validation errors, module descriptions -- even when the
/// dictionary already contained the translation. Applying this at the binding site translates on
/// display and leaves the view model's value untouched. Business data (a product title, a customer
/// name) passes through unchanged because only whole-string dictionary matches are translated.
/// </remarks>
public sealed class LocalizeConverter : IValueConverter
{
    private static ILocalizationService? _localizationService;

    /// <summary>Supplies the localization service. Called once during startup.</summary>
    /// <param name="localizationService">The application localization service.</param>
    public static void UseLocalization(ILocalizationService localizationService) =>
        _localizationService = localizationService;

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text || string.IsNullOrEmpty(text))
        {
            return value;
        }

        return _localizationService?.TranslateLiteral(text) ?? text;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("LocalizeConverter is one-way.");
}
