using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Settings.Services;
using BookStore.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Media;

namespace BookStore.UI.Services;

/// <summary>
/// Applies the WPF resource dictionaries used by the selected theme.
/// </summary>
public class ThemeService : IThemeService
{
    private static readonly Uri LightThemeSource = new("/BookStore.UI;component/Themes/Light.xaml", UriKind.Relative);
    private static readonly Uri DarkThemeSource = new("/BookStore.UI;component/Themes/Dark.xaml", UriKind.Relative);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ThemeService> _logger;
    private string _currentTheme = ApplicationConstants.DefaultTheme;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeService"/> class.
    /// </summary>
    /// <param name="settingsService">Centralized settings service.</param>
    /// <param name="notifier">Settings change notifier.</param>
    /// <param name="logger">The logger.</param>
    public ThemeService(IServiceScopeFactory scopeFactory, ISettingsChangedNotifier notifier, ILogger<ThemeService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        notifier.SettingsChanged += OnSettingsChanged;
    }

    /// <inheritdoc />
    public string CurrentTheme => _currentTheme;

    /// <inheritdoc />
    public async Task ApplyConfiguredThemeAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var appearance = await settingsService.GetAsync<AppearanceSettingsDto>();
        var theme = string.IsNullOrWhiteSpace(appearance.Theme)
            ? ApplicationConstants.DefaultTheme
            : appearance.Theme;

        ApplyTheme(theme);
    }

    /// <inheritdoc />
    public Task ToggleThemeAsync()
    {
        ApplyTheme(string.Equals(_currentTheme, "Dark", StringComparison.OrdinalIgnoreCase) ? "Light" : "Dark");
        return Task.CompletedTask;
    }

    private void ApplyTheme(string theme)
    {
        var application = System.Windows.Application.Current;
        if (application is null)
        {
            return;
        }

        // Brushes and the resource dictionary are dispatcher-affine, and settings changes can
        // be raised from a background thread.
        if (!application.Dispatcher.CheckAccess())
        {
            application.Dispatcher.Invoke(() => ApplyTheme(theme));
            return;
        }

        var isDark = string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase);
        _currentTheme = isDark ? "Dark" : "Light";

        // Swap the whole palette dictionary rather than overwriting a handful of brushes in code.
        // The old approach reassigned ten keys and left every other brush -- input backgrounds, grid
        // rows, status colours -- at its light value, so dark mode rendered near-white text on
        // near-white fills and Dark.xaml was never loaded at all.
        var dictionaries = application.Resources.MergedDictionaries;
        var replacement = new ResourceDictionary { Source = isDark ? DarkThemeSource : LightThemeSource };
        var existing = dictionaries.FirstOrDefault(IsThemeDictionary);

        if (existing is null)
        {
            dictionaries.Add(replacement);
        }
        else
        {
            dictionaries[dictionaries.IndexOf(existing)] = replacement;
        }

        _logger.LogInformation("Theme applied: {Theme}", _currentTheme);
    }

    /// <summary>
    /// Reports whether a merged dictionary is one of the swappable theme palettes.
    /// </summary>
    private static bool IsThemeDictionary(ResourceDictionary dictionary)
    {
        var source = dictionary.Source?.OriginalString;
        return source is not null
            && (source.EndsWith("Themes/Light.xaml", StringComparison.OrdinalIgnoreCase)
                || source.EndsWith("Themes/Dark.xaml", StringComparison.OrdinalIgnoreCase));
    }

    private async void OnSettingsChanged(object? sender, SettingsChangedEvent e)
    {
        if (e.Category != SettingsCategory.Appearance)
        {
            return;
        }

        try
        {
            await ApplyConfiguredThemeAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to reapply the theme after an appearance settings change");
        }
    }
}
