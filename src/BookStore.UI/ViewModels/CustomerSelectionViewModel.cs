using System.Collections.ObjectModel;
using BookStore.Application.Features.Customers.Commands.CreateCustomer;
using BookStore.Application.Features.Customers.DTOs;
using BookStore.Application.Features.Customers.Handlers;
using BookStore.Application.Features.Customers.Queries.SearchCustomers;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Reusable customer selection view model optimized for POS workflows.
/// </summary>
public partial class CustomerSelectionViewModel : BaseViewModel
{
    private readonly SearchCustomersHandler _searchHandler;
    private readonly CreateCustomerHandler _createHandler;
    private readonly IAuthorizationService _authorizationService;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private string searchTerm = string.Empty;
    [ObservableProperty] private CustomerSelectionItem? selectedCustomer;
    [ObservableProperty] private string newCustomerName = string.Empty;
    [ObservableProperty] private string newCustomerPhone = string.Empty;
    [ObservableProperty] private string? newCustomerEmail;
    [ObservableProperty] private bool wasCancelled = true;

    /// <summary>Initializes a new instance of the <see cref="CustomerSelectionViewModel"/> class.</summary>
    public CustomerSelectionViewModel(SearchCustomersHandler searchHandler, CreateCustomerHandler createHandler, IAuthorizationService authorizationService, INotificationService notificationService)
    {
        _searchHandler = searchHandler;
        _createHandler = createHandler;
        _authorizationService = authorizationService;
        _notificationService = notificationService;
        Title = "Select Customer";
    }

    /// <summary>Gets customer results.</summary>
    public ObservableCollection<CustomerSelectionItem> Customers { get; } = [];

    /// <summary>Gets whether customer creation is allowed.</summary>
    public bool CanCreateCustomer => _authorizationService.HasPermission(PermissionConstants.CustomerCreate);

    /// <summary>Occurs when selection completes.</summary>
    public event EventHandler<CustomerSelectionItem?>? SelectionCompleted;

    /// <summary>Searches customers.</summary>
    [RelayCommand]
    public async Task SearchAsync()
    {
        var result = await _searchHandler.HandleAsync(new SearchCustomersRequest(new CustomerFilter { SearchTerm = SearchTerm, IsActive = true, PageSize = 15 }));
        Customers.Clear();
        if (!result.IsSuccess || result.Value is null)
        {
            _notificationService.Show("Customers", result.Error ?? "Customer search failed.", NotificationSeverity.Error);
            return;
        }

        foreach (var customer in result.Value.Items.Select(item => new CustomerSelectionItem { Id = item.PersistedId, FullName = item.FullName, Phone = item.Phone }))
        {
            Customers.Add(customer);
        }
    }

    /// <summary>Selects the current customer.</summary>
    [RelayCommand]
    public void Select()
    {
        WasCancelled = false;
        SelectionCompleted?.Invoke(this, SelectedCustomer);
    }

    /// <summary>Clears selected customer.</summary>
    [RelayCommand]
    public void ClearSelection()
    {
        WasCancelled = false;
        SelectedCustomer = null;
        SelectionCompleted?.Invoke(this, null);
    }

    /// <summary>Cancels selection.</summary>
    [RelayCommand]
    public void Cancel()
    {
        WasCancelled = true;
        SelectionCompleted?.Invoke(this, null);
    }

    /// <summary>Creates a customer and returns it to the caller.</summary>
    [RelayCommand(CanExecute = nameof(CanCreateCustomer))]
    public async Task CreateNewCustomerAsync()
    {
        var result = await _createHandler.HandleAsync(new CreateCustomerRequest(new CustomerEditorModel
        {
            FullName = NewCustomerName,
            Phone = NewCustomerPhone,
            Email = NewCustomerEmail,
            IsActive = true
        }));
        if (!result.IsSuccess || result.Value is null)
        {
            _notificationService.Show("Customers", result.Error ?? "Unable to save customer.", NotificationSeverity.Error);
            return;
        }

        var selected = new CustomerSelectionItem { Id = result.Value.Id, FullName = result.Value.FullName, Phone = NewCustomerPhone };
        _notificationService.Show("Customers", "Customer created successfully.", NotificationSeverity.Success);
        WasCancelled = false;
        SelectionCompleted?.Invoke(this, selected);
    }
}
