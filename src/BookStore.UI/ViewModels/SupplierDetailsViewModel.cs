using System.Collections.ObjectModel;
using BookStore.Application.Features.Suppliers.DTOs;
using BookStore.Application.Features.Suppliers.Handlers;
using BookStore.Application.Features.Suppliers.Queries.GetSupplierById;
using BookStore.Application.Features.Suppliers.Queries.GetSupplierProducts;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Supplier details view model.
/// </summary>
public partial class SupplierDetailsViewModel : BaseViewModel
{
    private readonly GetSupplierByIdHandler _getByIdHandler;
    private readonly GetSupplierProductsHandler _productsHandler;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShellNavigationService _navigationService;
    private readonly ISupplierNavigationState _navigationState;

    [ObservableProperty] private SupplierDto? supplier;
    [ObservableProperty] private string message = string.Empty;
    [ObservableProperty] private int pageNumber = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalCount;

    /// <summary>Initializes a new instance of the <see cref="SupplierDetailsViewModel"/> class.</summary>
    public SupplierDetailsViewModel(GetSupplierByIdHandler getByIdHandler, GetSupplierProductsHandler productsHandler, IAuthorizationService authorizationService, IShellNavigationService navigationService, ISupplierNavigationState navigationState)
    {
        _getByIdHandler = getByIdHandler;
        _productsHandler = productsHandler;
        _authorizationService = authorizationService;
        _navigationService = navigationService;
        _navigationState = navigationState;
        Title = "Supplier Details";
        _ = LoadAsync();
    }

    /// <summary>Gets associated products.</summary>
    public ObservableCollection<SupplierProductItem> Products { get; } = [];

    /// <summary>Gets whether edit is allowed.</summary>
    public bool CanEdit => _authorizationService.HasPermission(PermissionConstants.SupplierEdit);

    /// <summary>Gets whether products are allowed.</summary>
    public bool CanViewProducts => _authorizationService.HasPermission(PermissionConstants.SupplierViewProducts);

    /// <summary>Navigates to edit supplier.</summary>
    [RelayCommand(CanExecute = nameof(CanEditSupplier))]
    private Task EditAsync()
    {
        if (Supplier is not null)
        {
            _navigationState.SelectedSupplierId = Supplier.Id;
        }

        return _navigationService.NavigateToAsync<SupplierEditorViewModel>("Suppliers > Edit Supplier");
    }

    /// <summary>Navigates back.</summary>
    [RelayCommand]
    private Task BackAsync() => _navigationService.GoBackAsync();

    /// <summary>Refreshes associated products.</summary>
    [RelayCommand]
    private Task RefreshProductsAsync() => LoadProductsAsync();

    private bool CanEditSupplier() => CanEdit && Supplier is not null;

    private async Task LoadAsync()
    {
        if (_navigationState.SelectedSupplierId is null)
        {
            Message = "No supplier selected.";
            return;
        }

        IsBusy = true;
        var result = await _getByIdHandler.HandleAsync(new GetSupplierByIdRequest(_navigationState.SelectedSupplierId.Value));
        IsBusy = false;
        if (!result.IsSuccess || result.Value is null)
        {
            Message = result.Error ?? "Supplier could not be found.";
            return;
        }

        Supplier = result.Value;
        EditCommand.NotifyCanExecuteChanged();
        await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        if (_navigationState.SelectedSupplierId is null || !CanViewProducts)
        {
            return;
        }

        var result = await _productsHandler.HandleAsync(new GetSupplierProductsRequest(_navigationState.SelectedSupplierId.Value, PageNumber, PageSize));
        Products.Clear();
        if (result.IsSuccess && result.Value is not null)
        {
            foreach (var product in result.Value.Items)
            {
                Products.Add(product);
            }

            TotalCount = result.Value.TotalCount;
        }
    }
}
