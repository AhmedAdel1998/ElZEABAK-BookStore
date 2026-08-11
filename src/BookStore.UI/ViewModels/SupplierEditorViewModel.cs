using BookStore.Application.Features.Suppliers.Commands.CreateSupplier;
using BookStore.Application.Features.Suppliers.Commands.UpdateSupplier;
using BookStore.Application.Features.Suppliers.DTOs;
using BookStore.Application.Features.Suppliers.Handlers;
using BookStore.Application.Features.Suppliers.Queries.GetSupplierById;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Supplier editor view model.
/// </summary>
public partial class SupplierEditorViewModel : BaseViewModel
{
    private readonly CreateSupplierHandler _createHandler;
    private readonly UpdateSupplierHandler _updateHandler;
    private readonly GetSupplierByIdHandler _getByIdHandler;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShellNavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly ISupplierNavigationState _navigationState;
    private SupplierEditorModel _original = new();

    [ObservableProperty] private Guid? supplierId;
    [ObservableProperty] private string companyName = string.Empty;
    [ObservableProperty] private string? contactName;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string? email;
    [ObservableProperty] private string? address;
    [ObservableProperty] private string? notes;
    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private string validationMessage = string.Empty;
    [ObservableProperty] private bool hasUnsavedChanges;

    /// <summary>Initializes a new instance of the <see cref="SupplierEditorViewModel"/> class.</summary>
    public SupplierEditorViewModel(CreateSupplierHandler createHandler, UpdateSupplierHandler updateHandler, GetSupplierByIdHandler getByIdHandler, IAuthorizationService authorizationService, IShellNavigationService navigationService, INotificationService notificationService, ISupplierNavigationState navigationState)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _getByIdHandler = getByIdHandler;
        _authorizationService = authorizationService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _navigationState = navigationState;
        SupplierId = navigationState.SelectedSupplierId;
        Title = SupplierId.HasValue ? "Edit Supplier" : "New Supplier";
        _ = LoadAsync();
    }

    /// <summary>Gets whether save is allowed.</summary>
    public bool CanSaveSupplier => SupplierId.HasValue
        ? _authorizationService.HasPermission(PermissionConstants.SupplierEdit)
        : _authorizationService.HasPermission(PermissionConstants.SupplierCreate);

    partial void OnCompanyNameChanged(string value) => MarkChanged();
    partial void OnContactNameChanged(string? value) => MarkChanged();
    partial void OnPhoneChanged(string value) => MarkChanged();
    partial void OnEmailChanged(string? value) => MarkChanged();
    partial void OnAddressChanged(string? value) => MarkChanged();
    partial void OnNotesChanged(string? value) => MarkChanged();
    partial void OnIsActiveChanged(bool value) => MarkChanged();

    /// <summary>Saves the supplier.</summary>
    [RelayCommand(CanExecute = nameof(CanSaveSupplier))]
    private async Task SaveAsync()
    {
        IsBusy = true;
        ValidationMessage = string.Empty;
        var model = BuildModel();
        var result = SupplierId.HasValue
            ? await _updateHandler.HandleAsync(new UpdateSupplierRequest(model))
            : await _createHandler.HandleAsync(new CreateSupplierRequest(model));
        IsBusy = false;

        if (!result.IsSuccess || result.Value is null)
        {
            ValidationMessage = result.Error ?? "Unable to save supplier.";
            _notificationService.Show("Suppliers", ValidationMessage, NotificationSeverity.Error);
            return;
        }

        _navigationState.SelectedSupplierId = result.Value.Id;
        _notificationService.Show("Suppliers", SupplierId.HasValue ? "Supplier updated successfully." : "Supplier created successfully.", NotificationSeverity.Success);
        await _navigationService.NavigateToAsync<SupplierDetailsViewModel>("Suppliers > Details");
    }

    /// <summary>Cancels editing.</summary>
    [RelayCommand]
    private Task CancelAsync() => _navigationService.GoBackAsync();

    /// <summary>Resets the editor.</summary>
    [RelayCommand]
    private void Reset() => ApplyModel(_original);

    private async Task LoadAsync()
    {
        if (!SupplierId.HasValue)
        {
            _original = BuildModel();
            return;
        }

        IsBusy = true;
        var result = await _getByIdHandler.HandleAsync(new GetSupplierByIdRequest(SupplierId.Value));
        IsBusy = false;
        if (!result.IsSuccess || result.Value is null)
        {
            ValidationMessage = result.Error ?? "Supplier could not be found.";
            return;
        }

        ApplyModel(result.Value);
        _original = BuildModel();
        HasUnsavedChanges = false;
    }

    private SupplierEditorModel BuildModel() => new()
    {
        Id = SupplierId,
        CompanyName = CompanyName,
        ContactName = ContactName,
        Phone = Phone,
        Email = Email,
        Address = Address,
        Notes = Notes,
        IsActive = IsActive
    };

    private void ApplyModel(SupplierEditorModel model)
    {
        SupplierId = model.Id;
        CompanyName = model.CompanyName;
        ContactName = model.ContactName;
        Phone = model.Phone;
        Email = model.Email;
        Address = model.Address;
        Notes = model.Notes;
        IsActive = model.IsActive;
        HasUnsavedChanges = false;
    }

    private void MarkChanged() => HasUnsavedChanges = true;
}
