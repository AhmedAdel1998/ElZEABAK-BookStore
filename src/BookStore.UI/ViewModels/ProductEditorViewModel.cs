using System.Collections.ObjectModel;
using AutoMapper;
using BookStore.Application.Features.Categories.DTOs;
using BookStore.Application.Features.Categories.Handlers;
using BookStore.Application.Features.Categories.Queries.SearchCategories;
using BookStore.Application.Features.Products.Commands.CreateProduct;
using BookStore.Application.Features.Products.Commands.DuplicateProduct;
using BookStore.Application.Features.Products.Commands.UpdateProduct;
using BookStore.Application.Features.Products.DTOs;
using BookStore.Application.Features.Products.Handlers;
using BookStore.Application.Features.Products.Queries.GetProductById;
using BookStore.Application.Interfaces;
using BookStore.Application.Features.Settings.Services;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using BarcodeHandlers = BookStore.Application.Features.Barcode.Handlers;
using BookStore.Application.Features.Barcode.Commands.GenerateBarcode;
using BookStore.Application.Features.Barcode.DTOs;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Product editor view model.
/// </summary>
public partial class ProductEditorViewModel : BaseViewModel
{
    private readonly CreateProductHandler _createHandler;
    private readonly UpdateProductHandler _updateHandler;
    private readonly DuplicateProductHandler _duplicateHandler;
    private readonly GetProductByIdHandler _getByIdHandler;
    private readonly SearchCategoriesHandler _categorySearchHandler;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShellNavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly IProductNavigationState _navigationState;
    private readonly IMapper _mapper;
    private readonly BarcodeHandlers.GenerateBarcodeHandler _generateBarcodeHandler;
    private readonly ISettingsService _settingsService;
    private BookStore.Application.Features.Settings.DTOs.BarcodeSettingsDto _barcodeSettings = new();
    private ProductEditorModel _original = new();

    [ObservableProperty] private Guid? productId;
    [ObservableProperty] private string barcode = string.Empty;
    [ObservableProperty] private string? isbn;
    [ObservableProperty] private string titleText = string.Empty;
    [ObservableProperty] private string? subtitle;
    [ObservableProperty] private string? description;
    [ObservableProperty] private string author = string.Empty;
    [ObservableProperty] private string? publisher;
    [ObservableProperty] private string? language;
    [ObservableProperty] private string? edition;
    [ObservableProperty] private DateTime? publishDate;
    [ObservableProperty] private decimal purchasePrice;
    [ObservableProperty] private decimal sellingPrice;
    [ObservableProperty] private string? taxCategory;
    [ObservableProperty] private int quantity;
    [ObservableProperty] private int minimumStock;
    [ObservableProperty] private string? shelfLocation;
    [ObservableProperty] private string? imagePath;
    [ObservableProperty] private Guid categoryId;
    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private string validationMessage = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="ProductEditorViewModel"/> class.</summary>
    public ProductEditorViewModel(
        CreateProductHandler createHandler,
        UpdateProductHandler updateHandler,
        DuplicateProductHandler duplicateHandler,
        GetProductByIdHandler getByIdHandler,
        SearchCategoriesHandler categorySearchHandler,
        IAuthorizationService authorizationService,
        IShellNavigationService navigationService,
        INotificationService notificationService,
        IProductNavigationState navigationState,
        IMapper mapper,
        BarcodeHandlers.GenerateBarcodeHandler generateBarcodeHandler,
        ISettingsService settingsService)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _duplicateHandler = duplicateHandler;
        _getByIdHandler = getByIdHandler;
        _categorySearchHandler = categorySearchHandler;
        _authorizationService = authorizationService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _navigationState = navigationState;
        _mapper = mapper;
        _generateBarcodeHandler = generateBarcodeHandler;
        _settingsService = settingsService;
        ProductId = navigationState.SelectedProductId;
        Title = ProductId.HasValue ? "Edit Product" : "New Product";
        _ = InitializeAsync();
    }

    /// <summary>Gets categories.</summary>
    public ObservableCollection<CategoryDto> Categories { get; } = [];

    /// <summary>Gets whether save is allowed.</summary>
    public bool CanSaveProduct => ProductId.HasValue
        ? _authorizationService.HasPermission(PermissionConstants.ProductEdit)
        : _authorizationService.HasPermission(PermissionConstants.ProductCreate);

    /// <summary>Gets whether manual barcode generation is enabled and authorized.</summary>
    public bool CanGenerateBarcode => _barcodeSettings.ManualGenerationEnabled && _authorizationService.HasPermission(PermissionConstants.BarcodeGenerate);

    /// <summary>Saves product.</summary>
    [RelayCommand(CanExecute = nameof(CanSaveProduct))]
    private async Task SaveAsync()
    {
        IsBusy = true;
        ValidationMessage = string.Empty;
        var model = BuildModel();
        var result = ProductId.HasValue
            ? await _updateHandler.HandleAsync(new UpdateProductRequest(model))
            : await _createHandler.HandleAsync(new CreateProductRequest(model));
        IsBusy = false;

        if (!result.IsSuccess)
        {
            ValidationMessage = result.Error ?? "Unable to save product.";
            _notificationService.Show("Product", ValidationMessage, NotificationSeverity.Error);
            return;
        }

        _navigationState.SelectedProductId = result.Value?.Id;
        _notificationService.Show("Product", ProductId.HasValue ? "Product updated successfully." : "Product created successfully.", NotificationSeverity.Success);
        await _navigationService.NavigateToAsync<ProductDetailsViewModel>("Products > Details");
    }

    /// <summary>Cancels editing.</summary>
    [RelayCommand]
    private Task CancelAsync() => _navigationService.GoBackAsync();

    /// <summary>Resets editor.</summary>
    [RelayCommand]
    private void Reset() => ApplyModel(_original);

    /// <summary>Duplicates the current product using a generated unique barcode.</summary>
    [RelayCommand]
    private async Task DuplicateAsync()
    {
        if (!ProductId.HasValue)
        {
            _notificationService.Show("Product", "Save the product before duplicating it.", NotificationSeverity.Warning);
            return;
        }

        var generated = await GenerateBarcodeCoreAsync();
        if (!generated.IsSuccess || generated.Value is null)
        {
            _notificationService.Show("Product", generated.Error ?? "Unable to generate a unique barcode for the duplicate.", NotificationSeverity.Error);
            return;
        }

        var result = await _duplicateHandler.HandleAsync(new DuplicateProductRequest(ProductId.Value, generated.Value.Value));
        if (!result.IsSuccess)
        {
            _notificationService.Show("Product", result.Error ?? "Unable to duplicate product.", NotificationSeverity.Error);
            return;
        }

        _navigationState.SelectedProductId = result.Value?.Id;
        _notificationService.Show("Product", "Product duplicated successfully.", NotificationSeverity.Success);
        await _navigationService.NavigateToAsync<ProductDetailsViewModel>("Products > Details");
    }

    /// <summary>Generates a unique barcode using the saved barcode settings.</summary>
    [RelayCommand(CanExecute = nameof(CanGenerateBarcode))]
    private async Task GenerateBarcodeAsync()
    {
        var result = await GenerateBarcodeCoreAsync();
        if (!result.IsSuccess || result.Value is null)
        {
            _notificationService.Show("Barcode", result.Error ?? "Unable to generate barcode.", NotificationSeverity.Error);
            return;
        }

        Barcode = result.Value.Value;
        _notificationService.Show("Barcode", "Barcode generated successfully.", NotificationSeverity.Success);
    }

    /// <summary>Selects product image path.</summary>
    [RelayCommand]
    private void SelectImage()
    {
        var dialog = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.webp", CheckFileExists = true };
        if (dialog.ShowDialog() == true)
        {
            ImagePath = dialog.FileName;
        }
    }

    /// <summary>Removes product image path.</summary>
    [RelayCommand]
    private void RemoveImage() => ImagePath = null;

    private async Task InitializeAsync()
    {
        try
        {
            _barcodeSettings = await _settingsService.GetAsync<BookStore.Application.Features.Settings.DTOs.BarcodeSettingsDto>();
            GenerateBarcodeCommand.NotifyCanExecuteChanged();
            var categories = await _categorySearchHandler.HandleAsync(new SearchCategoriesRequest(null, 1, PagingConstants.MaxPageSize, IsActive: true));
            if (categories.IsSuccess && categories.Value is not null)
            {
                foreach (var category in categories.Value.Items)
                {
                    Categories.Add(category);
                }
            }

            if (!ProductId.HasValue)
            {
                if (_barcodeSettings.AutoGenerate && _authorizationService.HasPermission(PermissionConstants.BarcodeGenerate))
                {
                    var generated = await GenerateBarcodeCoreAsync();
                    if (generated.IsSuccess && generated.Value is not null) Barcode = generated.Value.Value;
                }
                _original = BuildModel();
                return;
            }

            IsBusy = true;
            var product = await _getByIdHandler.HandleAsync(new GetProductByIdRequest(ProductId.Value));
            if (!product.IsSuccess || product.Value is null)
            {
                ValidationMessage = product.Error ?? "Product was not found.";
                return;
            }

            ApplyModel(_mapper.Map<ProductEditorModel>(product.Value));
            _original = BuildModel();
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Unable to initialize product editor: {ex.Message}";
            _notificationService.Show("Products", ValidationMessage, NotificationSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task<BookStore.Shared.Results.Result<BarcodeDto>> GenerateBarcodeCoreAsync()
    {
        var format = Enum.TryParse<BarcodeFormat>(_barcodeSettings.DefaultFormat, true, out var parsed) ? parsed : BarcodeFormat.Code128;
        return _generateBarcodeHandler.HandleAsync(new GenerateBarcodeRequest(format));
    }

    private ProductEditorModel BuildModel() => new()
    {
        Id = ProductId,
        Barcode = Barcode,
        ISBN = Isbn,
        Title = TitleText,
        Subtitle = Subtitle,
        Description = Description,
        Author = Author,
        Publisher = Publisher,
        Language = Language,
        Edition = Edition,
        PublishDate = PublishDate.HasValue ? DateOnly.FromDateTime(PublishDate.Value) : null,
        PurchasePrice = PurchasePrice,
        SellingPrice = SellingPrice,
        TaxCategory = TaxCategory,
        Quantity = Quantity,
        MinimumStock = MinimumStock,
        ShelfLocation = ShelfLocation,
        ImagePath = ImagePath,
        CategoryId = CategoryId,
        IsActive = IsActive
    };

    private void ApplyModel(ProductEditorModel model)
    {
        ProductId = model.Id;
        Barcode = model.Barcode;
        Isbn = model.ISBN;
        TitleText = model.Title;
        Subtitle = model.Subtitle;
        Description = model.Description;
        Author = model.Author;
        Publisher = model.Publisher;
        Language = model.Language;
        Edition = model.Edition;
        PublishDate = model.PublishDate?.ToDateTime(TimeOnly.MinValue);
        PurchasePrice = model.PurchasePrice;
        SellingPrice = model.SellingPrice;
        TaxCategory = model.TaxCategory;
        Quantity = model.Quantity;
        MinimumStock = model.MinimumStock;
        ShelfLocation = model.ShelfLocation;
        ImagePath = model.ImagePath;
        CategoryId = model.CategoryId;
        IsActive = model.IsActive;
    }
}
