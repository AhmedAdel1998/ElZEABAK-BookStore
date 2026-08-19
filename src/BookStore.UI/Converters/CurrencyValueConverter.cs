using System.Globalization;
using System.Windows.Data;
using BookStore.UI.Services;

namespace BookStore.UI.Converters;

/// <summary>
/// Formats a bound <see cref="decimal"/> using the store's configured currency instead of
/// <c>StringFormat={}{0:C}</c>, which renders the CLR's current-culture symbol (a literal "$" for
/// an EGP bookstore) regardless of what <c>Settings &gt; Currency</c> is set to.
/// </summary>
/// <remarks>
/// WPF instantiates converters from XAML with no constructor arguments, so this cannot take
/// <see cref="ICurrencyFormatterService"/> through DI. <see cref="FormatterService"/> is instead
/// assigned once, from the composition root in App.xaml.cs right after the host is built - the
/// same "assign a static from the composition root" shape this codebase already uses for
/// resources that XAML needs before dependency injection can reach it (compare
/// <c>LocalizationService.ApplyCulture</c> writing directly into <c>Application.Resources</c>).
/// </remarks>
public sealed class CurrencyValueConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets the formatter used to render bound amounts. Set once during application
    /// startup; null only before that point, in which case a plain invariant number is shown
    /// rather than crashing a binding.
    /// </summary>
    public static ICurrencyFormatterService? FormatterService { get; set; }

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            decimal amount => FormatterService?.Format(amount) ?? amount.ToString("N2", CultureInfo.CurrentCulture),
            _ => value ?? string.Empty
        };

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("Currency values are display-only; this converter does not support two-way binding.");
}
