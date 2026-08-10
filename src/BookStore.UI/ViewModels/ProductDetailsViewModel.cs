using BookStore.Application.Features.Products.DTOs;
using BookStore.Application.Features.Products.Handlers;
using BookStore.Application.Features.Products.Queries.GetProductById;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Product details view model.
/// </summary>
public partial class ProductDetailsViewModel : BaseViewModel
{
    private readonly GetProductByIdHandler _getByIdHandler;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShellNavigationService _navigationService;
    private readonly IProductNavigationState _navigationState;

    [ObservableProperty] private ProductDto? product;
    [ObservableProperty] private string message = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="ProductDetailsViewModel"/> class.</summary>
    public ProductDetailsViewModel(GetProductByIdHandler getByIdHandler, IAuthorizationService authorizationService, IShellNavigationService navigationService, IProductNavigationState navigationState)
    {
        _getByIdHandler = getByIdHandler;
        _authorizationService = authorizationService;
        _navigationService = navigationService;
        _navigationState = navigationState;
        Title = "Product Details";
        _ = LoadAsync();
    }

    /// <summary>Gets whether edit is allowed.</summary>
    public bool CanEdit => _authorizationService.HasPermission(PermissionConstants.ProductEdit);

    /// <summary>Navigates to product editor.</summary>
    [RelayCommand(CanExecute = nameof(CanEditProduct))]
    private Task EditAsync()
    {
        if (Product is not null)
        {
            _navigationState.SelectedProductId = Product.Id;
        }

        return _navigationService.NavigateToAsync<ProductEditorViewModel>("Products > Edit Product");
    }

    /// <summary>Navigates back.</summary>
    [RelayCommand]
    private Task BackAsync() => _navigationService.GoBackAsync();

    private bool CanEditProduct() => CanEdit && Product is not null;

    private async Task LoadAsync()
    {
        if (_navigationState.SelectedProductId is null)
        {
            Message = "No product selected.";
            return;
        }

        IsBusy = true;
        var result = await _getByIdHandler.HandleAsync(new GetProductByIdRequest(_navigationState.SelectedProductId.Value));
        IsBusy = false;
        if (!result.IsSuccess || result.Value is null)
        {
            Message = result.Error ?? "Product was not found.";
            return;
        }

        Product = result.Value;
        EditCommand.NotifyCanExecuteChanged();
    }
}
