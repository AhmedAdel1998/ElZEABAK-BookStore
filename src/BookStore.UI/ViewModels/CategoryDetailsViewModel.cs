using BookStore.Application.Features.Categories.DTOs;
using BookStore.Application.Features.Categories.Handlers;
using BookStore.Application.Features.Categories.Queries.GetCategoryById;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// View model for category details.
/// </summary>
public partial class CategoryDetailsViewModel : BaseViewModel
{
    private readonly GetCategoryByIdHandler _getByIdHandler;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShellNavigationService _navigationService;
    private readonly ICategoryNavigationState _navigationState;

    [ObservableProperty]
    private CategoryDto? category;

    [ObservableProperty]
    private string message = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryDetailsViewModel"/> class.
    /// </summary>
    public CategoryDetailsViewModel(
        GetCategoryByIdHandler getByIdHandler,
        IAuthorizationService authorizationService,
        IShellNavigationService navigationService,
        ICategoryNavigationState navigationState)
    {
        _getByIdHandler = getByIdHandler;
        _authorizationService = authorizationService;
        _navigationService = navigationService;
        _navigationState = navigationState;
        Title = "Category Details";
        _ = LoadAsync();
    }

    /// <summary>
    /// Gets a value indicating whether edit actions are allowed.
    /// </summary>
    public bool CanEdit => _authorizationService.HasPermission(PermissionConstants.CategoryEdit);

    /// <summary>
    /// Navigates to the editor for this category.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditCategory))]
    private Task EditAsync()
    {
        if (Category is not null)
        {
            _navigationState.SelectedCategoryId = Category.Id;
        }

        return _navigationService.NavigateToAsync<CategoryEditorViewModel>("Categories > Edit Category");
    }

    /// <summary>
    /// Navigates back to the category list.
    /// </summary>
    [RelayCommand]
    private Task BackAsync() => _navigationService.GoBackAsync();

    private bool CanEditCategory() => CanEdit && Category is not null;

    private async Task LoadAsync()
    {
        if (_navigationState.SelectedCategoryId is null)
        {
            Message = "No category selected.";
            return;
        }

        IsBusy = true;
        var result = await _getByIdHandler.HandleAsync(new GetCategoryByIdRequest(_navigationState.SelectedCategoryId.Value));
        IsBusy = false;

        if (!result.IsSuccess || result.Value is null)
        {
            Message = result.Error ?? "Category was not found.";
            return;
        }

        Category = result.Value;
        EditCommand.NotifyCanExecuteChanged();
    }
}
