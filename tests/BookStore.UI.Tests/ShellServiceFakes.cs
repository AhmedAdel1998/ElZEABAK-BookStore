using System.Collections.ObjectModel;
using System.Windows;
using BookStore.Application.Features.Authentication.DTOs;
using BookStore.Application.Features.Authentication.Responses;
using BookStore.Application.Interfaces;
using BookStore.Shared.Results;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using BookStore.UI.ViewModels;

namespace BookStore.UI.Tests;

/// <summary>
/// Records where the shell asked to navigate, without building the destination view model.
/// </summary>
internal sealed class FakeShellNavigationService : IShellNavigationService
{
    public List<(Type ViewModel, string Breadcrumb)> Navigations { get; } = [];

    public BaseViewModel? CurrentViewModel => null;

    public string Breadcrumb => Navigations.Count == 0 ? string.Empty : Navigations[^1].Breadcrumb;

    public Task NavigateToAsync<TViewModel>()
        where TViewModel : BaseViewModel => NavigateToAsync<TViewModel>(string.Empty);

    public Task NavigateToAsync<TViewModel>(string breadcrumb)
        where TViewModel : BaseViewModel
    {
        Navigations.Add((typeof(TViewModel), breadcrumb));
        return Task.CompletedTask;
    }

    public Task GoBackAsync() => Task.CompletedTask;

    public Task GoForwardAsync() => Task.CompletedTask;

    public Task RefreshAsync() => Task.CompletedTask;

    public void ClearHistory()
    {
    }
}

internal sealed class FakeApplicationNavigationService : INavigationService
{
    public List<Type> Navigations { get; } = [];

    public BaseViewModel? CurrentViewModel => null;

    public Task NavigateToAsync<TViewModel>()
        where TViewModel : BaseViewModel
    {
        Navigations.Add(typeof(TViewModel));
        return Task.CompletedTask;
    }

    public Task GoBackAsync() => Task.CompletedTask;

    public Task GoForwardAsync() => Task.CompletedTask;

    public Task RefreshAsync() => Task.CompletedTask;

    public void ClearHistory()
    {
    }
}

/// <summary>
/// Grants exactly the permissions a test hands it.
/// </summary>
internal sealed class FakeAuthorizationService : IAuthorizationService
{
    private readonly HashSet<string> _granted;

    public FakeAuthorizationService(params string[] granted) => _granted = [.. granted];

    public bool HasPermission(string permission) => _granted.Contains(permission);

    public bool HasPermissions(params string[] permissions) => permissions.All(_granted.Contains);

    public bool HasRole(string role) => true;

    public bool CanAccess(string requiredPermission) => HasPermission(requiredPermission);
}

/// <summary>
/// Returns keys unchanged so tests can assert on them, except navigation labels, which are
/// shortened to the words the real dictionary uses ("Nav.Products" becomes "Products").
/// </summary>
internal sealed class FakeLocalizationService : ILocalizationService
{
    public event EventHandler? CultureChanged;

    public string CurrentLanguage => "en-US";

    public bool IsRightToLeft => false;

    public FlowDirection FlowDirection => FlowDirection.LeftToRight;

    public Task ApplyConfiguredCultureAsync() => Task.CompletedTask;

    public void ApplyCulture(string language) => CultureChanged?.Invoke(this, EventArgs.Empty);

    public Task ToggleLanguageAsync() => Task.CompletedTask;

    public string T(string key) => key.StartsWith("Nav.", StringComparison.Ordinal) ? key["Nav.".Length..] : key;

    public string TranslateLiteral(string text) => text;
}

internal sealed class FakeLoadingService : ILoadingService
{
    public event EventHandler? StateChanged;

    public bool IsLoading { get; private set; }

    public string LoadingText { get; private set; } = string.Empty;

    public void Show(string text)
    {
        IsLoading = true;
        LoadingText = text;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Hide()
    {
        IsLoading = false;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}

internal sealed class FakeNotificationService : INotificationService
{
    public ObservableCollection<NotificationMessage> Notifications { get; } = [];

    public void Show(string title, string message, NotificationSeverity severity) =>
        Notifications.Add(new NotificationMessage { Title = title, Message = message, Severity = severity });

    public void Show(string title, string message, NotificationSeverity severity, TimeSpan duration) =>
        Show(title, message, severity);

    public void Clear() => Notifications.Clear();
}

internal sealed class FakeCurrentUserService : ICurrentUserService
{
    public bool IsAuthenticated => true;

    public Guid? UserId { get; } = Guid.NewGuid();

    public string? Username => "cashier";

    public string? FullName => "Test Cashier";

    public string? Role => "Cashier";

    public IReadOnlyCollection<string> Permissions => [];

    public Guid? SessionId { get; } = Guid.NewGuid();

    public DateTimeOffset? LoginTime => DateTimeOffset.UtcNow;

    public void SignIn(UserSessionSnapshot session)
    {
    }

    public void SignOut()
    {
    }
}

internal sealed class FakeAuthenticationService : IAuthenticationService
{
    public Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<AuthenticationResult> TryRestoreRememberedSessionAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task LogoutAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<OperationResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

internal sealed class FakeSessionTimeoutService : ISessionTimeoutService
{
    public void Start()
    {
    }

    public void Stop()
    {
    }
}

internal sealed class FakeThemeService : IThemeService
{
    public string CurrentTheme => "Light";

    public Task ApplyConfiguredThemeAsync() => Task.CompletedTask;

    public Task ToggleThemeAsync() => Task.CompletedTask;
}
