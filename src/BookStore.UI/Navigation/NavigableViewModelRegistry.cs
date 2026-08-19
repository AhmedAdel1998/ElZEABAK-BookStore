using BookStore.UI.ViewModels;

namespace BookStore.UI.Navigation;

/// <summary>
/// The set of view model types that navigation is allowed to construct.
/// </summary>
/// <remarks>
/// This replaces a dictionary of sixty self-resolving factory lambdas. Those lambdas each closed
/// over the root <see cref="IServiceProvider"/>, which is what made every scoped dependency behave
/// as a singleton. The registry carries only the allowlist; <see cref="ViewModelFactory"/> supplies
/// the scope.
/// </remarks>
public sealed class NavigableViewModelRegistry
{
    private readonly HashSet<Type> _types;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigableViewModelRegistry"/> class.
    /// </summary>
    /// <param name="types">The navigable view model types.</param>
    public NavigableViewModelRegistry(IEnumerable<Type> types)
    {
        _types = [.. types];
    }

    /// <summary>
    /// Gets the registered navigable view model types.
    /// </summary>
    public IReadOnlyCollection<Type> Types => _types;

    /// <summary>
    /// Reports whether the supplied type may be constructed by navigation.
    /// </summary>
    /// <param name="viewModelType">The candidate view model type.</param>
    /// <returns><see langword="true"/> when the type is navigable.</returns>
    public bool Contains(Type viewModelType) => _types.Contains(viewModelType);

    /// <summary>
    /// Builds the registry from every concrete <see cref="BaseViewModel"/> in this assembly.
    /// Discovery keeps the allowlist from drifting out of step with the view models that exist.
    /// </summary>
    /// <returns>The discovered registry.</returns>
    public static NavigableViewModelRegistry Discover() =>
        new(typeof(NavigableViewModelRegistry).Assembly
            .GetTypes()
            .Where(type => type.IsClass
                && !type.IsAbstract
                && !type.IsGenericTypeDefinition
                && typeof(BaseViewModel).IsAssignableFrom(type)));
}
