using BookStore.Application.Interfaces;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// First-run administrator setup screen view model.
/// </summary>
public partial class FirstRunSetupViewModel : BaseViewModel
{
    private readonly IFirstRunSetupService _setupService;
    private readonly INavigationService _navigationService;
    private readonly IApplicationShutdownService _shutdownService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty] private string username = "admin";
    [ObservableProperty] private string fullName = "System Administrator";
    [ObservableProperty] private string email = "admin@bookstore.local";
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string confirmPassword = string.Empty;
    [ObservableProperty] private string validationMessage = string.Empty;
    [ObservableProperty] private string languageToggleText = string.Empty;

    public FirstRunSetupViewModel(IFirstRunSetupService setupService, INavigationService navigationService, IApplicationShutdownService shutdownService, ILocalizationService localizationService)
    {
        _setupService = setupService;
        _navigationService = navigationService;
        _shutdownService = shutdownService;
        _localizationService = localizationService;
        _localizationService.CultureChanged += OnCultureChanged;
        Title = _localizationService.T("Setup.CreateAdministrator");
        UpdateLanguageToggleText();
    }

    [RelayCommand]
    private async Task CreateAdministratorAsync()
    {
        IsBusy = true;
        ValidationMessage = string.Empty;
        try
        {
            var result = await _setupService.CreateAdministratorAsync(Username, FullName, Email, Password, ConfirmPassword);
            if (!result.Succeeded)
            {
                ValidationMessage = result.ValidationErrors.FirstOrDefault()?.Message
                    ?? result.Errors.FirstOrDefault()?.Message
                    ?? "Administrator could not be created.";
                return;
            }

            Password = string.Empty;
            ConfirmPassword = string.Empty;
            await _navigationService.NavigateToAsync<LoginViewModel>();
        }
        finally
        {
            IsBusy = false;
        }
    }

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

    partial void OnPasswordChanged(string value) => UpdatePasswordGuidance();

    partial void OnConfirmPasswordChanged(string value) => UpdatePasswordGuidance();

    private void UpdatePasswordGuidance()
    {
        if (string.IsNullOrEmpty(Password) && string.IsNullOrEmpty(ConfirmPassword))
        {
            ValidationMessage = string.Empty;
            return;
        }

        var missingRequirements = new List<string>();
        if (Password.Length < 8)
        {
            missingRequirements.Add("8 characters");
        }

        if (!Password.Any(char.IsUpper))
        {
            missingRequirements.Add("uppercase letter");
        }

        if (!Password.Any(char.IsLower))
        {
            missingRequirements.Add("lowercase letter");
        }

        if (!Password.Any(char.IsDigit))
        {
            missingRequirements.Add("number");
        }

        if (Password.All(char.IsLetterOrDigit))
        {
            missingRequirements.Add("special character");
        }

        if (missingRequirements.Count > 0)
        {
            ValidationMessage = $"Password needs: {string.Join(", ", missingRequirements)}. Example: Admin123!";
            return;
        }

        ValidationMessage = string.Equals(Password, ConfirmPassword, StringComparison.Ordinal)
            ? string.Empty
            : "Passwords must match.";
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        Title = _localizationService.T("Setup.CreateAdministrator");
        UpdateLanguageToggleText();
    }

    private void UpdateLanguageToggleText()
    {
        LanguageToggleText = _localizationService.IsRightToLeft
            ? _localizationService.T("Language.SwitchToEnglish")
            : _localizationService.T("Language.SwitchToArabic");
    }
}
