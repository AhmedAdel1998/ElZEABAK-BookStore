using BookStore.Application.Features.Authentication.DTOs;
using BookStore.Application.Interfaces;
using BookStore.UI.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Change password screen view model.
/// </summary>
public partial class ChangePasswordViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IShellNavigationService _navigationService;

    [ObservableProperty]
    private string currentPassword = string.Empty;

    [ObservableProperty]
    private string newPassword = string.Empty;

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private string validationMessage = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePasswordViewModel"/> class.
    /// </summary>
    public ChangePasswordViewModel(IAuthenticationService authenticationService, IShellNavigationService navigationService)
    {
        _authenticationService = authenticationService;
        _navigationService = navigationService;
        Title = "Change Password";
    }

    /// <summary>
    /// Changes the current user's password.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        IsBusy = true;
        ValidationMessage = string.Empty;

        try
        {
            var result = await _authenticationService.ChangePasswordAsync(new ChangePasswordRequest
            {
                CurrentPassword = CurrentPassword,
                NewPassword = NewPassword,
                ConfirmPassword = ConfirmPassword
            });

            if (!result.Succeeded)
            {
                ValidationMessage = result.ValidationErrors.FirstOrDefault()?.Message
                    ?? result.Errors.FirstOrDefault()?.Message
                    ?? "Password change failed.";
                return;
            }

            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            await _navigationService.NavigateToAsync<DashboardViewModel>("Dashboard");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Returns to the dashboard screen.
    /// </summary>
    [RelayCommand]
    private Task CancelAsync()
    {
        return _navigationService.NavigateToAsync<DashboardViewModel>("Dashboard");
    }
}
