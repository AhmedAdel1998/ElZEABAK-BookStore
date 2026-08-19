using System.Globalization;
using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Settings.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BookStore.UI.Services;

/// <inheritdoc cref="ICurrencyFormatterService" />
public sealed class CurrencyFormatterService : ICurrencyFormatterService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CurrencyFormatterService> _logger;
    private CurrencySettingsDto _settings = new();

    /// <summary>Initializes a new instance of the <see cref="CurrencyFormatterService"/> class.</summary>
    public CurrencyFormatterService(IServiceScopeFactory scopeFactory, ISettingsChangedNotifier notifier, ILogger<CurrencyFormatterService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        notifier.SettingsChanged += OnSettingsChanged;
    }

    /// <inheritdoc />
    public async Task ApplyConfiguredCurrencyAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        _settings = await settingsService.GetAsync<CurrencySettingsDto>();
        _logger.LogInformation("Currency formatting applied: {Symbol} ({Position}, {DecimalPlaces} dp)", _settings.CurrencySymbol, _settings.CurrencyPosition, _settings.DecimalPlaces);
    }

    /// <inheritdoc />
    public string Format(decimal amount)
    {
        var decimalPlaces = Math.Clamp(_settings.DecimalPlaces, 0, 4);
        var number = amount.ToString("N" + decimalPlaces.ToString(CultureInfo.InvariantCulture), CultureInfo.CurrentCulture);
        var symbol = string.IsNullOrWhiteSpace(_settings.CurrencySymbol) ? _settings.CurrencyCode : _settings.CurrencySymbol;

        return string.Equals(_settings.CurrencyPosition, "After", StringComparison.OrdinalIgnoreCase)
            ? $"{number} {symbol}"
            : $"{symbol} {number}";
    }

    private async void OnSettingsChanged(object? sender, SettingsChangedEvent e)
    {
        if (e.Category != SettingsCategory.Currency)
        {
            return;
        }

        try
        {
            await ApplyConfiguredCurrencyAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to reapply currency formatting after a settings change");
        }
    }
}
