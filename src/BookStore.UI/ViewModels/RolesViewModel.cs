using System.Collections.ObjectModel;
using BookStore.Application.Features.Administration;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>Manages application roles and their permission sets.</summary>
public partial class RolesViewModel : BaseViewModel
{
    private readonly AdministrationService _administrationService;
    private readonly INotificationService _notificationService;
    [ObservableProperty] private RoleAdministrationDto? selectedRole;
    [ObservableProperty] private string roleName = string.Empty;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private bool isEditing;

    /// <summary>Initializes the role administration view model.</summary>
    public RolesViewModel(AdministrationService administrationService, INotificationService notificationService)
    {
        _administrationService = administrationService;
        _notificationService = notificationService;
        Title = "Roles";
        _ = LoadAsync();
    }

    /// <summary>Gets roles.</summary>
    public ObservableCollection<RoleAdministrationDto> Roles { get; } = [];

    /// <summary>Gets assignable permission choices.</summary>
    public ObservableCollection<PermissionSelectionItem> Permissions { get; } = [];

    partial void OnSelectedRoleChanged(RoleAdministrationDto? value)
    {
        if (value is null) return;
        IsEditing = true;
        RoleName = value.Name;
        Description = value.Description ?? string.Empty;
        foreach (var permission in Permissions) permission.IsSelected = value.Permissions.Contains(permission.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Reloads roles and permissions.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var roles = await _administrationService.GetRolesAsync();
            var permissions = await _administrationService.GetPermissionsAsync();
            if (!roles.IsSuccess || roles.Value is null || !permissions.IsSuccess || permissions.Value is null)
            {
                _notificationService.Show("Roles", roles.Error ?? permissions.Error ?? "Unable to load roles.", NotificationSeverity.Error);
                return;
            }
            Roles.Clear();
            foreach (var role in roles.Value) Roles.Add(role);
            Permissions.Clear();
            foreach (var permission in permissions.Value) Permissions.Add(new PermissionSelectionItem(permission.Id, permission.Name, permission.Description));
        }
        catch (Exception ex)
        {
            _notificationService.Show("Roles", $"Unable to load roles: {ex.Message}", NotificationSeverity.Error);
        }
        finally { IsBusy = false; }
    }

    /// <summary>Starts a new role form.</summary>
    [RelayCommand]
    private void New()
    {
        SelectedRole = null;
        IsEditing = false;
        RoleName = string.Empty;
        Description = string.Empty;
        foreach (var permission in Permissions) permission.IsSelected = false;
    }

    /// <summary>Saves the role and complete permission selection.</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var request = new SaveRoleAdministrationRequest(IsEditing ? SelectedRole?.Id : null, RoleName, Description, Permissions.Where(permission => permission.IsSelected).Select(permission => permission.Id).ToArray());
            var result = await _administrationService.SaveRoleAsync(request);
            _notificationService.Show("Roles", result.IsSuccess ? "Role saved successfully." : result.Error ?? "Unable to save role.", result.IsSuccess ? NotificationSeverity.Success : NotificationSeverity.Error);
            if (result.IsSuccess) { await LoadAsync(); New(); }
        }
        catch (Exception ex)
        {
            _notificationService.Show("Roles", $"Unable to save role: {ex.Message}", NotificationSeverity.Error);
        }
    }

    /// <summary>Selects every permission.</summary>
    [RelayCommand]
    private void SelectAll() { foreach (var permission in Permissions) permission.IsSelected = true; }

    /// <summary>Clears every permission.</summary>
    [RelayCommand]
    private void ClearAll() { foreach (var permission in Permissions) permission.IsSelected = false; }
}

/// <summary>Represents a checkable permission row.</summary>
public partial class PermissionSelectionItem : ObservableObject
{
    /// <summary>Initializes a permission selection row.</summary>
    public PermissionSelectionItem(Guid id, string name, string? description) { Id = id; Name = name; Description = description; }
    /// <summary>Gets the permission identifier.</summary>
    public Guid Id { get; }
    /// <summary>Gets the permission name.</summary>
    public string Name { get; }
    /// <summary>Gets the permission description.</summary>
    public string? Description { get; }
    [ObservableProperty] private bool isSelected;
}
