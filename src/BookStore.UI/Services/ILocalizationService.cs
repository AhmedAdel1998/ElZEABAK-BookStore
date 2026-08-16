using System.Globalization;
using System.Windows;

namespace BookStore.UI.Services;

/// <summary>
/// Applies the configured UI culture and exposes translated interface text.
/// </summary>
public interface ILocalizationService
{
    event EventHandler? CultureChanged;

    string CurrentLanguage { get; }
    bool IsRightToLeft { get; }
    FlowDirection FlowDirection { get; }

    Task ApplyConfiguredCultureAsync();
    void ApplyCulture(string language);
    Task ToggleLanguageAsync();
    string T(string key);
}
