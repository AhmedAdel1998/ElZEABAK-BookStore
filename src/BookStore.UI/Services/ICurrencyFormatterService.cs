namespace BookStore.UI.Services;

/// <summary>
/// Formats monetary amounts using the store's configured currency, decimal places, and symbol
/// position, instead of the CLR's built-in "C" format string.
/// </summary>
/// <remarks>
/// <see cref="decimal"/>.ToString("C") always renders the CURRENT CULTURE's currency symbol -
/// under en-US that is "$", regardless of what the store actually sells in. Every "{0:C}" binding
/// in the shell rendered a dollar sign for an EGP bookstore in both English and Arabic mode. This
/// service is the single source of truth for money display so <c>Settings &gt; Currency</c>
/// actually controls what appears on screen.
/// </remarks>
public interface ICurrencyFormatterService
{
    /// <summary>Loads the configured currency settings so <see cref="Format"/> reflects them.</summary>
    Task ApplyConfiguredCurrencyAsync();

    /// <summary>Formats an amount using the configured symbol, position, and decimal places.</summary>
    string Format(decimal amount);
}
