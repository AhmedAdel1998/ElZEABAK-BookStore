using System.Windows.Input;
using System.Windows.Threading;
using BookStore.Application.Interfaces;
using BookStore.Shared.Models;
using BookStore.UI.Navigation;
using BookStore.UI.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookStore.UI.Services;

/// <summary>
/// Logs out the current user after configurable inactivity.
/// </summary>
public class SessionTimeoutService : ISessionTimeoutService
{
    private readonly IAuthenticationService _authenticationService;
    private readonly INavigationService _navigationService;
    private readonly ILogger<SessionTimeoutService> _logger;
    private readonly DispatcherTimer _timer;
    private readonly TimeSpan _timeout;
    private DateTimeOffset _lastActivity;
    private bool _isStarted;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionTimeoutService"/> class.
    /// </summary>
    public SessionTimeoutService(
        IAuthenticationService authenticationService,
        INavigationService navigationService,
        IOptions<ApplicationSettings> options,
        ILogger<SessionTimeoutService> logger)
    {
        _authenticationService = authenticationService;
        _navigationService = navigationService;
        _logger = logger;
        _timeout = TimeSpan.FromMinutes(options.Value.Authentication.SessionTimeoutMinutes);
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
}
