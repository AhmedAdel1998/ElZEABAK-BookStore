using System.Collections.ObjectModel;
using BookStore.Application.Features.Suppliers.Commands.ActivateSupplier;
using BookStore.Application.Features.Suppliers.Commands.DeactivateSupplier;
using BookStore.Application.Features.Suppliers.Commands.DeleteSupplier;
using BookStore.Application.Features.Suppliers.DTOs;
using BookStore.Application.Features.Suppliers.Handlers;
using BookStore.Application.Features.Suppliers.Queries.SearchSuppliers;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Supplier list view model.
/// </summary>
public partial class SupplierListViewModel : BaseViewModel
{
    private readonly SearchSuppliersHandler _searchHandler;
    private readonly DeleteSupplierHandler _deleteHandler;
    private readonly ActivateSupplierHandler _activateHandler;
    private readonly DeactivateSupplierHandler _deactivateHandler;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShellNavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly IConfirmationDialogService _confirmationDialogService;
    private readonly ISupplierNavigationState _navigationState;

    [ObservableProperty] private string searchTerm = string.Empty;
    [ObservableProperty] private string statusFilter = string.Empty;
    [ObservableProperty] private SupplierListItem? selectedSupplier;
    [ObservableProperty] private int pageNumber = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string emptyMessage = "No suppliers found.";

    /// <summary>Initializes a new instance of the <see cref="SupplierListViewModel"/> class.</summary>
    public SupplierListViewModel(SearchSuppliersHandler searchHandler, DeleteSupplierHandler deleteHandler, ActivateSupplierHandler activateHandler, DeactivateSupplierHandler deactivateHandler, IAuthorizationService authorizationService, IShellNavigationService navigationService, INotificationService notificationService, IConfirmationDialogService confirmationDialogService, ISupplierNavigationState navigationState)
    {
        _searchHandler = searchHandler;
        _deleteHandler = deleteHandler;
        _activateHandler = activateHandler;
        _deactivateHandler = deactivateHandler;
        _authorizationService = authorizationService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _confirmationDialogService = confirmationDialogService;
        _navigationState = navigationState;
        Title = "Suppliers";
        _ = LoadAsync();
    }

    /// <summary>Gets suppliers.</summary>
    public ObservableCollection<SupplierListItem> Suppliers { get; } = [];

    /// <summary>Gets whether create is allowed.</summary>
    public bool CanCreate => _authorizationService.HasPermission(PermissionConstants.SupplierCreate);

    /// <summary>Gets whether edit is allowed.</summary>
    public bool CanEdit => _authorizationService.HasPermission(PermissionConstants.SupplierEdit);

    /// <summary>Gets whether delete is allowed.</summary>
    public bool CanDelete => _authorizationService.HasPermission(PermissionConstants.SupplierDelete);

    partial void OnSelectedSupplierChanged(SupplierListItem? value)
    {
        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        ViewDetailsCommand.NotifyCanExecuteChanged();
        ActivateCommand.NotifyCanExecuteChanged();
        DeactivateCommand.NotifyCanExecuteChanged();
    }

    partial void OnSearchTermChanged(string value) => _ = SearchAsync();
    partial void OnStatusFilterChanged(string value) => _ = SearchAsync();

    /// <summary>Refreshes suppliers.</summary>
    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    /// <summary>Searches suppliers.</summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadAsync();
    }

    /// <summary>Navigates to supplier creation.</summary>
    [RelayCommand(CanExecute = nameof(CanCreate))]
    private Task AddAsync()
    {
        _navigationState.SelectedSupplierId = null;
        return _navigationService.NavigateToAsync<SupplierEditorViewModel>("Suppliers > New Supplier");
    }

    /// <summary>Navigates to supplier editing.</summary>
    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private Task EditAsync()
    {
        if (SelectedSupplier is null) return Task.CompletedTask;
        _navigationState.SelectedSupplierId = SelectedSupplier.Id;
        return _navigationService.NavigateToAsync<SupplierEditorViewModel>("Suppliers > Edit Supplier");
    }

    /// <summary>Navigates to supplier details.</summary>
    [RelayCommand(CanExecute = nameof(CanSelectSupplier))]
    private Task ViewDetailsAsync()
    {
        if (SelectedSupplier is null) return Task.CompletedTask;
        _navigationState.SelectedSupplierId = SelectedSupplier.Id;
        return _navigationService.NavigateToAsync<SupplierDetailsViewModel>("Suppliers > Details");
    }

    /// <summary>Soft deletes the selected supplier.</summary>
    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private async Task DeleteAsync()
    {
        if (SelectedSupplier is null || !await _confirmationDialogService.ConfirmAsync("Delete Supplier", $"Delete supplier '{SelectedSupplier.CompanyName}'? Products will be preserved."))
        {
            return;
        }

        var result = await _deleteHandler.HandleAsync(new DeleteSupplierRequest(SelectedSupplier.Id));
        ShowOperation(result.Succeeded, SelectedSupplier.ProductCount > 0 ? "Supplier contains associated products. Supplier deleted and products were preserved." : "Supplier deleted successfully.", result.Errors.FirstOrDefault()?.Message);
        await LoadAsync();
    }

    /// <summary>Activates the selected supplier.</summary>
    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task ActivateAsync()
    {
        if (SelectedSupplier is null) return;
        var result = await _activateHandler.HandleAsync(new ActivateSupplierRequest(SelectedSupplier.Id));
        ShowOperation(result.Succeeded, "Supplier activated successfully.", result.Errors.FirstOrDefault()?.Message);
        await LoadAsync();
    }

    /// <summary>Deactivates the selected supplier.</summary>
    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task DeactivateAsync()
    {
        if (SelectedSupplier is null) return;
        var result = await _deactivateHandler.HandleAsync(new DeactivateSupplierRequest(SelectedSupplier.Id));
        ShowOperation(result.Succeeded, "Supplier deactivated successfully.", result.Errors.FirstOrDefault()?.Message);
        await LoadAsync();
    }

    private bool CanEditSelected() => CanEdit && SelectedSupplier is not null;
    private bool CanDeleteSelected() => CanDelete && SelectedSupplier is not null;
    private bool CanSelectSupplier() => SelectedSupplier is not null && _authorizationService.HasPermission(PermissionConstants.SupplierView);

    private async Task LoadAsync()
    {
        IsBusy = true;
        var result = await _searchHandler.HandleAsync(new SearchSuppliersRequest(new SupplierFilter
        {
            SearchTerm = SearchTerm,
            IsActive = ParseStatusFilter(),
            PageNumber = PageNumber,
            PageSize = PageSize
        }));

        Suppliers.Clear();
        if (result.IsSuccess && result.Value is not null)
        {
            foreach (var supplier in result.Value.Items)
            {
                Suppliers.Add(supplier);
            }

            TotalCount = result.Value.TotalCount;
        }
        else
        {
            _notificationService.Show("Suppliers", result.Error ?? "Unable to load suppliers.", NotificationSeverity.Error);
        }

        IsBusy = false;
    }

    private void ShowOperation(bool succeeded, string successMessage, string? failureMessage)
    {
        _notificationService.Show("Suppliers", succeeded ? successMessage : failureMessage ?? "Operation failed.", succeeded ? NotificationSeverity.Success : NotificationSeverity.Error);
    }

    private bool? ParseStatusFilter() => StatusFilter switch
    {
        "Active" => true,
        "Inactive" => false,
        _ => null
    };
}
