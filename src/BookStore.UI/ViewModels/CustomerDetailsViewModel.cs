using System.Collections.ObjectModel;
using BookStore.Application.Features.Customers.DTOs;
using BookStore.Application.Features.Customers.Handlers;
using BookStore.Application.Features.Customers.Queries.GetCustomerById;
using BookStore.Application.Features.Customers.Queries.GetCustomerSalesHistory;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Customer details view model.
/// </summary>
public partial class CustomerDetailsViewModel : BaseViewModel
{
    private readonly GetCustomerByIdHandler _getByIdHandler;
    private readonly GetCustomerSalesHistoryHandler _historyHandler;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShellNavigationService _navigationService;
    private readonly ICustomerNavigationState _navigationState;

    [ObservableProperty] private CustomerDto? customer;
    [ObservableProperty] private string message = string.Empty;
    [ObservableProperty] private DateTime? dateFrom;
    [ObservableProperty] private DateTime? dateTo;
    [ObservableProperty] private int pageNumber = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalCount;

    /// <summary>Initializes a new instance of the <see cref="CustomerDetailsViewModel"/> class.</summary>
    public CustomerDetailsViewModel(GetCustomerByIdHandler getByIdHandler, GetCustomerSalesHistoryHandler historyHandler, IAuthorizationService authorizationService, IShellNavigationService navigationService, ICustomerNavigationState navigationState)
    {
        _getByIdHandler = getByIdHandler;
        _historyHandler = historyHandler;
        _authorizationService = authorizationService;
        _navigationService = navigationService;
        _navigationState = navigationState;
        Title = "Customer Details";
        _ = LoadAsync();
    }

    /// <summary>Gets sales history.</summary>
    public ObservableCollection<CustomerSaleHistoryItem> SalesHistory { get; } = [];

    /// <summary>Gets whether edit is allowed.</summary>
    public bool CanEdit => _authorizationService.HasPermission(PermissionConstants.CustomerEdit);

    /// <summary>Gets whether history is allowed.</summary>
    public bool CanViewHistory => _authorizationService.HasPermission(PermissionConstants.CustomerViewHistory);

    /// <summary>Navigates to edit customer.</summary>
    [RelayCommand(CanExecute = nameof(CanEditCustomer))]
    private Task EditAsync()
    {
        if (Customer is not null)
        {
            _navigationState.SelectedCustomerId = Customer.Id;
        }

        return _navigationService.NavigateToAsync<CustomerEditorViewModel>("Customers > Edit Customer");
    }

    /// <summary>Navigates back.</summary>
    [RelayCommand]
    private Task BackAsync() => _navigationService.GoBackAsync();

    /// <summary>Refreshes sales history.</summary>
    [RelayCommand]
    private Task RefreshHistoryAsync() => LoadHistoryAsync();

    private bool CanEditCustomer() => CanEdit && Customer is not null;

    private async Task LoadAsync()
    {
        if (_navigationState.SelectedCustomerId is null)
        {
            Message = "No customer selected.";
            return;
        }

        IsBusy = true;
        var result = await _getByIdHandler.HandleAsync(new GetCustomerByIdRequest(_navigationState.SelectedCustomerId.Value));
        IsBusy = false;
        if (!result.IsSuccess || result.Value is null)
        {
            Message = result.Error ?? "Customer could not be found.";
            return;
        }

        Customer = result.Value;
        EditCommand.NotifyCanExecuteChanged();
        await LoadHistoryAsync();
    }

    private static DateTimeOffset? ToOffset(DateTime? value)
    {
        return value.HasValue ? new DateTimeOffset(value.Value.Date, TimeZoneInfo.Local.GetUtcOffset(value.Value.Date)) : null;
    }

    /// <summary>
    /// Turns the day chosen in the "to" picker into an exclusive upper bound, so the whole of that
    /// day is included rather than only its first instant.
    /// </summary>
    private static DateTimeOffset? ToExclusiveEnd(DateTime? value)
    {
        return value.HasValue ? ToOffset(value.Value.Date.AddDays(1)) : null;
    }

    private async Task LoadHistoryAsync()
    {
        if (_navigationState.SelectedCustomerId is null || !CanViewHistory)
        {
            return;
        }

        var result = await _historyHandler.HandleAsync(new GetCustomerSalesHistoryRequest(
            _navigationState.SelectedCustomerId.Value,
            ToOffset(DateFrom),
            ToExclusiveEnd(DateTo),
            PageNumber,
            PageSize));
        SalesHistory.Clear();
        if (result.IsSuccess && result.Value is not null)
        {
            foreach (var sale in result.Value.Items)
            {
                SalesHistory.Add(sale);
            }

            TotalCount = result.Value.TotalCount;
        }
    }
}
