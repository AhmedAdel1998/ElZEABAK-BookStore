using BookStore.UI.ViewModels;

namespace BookStore.UI.Navigation;

/// <summary>
/// Creates view model instances for navigation, each inside its own dependency injection scope.
/// </summary>
public interface IViewModelFactory
{
    /// <summary>
    /// Creates a view model together with the scope that owns its dependencies.
    /// </summary>
    /// <param name="viewModelType">The view model type to create.</param>
    /// <returns>
    /// A lease the caller must dispose once the view model is no longer displayed. Disposing the
    /// lease disposes the view model and then the scope, releasing that page's database context.
    /// </returns>
    IViewModelLease Create(Type viewModelType);
}

/// <summary>
/// Owns a view model and the dependency injection scope its services were resolved from.
/// </summary>
/// <remarks>
/// Navigation used to resolve view models straight from the root service provider, which meant the
/// scoped <c>BookStoreDbContext</c> behaved as a singleton: one context, one change tracker, shared
/// by every screen for the lifetime of the process. Because a DbContext is neither thread-safe nor
/// re-entrant, two overlapping queries -- a search box firing once per keystroke was enough -- threw
/// "a second operation was started on this context", and abandoned entities from a failed operation
/// were flushed by the next unrelated save. A lease gives each page its own scope and disposes it on
/// navigation.
/// </remarks>
public interface IViewModelLease : IDisposable
{
    /// <summary>
    /// Gets the view model this lease owns.
    /// </summary>
    BaseViewModel ViewModel { get; }
}
