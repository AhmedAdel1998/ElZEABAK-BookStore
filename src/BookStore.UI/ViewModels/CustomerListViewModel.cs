using System.Collections.ObjectModel;
using BookStore.Application.Features.Customers.Commands.ActivateCustomer;
using BookStore.Application.Features.Customers.Commands.DeactivateCustomer;
using BookStore.Application.Features.Customers.Commands.DeleteCustomer;
using BookStore.Application.Features.Customers.DTOs;
using BookStore.Application.Features.Customers.Handlers;
using BookStore.Application.Features.Customers.Queries.SearchCustomers;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Customer list view model.
/// </summary>
public partial class CustomerListViewModel : BaseViewModel
{
    private readonly SearchCustomersHandler _searchHandler;
    private readonly DeleteCustomerHandler _deleteHandler;
    private readonly ActivateCustomerHandler _activateHandler;
    private readonly DeactivateCustomerHandler _deactivateHandler;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShellNavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly IConfirmationDialogService _confirmationDialogService;
    private readonly ICustomerNavigationState _navigationState;

    [ObservableProperty] private string searchTerm = string.Empty;
    [ObservableProperty] private string statusFilter = string.Empty;
    [ObservableProperty] private CustomerListItem? selectedCustomer;
    [ObservableProperty] private int pageNumber = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string emptyMessage = "No customers found.";

    /// <summary>Initializes a new instance of the <see cref="CustomerListViewModel"/> class.</summary>
    public CustomerListViewModel(SearchCustomersHandler searchHandler, DeleteCustomerHandler deleteHandler, ActivateCustomerHandler activateHandler, DeactivateCustomerHandler deactivateHandler, IAuthorizationService authorizationService, IShellNavigationService navigationService, INotificationService notificationService, IConfirmationDialogService confirmationDialogService, ICustomerNavigationState navigationState)
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
        Title = "Customers";
        _ = LoadAsync();
    }

    /// <summary>Gets customers.</summary>
    public ObservableCollection<CustomerListItem> Customers { get; } = [];

    /// <summary>Gets whether create is allowed.</summary>
    public bool CanCreate => _authorizationService.HasPermission(PermissionConstants.CustomerCreate);

    /// <summary>Gets whether edit is allowed.</summary>
    public bool CanEdit => _authorizationService.HasPermission(PermissionConstants.CustomerEdit);

    /// <summary>Gets whether delete is allowed.</summary>
    public bool CanDelete => _authorizationService.HasPermission(PermissionConstants.CustomerDelete);

    partial void OnSelectedCustomerChanged(CustomerListItem? value)
    {
        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        ViewDetailsCommand.NotifyCanExecuteChanged();
        ActivateCommand.NotifyCanExecuteChanged();
        DeactivateCommand.NotifyCanExecuteChanged();
    }

    partial void OnSearchTermChanged(string value) => _ = SearchAsync();
    partial void OnStatusFilterChanged(string value) => _ = SearchAsync();

    /// <summary>Refreshes customers.</summary>
    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    /// <summary>Searches customers.</summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadAsync();
    }

    /// <summary>Navigates to customer creation.</summary>
    [RelayCommand(CanExecute = nameof(CanCreate))]
    private Task AddAsync()
    {
        _navigationState.SelectedCustomerId = null;
        return _navigationService.NavigateToAsync<CustomerEditorViewModel>("Customers > New Customer");
    }

    /// <summary>Navigates to customer editing.</summary>
    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private Task EditAsync()
    {
        if (SelectedCustomer is null) return Task.CompletedTask;
        _navigationState.SelectedCustomerId = SelectedCustomer.Id;
        return _navigationService.NavigateToAsync<CustomerEditorViewModel>("Customers > Edit Customer");
    }

    /// <summary>Navigates to customer details.</summary>
    [RelayCommand(CanExecute = nameof(CanSelectCustomer))]
    private Task ViewDetailsAsync()
    {
        if (SelectedCustomer is null) return Task.CompletedTask;
        _navigationState.SelectedCustomerId = SelectedCustomer.Id;
        return _navigationService.NavigateToAsync<CustomerDetailsViewModel>("Customers > Details");
    }

    /// <summary>Soft deletes the selected customer.</summary>
    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private async Task DeleteAsync()
    {
        if (SelectedCustomer is null || !await _confirmationDialogService.ConfirmAsync("Delete Customer", $"Delete customer '{SelectedCustomer.FullName}'?"))
        {
            return;
        }

        var result = await _deleteHandler.HandleAsync(new DeleteCustomerRequest(SelectedCustomer.PersistedId));
        ShowOperation(result.Succeeded, "Customer deleted successfully.", result.Errors.FirstOrDefault()?.Message);
        await LoadAsync();
    }

    /// <summary>Activates the selected customer.</summary>
    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task ActivateAsync()
    {
        if (SelectedCustomer is null) return;
        var result = await _activateHandler.HandleAsync(new ActivateCustomerRequest(SelectedCustomer.PersistedId));
        ShowOperation(result.Succeeded, "Customer activated successfully.", result.Errors.FirstOrDefault()?.Message);
        await LoadAsync();
    }

    /// <summary>Deactivates the selected customer.</summary>
    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task DeactivateAsync()
    {
        if (SelectedCustomer is null) return;
        var result = await _deactivateHandler.HandleAsync(new DeactivateCustomerRequest(SelectedCustomer.PersistedId));
        ShowOperation(result.Succeeded, "Customer deactivated successfully.", result.Errors.FirstOrDefault()?.Message);
        await LoadAsync();
    }

    private bool CanEditSelected() => CanEdit && SelectedCustomer is not null;
    private bool CanDeleteSelected() => CanDelete && SelectedCustomer is not null;
    private bool CanSelectCustomer() => SelectedCustomer is not null && _authorizationService.HasPermission(PermissionConstants.CustomerView);

    /// <summary>Gets the number of pages available for the current filters.</summary>
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    /// <summary>Gets whether an earlier page exists.</summary>
    public bool CanGoToPreviousPage => PageNumber > 1;

    /// <summary>Gets whether a later page exists.</summary>
    public bool CanGoToNextPage => PageNumber < TotalPages;

    partial void OnPageNumberChanged(int value) => NotifyPagingChanged();

    partial void OnTotalCountChanged(int value) => NotifyPagingChanged();

    partial void OnPageSizeChanged(int value) => NotifyPagingChanged();

    private void NotifyPagingChanged()
    {
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        NextPageCommand.NotifyCanExecuteChanged();
        PreviousPageCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Moves to the next page of results.</summary>
    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private async Task NextPageAsync()
    {
        PageNumber++;
        await LoadAsync();
    }

    /// <summary>Moves to the previous page of results.</summary>
    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private async Task PreviousPageAsync()
    {
        PageNumber--;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        var result = await _searchHandler.HandleAsync(new SearchCustomersRequest(new CustomerFilter
        {
            SearchTerm = SearchTerm,
            IsActive = ParseStatusFilter(),
            PageNumber = PageNumber,
            PageSize = PageSize
        }));

        Customers.Clear();
        if (result.IsSuccess && result.Value is not null)
        {
            foreach (var customer in result.Value.Items)
            {
                Customers.Add(customer);
            }

            TotalCount = result.Value.TotalCount;
        }
        else
        {
            _notificationService.Show("Customers", result.Error ?? "Unable to load customers.", NotificationSeverity.Error);
        }

        IsBusy = false;
    }

    private void ShowOperation(bool succeeded, string successMessage, string? failureMessage)
    {
        _notificationService.Show("Customers", succeeded ? successMessage : failureMessage ?? "Operation failed.", succeeded ? NotificationSeverity.Success : NotificationSeverity.Error);
    }

    private bool? ParseStatusFilter() => StatusFilter switch
    {
        "Active" => true,
        "Inactive" => false,
        _ => null
    };
}
