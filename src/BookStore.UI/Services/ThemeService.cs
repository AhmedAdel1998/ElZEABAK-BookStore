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
        _currentTheme = string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase) ? "Dark" : "Light";
        var resources = System.Windows.Application.Current.Resources;

        if (_currentTheme == "Dark")
        {
            resources["AppBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(17, 24, 39));
            resources["SurfaceBrush"] = new SolidColorBrush(Color.FromRgb(31, 41, 55));
            resources["SurfaceAltBrush"] = new SolidColorBrush(Color.FromRgb(39, 52, 73));
            resources["PrimaryTextBrush"] = new SolidColorBrush(Color.FromRgb(243, 244, 246));
            resources["SecondaryTextBrush"] = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            resources["MutedTextBrush"] = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(55, 65, 81));
            resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(96, 165, 250));
            resources["AccentHoverBrush"] = new SolidColorBrush(Color.FromRgb(147, 197, 253));
            resources["OverlayBrush"] = new SolidColorBrush(Color.FromArgb(179, 0, 0, 0));
        }
        else
        {
            resources["AppBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(246, 247, 249));
            resources["SurfaceBrush"] = new SolidColorBrush(Colors.White);
            resources["SurfaceAltBrush"] = new SolidColorBrush(Color.FromRgb(241, 245, 249));
            resources["PrimaryTextBrush"] = new SolidColorBrush(Color.FromRgb(31, 41, 51));
            resources["SecondaryTextBrush"] = new SolidColorBrush(Color.FromRgb(100, 116, 139));
            resources["MutedTextBrush"] = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(214, 218, 225));
            resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(37, 99, 235));
            resources["AccentHoverBrush"] = new SolidColorBrush(Color.FromRgb(29, 78, 216));
            resources["OverlayBrush"] = new SolidColorBrush(Color.FromArgb(153, 17, 24, 39));
        }

        _logger.LogInformation("Theme applied: {Theme}", _currentTheme);
    }

    private async void OnSettingsChanged(object? sender, SettingsChangedEvent e)
    {
        if (e.Category == SettingsCategory.Appearance)
        {
            await ApplyConfiguredThemeAsync();
        }
    }
}
