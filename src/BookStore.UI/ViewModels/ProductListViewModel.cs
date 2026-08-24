using System.Collections.ObjectModel;
using BookStore.Application.Features.Categories.DTOs;
using BookStore.Application.Features.Categories.Handlers;
using BookStore.Application.Features.Categories.Queries.SearchCategories;
using BookStore.Application.Features.Products.Commands.ActivateProduct;
using BookStore.Application.Features.Products.Commands.DeactivateProduct;
using BookStore.Application.Features.Products.Commands.DeleteProduct;
using BookStore.Application.Features.Products.Commands.DuplicateProduct;
using BookStore.Application.Features.Products.DTOs;
using BookStore.Application.Features.Products.Handlers;
using BookStore.Application.Features.Products.Queries.SearchProducts;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Product list view model.
/// </summary>
public partial class ProductListViewModel : BaseViewModel
{
    private readonly SearchProductsHandler _searchHandler;
    private readonly DeleteProductHandler _deleteHandler;
    private readonly ActivateProductHandler _activateHandler;
    private readonly DeactivateProductHandler _deactivateHandler;
    private readonly DuplicateProductHandler _duplicateHandler;
    private readonly SearchCategoriesHandler _categorySearchHandler;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShellNavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly IConfirmationDialogService _confirmationDialogService;
    private readonly IProductNavigationState _navigationState;

    [ObservableProperty] private string searchTerm = string.Empty;
    [ObservableProperty] private ProductListItem? selectedProduct;
    [ObservableProperty] private Guid? selectedCategoryId;
    [ObservableProperty] private string statusFilter = string.Empty;
    [ObservableProperty] private bool lowStockOnly;
    [ObservableProperty] private decimal? minPrice;
    [ObservableProperty] private decimal? maxPrice;
    [ObservableProperty] private int? minQuantity;
    [ObservableProperty] private int? maxQuantity;
    [ObservableProperty] private int pageNumber = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string emptyMessage = "No products found.";

    /// <summary>Initializes a new instance of the <see cref="ProductListViewModel"/> class.</summary>
    public ProductListViewModel(
        SearchProductsHandler searchHandler,
        DeleteProductHandler deleteHandler,
        ActivateProductHandler activateHandler,
        DeactivateProductHandler deactivateHandler,
        DuplicateProductHandler duplicateHandler,
        SearchCategoriesHandler categorySearchHandler,
        IAuthorizationService authorizationService,
        IShellNavigationService navigationService,
        INotificationService notificationService,
        IConfirmationDialogService confirmationDialogService,
        IProductNavigationState navigationState,
        IGlobalSearchState globalSearchState)
    {
        _searchHandler = searchHandler;
        _deleteHandler = deleteHandler;
        _activateHandler = activateHandler;
        _deactivateHandler = deactivateHandler;
        _duplicateHandler = duplicateHandler;
        _categorySearchHandler = categorySearchHandler;
        _authorizationService = authorizationService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _confirmationDialogService = confirmationDialogService;
        _navigationState = navigationState;

        // Assigned to the field rather than the property: the setter starts a search of its own,
        // which would race the initial load below over the Products collection.
        searchTerm = globalSearchState.ConsumePendingFilter() ?? string.Empty;
        Title = "Products";
        _ = InitializeAsync();
    }

    /// <summary>Gets products.</summary>
    public ObservableCollection<ProductListItem> Products { get; } = [];

    /// <summary>Gets categories for filtering.</summary>
    public ObservableCollection<CategoryDto> Categories { get; } = [];

    /// <summary>Gets whether create is allowed.</summary>
    public bool CanCreate => _authorizationService.HasPermission(PermissionConstants.ProductCreate);

    /// <summary>Gets whether edit is allowed.</summary>
    public bool CanEdit => _authorizationService.HasPermission(PermissionConstants.ProductEdit);

    /// <summary>Gets whether delete is allowed.</summary>
    public bool CanDelete => _authorizationService.HasPermission(PermissionConstants.ProductDelete);

    /// <summary>Gets whether export is allowed.</summary>
    public bool CanExport => _authorizationService.HasPermission(PermissionConstants.ProductExport);

    /// <summary>Gets whether import is allowed.</summary>
    public bool CanImport => _authorizationService.HasPermission(PermissionConstants.ProductImport);

    partial void OnSelectedProductChanged(ProductListItem? value)
    {
        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        ViewDetailsCommand.NotifyCanExecuteChanged();
        ActivateCommand.NotifyCanExecuteChanged();
        DeactivateCommand.NotifyCanExecuteChanged();
        DuplicateCommand.NotifyCanExecuteChanged();
    }

    partial void OnSearchTermChanged(string value) => _ = SearchAsync();
    partial void OnSelectedCategoryIdChanged(Guid? value) => _ = SearchAsync();
    partial void OnStatusFilterChanged(string value) => _ = SearchAsync();
    partial void OnLowStockOnlyChanged(bool value) => _ = SearchAsync();
    partial void OnMinPriceChanged(decimal? value) => _ = SearchAsync();
    partial void OnMaxPriceChanged(decimal? value) => _ = SearchAsync();
    partial void OnMinQuantityChanged(int? value) => _ = SearchAsync();
    partial void OnMaxQuantityChanged(int? value) => _ = SearchAsync();

    /// <summary>Refreshes products.</summary>
    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    /// <summary>Searches products.</summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadAsync();
    }

    /// <summary>Navigates to create product.</summary>
    [RelayCommand(CanExecute = nameof(CanCreate))]
    private Task AddAsync()
    {
        _navigationState.SelectedProductId = null;
        return _navigationService.NavigateToAsync<ProductEditorViewModel>("Products > New Product");
    }

    /// <summary>Navigates to edit product.</summary>
    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private Task EditAsync()
    {
        if (SelectedProduct is null)
        {
            return Task.CompletedTask;
        }

        _navigationState.SelectedProductId = SelectedProduct.Id;
        return _navigationService.NavigateToAsync<ProductEditorViewModel>("Products > Edit Product");
    }

    /// <summary>Navigates to product details.</summary>
    [RelayCommand(CanExecute = nameof(CanSelectProduct))]
    private Task ViewDetailsAsync()
    {
        if (SelectedProduct is null)
        {
            return Task.CompletedTask;
        }

        _navigationState.SelectedProductId = SelectedProduct.Id;
        return _navigationService.NavigateToAsync<ProductDetailsViewModel>("Products > Details");
    }

    /// <summary>Deletes selected product.</summary>
    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private async Task DeleteAsync()
    {
        if (SelectedProduct is null)
        {
            return;
        }

        if (!await _confirmationDialogService.ConfirmAsync("Delete Product", $"Delete product '{SelectedProduct.Title}'?"))
        {
            return;
        }

        var result = await _deleteHandler.HandleAsync(new DeleteProductRequest(SelectedProduct.Id));
        ShowOperation(result.Succeeded, "Product deleted successfully.", result.Errors.FirstOrDefault()?.Message);
        await LoadAsync();
    }

    /// <summary>Activates selected product.</summary>
    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task ActivateAsync()
    {
        if (SelectedProduct is null) return;
        var result = await _activateHandler.HandleAsync(new ActivateProductRequest(SelectedProduct.Id));
        ShowOperation(result.Succeeded, "Product activated successfully.", result.Errors.FirstOrDefault()?.Message);
        await LoadAsync();
    }

    /// <summary>Deactivates selected product.</summary>
    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task DeactivateAsync()
    {
        if (SelectedProduct is null) return;
        var result = await _deactivateHandler.HandleAsync(new DeactivateProductRequest(SelectedProduct.Id));
        ShowOperation(result.Succeeded, "Product deactivated successfully.", result.Errors.FirstOrDefault()?.Message);
        await LoadAsync();
    }

    /// <summary>Duplicates selected product with a generated placeholder barcode.</summary>
    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task DuplicateAsync()
    {
        if (SelectedProduct is null) return;
        var result = await _duplicateHandler.HandleAsync(new DuplicateProductRequest(SelectedProduct.Id, $"{SelectedProduct.Barcode}-COPY"));
        if (!result.IsSuccess)
        {
            _notificationService.Show("Product", result.Error ?? "Unable to duplicate product.", NotificationSeverity.Error);
            return;
        }

        _notificationService.Show("Product", "Product duplicated successfully.", NotificationSeverity.Success);
        await LoadAsync();
    }

    /// <summary>Shows export placeholder.</summary>
    [RelayCommand(CanExecute = nameof(CanExport))]
    private void Export() => _notificationService.Show("Products", "Product export is prepared for future implementation.", NotificationSeverity.Information);

    /// <summary>Shows import placeholder.</summary>
    [RelayCommand(CanExecute = nameof(CanImport))]
    private void Import() => _notificationService.Show("Products", "Product import is prepared for future implementation.", NotificationSeverity.Information);

    private bool CanEditSelected() => CanEdit && SelectedProduct is not null;
    private bool CanDeleteSelected() => CanDelete && SelectedProduct is not null;
    private bool CanSelectProduct() => SelectedProduct is not null && _authorizationService.HasPermission(PermissionConstants.ProductView);

    private async Task InitializeAsync()
    {
        var categories = await _categorySearchHandler.HandleAsync(new SearchCategoriesRequest(null, 1, PagingConstants.MaxPageSize, IsActive: true));
        if (categories.IsSuccess && categories.Value is not null)
        {
            foreach (var category in categories.Value.Items)
            {
                Categories.Add(category);
            }
        }

        await LoadAsync();
    }

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
        var result = await _searchHandler.HandleAsync(new SearchProductsRequest(new ProductFilter
        {
            SearchTerm = SearchTerm,
            CategoryId = SelectedCategoryId,
            IsActive = ParseStatusFilter(),
            LowStockOnly = LowStockOnly,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            MinQuantity = MinQuantity,
            MaxQuantity = MaxQuantity,
            PageNumber = PageNumber,
            PageSize = PageSize
        }));

        Products.Clear();
        if (result.IsSuccess && result.Value is not null)
        {
            foreach (var product in result.Value.Items)
            {
                Products.Add(product);
            }

            TotalCount = result.Value.TotalCount;
        }
        else
        {
            _notificationService.Show("Products", result.Error ?? "Unable to load products.", NotificationSeverity.Error);
        }

        IsBusy = false;
    }

    private void ShowOperation(bool succeeded, string successMessage, string? failureMessage)
    {
        _notificationService.Show("Product", succeeded ? successMessage : failureMessage ?? "Operation failed.", succeeded ? NotificationSeverity.Success : NotificationSeverity.Error);
    }

    private bool? ParseStatusFilter()
    {
        return StatusFilter switch
        {
            "Active" => true,
            "Inactive" => false,
            _ => null
        };
    }
}
