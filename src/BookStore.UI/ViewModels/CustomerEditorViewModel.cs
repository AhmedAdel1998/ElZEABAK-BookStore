using BookStore.Application.Features.Customers.Commands.CreateCustomer;
using BookStore.Application.Features.Customers.Commands.UpdateCustomer;
using BookStore.Application.Features.Customers.DTOs;
using BookStore.Application.Features.Customers.Handlers;
using BookStore.Application.Features.Customers.Queries.GetCustomerById;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Customer editor view model.
/// </summary>
public partial class CustomerEditorViewModel : BaseViewModel
{
    private readonly CreateCustomerHandler _createHandler;
    private readonly UpdateCustomerHandler _updateHandler;
    private readonly GetCustomerByIdHandler _getByIdHandler;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShellNavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly ICustomerNavigationState _navigationState;
    private CustomerEditorModel _original = new();

    [ObservableProperty] private Guid? customerId;
    [ObservableProperty] private string fullName = string.Empty;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string? email;
    [ObservableProperty] private string? address;
    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private string validationMessage = string.Empty;
    [ObservableProperty] private bool hasUnsavedChanges;

    /// <summary>Initializes a new instance of the <see cref="CustomerEditorViewModel"/> class.</summary>
    public CustomerEditorViewModel(CreateCustomerHandler createHandler, UpdateCustomerHandler updateHandler, GetCustomerByIdHandler getByIdHandler, IAuthorizationService authorizationService, IShellNavigationService navigationService, INotificationService notificationService, ICustomerNavigationState navigationState)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _getByIdHandler = getByIdHandler;
        _authorizationService = authorizationService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _navigationState = navigationState;
        CustomerId = navigationState.SelectedCustomerId;
        Title = CustomerId.HasValue ? "Edit Customer" : "New Customer";
        _ = LoadAsync();
    }

    /// <summary>Gets whether save is allowed.</summary>
    public bool CanSaveCustomer => CustomerId.HasValue
        ? _authorizationService.HasPermission(PermissionConstants.CustomerEdit)
        : _authorizationService.HasPermission(PermissionConstants.CustomerCreate);

    partial void OnFullNameChanged(string value) => MarkChanged();
    partial void OnPhoneChanged(string value) => MarkChanged();
    partial void OnEmailChanged(string? value) => MarkChanged();
    partial void OnAddressChanged(string? value) => MarkChanged();
    partial void OnIsActiveChanged(bool value) => MarkChanged();

    /// <summary>Saves the customer.</summary>
    [RelayCommand(CanExecute = nameof(CanSaveCustomer))]
    private async Task SaveAsync()
    {
        IsBusy = true;
        ValidationMessage = string.Empty;
        var model = BuildModel();
        var result = CustomerId.HasValue
            ? await _updateHandler.HandleAsync(new UpdateCustomerRequest(model))
            : await _createHandler.HandleAsync(new CreateCustomerRequest(model));
        IsBusy = false;

        if (!result.IsSuccess || result.Value is null)
        {
            ValidationMessage = result.Error ?? "Unable to save customer.";
            _notificationService.Show("Customers", ValidationMessage, NotificationSeverity.Error);
            return;
        }

        _navigationState.SelectedCustomerId = result.Value.Id;
        _notificationService.Show("Customers", CustomerId.HasValue ? "Customer updated successfully." : "Customer created successfully.", NotificationSeverity.Success);
        await _navigationService.NavigateToAsync<CustomerDetailsViewModel>("Customers > Details");
    }

    /// <summary>Cancels editing.</summary>
    [RelayCommand]
    private Task CancelAsync() => _navigationService.GoBackAsync();

    /// <summary>Resets the editor.</summary>
    [RelayCommand]
    private void Reset() => ApplyModel(_original);

    private async Task LoadAsync()
    {
        if (!CustomerId.HasValue)
        {
            _original = BuildModel();
            return;
        }

        IsBusy = true;
        var result = await _getByIdHandler.HandleAsync(new GetCustomerByIdRequest(CustomerId.Value));
        IsBusy = false;
        if (!result.IsSuccess || result.Value is null)
        {
            ValidationMessage = result.Error ?? "Customer could not be found.";
            return;
        }

        ApplyModel(result.Value);
        _original = BuildModel();
        HasUnsavedChanges = false;
    }

    private CustomerEditorModel BuildModel() => new()
    {
        Id = CustomerId,
        FullName = FullName,
        Phone = Phone,
        Email = Email,
        Address = Address,
        IsActive = IsActive
    };

    private void ApplyModel(CustomerEditorModel model)
    {
        CustomerId = model.Id;
        FullName = model.FullName;
        Phone = model.Phone;
        Email = model.Email;
        Address = model.Address;
        IsActive = model.IsActive;
        HasUnsavedChanges = false;
    }

    private void MarkChanged() => HasUnsavedChanges = true;
}
