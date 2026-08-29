using System.Collections.ObjectModel;
using BookStore.Application.Features.Administration;
using BookStore.Application.Interfaces;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>Manages user accounts, role assignments, status, lockouts, and password resets.</summary>
public partial class UsersViewModel : BaseViewModel
{
    private readonly AdministrationService _administrationService;
    private readonly INotificationService _notificationService;
    private readonly IConfirmationDialogService _confirmationService;

    [ObservableProperty] private UserAdministrationDto? selectedUser;
    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string fullName = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private Guid? selectedRoleId;
    [ObservableProperty] private string newPassword = string.Empty;
    [ObservableProperty] private bool isEditing;

    /// <summary>Initializes the user administration view model.</summary>
    public UsersViewModel(AdministrationService administrationService, INotificationService notificationService, IConfirmationDialogService confirmationService)
    {
        _administrationService = administrationService;
        _notificationService = notificationService;
        _confirmationService = confirmationService;
        Title = "Users";
        _ = LoadAsync();
    }

    /// <summary>Gets user rows.</summary>
    public ObservableCollection<UserAdministrationDto> Users { get; } = [];

    /// <summary>Gets roles available for assignment.</summary>
    public ObservableCollection<RoleAdministrationDto> Roles { get; } = [];

    partial void OnSelectedUserChanged(UserAdministrationDto? value)
    {
        if (value is null) return;
        IsEditing = true;
        Username = value.Username;
        FullName = value.FullName;
        Email = value.Email ?? string.Empty;
        SelectedRoleId = value.RoleId;
        NewPassword = string.Empty;
        ToggleActiveCommand.NotifyCanExecuteChanged();
        ResetPasswordCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Reloads users and roles.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var users = await _administrationService.GetUsersAsync();
            var roles = await _administrationService.GetRolesAsync();
            if (!users.IsSuccess || users.Value is null || !roles.IsSuccess || roles.Value is null)
            {
                _notificationService.Show("Users", users.Error ?? roles.Error ?? "Unable to load user administration.", NotificationSeverity.Error);
                return;
            }
            Users.Clear();
            foreach (var user in users.Value) Users.Add(user);
            Roles.Clear();
            foreach (var role in roles.Value) Roles.Add(role);
        }
        catch (Exception ex)
        {
            _notificationService.Show("Users", $"Unable to load user administration: {ex.Message}", NotificationSeverity.Error);
        }
        finally { IsBusy = false; }
    }

    /// <summary>Starts a new user form.</summary>
    [RelayCommand]
    private void New()
    {
        SelectedUser = null;
        IsEditing = false;
        Username = string.Empty;
        FullName = string.Empty;
        Email = string.Empty;
        SelectedRoleId = Roles.FirstOrDefault()?.Id;
        NewPassword = string.Empty;
        ToggleActiveCommand.NotifyCanExecuteChanged();
        ResetPasswordCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Creates or updates the form user.</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!SelectedRoleId.HasValue)
        {
            _notificationService.Show("Users", "Select a role.", NotificationSeverity.Error);
            return;
        }
        IsBusy = true;
        try
        {
            var result = IsEditing && SelectedUser is not null
                ? await _administrationService.UpdateUserAsync(new UpdateUserAdministrationRequest(SelectedUser.Id, Username, FullName, Email, SelectedRoleId.Value))
                : await _administrationService.CreateUserAsync(new CreateUserAdministrationRequest(Username, FullName, Email, SelectedRoleId.Value, NewPassword));
            _notificationService.Show("Users", result.IsSuccess ? "User saved successfully." : result.Error ?? "Unable to save user.", result.IsSuccess ? NotificationSeverity.Success : NotificationSeverity.Error);
            if (result.IsSuccess) { NewPassword = string.Empty; await LoadAsync(); New(); }
        }
        catch (Exception ex)
        {
            _notificationService.Show("Users", $"Unable to save user: {ex.Message}", NotificationSeverity.Error);
        }
        finally { IsBusy = false; }
    }

    private bool HasSelectedUser() => SelectedUser is not null;

    /// <summary>Activates or deactivates the selected user.</summary>
    [RelayCommand(CanExecute = nameof(HasSelectedUser))]
    private async Task ToggleActiveAsync()
    {
        if (SelectedUser is null) return;
        try
        {
            var activate = !SelectedUser.IsActive;
            if (!activate && !await _confirmationService.ConfirmAsync("Deactivate User", $"Deactivate '{SelectedUser.Username}'?")) return;
            var result = await _administrationService.SetUserActiveAsync(SelectedUser.Id, activate);
            _notificationService.Show("Users", result.IsSuccess ? $"User {(activate ? "activated" : "deactivated")}." : result.Error ?? "Unable to change user status.", result.IsSuccess ? NotificationSeverity.Success : NotificationSeverity.Error);
            if (result.IsSuccess) await LoadAsync();
        }
        catch (Exception ex)
        {
            _notificationService.Show("Users", $"Unable to change user status: {ex.Message}", NotificationSeverity.Error);
        }
    }

    /// <summary>Resets and unlocks the selected user's password.</summary>
    [RelayCommand(CanExecute = nameof(HasSelectedUser))]
    private async Task ResetPasswordAsync()
    {
        if (SelectedUser is null) return;
        try
        {
            if (!await _confirmationService.ConfirmAsync("Reset Password", $"Reset the password for '{SelectedUser.Username}'?")) return;
            var result = await _administrationService.ResetPasswordAsync(SelectedUser.Id, NewPassword);
            _notificationService.Show("Users", result.IsSuccess ? "Password reset and account unlocked." : result.Error ?? "Unable to reset password.", result.IsSuccess ? NotificationSeverity.Success : NotificationSeverity.Error);
            if (result.IsSuccess) NewPassword = string.Empty;
        }
        catch (Exception ex)
        {
            _notificationService.Show("Users", $"Unable to reset password: {ex.Message}", NotificationSeverity.Error);
        }
    }
}
