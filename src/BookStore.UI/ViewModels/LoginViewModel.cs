using BookStore.Application.Features.Authentication.DTOs;
using BookStore.Application.Interfaces;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Login screen view model.
/// </summary>
public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authenticationService;
    private readonly INavigationService _navigationService;
    private readonly IApplicationShutdownService _shutdownService;
    private readonly ISessionTimeoutService _sessionTimeoutService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool rememberMe;

    [ObservableProperty]
    private bool isPasswordVisible;

    [ObservableProperty]
    private string validationMessage = string.Empty;

    [ObservableProperty]
    private string languageToggleText = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginViewModel"/> class.
    /// </summary>
    public LoginViewModel(
        IAuthenticationService authenticationService,
        INavigationService navigationService,
        IApplicationShutdownService shutdownService,
        ISessionTimeoutService sessionTimeoutService,
        ILocalizationService localizationService)
    {
        _authenticationService = authenticationService;
        _navigationService = navigationService;
        _shutdownService = shutdownService;
        _sessionTimeoutService = sessionTimeoutService;
        _localizationService = localizationService;
        _localizationService.CultureChanged += OnCultureChanged;
        Title = _localizationService.T("Auth.SignIn");
        UpdateLanguageToggleText();
    }

    /// <summary>
    /// Attempts to sign in with the current credentials.
    /// </summary>
    [RelayCommand]
    private async Task LoginAsync()
    {
        IsBusy = true;
        ValidationMessage = string.Empty;

        try
        {
            var result = await _authenticationService.LoginAsync(new LoginRequest
            {
                Username = Username,
                Password = Password,
                RememberMe = RememberMe
            });

            if (!result.Succeeded)
            {
                ValidationMessage = result.Errors.FirstOrDefault()?.Message ?? "Login failed.";
                return;
            }

            Password = string.Empty;
            _sessionTimeoutService.Start();
            await _navigationService.NavigateToAsync<AuthenticatedHomeViewModel>();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Exits the application.
    /// </summary>
    [RelayCommand]
    private void Exit()
    {
        _shutdownService.Shutdown();
    }

    [RelayCommand]
    private async Task ToggleLanguageAsync()
    {
        await _localizationService.ToggleLanguageAsync();
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        Title = _localizationService.T("Auth.SignIn");
        UpdateLanguageToggleText();
    }

    private void UpdateLanguageToggleText()
    {
        LanguageToggleText = _localizationService.IsRightToLeft
            ? _localizationService.T("Language.SwitchToEnglish")
            : _localizationService.T("Language.SwitchToArabic");
    }
}
