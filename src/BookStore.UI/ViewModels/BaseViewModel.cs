using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Base class for all presentation view models.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Title"/> is stored in the language it was authored in and translated on read. View
/// models assign it as an English literal (<c>Title = "Products"</c>) and views bind it to a header,
/// but the tree localizer deliberately skips bound properties -- so 44 page titles that were already
/// present in the Arabic dictionary still displayed in English. Translating on read fixes them all at
/// once and keeps working when the language is switched at runtime.
/// </para>
/// <para>
/// The localization service is supplied once at startup rather than injected into every view model,
/// which would mean touching more than forty constructors for a purely presentational concern.
/// </para>
/// </remarks>
public abstract partial class BaseViewModel : ObservableObject, IDisposable
{
    private static ILocalizationService? _localizationService;

    private string _titleSource = string.Empty;
    private bool _subscribed;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="BaseViewModel"/> class.</summary>
    protected BaseViewModel()
    {
        if (_localizationService is not null)
        {
            _localizationService.CultureChanged += OnCultureChanged;
            _subscribed = true;
        }
    }

    [ObservableProperty]
    private bool isBusy;

    /// <summary>
    /// Gets or sets the page title. The value assigned is kept verbatim; the value read is translated
    /// into the current language.
    /// </summary>
    public string Title
    {
        get => Localize(_titleSource);
        set
        {
            if (string.Equals(_titleSource, value, StringComparison.Ordinal))
            {
                return;
            }

            _titleSource = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Supplies the localization service used by every view model. Called once during startup.
    /// </summary>
    /// <param name="localizationService">The application localization service.</param>
    public static void UseLocalization(ILocalizationService localizationService) =>
        _localizationService = localizationService;

    /// <summary>
    /// Translates an authored English literal into the current language, returning it unchanged when
    /// there is no translation for it.
    /// </summary>
    /// <param name="text">The authored text.</param>
    /// <returns>The translated text.</returns>
    protected static string Localize(string? text) =>
        string.IsNullOrEmpty(text) ? string.Empty : _localizationService?.TranslateLiteral(text) ?? text;

    /// <summary>
    /// Raises change notifications for properties whose displayed value depends on the language.
    /// Override to include a screen's own localized properties, and call the base implementation.
    /// </summary>
    protected virtual void OnLanguageChanged() => OnPropertyChanged(nameof(Title));

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases resources held by this view model.</summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (disposing && _subscribed && _localizationService is not null)
        {
            _localizationService.CultureChanged -= OnCultureChanged;
            _subscribed = false;
        }
    }

    private void OnCultureChanged(object? sender, EventArgs e) => OnLanguageChanged();
}
