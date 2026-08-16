using System.Windows.Input;
using System.Windows.Threading;
using BookStore.Application.Features.Settings.DTOs;
using BookStore.Application.Features.Settings.Services;
using BookStore.Application.Interfaces;
using BookStore.UI.Navigation;
using BookStore.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BookStore.UI.Services;

/// <summary>
/// Logs out the current user after configurable inactivity.
/// </summary>
public class SessionTimeoutService : ISessionTimeoutService
{
    private readonly IAuthenticationService _authenticationService;
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionTimeoutService> _logger;
    private readonly DispatcherTimer _timer;
    private TimeSpan _timeout;
    private DateTimeOffset _lastActivity;
    private bool _isStarted;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionTimeoutService"/> class.
    /// </summary>
    public SessionTimeoutService(
        IAuthenticationService authenticationService,
        INavigationService navigationService,
        IServiceScopeFactory scopeFactory,
        ISettingsChangedNotifier notifier,
        ILogger<SessionTimeoutService> logger)
    {
        _authenticationService = authenticationService;
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _timeout = TimeSpan.FromMinutes(30);
        _ = RefreshSecuritySettingsAsync();
        notifier.SettingsChanged += async (_, change) =>
        {
            if (change.Category == SettingsCategory.Security)
            {
                await RefreshSecuritySettingsAsync();
            }
        };
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += OnTimerTick;
    }

    /// <inheritdoc />
    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        _lastActivity = DateTimeOffset.UtcNow;
        InputManager.Current.PreProcessInput += OnPreProcessInput;
        _timer.Start();
        _isStarted = true;
    }

    /// <inheritdoc />
    public void Stop()
    {
        if (!_isStarted)
        {
            return;
        }

        InputManager.Current.PreProcessInput -= OnPreProcessInput;
        _timer.Stop();
        _isStarted = false;
    }

    private void OnPreProcessInput(object sender, PreProcessInputEventArgs e)
    {
        if (e.StagingItem.Input is KeyboardEventArgs or MouseEventArgs)
        {
            _lastActivity = DateTimeOffset.UtcNow;
        }
    }

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        if (DateTimeOffset.UtcNow - _lastActivity < _timeout)
        {
            return;
        }

        Stop();
        await _authenticationService.LogoutAsync();
        await _navigationService.NavigateToAsync<LoginViewModel>();
        _logger.LogInformation("Session expired because of inactivity");
    }

    private async Task RefreshSecuritySettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settings = await scope.ServiceProvider.GetRequiredService<ISettingsService>().GetAsync<SecuritySettingsDto>(cancellationToken);
            _timeout = TimeSpan.FromMinutes(Math.Max(settings.SessionTimeout, 1));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Security settings could not be loaded for session timeout. Keeping current timeout.");
        }
    }
}
