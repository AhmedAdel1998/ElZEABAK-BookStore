using System.Collections.ObjectModel;
using BookStore.Application.Features.Categories.Commands.ActivateCategory;
using BookStore.Application.Features.Categories.Commands.DeactivateCategory;
using BookStore.Application.Features.Categories.Commands.DeleteCategory;
using BookStore.Application.Features.Categories.DTOs;
using BookStore.Application.Features.Categories.Handlers;
using BookStore.Application.Features.Categories.Queries.SearchCategories;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Navigation;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// View model for the category list screen.
/// </summary>
public partial class CategoryListViewModel : BaseViewModel
{
    private readonly SearchCategoriesHandler _searchHandler;
    private readonly DeleteCategoryHandler _deleteHandler;
    private readonly ActivateCategoryHandler _activateHandler;
    private readonly DeactivateCategoryHandler _deactivateHandler;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShellNavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly IConfirmationDialogService _confirmationDialogService;
    private readonly ICategoryNavigationState _navigationState;

    [ObservableProperty]
    private string searchTerm = string.Empty;

    [ObservableProperty]
    private CategoryDto? selectedCategory;

    [ObservableProperty]
    private string emptyMessage = "No categories found.";

    [ObservableProperty]
    private int pageNumber = 1;

    [ObservableProperty]
    private int pageSize = 25;

    [ObservableProperty]
    private int totalCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryListViewModel"/> class.
    /// </summary>
    public CategoryListViewModel(
        SearchCategoriesHandler searchHandler,
        DeleteCategoryHandler deleteHandler,
        ActivateCategoryHandler activateHandler,
        DeactivateCategoryHandler deactivateHandler,
        IAuthorizationService authorizationService,
        IShellNavigationService navigationService,
        INotificationService notificationService,
        IConfirmationDialogService confirmationDialogService,
        ICategoryNavigationState navigationState)
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
        Title = "Categories";
        _ = LoadAsync();
    }

    /// <summary>
    /// Gets category rows.
    /// </summary>
    public ObservableCollection<CategoryDto> Categories { get; } = [];

    /// <summary>
    /// Gets a value indicating whether create actions are allowed.
    /// </summary>
    public bool CanCreate => _authorizationService.HasPermission(PermissionConstants.CategoryCreate);

    /// <summary>
    /// Gets a value indicating whether edit actions are allowed.
    /// </summary>
    public bool CanEdit => _authorizationService.HasPermission(PermissionConstants.CategoryEdit);

    /// <summary>
    /// Gets a value indicating whether delete actions are allowed.
    /// </summary>
    public bool CanDelete => _authorizationService.HasPermission(PermissionConstants.CategoryDelete);

    /// <summary>
    /// Gets a value indicating whether category details are allowed.
    /// </summary>
    public bool CanView => _authorizationService.HasPermission(PermissionConstants.CategoryView);

    partial void OnSearchTermChanged(string value)
    {
        _ = SearchAsync();
    }

    /// <summary>
    /// Refreshes category rows.
    /// </summary>
    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    /// <summary>
    /// Searches category rows.
    /// </summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadAsync();
    }

    /// <summary>
    /// Navigates to the create editor.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCreate))]
    private Task AddAsync()
    {
        _navigationState.SelectedCategoryId = null;
        return _navigationService.NavigateToAsync<CategoryEditorViewModel>("Categories > New Category");
    }

    /// <summary>
    /// Navigates to the selected category editor.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private Task EditAsync()
    {
        if (SelectedCategory is null)
        {
            return Task.CompletedTask;
        }

        _navigationState.SelectedCategoryId = SelectedCategory.Id;
        return _navigationService.NavigateToAsync<CategoryEditorViewModel>("Categories > Edit Category");
    }

    /// <summary>
    /// Navigates to selected category details.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSelectCategory))]
    private Task ViewDetailsAsync()
    {
        if (SelectedCategory is null)
        {
            return Task.CompletedTask;
        }

        _navigationState.SelectedCategoryId = SelectedCategory.Id;
        return _navigationService.NavigateToAsync<CategoryDetailsViewModel>("Categories > Details");
    }

    /// <summary>
    /// Deletes the selected category using soft delete.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private async Task DeleteAsync()
    {
        if (SelectedCategory is null)
        {
            return;
        }

        var confirmed = await _confirmationDialogService.ConfirmAsync("Delete Category", $"Delete category '{SelectedCategory.Name}'?");
        if (!confirmed)
        {
            return;
        }

        IsBusy = true;
        var result = await _deleteHandler.HandleAsync(new DeleteCategoryRequest(SelectedCategory.Id));
        IsBusy = false;

        if (!result.Succeeded)
        {
            var message = result.Errors.FirstOrDefault()?.Message ?? result.ValidationErrors.FirstOrDefault()?.Message ?? "Unable to delete category.";
            _notificationService.Show("Category", message, NotificationSeverity.Error);
            return;
        }

        _notificationService.Show("Category", "Category deleted successfully.", NotificationSeverity.Success);
        await LoadAsync();
    }

    /// <summary>
    /// Activates the selected category.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task ActivateAsync()
    {
        if (SelectedCategory is null)
        {
            return;
        }

        var result = await _activateHandler.HandleAsync(new ActivateCategoryRequest(SelectedCategory.Id));
        ShowOperationResult(result.Succeeded, "Category activated successfully.", result.Errors.FirstOrDefault()?.Message);
        await LoadAsync();
    }

    /// <summary>
    /// Deactivates the selected category.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task DeactivateAsync()
    {
        if (SelectedCategory is null)
        {
            return;
        }

        var result = await _deactivateHandler.HandleAsync(new DeactivateCategoryRequest(SelectedCategory.Id));
        ShowOperationResult(result.Succeeded, "Category deactivated successfully.", result.Errors.FirstOrDefault()?.Message);
        await LoadAsync();
    }

    partial void OnSelectedCategoryChanged(CategoryDto? value)
    {
        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        ViewDetailsCommand.NotifyCanExecuteChanged();
        ActivateCommand.NotifyCanExecuteChanged();
        DeactivateCommand.NotifyCanExecuteChanged();
    }

    private bool CanEditSelected() => CanEdit && SelectedCategory is not null;

    private bool CanDeleteSelected() => CanDelete && SelectedCategory is not null;

    private bool CanSelectCategory() => CanView && SelectedCategory is not null;

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
        var result = await _searchHandler.HandleAsync(new SearchCategoriesRequest(SearchTerm, PageNumber, PageSize));
        Categories.Clear();

        if (result.IsSuccess && result.Value is not null)
        {
            foreach (var category in result.Value.Items)
            {
                Categories.Add(category);
            }

            TotalCount = result.Value.TotalCount;
            EmptyMessage = string.IsNullOrWhiteSpace(SearchTerm) ? "No categories found." : "No categories matched your search.";
        }
        else
        {
            _notificationService.Show("Categories", result.Error ?? "Unable to load categories.", NotificationSeverity.Error);
        }

        IsBusy = false;
    }

    private void ShowOperationResult(bool succeeded, string successMessage, string? failureMessage)
    {
        _notificationService.Show("Category", succeeded ? successMessage : failureMessage ?? "Operation failed.", succeeded ? NotificationSeverity.Success : NotificationSeverity.Error);
    }
}
