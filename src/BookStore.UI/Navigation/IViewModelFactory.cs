using BookStore.UI.ViewModels;

namespace BookStore.UI.Navigation;

/// <summary>
/// Creates view model instances for navigation.
/// </summary>
public interface IViewModelFactory
{
    /// <summary>
    /// Creates a view model instance.
    /// </summary>
    /// <param name="viewModelType">The view model type to create.</param>
    /// <returns>The created view model.</returns>
    BaseViewModel Create(Type viewModelType);
}
