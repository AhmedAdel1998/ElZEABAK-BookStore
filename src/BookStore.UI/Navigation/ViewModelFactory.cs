using BookStore.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.UI.Navigation;

/// <summary>
/// Creates navigable view models, each resolved from a fresh dependency injection scope.
/// </summary>
public sealed class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NavigableViewModelRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModelFactory"/> class.
    /// </summary>
    /// <param name="scopeFactory">Creates a scope per view model.</param>
    /// <param name="registry">The view model types reachable through navigation.</param>
    public ViewModelFactory(IServiceScopeFactory scopeFactory, NavigableViewModelRegistry registry)
    {
        _scopeFactory = scopeFactory;
        _registry = registry;
    }

    /// <inheritdoc />
    public IViewModelLease Create(Type viewModelType)
    {
        if (!_registry.Contains(viewModelType))
        {
            throw new InvalidOperationException($"View model is not registered for navigation: {viewModelType.FullName}");
        }

        var scope = _scopeFactory.CreateScope();
        try
        {
            var viewModel = (BaseViewModel)scope.ServiceProvider.GetRequiredService(viewModelType);
            return new ViewModelLease(scope, viewModel);
        }
        catch
        {
            // Never leak a scope (and its database context) when construction fails.
            scope.Dispose();
            throw;
        }
    }

    private sealed class ViewModelLease : IViewModelLease
    {
        private readonly IServiceScope _scope;
        private bool _disposed;

        public ViewModelLease(IServiceScope scope, BaseViewModel viewModel)
        {
            _scope = scope;
            ViewModel = viewModel;
        }

        public BaseViewModel ViewModel { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // The view model first, so it can cancel in-flight work before the scope tears down
            // the database context that work is using.
            if (ViewModel is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _scope.Dispose();
        }
    }
}
