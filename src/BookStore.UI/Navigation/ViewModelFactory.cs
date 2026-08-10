using BookStore.UI.ViewModels;

namespace BookStore.UI.Navigation;

/// <summary>
/// Creates view models from configured factories.
/// </summary>
public class ViewModelFactory : IViewModelFactory
{
    private readonly IReadOnlyDictionary<Type, Func<BaseViewModel>> _factories;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModelFactory"/> class.
    /// </summary>
    /// <param name="factories">View model factories keyed by view model type.</param>
    public ViewModelFactory(IReadOnlyDictionary<Type, Func<BaseViewModel>> factories)
    {
        _factories = factories;
    }

    /// <inheritdoc />
    public BaseViewModel Create(Type viewModelType)
    {
        if (!_factories.TryGetValue(viewModelType, out var factory))
        {
            throw new InvalidOperationException($"View model is not registered for navigation: {viewModelType.FullName}");
        }

        return factory();
    }
}
