using BookStore.Application.Features.Categories.Commands.CreateCategory;
using BookStore.Application.Features.Categories.Commands.UpdateCategory;
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
/// View model for creating and editing categories.
/// </summary>
public partial class CategoryEditorViewModel : BaseViewModel
{
    private readonly CreateCategoryHandler _createHandler;
    private readonly UpdateCategoryHandler _updateHandler;
    private readonly GetCategoryByIdHandler _getByIdHandler;
    private readonly IAuthorizationService _authorizationService;
    private readonly IShellNavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly ICategoryNavigationState _navigationState;
    private string _originalName = string.Empty;
    private string? _originalDescription;
    private bool _originalIsActive = true;

    [ObservableProperty]
    private Guid? categoryId;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string? description;

    [ObservableProperty]
    private bool isActive = true;

    [ObservableProperty]
    private string validationMessage = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryEditorViewModel"/> class.
    /// </summary>
    public CategoryEditorViewModel(
        CreateCategoryHandler createHandler,
        UpdateCategoryHandler updateHandler,
        GetCategoryByIdHandler getByIdHandler,
        IAuthorizationService authorizationService,
        IShellNavigationService navigationService,
        INotificationService notificationService,
        ICategoryNavigationState navigationState)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _getByIdHandler = getByIdHandler;
        _authorizationService = authorizationService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _navigationState = navigationState;
        CategoryId = navigationState.SelectedCategoryId;
        Title = CategoryId.HasValue ? "Edit Category" : "New Category";
        _ = LoadAsync();
    }

    /// <summary>
    /// Gets a value indicating whether the current user can save this editor.
    /// </summary>
    public bool CanSaveCategory => CategoryId.HasValue
        ? _authorizationService.HasPermission(PermissionConstants.CategoryEdit)
        : _authorizationService.HasPermission(PermissionConstants.CategoryCreate);

    /// <summary>
    /// Gets a value indicating whether the editor has unsaved changes.
    /// </summary>
    public bool HasUnsavedChanges => !string.Equals(Name, _originalName, StringComparison.Ordinal)
        || !string.Equals(Description ?? string.Empty, _originalDescription ?? string.Empty, StringComparison.Ordinal)
        || IsActive != _originalIsActive;

    partial void OnNameChanged(string value) => SaveCommand.NotifyCanExecuteChanged();

    partial void OnDescriptionChanged(string? value) => SaveCommand.NotifyCanExecuteChanged();

    partial void OnIsActiveChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();

    /// <summary>
    /// Saves the category.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSaveCategory))]
    private async Task SaveAsync()
    {
        IsBusy = true;
        ValidationMessage = string.Empty;

        var result = CategoryId.HasValue
            ? await _updateHandler.HandleAsync(new UpdateCategoryRequest(CategoryId.Value, Name, Description, IsActive))
            : await _createHandler.HandleAsync(new CreateCategoryRequest(Name, Description, IsActive));

        IsBusy = false;

        if (!result.IsSuccess)
        {
            ValidationMessage = result.Error ?? "Unable to save category.";
            _notificationService.Show("Category", ValidationMessage, NotificationSeverity.Error);
            return;
        }

        _notificationService.Show("Category", CategoryId.HasValue ? "Category updated successfully." : "Category created successfully.", NotificationSeverity.Success);
        _navigationState.SelectedCategoryId = result.Value?.Id;
        await _navigationService.NavigateToAsync<CategoryDetailsViewModel>("Categories > Details");
    }

    /// <summary>
    /// Cancels editing and navigates back.
    /// </summary>
    [RelayCommand]
    private Task CancelAsync() => _navigationService.GoBackAsync();

    /// <summary>
    /// Resets the editor to its loaded values.
    /// </summary>
    [RelayCommand]
    private void Reset()
    {
        Name = _originalName;
        Description = _originalDescription;
        IsActive = _originalIsActive;
        ValidationMessage = string.Empty;
    }

    private async Task LoadAsync()
    {
        if (!CategoryId.HasValue)
        {
            CaptureOriginalValues();
            return;
        }

        IsBusy = true;
        var result = await _getByIdHandler.HandleAsync(new GetCategoryByIdRequest(CategoryId.Value));
        IsBusy = false;

        if (!result.IsSuccess || result.Value is null)
        {
            ValidationMessage = result.Error ?? "Category was not found.";
            _notificationService.Show("Category", ValidationMessage, NotificationSeverity.Error);
            return;
        }

        Name = result.Value.Name;
        Description = result.Value.Description;
        IsActive = result.Value.IsActive;
        CaptureOriginalValues();
    }

    private void CaptureOriginalValues()
    {
        _originalName = Name;
        _originalDescription = Description;
        _originalIsActive = IsActive;
    }
}
