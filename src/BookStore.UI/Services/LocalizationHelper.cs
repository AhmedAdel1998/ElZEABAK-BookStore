namespace BookStore.UI.Services;

/// <summary>
/// Gives code-behind access to literal translation without taking a constructor dependency.
/// </summary>
/// <remarks>
/// WPF instantiates views with a parameterless constructor, so a view's code-behind cannot receive
/// the localization service through dependency injection. The service is registered here once during
/// startup, alongside the equivalent hooks for the view model base class and the binding converter.
/// </remarks>
public static class LocalizationHelper
{
    private static ILocalizationService? _localizationService;

    /// <summary>Supplies the localization service. Called once during startup.</summary>
    /// <param name="localizationService">The application localization service.</param>
    public static void UseLocalization(ILocalizationService localizationService) =>
        _localizationService = localizationService;

    /// <summary>Translates an authored English literal, returning it unchanged when unknown.</summary>
    /// <param name="text">The authored text.</param>
    /// <returns>The translated text.</returns>
    public static string Translate(string text) =>
        string.IsNullOrEmpty(text) ? text : _localizationService?.TranslateLiteral(text) ?? text;
}
